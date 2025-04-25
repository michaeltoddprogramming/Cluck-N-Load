using System.Collections.Generic;
using UnityEngine;

namespace FarmDefender.Core.AI.FlowField
{
    public class FlowFieldAgent : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 7f;
        public float rotationSpeed = 5f;
        public float arrivalThreshold = 0.1f;
        public float pathRefreshRate = 0.1f;
        
        [Header("Debug")]
        public bool showDebugInfo = false;

        [Header("Smooth Movement Settings")]
        [Tooltip("Enables bilinear interpolation for smoother flow field following")]
        public bool useBilinearInterpolation = true;
        [Tooltip("Base strength of interpolation (0 = none, 1 = full)")]
        [Range(0f, 1f)]
        public float interpolationStrength = 0.8f;

        // Internal references
        private GridController gridController;
        private FlowFieldManager flowFieldManager; // Changed from FlowFieldGenerator
        private GridDataGenerator gridDataGenerator;
        private Vector2Int currentCellCoord;
        private Vector3 targetPosition;
        private bool isMoving = false;
        private float pathUpdateTimer = 0f;
        private bool isInitialized = false;
        private Vector3 lastPosition;

        public Vector3 velocity { get; private set; }

        private void Awake()
        {
            lastPosition = transform.position;
            FindRequiredComponents();
        }

        private void FindRequiredComponents()
        {
            gridController = FindObjectOfType<GridController>();
            flowFieldManager = FindObjectOfType<FlowFieldManager>(); // Changed from FlowFieldGenerator
            gridDataGenerator = FindObjectOfType<GridDataGenerator>();
            isInitialized = (gridController != null && flowFieldManager != null && gridDataGenerator != null);
            
            if (!isInitialized)
            {
                Debug.LogWarning($"FlowFieldAgent on {gameObject.name} missing required components");
            }
        }

        private void Start()
        {
            if (isInitialized)
                InitializePosition();
        }

        private void InitializePosition()
        {
            currentCellCoord = gridController.WorldToGridCoords(transform.position);
            
            if (!gridController.IsValidCell(currentCellCoord.x, currentCellCoord.y))
                FindNearestValidCell();
        }

