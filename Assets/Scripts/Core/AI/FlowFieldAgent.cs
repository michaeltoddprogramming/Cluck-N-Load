using UnityEngine;

namespace FarmDefender.Core.AI.FlowField
{
    /// <summary>
    /// Agent that follows flow field paths with bilinear interpolation.
    /// Reduces interpolation on priority paths and disables it entirely on the strongest paths.
    /// </summary>
    public class FlowFieldAgent : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float stoppingDistance = 0.1f;
        
        [Header("Interpolation Settings")]
        [SerializeField] private bool useInterpolation = true;
        [SerializeField] private float priorityPathThreshold = 0.3f; // Above this value, reduce interpolation
        [SerializeField] private float maxPriorityPathValue = 0.7f; // At this value, no interpolation
        
        [Header("Dependencies")]
        [SerializeField] private FlowFieldManager flowFieldManager;
        
        [Header("Movement Smoothing")]
        [SerializeField] private float velocitySmoothTime = 0.5f; // Try 0.5-0.8 instead of 0.3
        [SerializeField] private float minMoveThreshold = 0.05f; // Add minimum velocity threshold to prevent tiny movements
        [SerializeField] private float directionPersistence = 0.2f; // How much previous direction affects current
        
        [Header("Debug")]
        [SerializeField] private bool showGizmos = true; // New field to control gizmo visibility
        
        private Vector2 previousFlowDirection = Vector2.zero;
        private Vector3 currentVelocity = Vector3.zero;
        public bool isMoving = true;

        public bool IsMoving()
        {
            return isMoving;
        }
                
        private void Start()
        {
            if (flowFieldManager == null)
            {
                flowFieldManager = FindObjectOfType<FlowFieldManager>();
                if (flowFieldManager == null)
                {
                    Debug.LogError("FlowFieldAgent requires a FlowFieldManager in the scene");
                    enabled = false;
                }
            }
        }
        
        private void Update()
        {
            if (isMoving)
            {
                FollowFlowField();
            }
        }
        
