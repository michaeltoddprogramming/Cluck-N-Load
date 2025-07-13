using UnityEngine;
using System.Collections.Generic;

namespace FarmDefender.Core.AI.FlowField
{
    /// <summary>
    /// Main coordinator for the flow field system.
    /// This replaces the original FlowFieldGenerator with a modular architecture.
    /// </summary>
    public class FlowFieldManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GridController gridController;
        [SerializeField] private GridDataGenerator gridDataGenerator;
        [SerializeField] private FlowFieldSettings settings;
        [SerializeField] private FlowFieldTargetManager targetManager;
        [SerializeField] private FlowFieldVisualizer visualizer;
        
        [Header("Grid Monitor Integration")]
        [SerializeField] private bool useGridMonitor = true;
        
        [Header("Performance Optimization")]
        [Tooltip("Should the flow field update only when in build mode?")]
        [SerializeField] private bool updateOnlyInBuildMode = true;
        
        [Tooltip("Number of destroyed buildings before flow field update")]
        [SerializeField] private int buildingDestructionThreshold = 3;
        
        [Tooltip("Minimum time (in seconds) between flow field updates")]
        [SerializeField] private float updateThrottleTime = 0.5f;
        
        private FlowFieldAlgorithm algorithm;
        private GridMonitor gridMonitor;
        private bool initialized = false;
        
        // Optimization tracking variables
        private int destroyedBuildingsCounter = 0;
        private float lastUpdateTime = 0f;
        private bool buildModeActive = false;
        private bool updateRequested = false;
        
        // Unity Lifecycle Methods
        
        private void Awake()
        {
            FindRequiredComponents();
        }
        
        private void Start()
        {
            if (!initialized)
                return;
                
            // Initialize the algorithm with settings
            algorithm = new FlowFieldAlgorithm(settings);
            
            // Connect visualizer to algorithm data
            visualizer.Initialize(algorithm.FlowStrengthMap, algorithm.StreamInfluenceMap);
            
            // Subscribe to grid change events
            if (useGridMonitor && gridMonitor != null)
            {
                gridMonitor.OnGridChanged += HandleGridChanged;
                }
            
            // Generate initial flow field after a short delay
            Invoke("GenerateInitialFlowField", 0.5f);
            
            // Set initial update time
            lastUpdateTime = Time.time;
        }
        
        private void Update()
        {
            if (!initialized)
                return;
            
            // Only check for target changes - don't regenerate based on timing
            bool targetChanged = targetManager.HasTargetChanged();
            
            // Process updates only when explicitly requested or target changed
            if (targetChanged || updateRequested)
            {
                // Throttle updates based on time if needed
                if (Time.time - lastUpdateTime < updateThrottleTime)
                    return;
                    
                Vector2Int target = targetManager.GetTargetCoordinates();
                if (targetManager.IsValidTarget(target))
                {
                    GenerateFlowField(target);
                    lastUpdateTime = Time.time;
                    updateRequested = false;
                    destroyedBuildingsCounter = 0;
                    
                    }
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from grid monitor events
            if (gridMonitor != null)
            {
                gridMonitor.OnGridChanged -= HandleGridChanged;
            }
        }
        
        // Private Methods
        
        private void FindRequiredComponents()
        {
            // Find or create settings
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<FlowFieldSettings>();
                }
            
            // Find GridController
            if (gridController == null)
                gridController = FindFirstObjectByType<GridController>();
                
            // Find GridDataGenerator
            if (gridDataGenerator == null && gridController != null)
                gridDataGenerator = FindFirstObjectByType<GridDataGenerator>();
                
            // Find GridMonitor if needed
            if (useGridMonitor)
                gridMonitor = FindFirstObjectByType<GridMonitor>();
                
            // Create target manager if not assigned
            if (targetManager == null)
            {
                var targetManagerObj = new GameObject("FlowFieldTargetManager");
                targetManagerObj.transform.SetParent(transform);
                targetManager = targetManagerObj.AddComponent<FlowFieldTargetManager>();
            }
            
            // Create visualizer if not assigned
            if (visualizer == null)
            {
                var visualizerObj = new GameObject("FlowFieldVisualizer");
                visualizerObj.transform.SetParent(transform);
                visualizer = visualizerObj.AddComponent<FlowFieldVisualizer>();
            }
            
            // Check if we have all required components
            initialized = gridController != null && gridDataGenerator != null;
            
            if (!initialized)
            {
                Debug.LogError("Missing required components for FlowFieldManager to function.");
            }
        }
        
        private void GenerateInitialFlowField()
        {
            if (!initialized)
                return;
                
            Vector2Int target = targetManager.GetTargetCoordinates();
            if (targetManager.IsValidTarget(target))
            {
                GenerateFlowField(target);
                }
            else
            {
                Debug.LogWarning("Could not generate initial flow field: invalid target");
            }
        }
        