        private void FindNearestValidCell()
        {
            int maxSearchRadius = 5;
            
            for (int radius = 1; radius <= maxSearchRadius; radius++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        if (Mathf.Abs(x) == radius || Mathf.Abs(y) == radius)
                        {
                            int testX = currentCellCoord.x + x;
                            int testY = currentCellCoord.y + y;
                            
                            if (gridController.IsValidCell(testX, testY))
                            {
                                currentCellCoord = new Vector2Int(testX, testY);
                                Vector3 validPos = gridController.GetCellCenterFromTexture(testX, testY);
                                transform.position = new Vector3(validPos.x, transform.position.y, validPos.z);
                                return;
                            }
                        }
                    }
                }
            }
        }

        private void Update()
        {
            // Calculate velocity
            velocity = (transform.position - lastPosition) / Time.deltaTime;
            lastPosition = transform.position;

            if (!isInitialized)
            {
                FindRequiredComponents();
                if (isInitialized)
                    InitializePosition();
                return;
            }

            // Path update timing
            pathUpdateTimer += Time.deltaTime;
            if (pathUpdateTimer >= pathRefreshRate)
            {
                pathUpdateTimer = 0f;
                UpdatePathFollowing();
            }
            
            // Movement handling
            if (isMoving)
                MoveTowardTarget();
        }

        private void UpdatePathFollowing()
        {
            if (!isInitialized) return;
            
            // Update current cell coordinates
            currentCellCoord = gridController.WorldToGridCoords(transform.position);
            
            // CRITICAL: First check for strong streams BEFORE any interpolation logic
            GridCell currentCell = gridController.GetCell(currentCellCoord.x, currentCellCoord.y);
            float currentCellStrength = GetFlowStrength(currentCellCoord.x, currentCellCoord.y);
            
            // If we're on a strong stream, SKIP all interpolation completely
            if (currentCell != null && currentCellStrength > 0.2f && 
                currentCell.flowDirection != Vector2.zero && 
                currentCell.integrationCost != int.MaxValue)
            {
                // We're on a priority stream - use exact cell direction with ZERO interpolation
                float cellSize = gridController.GetCellSize();
                Vector3 currentPos = gridController.GetCellCenterFromTexture(currentCellCoord.x, currentCellCoord.y);
                Vector3 targetDirection = new Vector3(currentCell.flowDirection.x, 0, currentCell.flowDirection.y);
                Vector3 worldTargetPos = currentPos + targetDirection * cellSize;
                
                targetPosition = new Vector3(worldTargetPos.x, transform.position.y, worldTargetPos.z);
                isMoving = true;
                return; // Skip ALL interpolation logic
            }
            
            // Continue with normal path following only if not on a strong stream
            if (useBilinearInterpolation)
            {
                Vector2 flowDirection = GetInterpolatedFlowDirection();
                
                if (flowDirection != Vector2.zero)
                {
                    // Calculate the target position using the interpolated flow direction
                    float cellSize = gridController.GetCellSize();
                    Vector3 currentPos = transform.position;
                    Vector3 targetDirection = new Vector3(flowDirection.x, 0, flowDirection.y);
                    Vector3 worldTargetPos = currentPos + targetDirection * cellSize;
                    
                    // Set the target position, preserving the agent's height
                    targetPosition = new Vector3(worldTargetPos.x, transform.position.y, worldTargetPos.z);
                    isMoving = true;
                }
            }
            // Otherwise use the standard cell-based approach
            else
            {
                currentCell = gridController.GetCell(currentCellCoord.x, currentCellCoord.y);
                
                if (currentCell == null)
                    return;
                    
                // Check if we have a valid flow direction
                if (currentCell.flowDirection != Vector2.zero && currentCell.integrationCost != int.MaxValue)
                {
                    // Get the world position of the current cell
                    Vector3 currentPos = gridController.GetCellCenterFromTexture(currentCellCoord.x, currentCellCoord.y);
                    
                    // Calculate the target position using the flow direction
                    float cellSize = gridController.GetCellSize();
                    Vector3 targetDirection = new Vector3(currentCell.flowDirection.x, 0, currentCell.flowDirection.y);
                    Vector3 worldTargetPos = currentPos + targetDirection * cellSize;
                    
                    // Set the target position, preserving the agent's height
                    targetPosition = new Vector3(worldTargetPos.x, transform.position.y, worldTargetPos.z);
                    isMoving = true;
                }
            }
        }

        private Vector2 GetInterpolatedFlowDirection()
        {
            // Get exact world position
            Vector3 worldPos = transform.position;
            
            // Get the grid cell size
            float cellSize = gridController.GetCellSize();
            
            // Get grid origin
            Vector4 gridOrigin = gridDataGenerator.GetGridOrigin();
            
            // Convert to floating point grid coordinates
            float gridX = (worldPos.x - gridOrigin.x) / cellSize;
            float gridZ = (worldPos.z - gridOrigin.y) / cellSize;
            
            // Get current cell (the one agent is directly on)
            int currentX = Mathf.FloorToInt(gridX);
            int currentY = Mathf.FloorToInt(gridZ);
            
            // NEW: First check if current cell is the strongest among neighbors
            float currentStrength = GetFlowStrength(currentX, currentY);
            bool currentIsStrongest = true;
            
            // Check all 8 neighbors plus current cell
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue; // Skip current cell in comparison
                    
                    int nx = currentX + dx;
                    int ny = currentY + dy;
                    
                    if (gridController.IsValidCell(nx, ny))
                    {
                        // If any neighbor has stronger flow, current is not strongest
                        float neighborStrength = GetFlowStrength(nx, ny);
                        if (neighborStrength > currentStrength)
                        {
                            currentIsStrongest = false;
                            break;
                        }
                    }
                }
                if (!currentIsStrongest) break;
            }
            
            // If current cell is the strongest field among all neighbors, don't interpolate
            if (currentIsStrongest && currentStrength > 0.01f)
            {
                GridCell currentCell = gridController.GetCell(currentX, currentY);
                if (currentCell != null && currentCell.flowDirection != Vector2.zero)
                {
                    return currentCell.flowDirection;
                }
            }
            
            // CRITICAL ENHANCEMENT: First, check if we're very close to an obstacle
            // by testing more surrounding cells in a slightly larger area
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = currentX + dx;
                    int ny = currentY + dy;
                    
                    if (gridController.IsValidCell(nx, ny))
                    {
                        GridCell nearbyCell = gridController.GetCell(nx, ny);
                        if (nearbyCell != null && (nearbyCell.flags.isObstacle || nearbyCell.flags.isOccupied))
                        {
                            // IMMEDIATE RESPONSE: If near obstacle, use pure pathfinding from current cell
                            // with absolutely NO interpolation
                            GridCell currentCell = gridController.GetCell(currentX, currentY);
                            if (currentCell != null && currentCell.flowDirection != Vector2.zero && 
                                currentCell.integrationCost != int.MaxValue)
                            {
                                return currentCell.flowDirection;
                            }
                        }
                    }
                }
            }
            
            // Check if current cell has a strong flow - if so, no interpolation at all
            // LOWER THRESHOLD from 0.5 to 0.2 to catch more priority flows
            float currentCellStrength = GetFlowStrength(currentX, currentY);
            if (currentCellStrength > 0.2f)  // REDUCED FROM 0.3f TO 0.2f
            {
                // CRITICAL: On strong flows, completely disable interpolation
                // and use the exact flow direction from the current cell
                GridCell currentCell = gridController.GetCell(currentX, currentY);
                if (currentCell != null && currentCell.flowDirection != Vector2.zero)
                {
                    return currentCell.flowDirection;
                }
            }
            
            // Get the four surrounding cell indices
            int x0 = Mathf.FloorToInt(gridX);
            int x1 = Mathf.CeilToInt(gridX);
            int y0 = Mathf.FloorToInt(gridZ);
            int y1 = Mathf.CeilToInt(gridZ);
            
            // NEW: Check if any of the corner cells is an obstacle BEFORE proceeding
            bool anyObstacleNearby = false;
            if (IsObstacleAt(x0, y0) || IsObstacleAt(x1, y0) || 
                IsObstacleAt(x0, y1) || IsObstacleAt(x1, y1))
            {
                anyObstacleNearby = true;
            }
            
            // If we're near obstacles, use the cell with the strongest flow
            if (anyObstacleNearby)
            {
                // Find best non-obstacle cell
                GridCell currentCell = gridController.GetCell(currentX, currentY);
                
                // Collect valid neighbors
                List<GridCell> validNeighbors = new List<GridCell>();
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue; // Skip current cell
                        
                        int nx = currentX + dx;
                        int ny = currentY + dy;
                        
                        if (gridController.IsValidCell(nx, ny))
                        {
                            GridCell neighborCell = gridController.GetCell(nx, ny);
                            if (neighborCell != null && !neighborCell.flags.isObstacle && 
                                !neighborCell.flags.isOccupied && neighborCell.flowDirection != Vector2.zero)
                            {
                                validNeighbors.Add(neighborCell);
                            }
                        }
                    }
                }
                
                // Find neighbor with best flow
                GridCell bestCell = currentCell;
                float bestStrength = GetFlowStrength(currentX, currentY);
                
                foreach (var cell in validNeighbors)
                {
                    float strength = GetFlowStrength(cell.x, cell.y);
                    if (strength > bestStrength)
                    {
                        bestStrength = strength;
                        bestCell = cell;
                    }
                }
                
                return bestCell.flowDirection;
            }
            
            // Calculate interpolation factors
            float tx = gridX - x0;
            float ty = gridZ - y0;
            
            // Clamp indices to valid range
            x0 = Mathf.Clamp(x0, 0, gridController.TextureWidth - 1);
            x1 = Mathf.Clamp(x1, 0, gridController.TextureWidth - 1);
            y0 = Mathf.Clamp(y0, 0, gridController.TextureHeight - 1);
            y1 = Mathf.Clamp(y1, 0, gridController.TextureHeight - 1);
            
            // Get the four cells
            GridCell c00 = gridController.GetCell(x0, y0);
            GridCell c10 = gridController.GetCell(x1, y0);
            GridCell c01 = gridController.GetCell(x0, y1);
            GridCell c11 = gridController.GetCell(x1, y1);
            
            // If any cell is null, invalid, or an obstacle, default to the current cell
            if (c00 == null || c10 == null || c01 == null || c11 == null ||
                c00.integrationCost == int.MaxValue || c10.integrationCost == int.MaxValue ||
                c01.integrationCost == int.MaxValue || c11.integrationCost == int.MaxValue ||
                c00.flags.isObstacle || c10.flags.isObstacle ||
                c01.flags.isObstacle || c11.flags.isObstacle)
            {
                GridCell currentCell = gridController.GetCell(currentCellCoord.x, currentCellCoord.y);
                return currentCell != null ? currentCell.flowDirection : Vector2.zero;
            }
            
            // Check for strong flows in ANY of the four cells
            float flowStrength00 = GetFlowStrength(x0, y0);
            float flowStrength10 = GetFlowStrength(x1, y0);
            float flowStrength01 = GetFlowStrength(x0, y1);
            float flowStrength11 = GetFlowStrength(x1, y1);
            
            // If ANY cell has a strong flow, use the strongest one with NO interpolation
            float maxFlowStrength = Mathf.Max(flowStrength00, flowStrength10, flowStrength01, flowStrength11);
            if (maxFlowStrength > 0.2f) // LOWERED from 0.3f to 0.2f
            {
                // On strong flows, completely disable interpolation
                return FindStrongestFlowCell(c00, c10, c01, c11).flowDirection;
            }
            
            // For normal flow areas, use bilinear interpolation with strength adjustment
            float avgFlowStrength = (flowStrength00 + flowStrength10 + flowStrength01 + flowStrength11) / 4f;
            
            // INCREASED SCALING: More aggressive reduction of interpolation strength
            float adjustedInterpolationStrength = interpolationStrength * (1f - (avgFlowStrength * 2f));
            adjustedInterpolationStrength = Mathf.Max(0, adjustedInterpolationStrength); // Ensure it doesn't go negative
            
            // If interpolation is effectively zero, just use the strongest cell's direction
            if (adjustedInterpolationStrength < 0.05f)
            {
                return FindStrongestFlowCell(c00, c10, c01, c11).flowDirection;
            }
            
            // Perform bilinear interpolation with adjusted strength
            Vector2 dir00 = c00.flowDirection;
            Vector2 dir10 = c10.flowDirection;
            Vector2 dir01 = c01.flowDirection;
            Vector2 dir11 = c11.flowDirection;
            
            // Blend factors based on adjusted interpolation strength
            float finalTx = Mathf.Lerp(0, tx, adjustedInterpolationStrength);
            float finalTy = Mathf.Lerp(0, ty, adjustedInterpolationStrength);
            
            // Bilinear interpolation formula
            Vector2 horizontalBlend0 = Vector2.Lerp(dir00, dir10, finalTx);
            Vector2 horizontalBlend1 = Vector2.Lerp(dir01, dir11, finalTx);
            Vector2 result = Vector2.Lerp(horizontalBlend0, horizontalBlend1, finalTy);
            
            // Ensure we have a normalized vector
            if (result.magnitude > 0)
                result.Normalize();
                
            return result;
        }

        // Add this helper method to check for obstacles
        private bool IsObstacleAt(int x, int y)
        {
            if (!gridController.IsValidCell(x, y))
                return true; // Treat out-of-bounds as obstacles
                
            GridCell cell = gridController.GetCell(x, y);
            return cell == null || cell.flags.isObstacle || cell.flags.isOccupied;
        }

        private float GetFlowStrength(int x, int y)
        {
            if (flowFieldManager == null) return 0f;
            
            Vector2Int cellPos = new Vector2Int(x, y);
            
            // Initialize variables before TryGetValue calls
            float flowStrength = 0f;
            float streamInfluence = 0f;
            
            // Get access to maps through the FlowFieldManager
            Dictionary<Vector2Int, float> flowStrengthMap = flowFieldManager.GetFlowStrengthMap();
            if (flowStrengthMap != null && flowStrengthMap.TryGetValue(cellPos, out flowStrength))
            {
                return flowStrength;
            }
            
            Dictionary<Vector2Int, float> streamInfluenceMap = flowFieldManager.GetStreamInfluenceMap();
            if (streamInfluenceMap != null && streamInfluenceMap.TryGetValue(cellPos, out streamInfluence))
            {
                return streamInfluence * 0.8f; // Slightly weaker than direct flow strength
            }
            
            return 0f;
        }

        private GridCell FindStrongestFlowCell(GridCell c00, GridCell c10, GridCell c01, GridCell c11)
        {
            GridCell strongest = c00;
            float maxStrength = GetFlowStrength(c00.x, c00.y);
            
            float s10 = GetFlowStrength(c10.x, c10.y);
            if (s10 > maxStrength)
            {
                maxStrength = s10;
                strongest = c10;
            }
            
            float s01 = GetFlowStrength(c01.x, c01.y);
            if (s01 > maxStrength)
            {
                maxStrength = s01;
                strongest = c01;
            }
            
            float s11 = GetFlowStrength(c11.x, c11.y);
            if (s11 > maxStrength)
            {
                maxStrength = s11;
                strongest = c11;
            }
            
            return strongest;
        }

        private void MoveTowardTarget()
        {
            // Calculate distance to target (ignoring Y for ground movement)
            float distanceToTarget = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(targetPosition.x, 0, targetPosition.z)
            );
            
            // If we're close enough to the target, stop moving
            if (distanceToTarget < arrivalThreshold)
            {
                isMoving = false;
                return;
            }
            
            // Calculate movement direction
            Vector3 movementDirection = (targetPosition - transform.position).normalized;
            
            // Move toward target
            transform.position += movementDirection * moveSpeed * Time.deltaTime;
            
            // Rotate toward movement direction
            if (movementDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        private void OnDrawGizmos()
        {
            if (!showDebugInfo || !Application.isPlaying || !isInitialized)
                return;
                
            // Draw current cell
            Gizmos.color = Color.yellow;
            if (gridController != null && gridController.IsValidCell(currentCellCoord.x, currentCellCoord.y))
            {
                Vector3 cellCenter = gridController.GetCellCenterFromTexture(currentCellCoord.x, currentCellCoord.y);
                Gizmos.DrawWireCube(cellCenter, new Vector3(0.9f, 0.1f, 0.9f));
            }
            
            // Draw target position
            if (isMoving)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(targetPosition, 0.2f);
            }
        }

        public void SetGridController(GridController controller)
        {
            gridController = controller;
        }

        public void SetFlowFieldManager(FlowFieldManager manager)
        {
            flowFieldManager = manager;
            isInitialized = (gridController != null && flowFieldManager != null && gridDataGenerator != null);
        }
    }
}