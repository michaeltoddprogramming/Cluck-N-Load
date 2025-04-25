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
        
        private FlowFieldAlgorithm algorithm;
        private GridMonitor gridMonitor;
        private bool initialized = false;
        
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
                Debug.Log("FlowFieldManager connected to GridMonitor");
            }
            
            // Generate initial flow field after a short delay
            Invoke("GenerateInitialFlowField", 0.5f);
        }
        
        private void Update()
        {
            if (!initialized)
                return;
                
            // Check for target changes
            if (targetManager.HasTargetChanged())
            {
                Vector2Int target = targetManager.GetTargetCoordinates();
                if (targetManager.IsValidTarget(target))
                {
                    GenerateFlowField(target);
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
                Debug.LogWarning("No FlowFieldSettings assigned. Using default settings.");
            }
            
            // Find GridController
            if (gridController == null)
                gridController = FindObjectOfType<GridController>();
                
            // Find GridDataGenerator
            if (gridDataGenerator == null && gridController != null)
                gridDataGenerator = FindObjectOfType<GridDataGenerator>();
                
            // Find GridMonitor if needed
            if (useGridMonitor)
                gridMonitor = FindObjectOfType<GridMonitor>();
                
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
                Debug.Log($"Generated initial flow field to target: {target}");
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
                // Refresh flow field when grid structure changes
                Vector2Int target = targetManager.GetTargetCoordinates();
                if (targetManager.IsValidTarget(target))
                {
                    Debug.Log("Regenerating flow field due to grid structural changes");
                    GenerateFlowField(target);
                }
            }
        }
        
        // Public Methods
        
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
                Debug.Log($"Manually triggered flow field generation to {target}");
            }
            else
            {
                Debug.LogWarning("Invalid target for flow field generation");
            }
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
            
            Debug.Log($"Grid obstacles: {obstacleCount} obstacles, {occupiedCount} occupied, {bothCount} both");
        }

        // Add these methods to your FlowFieldManager class
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
    }
}