        private void HandleGridChanged(GridChangeType changeType)
        {
            if (changeType == GridChangeType.Structural)
            {
                // Don't increment counter if in build mode (building placement)
                if (buildModeActive)
                {
                    // When in build mode, ignore structural changes
                    // The update will be triggered when build mode is exited
                    return;
                }
                
                // Outside of build mode, changes are likely building destruction
                // No need to increment here as GridMonitor will call NotifyBuildingDestroyed
            }
        }
        
        // Public Methods
        
        /// <summary>
        /// Set the build mode status to control when flow field updates occur.
        /// </summary>
        public void SetBuildModeActive(bool isActive)
        {
            // If exiting build mode, request an update
            if (buildModeActive && !isActive)
            {
                updateRequested = true;
                }
            
            buildModeActive = isActive;
        }
        
        /// <summary>
        /// Notify the flow field that a building was destroyed.
        /// </summary>
        public void NotifyBuildingDestroyed()
        {
            destroyedBuildingsCounter++;
            
            // Check if we've reached the threshold for updating
            if (destroyedBuildingsCounter >= buildingDestructionThreshold)
            {
                // Request an update rather than updating immediately
                updateRequested = true;
                }
        }
        
        /// <summary>
        /// Generates a new flow field with the specified target.
        /// </summary>
        public void GenerateFlowField(Vector2Int target)
        {
            if (!initialized || algorithm == null)
                return;
                
            if (!targetManager.IsValidTarget(target))
            {
                Debug.LogWarning($"Invalid target for flow field generation: {target}");
                return;
            }
            
            algorithm.GenerateFlowField(gridDataGenerator, target);
        }
        
        /// <summary>
        /// Sets the target to use for the flow field.
        /// </summary>
        public void SetTarget(Vector2Int target)
        {
            if (targetManager != null)
            {
                targetManager.SetManualTarget(target);
            }
        }
        
        /// <summary>
        /// Sets a transform to follow for the flow field.
        /// </summary>
        public void SetTargetTransform(Transform target)
        {
            if (targetManager != null)
            {
                targetManager.SetTargetTransform(target);
            }
        }

        /// <summary>
        /// Sets a transform and specific world point to follow for the flow field.
        /// </summary>
        public void SetTargetTransformWithPoint(Transform target, Vector3 worldPoint)
        {
            if (targetManager != null)
            {
                targetManager.SetTargetTransformWithPoint(target, worldPoint);
            }
        }
        
        // Manual utility methods
        
        [ContextMenu("Generate Flow Field")]
        public void GenerateFlowFieldManually()
        {
            if (!initialized || targetManager == null)
                return;
                
            Vector2Int target = targetManager.GetTargetCoordinates();
            if (targetManager.IsValidTarget(target))
            {
                GenerateFlowField(target);
                }
            else
            {
                Debug.LogWarning("Invalid target for flow field generation");
            }
        }
        
        [ContextMenu("Force Flow Field Update")]
        public void ForceFlowFieldUpdate()
        {
            updateRequested = true;
        }
        
        [ContextMenu("Print Obstacle Stats")]
        public void PrintObstacleStats()
        {
            if (!initialized)
                return;
                
            int gridWidth = gridDataGenerator.GetGridWidth();
            int gridHeight = gridDataGenerator.GetGridHeight();
            
            int occupiedCount = 0;
            int obstacleCount = 0;
            int bothCount = 0;
            
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    GridCell cell = gridDataGenerator.GetCell(x, y);
                    if (cell != null)
                    {
                        if (cell.flags.isOccupied) occupiedCount++;
                        if (cell.flags.isObstacle) obstacleCount++;
                        if (cell.flags.isOccupied && cell.flags.isObstacle) bothCount++;
                    }
                }
            }
            
            }

        public Dictionary<Vector2Int, float> GetFlowStrengthMap()
        {
            if (algorithm == null) return null;
            return algorithm.FlowStrengthMap;
        }

        public Dictionary<Vector2Int, float> GetStreamInfluenceMap()
        {
            if (algorithm == null) return null;
            return algorithm.StreamInfluenceMap;
        }

        /// <summary>
        /// Returns the current target coordinates of the flow field.
        /// </summary>
        public Vector2Int GetTargetCoordinates()
        {
            if (targetManager != null)
            {
                return targetManager.GetTargetCoordinates();
            }
            return Vector2Int.zero;
        }

        /// <summary>
        /// Provides public access to the GridController
        /// </summary>
        public GridController GridController => gridController;

        /// <summary>
        /// Regenerates the flow field targeting the specified coordinates.
        /// </summary>
        public void SetTarget(int x, int y)
        {
            SetTarget(new Vector2Int(x, y));
        }

        /// <summary>
        /// Explicitly manually regenerate the flow field.
        /// </summary>
        public void RegenerateFlowField()
        {
            GenerateFlowFieldManually();
        }
    }
}