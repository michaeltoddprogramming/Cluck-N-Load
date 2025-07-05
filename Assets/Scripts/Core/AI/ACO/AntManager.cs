using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace FarmDefender.Core.AI.ACO
{
    public class AntManager : MonoBehaviour
    {
        [Header("Ant Settings")]
        [Tooltip("Minimum number of ants to spawn")]
        [SerializeField] private int maxAnts = 10;
        [SerializeField] private int minStructuresPerAnt = 3;
        [SerializeField] private float baseFlowFieldDesirability = 0.2f;
        [Tooltip("Set to 0 for instant algorithm execution")]
        [SerializeField] private float antSpeed = 8f;
        [Tooltip("How long ants stay alive before expiring (seconds)")]
        [SerializeField] private float antLifetime = 30f;
        
        [Header("Ant Navigation")]
        [Tooltip("How long before ants start using more flow field (seconds)")]
        [SerializeField] private float timeBeforeFlowFieldIncrease = 5f;
        [Tooltip("How quickly flow field influence grows when searching")]
        [SerializeField] private float flowFieldInfluenceGrowthRate = 0.05f;
        [Tooltip("Maximum flow field influence when snooping")]
        [SerializeField] private float maxSnoopingFlowFieldInfluence = 0.6f;
        [Tooltip("Radius to search for structures")]
        [SerializeField] private float structureSearchRadius = 2f;
        [Tooltip("Chance of random movement during exploration")]
        [Range(0f, 1f)]
        [SerializeField] private float randomMovementFactor = 0.3f;
        
        [Header("Return Path Settings")]
        [Tooltip("How strongly ants are drawn to edges when returning")]
        [SerializeField] private float edgeDirectionWeight = 2.0f;
        [Tooltip("How strongly ants avoid existing pheromones when returning")]
        [SerializeField] private float pheromoneAvoidanceWeight = 1.5f;
        [Tooltip("How strongly ants follow the inverse flow field when returning")]
        [SerializeField] private float flowFieldAlignmentWeight = 1.0f;
        [Tooltip("Randomness in return path selection")]
        [Range(0f, 1f)]
        [SerializeField] private float returnPathRandomness = 0.2f;
        
        [Header("Pheromone Settings")]
        [Tooltip("Interval between laying pheromones when returning (seconds)")]
        [SerializeField] private float pheromoneLayInterval = 0.2f;
        [Tooltip("Base strength of laid pheromones")]
        [SerializeField] private float basePheromoneStrength = 1f;
        [Tooltip("Whether to scale pheromone strength by number of structures found")]
        [SerializeField] private bool scalePheromonesByStructures = true;
        [Tooltip("Maximum pheromone strength")]
        [SerializeField] private float maxPheromoneStrength = 5f;
        [Tooltip("How far pheromones spread from their source")]
        [SerializeField] private int pheromoneSpreadRadius = 1;
        [Tooltip("Pheromone type to use (0=Regular, 1=Fast, 2=Strong)")]
        [SerializeField] private int defaultEnemyTypeIndex = 0;
        [SerializeField] private bool enablePheromoneVisualization = true;
        
        [Header("Visualization")]
        [Tooltip("Enable to visualize ant movement (for debugging)")]
        [SerializeField] private bool visualizeAnts = false;
        [SerializeField] private bool showDiscoveredStructures = true;
        [SerializeField] private Color discoveredStructureColor = Color.cyan;
        [SerializeField] private Color exploringColor = Color.green;
        [SerializeField] private Color returningColor = Color.yellow;
        [SerializeField] private Color snoopingColor = Color.magenta;
        
        [Header("Spawn Settings")]
        [SerializeField] private float spawnDistanceFromEdge = 2f;
        
        [Header("Debug")]
        [SerializeField] private bool showStructureDebug = true;
        
        // References to subsystems
        private AntNavigationSystem navigationSystem;
        private PheromoneLayers pheromoneSystem;
        
        // References
        private GridController gridController;
        private GridDataGenerator gridDataGenerator;
        private FarmDefender.Core.AI.FlowField.FlowFieldManager flowFieldManager;
        private GridMonitor gridMonitor;
        
        // Internal state
        private List<VirtualAnt> virtualAnts = new List<VirtualAnt>();
        private HashSet<Vector2Int> discoveredStructures = new HashSet<Vector2Int>();
        private HashSet<Vector2Int> allPlayerStructures = new HashSet<Vector2Int>();
        private HashSet<Vector2Int> remainingStructuresToFind = new HashSet<Vector2Int>();
        private int totalStructuresInWorld = 0;
        private bool hasCountedStructures = false;
        private float updateInterval = 0.1f; // How often to update the virtual ants
        
        private void Start()
        {
            // Get required references
            gridController = FindObjectOfType<GridController>();
            gridDataGenerator = FindObjectOfType<GridDataGenerator>();
            flowFieldManager = FindObjectOfType<FarmDefender.Core.AI.FlowField.FlowFieldManager>();
            gridMonitor = FindObjectOfType<GridMonitor>();
            
            if (gridController == null || gridDataGenerator == null || flowFieldManager == null)
            {
                Debug.LogError("AntManager is missing required references");
                enabled = false;
                return;
            }
            
            // Create subsystems
            navigationSystem = new AntNavigationSystem(
                this, gridController, gridDataGenerator, flowFieldManager, updateInterval);
                
            pheromoneSystem = new PheromoneLayers(
                gridController, gridDataGenerator, showStructureDebug);
                
            // Configure subsystems
            pheromoneSystem.InitializeSettings(this);
            navigationSystem.InitializeSettings(this);
            
            // Subscribe to grid monitor events
            if (gridMonitor != null)
            {
                gridMonitor.OnCellOccupied += HandleCellOccupied;
                gridMonitor.OnCellCleared += HandleCellCleared;
            }
            
            // Initial scan for structures
            UpdatePlayerStructures();
            
            // Setup visualization
            SetupPheromoneVisualizer();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (gridMonitor != null)
            {
                gridMonitor.OnCellOccupied -= HandleCellOccupied;
                gridMonitor.OnCellCleared -= HandleCellCleared;
            }
        }
        
        private void Update()
        {
            // Check for keyboard shortcut (Shift + P)
            if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.P))
            {
                TriggerAnts();
                Debug.Log("ACO algorithm triggered with Shift+P");
            }
        }
        
        // Method to scan for all player structures in the world
        public void UpdatePlayerStructures()
        {
            int width = gridDataGenerator.GetGridWidth();
            int height = gridDataGenerator.GetGridHeight();
            
            // Store the current list to detect removed structures
            HashSet<Vector2Int> oldStructures = new HashSet<Vector2Int>(allPlayerStructures);
            allPlayerStructures.Clear();
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    GridCell cell = gridController.GetCell(x, y);
                    if (cell != null && cell.flags.isOccupied && cell.flags.isOwned)
                    {
                        Vector2Int pos = new Vector2Int(x, y);
                        allPlayerStructures.Add(pos);
                        
                        // If this is a new structure, also add it to the remaining structures
                        if (!oldStructures.Contains(pos))
                        {
                            remainingStructuresToFind.Add(pos);
                        }
                    }
                }
            }
            
            if (showStructureDebug)
            {
                Debug.Log($"Updated player structures: {allPlayerStructures.Count} total, {remainingStructuresToFind.Count} remaining to find");
            }
        }
        
        private void HandleCellOccupied(Vector2Int cell)
        {
            GridCell gridCell = gridController.GetCell(cell.x, cell.y);
            if (gridCell != null && gridCell.flags.isOwned && gridCell.flags.isOccupied)
            {
                // A new structure was placed
                allPlayerStructures.Add(cell);
                remainingStructuresToFind.Add(cell);
                
                if (showStructureDebug)
                {
                    Debug.Log($"New structure detected at {cell}, adding to structures to find. " +
                              $"Total: {allPlayerStructures.Count}, Remaining: {remainingStructuresToFind.Count}");
                }
            }
        }
        
        private void HandleCellCleared(Vector2Int cell)
        {
            // A structure was removed
            allPlayerStructures.Remove(cell);
            remainingStructuresToFind.Remove(cell);
            
            if (showStructureDebug)
            {
                Debug.Log($"Structure removed at {cell}. " +
                          $"Total: {allPlayerStructures.Count}, Remaining: {remainingStructuresToFind.Count}");
            }
        }
        
        // Count all player structures in the world
        private int CountTotalStructures()
        {
            if (hasCountedStructures)
                return totalStructuresInWorld;
                
            UpdatePlayerStructures();
            totalStructuresInWorld = allPlayerStructures.Count;
            hasCountedStructures = true;
            return totalStructuresInWorld;
        }
        
        // Determine how many ants to spawn based on structure count
        private int DetermineOptimalAntCount()
        {
            int structureCount = CountTotalStructures();
            
            // Calculate needed ants based on structures and min structures per ant
            int neededAnts = Mathf.CeilToInt((float)structureCount / minStructuresPerAnt);
            
            // Use at least the minimum number specified
            return Mathf.Max(neededAnts, maxAnts);
        }
        
        private void SpawnVirtualAnt()
        {
            // Get a random edge position
            Vector2Int spawnPos = GetRandomEdgeCellPosition();
            
            // Create a new virtual ant
            VirtualAnt ant = new VirtualAnt(spawnPos, antLifetime);
            virtualAnts.Add(ant);
        }
        
        private Vector2Int GetRandomEdgeCellPosition()
        {
            int gridWidth = gridDataGenerator.GetGridWidth();
            int gridHeight = gridDataGenerator.GetGridHeight();
            
            // Get a random edge cell
            Vector2Int edgeCell = Vector2Int.zero;
            
            // Randomly choose which edge to spawn on
            int edge = Random.Range(0, 4);
            
            switch (edge)
            {
                case 0: // Top
                    edgeCell = new Vector2Int(Random.Range(0, gridWidth), gridHeight - 1);
                    break;
                case 1: // Right
                    edgeCell = new Vector2Int(gridWidth - 1, Random.Range(0, gridHeight));
                    break;
                case 2: // Bottom
                    edgeCell = new Vector2Int(Random.Range(0, gridWidth), 0);
                    break;
                case 3: // Left
                    edgeCell = new Vector2Int(0, Random.Range(0, gridHeight));
                    break;
            }
            
            return edgeCell;
        }
        
        // Called by game events to trigger ant spawning
        public void TriggerAnts()
        {
            // Clear existing ants
            virtualAnts.Clear();
            
            // Reset discovery data
            discoveredStructures.Clear();
            hasCountedStructures = false;
            
            // Reset pheromone statistics
            pheromoneSystem.ResetStatistics();
            
            // Update player structures
            UpdatePlayerStructures();
            
            // Reset the remaining structures to find
            remainingStructuresToFind = new HashSet<Vector2Int>(allPlayerStructures);
            
            // Determine how many ants to spawn
            int antsToSpawn = DetermineOptimalAntCount();
            
            // Spawn all ants at once
            for (int i = 0; i < antsToSpawn; i++)
            {
                SpawnVirtualAnt();
            }
            
            Debug.Log($"ACO algorithm spawned {antsToSpawn} virtual ants to explore the map");
            Debug.Log($"Structures to find: {remainingStructuresToFind.Count}");
            
            // Always use the fast algorithm
            StartCoroutine(RunFastAlgorithm());
        }
        
        // Optimized RunFastAlgorithm coroutine
        private IEnumerator RunFastAlgorithm()
        {
            Debug.Log("Running ACO algorithm in fast mode");
            
            // Temporarily disable pheromone visualization during execution
            PheromoneVisualizer[] visualizers = FindObjectsOfType<PheromoneVisualizer>();
            bool wasVisualizationEnabled = enablePheromoneVisualization;
            
            if (wasVisualizationEnabled)
            {
                foreach (var viz in visualizers)
                {
                    viz.enabled = false;
                }
            }
            
            // Optimization: Larger batch size for faster processing
            int batchSize = 250;
            int antUpdatesThisFrame = 0;
            
            // Optimization: Larger time steps for faster simulation
            float fastTimeStep = 0.25f;
            
            // Cache counts for progress tracking
            int totalAnts = virtualAnts.Count;
            int completedAnts = 0;
            float startTime = Time.realtimeSinceStartup;
            
            // Keep running until all ants are done
            while (virtualAnts.Count > 0)
            {
                // Process each ant
                for (int i = virtualAnts.Count - 1; i >= 0; i--)
                {
                    if (i >= virtualAnts.Count) 
                        continue; // Safety check
                        
                    VirtualAnt ant = virtualAnts[i];
                    
                    // Update lifetime
                    ant.lifetime -= fastTimeStep;
                    
                    // If lifetime is expired, remove the ant
                    if (ant.lifetime <= 0)
                    {
                        virtualAnts.RemoveAt(i);
                        completedAnts++;
                        continue;
                    }
                    
                    // Check if we need to start returning
                    navigationSystem.CheckReturnConditions(ant, remainingStructuresToFind, discoveredStructures, allPlayerStructures);
                    
                    // Different behavior based on state
                    if (ant.isReturning)
                    {
                        // Process multiple steps at once for returning ants
                        for (int step = 0; step < 3; step++)
                        {
                            navigationSystem.ReturnToEdge(ant);
                            pheromoneSystem.LayPheromones(ant, updateInterval);
                            
                            // Check if reached edge
                            if (navigationSystem.IsAtEdge(ant.position))
                            {
                                virtualAnts.RemoveAt(i);
                                completedAnts++;
                                break;
                            }
                        }
                    }
                    else
                    {
                        navigationSystem.UpdateExplorationBehavior(ant, remainingStructuresToFind, discoveredStructures);
                    }
                    
                    // Count this update
                    antUpdatesThisFrame++;
                    
                    // If we've hit our batch size, yield
                    if (antUpdatesThisFrame >= batchSize)
                    {
                        antUpdatesThisFrame = 0;
                        yield return null;
                    }
                }
                
                // Always yield once per loop
                yield return null;
            }
            
            // Re-enable visualizers
            if (wasVisualizationEnabled)
            {
                foreach (var viz in visualizers)
                {
                    viz.enabled = true;
                }
            }
            
            float endTime = Time.realtimeSinceStartup;
            
            Debug.Log($"ACO algorithm completed in {(endTime - startTime):F2} seconds. " +
                      $"Found {discoveredStructures.Count}/{allPlayerStructures.Count} structures. " +
                      $"Laid {pheromoneSystem.GetTotalPheromonesLaid()} pheromones.");
                      
            // Final update to visualize pheromones
            if (enablePheromoneVisualization && visualizers.Length > 0)
            {
                visualizers[0].ForceUpdate();
            }
        }
        
        // Public accessor for discovered structures
        public HashSet<Vector2Int> GetDiscoveredStructures()
        {
            return discoveredStructures;
        }
        
        // Setup pheromone visualizer
        private void SetupPheromoneVisualizer()
        {
            // Check if visualization should be enabled
            PheromoneVisualizer existingVisualizer = FindObjectOfType<PheromoneVisualizer>();

            if (!enablePheromoneVisualization)
            {
                // Disable existing visualizer
                if (existingVisualizer != null)
                {
                    existingVisualizer.gameObject.SetActive(false);
                }
                return;
            }
            
            // Check if visualizer already exists
            if (existingVisualizer != null)
            {
                existingVisualizer.gameObject.SetActive(true);
                return;
            }
            
            // Create a new visualizer
            GameObject visualizerObject = new GameObject("PheromoneVisualizer");
            visualizerObject.transform.position = new Vector3(0, 0.05f, 0);
            
            // Add quad mesh
            MeshFilter meshFilter = visualizerObject.AddComponent<MeshFilter>();
            meshFilter.mesh = CreateQuadMesh();
            
            // Add renderer and component
            visualizerObject.AddComponent<MeshRenderer>();
            visualizerObject.AddComponent<PheromoneVisualizer>();
        }

        private Mesh CreateQuadMesh()
        {
            Mesh mesh = new Mesh();
            
            // Define vertices (simple quad)
            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(-0.5f, 0, -0.5f),
                new Vector3(0.5f, 0, -0.5f),
                new Vector3(-0.5f, 0, 0.5f),
                new Vector3(0.5f, 0, 0.5f)
            };
            
            // Define UVs
            Vector2[] uv = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };
            
            // Define triangles
            int[] triangles = new int[6]
            {
                0, 2, 1,
                2, 3, 1
            };
            
            // Apply to mesh
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            
            return mesh;
        }
        
        // Draw debug visualizations
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !visualizeAnts)
                return;
            
            // Draw virtual ants
            foreach (var ant in virtualAnts)
            {
                // Skip if grid controller is not available
                if (gridController == null)
                    continue;
                    
                // Calculate world position
                Vector3 worldPos = gridController.GetCellCenterFromTexture(ant.position.x, ant.position.y);
                
                // Draw ant based on state
                if (ant.isReturning)
                    Gizmos.color = returningColor;
                else if (ant.isSnooping)
                    Gizmos.color = snoopingColor;
                else
                    Gizmos.color = exploringColor;
                    
                Gizmos.DrawWireSphere(worldPos, 0.3f);
                
                // Draw line to target when returning
                if (ant.isReturning)
                {
                    Vector3 targetWorld = gridController.GetCellCenterFromTexture(ant.targetPosition.x, ant.targetPosition.y);
                    Gizmos.DrawLine(worldPos, targetWorld);
                }
                
                // Draw ant info
                if (visualizeAnts)
                {
                    // Create a formatted label
                    string label = $"{ant.discoveredStructures.Count}/{minStructuresPerAnt}\nFF: {ant.currentFlowFieldInfluence:F2}";
                    UnityEditor.Handles.Label(worldPos + Vector3.up * 0.5f, label);
                    
                    // Visualize time without finding structures
                    if (ant.timeSinceLastStructureFound > navigationSystem.GetTimeBeforeFlowIncrease() && !ant.isReturning)
                    {
                        // Draw timer indicator
                        float timerRatio = Mathf.Clamp01((ant.timeSinceLastStructureFound - navigationSystem.GetTimeBeforeFlowIncrease()) / 10f);
                        Gizmos.color = Color.Lerp(Color.yellow, Color.red, timerRatio);
                        Gizmos.DrawWireSphere(worldPos, 0.3f + timerRatio * 0.2f);
                    }
                }
            }
            
            // Draw discovered structures
            if (showDiscoveredStructures && discoveredStructures != null && gridController != null)
            {
                Gizmos.color = discoveredStructureColor;
                
                foreach (Vector2Int pos in discoveredStructures)
                {
                    Vector3 worldPos = gridController.GetCellCenterFromTexture(pos.x, pos.y);
                    Gizmos.DrawWireCube(worldPos, new Vector3(1f, 0.1f, 1f));
                }
            }
            
            // Draw remaining structures
            if (showStructureDebug && remainingStructuresToFind != null && gridController != null)
            {
                Gizmos.color = Color.magenta;
                
                foreach (Vector2Int pos in remainingStructuresToFind)
                {
                    Vector3 worldPos = gridController.GetCellCenterFromTexture(pos.x, pos.y);
                    Gizmos.DrawWireSphere(worldPos, 0.2f);
                }
            }
        }
        
        // Navigation settings
        public float BaseFlowFieldDesirability => baseFlowFieldDesirability;
        public float TimeBeforeFlowFieldIncrease => timeBeforeFlowFieldIncrease;
        public float FlowFieldInfluenceGrowthRate => flowFieldInfluenceGrowthRate;
        public float MaxSnoopingFlowFieldInfluence => maxSnoopingFlowFieldInfluence;
        public float StructureSearchRadius => structureSearchRadius;
        public float RandomMovementFactor => randomMovementFactor;
        public int MinStructuresPerAnt => minStructuresPerAnt;
        public bool ShowStructureDebug => showStructureDebug;
        
        // Return path settings
        public float EdgeDirectionWeight => edgeDirectionWeight;
        public float PheromoneAvoidanceWeight => pheromoneAvoidanceWeight;
        public float FlowFieldAlignmentWeight => flowFieldAlignmentWeight;
        public float ReturnPathRandomness => returnPathRandomness;
        
        // Pheromone settings
        public float PheromoneLayInterval => pheromoneLayInterval;
        public float BasePheromoneStrength => basePheromoneStrength;
        public bool ScalePheromonesByStructures => scalePheromonesByStructures;
        public float MaxPheromoneStrength => maxPheromoneStrength;
        public int PheromoneSpreadRadius => pheromoneSpreadRadius;
        public int DefaultEnemyTypeIndex => defaultEnemyTypeIndex;
    }
}