        private void FollowFlowField()
        {
            // Get current world position (ignoring y-component for 2D flow field)
            Vector3 currentPosition = transform.position;
            Vector2 position2D = new Vector2(currentPosition.x, currentPosition.z);
            
            // Get current grid cell
            var gridController = flowFieldManager.GridController;
            Vector2Int gridCoords = gridController.WorldToGridCoords(currentPosition);
            
            // Check if we're in a valid grid area
            if (!gridController.IsValidCell(gridCoords.x, gridCoords.y))
                return;
            
            // Determine flow direction based on priority path status
            Vector2 flowDirection;
            
            // Check if we're on or near a priority path
            var flowStrengthMap = flowFieldManager.GetFlowStrengthMap();
            float flowStrength = 0f;
            
            if (flowStrengthMap != null && flowStrengthMap.TryGetValue(gridCoords, out flowStrength))
            {
                if (flowStrength >= maxPriorityPathValue || !useInterpolation)
                {
                    // On strongest paths - NO interpolation
                    flowDirection = GetCellFlowDirection(gridCoords);
                }
                else if (flowStrength > priorityPathThreshold)
                {
                    // Partial interpolation based on priority strength
                    float interpolationFactor = 1.0f - ((flowStrength - priorityPathThreshold) / 
                                                     (maxPriorityPathValue - priorityPathThreshold));
                    flowDirection = GetInterpolatedDirection(position2D, interpolationFactor);
                }
                else
                {
                    // Full interpolation
                    flowDirection = GetInterpolatedDirection(position2D, 1.0f);
                }
            }
            else
            {
                // No priority path information, use full interpolation
                flowDirection = GetInterpolatedDirection(position2D, 1.0f);
            }
            
            // Skip movement if no valid direction
            if (flowDirection == Vector2.zero)
                return;
                
            // Apply direction persistence
            if (previousFlowDirection != Vector2.zero)
            {
                flowDirection = Vector2.Lerp(previousFlowDirection, flowDirection, 1f - directionPersistence);
            }
            previousFlowDirection = flowDirection;
            
            // Convert 2D direction to 3D movement vector
            Vector3 desiredDirection = new Vector3(flowDirection.x, 0, flowDirection.y);
            
            // Calculate target velocity based on desired direction
            Vector3 targetVelocity = desiredDirection * moveSpeed;
            
            // Apply velocity smoothing - this is the key to preventing oscillation
            Vector3 smoothedVelocity = Vector3.SmoothDamp(
                currentVelocity,
                targetVelocity, 
                ref currentVelocity, 
                velocitySmoothTime
            );
            
            // Add minimum velocity threshold to prevent tiny movements
            if (smoothedVelocity.magnitude < minMoveThreshold)
            {
                smoothedVelocity = Vector3.zero;
            }
            
            // Apply movement using the smoothed velocity
            transform.position += smoothedVelocity * Time.deltaTime;
            
            // Apply rotation to face direction of movement
            if (smoothedVelocity.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(smoothedVelocity);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                                                    rotationSpeed * Time.deltaTime);
            }
        }
        
        private Vector2 GetInterpolatedDirection(Vector2 worldPosition, float interpolationStrength)
        {
            var gridController = flowFieldManager.GridController;
            GridDataGenerator dataGenerator = gridController.GetComponent<GridDataGenerator>();
            if (dataGenerator == null)
                return Vector2.zero;
            
            // Convert world position to grid position
            Vector2Int gridPos = gridController.WorldToGridCoords(new Vector3(worldPosition.x, 0, worldPosition.y));
            
            // Get grid dimensions to check boundaries
            int gridWidth = dataGenerator.GetGridWidth();
            int gridHeight = dataGenerator.GetGridHeight();
            
            // Check if position is valid
            if (gridPos.x < 0 || gridPos.x >= gridWidth - 1 || gridPos.y < 0 || gridPos.y >= gridHeight - 1)
            {
                // Near grid edge, fall back to non-interpolated direction
                return GetCellFlowDirection(gridPos);
            }
            
            // Get the positions of the four surrounding grid cells
            Vector2Int bl = gridPos;                            // Bottom Left
            Vector2Int br = new Vector2Int(gridPos.x + 1, gridPos.y);   // Bottom Right
            Vector2Int tl = new Vector2Int(gridPos.x, gridPos.y + 1);   // Top Left
            Vector2Int tr = new Vector2Int(gridPos.x + 1, gridPos.y + 1);   // Top Right
            
            // Now safely get the cells - we've already checked that these coordinates are valid
            GridCell blCell = dataGenerator.GetCell(bl.x, bl.y);
            GridCell brCell = dataGenerator.GetCell(br.x, br.y);
            GridCell tlCell = dataGenerator.GetCell(tl.x, tl.y);
            GridCell trCell = dataGenerator.GetCell(tr.x, tr.y);
            
            // If any surrounding cell is null, fall back to direct cell direction
            if (blCell == null || brCell == null || tlCell == null || trCell == null)
                return GetCellFlowDirection(gridPos);
                
            Vector3 blWorld = blCell.worldPosition;
            Vector3 brWorld = brCell.worldPosition;
            Vector3 tlWorld = tlCell.worldPosition;
            
            // Get flow directions for each surrounding cell
            Vector2 blDir = GetCellFlowDirection(bl);
            Vector2 brDir = GetCellFlowDirection(br);
            Vector2 tlDir = GetCellFlowDirection(tl);
            Vector2 trDir = GetCellFlowDirection(tr);
            
            // Calculate the factors for interpolation
            float cellWidth = Vector3.Distance(blWorld, brWorld);
            float cellHeight = Vector3.Distance(blWorld, tlWorld);
            
            if (cellWidth <= 0 || cellHeight <= 0)
                return GetCellFlowDirection(gridPos);
            
            float fracX = Mathf.Clamp01((worldPosition.x - blWorld.x) / cellWidth);
            float fracY = Mathf.Clamp01((worldPosition.y - blWorld.z) / cellHeight);
            
            // Handle cases where flow directions are zero
            if (blDir == Vector2.zero) blDir = GetFirstValidDirection(brDir, tlDir, trDir);
            if (brDir == Vector2.zero) brDir = GetFirstValidDirection(blDir, trDir, tlDir);
            if (tlDir == Vector2.zero) tlDir = GetFirstValidDirection(blDir, trDir, brDir);
            if (trDir == Vector2.zero) trDir = GetFirstValidDirection(tlDir, brDir, blDir);
            
            // Check if all directions are still invalid
            if (blDir == Vector2.zero && brDir == Vector2.zero && tlDir == Vector2.zero && trDir == Vector2.zero)
                return Vector2.zero;
            
            // Apply bilinear interpolation
            Vector2 bottom = Vector2.Lerp(blDir, brDir, fracX);
            Vector2 top = Vector2.Lerp(tlDir, trDir, fracX);
            Vector2 interpolated = Vector2.Lerp(bottom, top, fracY);
            
            // If interpolation strength < 1, blend with the current cell's exact direction
            if (interpolationStrength < 1.0f)
            {
                Vector2 directDirection = GetCellFlowDirection(gridPos);
                if (directDirection != Vector2.zero)
                {
                    interpolated = Vector2.Lerp(directDirection, interpolated, interpolationStrength);
                }
            }
            
            // Normalize the result
            if (interpolated != Vector2.zero)
                interpolated.Normalize();
            
            return interpolated;
        }
        
        private Vector2 GetFirstValidDirection(params Vector2[] directions)
        {
            foreach (var dir in directions)
            {
                if (dir != Vector2.zero)
                    return dir;
            }
            return Vector2.zero;
        }
        
        private Vector2 GetCellFlowDirection(Vector2Int coords)
        {
            var gridController = flowFieldManager.GridController;
            GridDataGenerator dataGenerator = gridController.GetComponent<GridDataGenerator>();
            if (dataGenerator == null)
                return Vector2.zero;
                
            GridCell cell = dataGenerator.GetCell(coords.x, coords.y);
            if (cell == null || cell.flowDirection == Vector2.zero || cell.integrationCost == int.MaxValue)
                return Vector2.zero;
                
            return cell.flowDirection;
        }
        
        // Public method to enable/disable movement
        public void SetMoving(bool moving)
        {
            isMoving = moving;
        }
        
        // For debugging - visualize current flow direction
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || flowFieldManager == null || !enabled || !showGizmos)
                return;
                
            Vector3 position = transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(position, 0.3f);
            
            // Draw the flow direction the agent is following
            Vector2Int gridCoords = flowFieldManager.GridController.WorldToGridCoords(position);
            var flowStrengthMap = flowFieldManager.GetFlowStrengthMap();
            
            if (flowStrengthMap != null && flowStrengthMap.TryGetValue(gridCoords, out float flowStrength))
            {
                if (flowStrength >= maxPriorityPathValue)
                {
                    // Strong path - red
                    Gizmos.color = Color.red;
                }
                else if (flowStrength > priorityPathThreshold)
                {
                    // Medium path - orange
                    Gizmos.color = Color.Lerp(Color.yellow, Color.red, 
                                           (flowStrength - priorityPathThreshold) / 
                                           (maxPriorityPathValue - priorityPathThreshold));
                }
                else
                {
                    // Normal path - cyan
                    Gizmos.color = Color.cyan;
                }
            }
            else
            {
                Gizmos.color = Color.cyan;
            }
            
            // Draw flow direction
            Vector2 worldPos2D = new Vector2(position.x, position.z);
            Vector2 flowDir = GetInterpolatedDirection(worldPos2D, 1.0f);
            if (flowDir != Vector2.zero)
            {
                Vector3 dirVector = new Vector3(flowDir.x, 0, flowDir.y);
                Gizmos.DrawLine(position, position + dirVector * 1.5f);
                Gizmos.DrawSphere(position + dirVector * 1.5f, 0.1f);
            }
        }
        
        // Public method to toggle gizmo visibility
        public void SetGizmosVisible(bool visible)
        {
            showGizmos = visible;
        }
    }
}

