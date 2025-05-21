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
        [SerializeField] private int maxStructuresPerAnt = 3;
        [SerializeField] private float baseFlowFieldDesirability = 0.2f;
        [SerializeField] private float flowFieldInfluenceGrowthRate = 0.05f;
        [Tooltip("Set to 0 for instant algorithm execution")]
        [SerializeField] private float antSpeed = 8f;
        
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
        
        // Virtual ant data structure
        private class VirtualAnt
        {
            public Vector2Int position;
            public float lifetime;
            public float lastPheromoneTime;
            public bool isReturning;
            public bool isInOwnedTerritory;
            public bool isSnooping;
            public Vector2Int lastDirection;
            public Vector2Int targetPosition;
            public HashSet<Vector2Int> discoveredStructures = new HashSet<Vector2Int>();
            public List<Vector2Int> visitedCells = new List<Vector2Int>();
            public float currentFlowFieldInfluence;
            public float timeSinceLastStructureFound;
            
            public VirtualAnt(Vector2Int position, float lifetime)
            {
                this.position = position;
                this.lifetime = lifetime;
                this.lastPheromoneTime = 0f;
                this.isReturning = false;
                this.isInOwnedTerritory = false;
                this.isSnooping = false;
                this.lastDirection = Vector2Int.zero;
                this.currentFlowFieldInfluence = 1f;
                this.timeSinceLastStructureFound = 0f;
            }
        }
        
        // References
        private GridController gridController;
        private GridDataGenerator gridDataGenerator;
        private FarmDefender.Core.AI.FlowField.FlowFieldManager flowFieldManager;
        
        // Internal state
        private List<VirtualAnt> virtualAnts = new List<VirtualAnt>();
        private HashSet<Vector2Int> discoveredStructures = new HashSet<Vector2Int>();
        private int totalStructuresInWorld = 0;
        private bool hasCountedStructures = false;
        private float updateInterval = 0.1f; // How often to update the virtual ants
        private float timeSinceLastUpdate = 0f;
        private float structureSearchRadius = 2f;
        private float pheromoneLayInterval = 0.2f;
        private float pheromoneStrength = 1f;
        private int defaultEnemyTypeIndex = 0; // 0=regular, 1=fast, 2=strong
        private float timeBeforeFlowFieldIncrease = 5f;
        private float randomMovementFactor = 0.3f;
        
        private void Start()
        {
            gridController = FindObjectOfType<GridController>();
            gridDataGenerator = FindObjectOfType<GridDataGenerator>();
            flowFieldManager = FindObjectOfType<FarmDefender.Core.AI.FlowField.FlowFieldManager>();
            
            if (gridController == null || gridDataGenerator == null || flowFieldManager == null)
            {
                Debug.LogError("AntManager is missing required references");
                enabled = false;
                return;
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
            
            // Update virtual ants based on time interval
            timeSinceLastUpdate += Time.deltaTime;
            
            if (timeSinceLastUpdate >= updateInterval && virtualAnts.Count > 0)
            {
                UpdateVirtualAnts();
                timeSinceLastUpdate = 0f;
            }
        }
        
        private void UpdateVirtualAnts()
        {
            // Process each virtual ant
            for (int i = virtualAnts.Count - 1; i >= 0; i--)
            {
                VirtualAnt ant = virtualAnts[i];
                
                // Update lifetime
                ant.lifetime -= updateInterval;
                
                // If lifetime is expired, remove the ant
                if (ant.lifetime <= 0)
                {
                    virtualAnts.RemoveAt(i);
                    continue;
                }
                
                // Check if we need to start returning
                CheckReturnConditions(ant);
                
                // Different behavior based on state
                if (ant.isReturning)
                {
                    ReturnToEdge(ant);
                    LayPheromones(ant);
                    
                    // Check if reached edge
                    if (IsAtEdge(ant.position))
                    {
                        virtualAnts.RemoveAt(i);
                    }
                }
                else
                {
                    UpdateExplorationBehavior(ant);
                }
            }
        }
        
        private void CheckReturnConditions(VirtualAnt ant)
        {
            // Return if we've found our max number of structures
            if (!ant.isReturning && ant.discoveredStructures.Count >= maxStructuresPerAnt)
            {
                ant.isReturning = true;
                DetermineReturnTarget(ant);
                return;
            }
            
            // Return if we've reached flow field target and found no structures
            if (!ant.isReturning && HasReachedFlowFieldTarget(ant) && ant.discoveredStructures.Count == 0)
            {
                ant.isReturning = true;
                DetermineReturnTarget(ant);
                return;
            }
            
            // Return if we're halfway through our lifetime
            float initialLifetime = 30f; // Assuming default lifetime
            if (!ant.isReturning && ant.lifetime < initialLifetime * 0.5f)
            {
                ant.isReturning = true;
                DetermineReturnTarget(ant);
                return;
            }
        }
        
        private bool HasReachedFlowFieldTarget(VirtualAnt ant)
        {
            Vector2Int targetCoords = flowFieldManager.GetTargetCoordinates();
            
            // Check if we're at the target coordinates
            return Vector2Int.Distance(ant.position, targetCoords) < 2f;
        }
        
        private void DetermineReturnTarget(VirtualAnt ant)
        {
            // Find closest edge
            Vector2Int gridPos = ant.position;
            int width = gridDataGenerator.GetGridWidth();
            int height = gridDataGenerator.GetGridHeight();
            
            int distToLeft = gridPos.x;
            int distToRight = width - 1 - gridPos.x;
            int distToBottom = gridPos.y;
            int distToTop = height - 1 - gridPos.y;
            
            // Find the shortest distance to an edge
            int minDist = Mathf.Min(distToLeft, distToRight, distToBottom, distToTop);
            
            if (minDist == distToLeft)
                ant.targetPosition = new Vector2Int(0, gridPos.y);
            else if (minDist == distToRight)
                ant.targetPosition = new Vector2Int(width - 1, gridPos.y);
            else if (minDist == distToBottom)
                ant.targetPosition = new Vector2Int(gridPos.x, 0);
            else
                ant.targetPosition = new Vector2Int(gridPos.x, height - 1);
        }
        
        private bool IsAtEdge(Vector2Int position)
        {
            int width = gridDataGenerator.GetGridWidth();
            int height = gridDataGenerator.GetGridHeight();
            
            return position.x == 0 || position.x == width - 1 || 
                   position.y == 0 || position.y == height - 1;
        }
        
        private void UpdateExplorationBehavior(VirtualAnt ant)
        {
            // Track visited cells
            if (!ant.visitedCells.Contains(ant.position))
            {
                ant.visitedCells.Add(ant.position);
            }
            
            // Check if we're in owned territory
            GridCell cell = gridController.GetCell(ant.position.x, ant.position.y);
            bool wasInOwnedTerritory = ant.isInOwnedTerritory;
            ant.isInOwnedTerritory = cell != null && cell.flags.isOwned;
            
            // If we just entered owned territory, update desirability
            if (!wasInOwnedTerritory && ant.isInOwnedTerritory)
            {
                ant.currentFlowFieldInfluence = baseFlowFieldDesirability;
            }
            
            // Check for structures around us
            CheckForStructuresAround(ant);
            
            // Update snooping state based on structure discovery
            if (ant.discoveredStructures.Count > 0 && !ant.isSnooping)
            {
                ant.isSnooping = true;
                ant.currentFlowFieldInfluence = 0f;
                ant.timeSinceLastStructureFound = 0f;
            }
            
            // When snooping or in owned territory, increase flow field influence over time if no structures found
            if ((ant.isSnooping || ant.isInOwnedTerritory) && ant.discoveredStructures.Count > 0)
            {
                ant.timeSinceLastStructureFound += updateInterval;
                
                if (ant.timeSinceLastStructureFound > timeBeforeFlowFieldIncrease)
                {
                    // Gradually increase flow field influence
                    ant.currentFlowFieldInfluence += flowFieldInfluenceGrowthRate * updateInterval;
                    ant.currentFlowFieldInfluence = Mathf.Clamp01(ant.currentFlowFieldInfluence);
                }
            }
            
            // Now decide movement direction based on our state
            if (ant.isInOwnedTerritory)
            {
                // In owned territory: blend between flow field and random exploration
                MoveWithSnoop(ant);
            }
            else
            {
                // Outside owned territory: use flow field
                MoveWithFlowField(ant);
            }
        }
        
        private void CheckForStructuresAround(VirtualAnt ant)
        {
            int radius = (int)structureSearchRadius;
            
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector2Int checkPos = new Vector2Int(ant.position.x + x, ant.position.y + y);
                    
                    // Skip invalid cells
                    if (!gridController.IsValidCell(checkPos.x, checkPos.y))
                        continue;
                        
                    GridCell checkCell = gridController.GetCell(checkPos.x, checkPos.y);
                    if (checkCell != null && checkCell.flags.isOccupied)
                    {
                        // Found a structure, add it to our list
                        if (!ant.discoveredStructures.Contains(checkPos))
                        {
                            ant.discoveredStructures.Add(checkPos);
                            ant.timeSinceLastStructureFound = 0f;
                            
                            // Add to manager's master list of discovered structures
                            discoveredStructures.Add(checkPos);
                        }
                    }
                }
            }
        }
        
        private void MoveWithSnoop(VirtualAnt ant)
        {
            Vector2Int newPosition = ant.position;
            
            // Blend between flow field and random exploration
            if (Random.value < ant.currentFlowFieldInfluence)
            {
                // Use flow field
                GridCell cell = gridController.GetCell(ant.position.x, ant.position.y);
                if (cell != null && cell.flowDirection != Vector2.zero)
                {
                    // Convert flow direction to a grid movement
                    Vector2Int flowMove = GetGridMoveFromDirection(cell.flowDirection);
                    newPosition = ant.position + flowMove;
                }
            }
            else
            {
                // Look for unvisited cells around current position
                List<Vector2Int> possibleDirections = new List<Vector2Int>();
                
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        // Skip current cell
                        if (x == 0 && y == 0)
                            continue;
                        
                        // Check if valid and not visited
                        Vector2Int checkPos = new Vector2Int(ant.position.x + x, ant.position.y + y);
                        if (gridController.IsValidCell(checkPos.x, checkPos.y))
                        {
                            // Prefer unvisited cells
                            if (!ant.visitedCells.Contains(checkPos))
                            {
                                // Double the chance for unvisited cells
                                possibleDirections.Add(checkPos);
                                possibleDirections.Add(checkPos);
                            }
                            else
                            {
                                // Already visited, but still an option
                                possibleDirections.Add(checkPos);
                            }
                        }
                    }
                }
                
                // Prefer continuing in previous direction
                if (ant.lastDirection != Vector2Int.zero)
                {
                    Vector2Int continueDir = ant.position + ant.lastDirection;
                    if (gridController.IsValidCell(continueDir.x, continueDir.y))
                    {
                        // Add it a few times to increase probability
                        possibleDirections.Add(continueDir);
                        possibleDirections.Add(continueDir);
                    }
                }
                
                if (possibleDirections.Count > 0)
                {
                    // Randomly select a direction
                    newPosition = possibleDirections[Random.Range(0, possibleDirections.Count)];
                    ant.lastDirection = newPosition - ant.position;
                }
                else
                {
                    // Fallback: use flow field direction
                    GridCell cell = gridController.GetCell(ant.position.x, ant.position.y);
                    if (cell != null && cell.flowDirection != Vector2.zero)
                    {
                        Vector2Int flowMove = GetGridMoveFromDirection(cell.flowDirection);
                        newPosition = ant.position + flowMove;
                    }
                }
            }
            
            // Add some randomness if specified
            if (randomMovementFactor > 0 && Random.value < randomMovementFactor)
            {
                int randomX = Random.Range(-1, 2);
                int randomY = Random.Range(-1, 2);
                Vector2Int randomPos = new Vector2Int(ant.position.x + randomX, ant.position.y + randomY);
                
                if (gridController.IsValidCell(randomPos.x, randomPos.y))
                {
                    newPosition = randomPos;
                }
            }
            
            // Ensure we're not moving to an invalid cell
            if (gridController.IsValidCell(newPosition.x, newPosition.y))
            {
                // Only move if the cell isn't an obstacle
                GridCell targetCell = gridController.GetCell(newPosition.x, newPosition.y);
                if (targetCell != null && !targetCell.flags.isObstacle)
                {
                    ant.position = newPosition;
                }
            }
        }
        
        private void MoveWithFlowField(VirtualAnt ant)
        {
            // Use flow field to guide toward player base
            GridCell cell = gridController.GetCell(ant.position.x, ant.position.y);
            if (cell != null && cell.flowDirection != Vector2.zero)
            {
                // Convert flow direction to a grid movement
                Vector2Int flowMove = GetGridMoveFromDirection(cell.flowDirection);
                Vector2Int newPosition = ant.position + flowMove;
                
                // Ensure we're not moving to an invalid cell
                if (gridController.IsValidCell(newPosition.x, newPosition.y))
                {
                    // Only move if the cell isn't an obstacle
                    GridCell targetCell = gridController.GetCell(newPosition.x, newPosition.y);
                    if (targetCell != null && !targetCell.flags.isObstacle)
                    {
                        ant.position = newPosition;
                    }
                }
            }
        }
        
        private Vector2Int GetGridMoveFromDirection(Vector2 direction)
        {
            // Convert a normalized flow direction to a grid move
            // We need to round to the nearest grid cell move (-1, 0, 1)
            int x = Mathf.RoundToInt(direction.x);
            int y = Mathf.RoundToInt(direction.y);
            
            // If we ended up with (0,0) but had a direction, force a move
            if (x == 0 && y == 0 && direction.magnitude > 0.1f)
            {
                // Take the larger component
                if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                {
                    x = direction.x > 0 ? 1 : -1;
                }
                else
                {
                    y = direction.y > 0 ? 1 : -1;
                }
            }
            
            return new Vector2Int(x, y);
        }
        
        private void ReturnToEdge(VirtualAnt ant)
        {
            // Calculate direction toward target edge position
            Vector2Int direction = ant.targetPosition - ant.position;
            
            if (direction.sqrMagnitude > 0)
            {
                // Normalize to grid movement
                Vector2Int moveDir = new Vector2Int(
                    Mathf.Clamp(direction.x, -1, 1),
                    Mathf.Clamp(direction.y, -1, 1)
                );
                
                Vector2Int newPosition = ant.position + moveDir;
                
                // Ensure we're not moving to an invalid cell
                if (gridController.IsValidCell(newPosition.x, newPosition.y))
                {
                    // Only move if the cell isn't an obstacle
                    GridCell targetCell = gridController.GetCell(newPosition.x, newPosition.y);
                    if (targetCell != null && !targetCell.flags.isObstacle)
                    {
                        ant.position = newPosition;
                    }
                }
            }
            else
            {
                // Fallback: use inverted flow field
                GridCell cell = gridController.GetCell(ant.position.x, ant.position.y);
                if (cell != null && cell.flowDirection != Vector2.zero)
                {
                    // Invert the flow direction and convert to a grid movement
                    Vector2 inverseFlow = -cell.flowDirection;
                    Vector2Int flowMove = GetGridMoveFromDirection(inverseFlow);
                    Vector2Int newPosition = ant.position + flowMove;
                    
                    // Ensure we're not moving to an invalid cell
                    if (gridController.IsValidCell(newPosition.x, newPosition.y))
                    {
                        // Only move if the cell isn't an obstacle
                        GridCell targetCell = gridController.GetCell(newPosition.x, newPosition.y);
                        if (targetCell != null && !targetCell.flags.isObstacle)
                        {
                            ant.position = newPosition;
                        }
                    }
                }
            }
        }
        
        private void LayPheromones(VirtualAnt ant)
        {
            // Only lay pheromones when returning and at specified interval
            ant.lastPheromoneTime += updateInterval;
            if (ant.lastPheromoneTime < pheromoneLayInterval)
                return;
                
            ant.lastPheromoneTime = 0f;
            
            if (!gridController.IsValidCell(ant.position.x, ant.position.y))
                return;
                
            GridCell cell = gridController.GetCell(ant.position.x, ant.position.y);
            if (cell != null)
            {
                // Increase pheromone level for this enemy type
                cell.pheromones[defaultEnemyTypeIndex] += pheromoneStrength;
            }
        }
        
        // Count all player structures in the world
        private int CountTotalStructures()
        {
            if (hasCountedStructures)
                return totalStructuresInWorld;
                
            int count = 0;
            int width = gridDataGenerator.GetGridWidth();
            int height = gridDataGenerator.GetGridHeight();
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    GridCell cell = gridController.GetCell(x, y);
                    if (cell != null && cell.flags.isOccupied && cell.flags.isOwned)
                    {
                        count++;
                    }
                }
            }
            
            totalStructuresInWorld = count;
            hasCountedStructures = true;
            return count;
        }
        
        // Determine how many ants to spawn based on structure count
        private int DetermineOptimalAntCount()
        {
            int structureCount = CountTotalStructures();
            
            // Calculate needed ants based on structures and max structures per ant
            int neededAnts = Mathf.CeilToInt((float)structureCount / maxStructuresPerAnt);
            
            // Use at least the minimum number specified
            return Mathf.Max(neededAnts, maxAnts);
        }
        
        private void SpawnVirtualAnt()
        {
            // Get a random edge position
            Vector2Int spawnPos = GetRandomEdgeCellPosition();
            
            // Create a new virtual ant
            VirtualAnt ant = new VirtualAnt(spawnPos, 30f); // 30 seconds lifetime
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
            
            // Determine how many ants to spawn
            int antsToSpawn = DetermineOptimalAntCount();
            
            // Spawn all ants at once
            for (int i = 0; i < antsToSpawn; i++)
            {
                SpawnVirtualAnt();
            }
            
            Debug.Log($"ACO algorithm spawned {antsToSpawn} virtual ants to explore the map");
            
            // If speed is set to 0, run all ants to completion immediately
            if (antSpeed <= 0)
            {
                RunInstantAlgorithm();
            }
        }
        
        private void RunInstantAlgorithm()
        {
            Debug.Log("Running ACO algorithm in instant mode");
            
            // Set a very short update interval for fast processing
            float savedInterval = updateInterval;
            updateInterval = 0.0001f;
            
            // Process ants until all are done
            while (virtualAnts.Count > 0)
            {
                UpdateVirtualAnts();
            }
            
            // Restore the normal update interval
            updateInterval = savedInterval;
            
            Debug.Log("ACO algorithm completed in instant mode");
        }
        
        // Public accessor for discovered structures
        public HashSet<Vector2Int> GetDiscoveredStructures()
        {
            return discoveredStructures;
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
                    
                // Calculate world position for visualization
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
        }
    }
}