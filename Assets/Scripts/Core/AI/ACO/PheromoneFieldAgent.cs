using UnityEngine;
using System.Collections.Generic;

namespace FarmDefender.Core.AI
{
    /// <summary>
    /// Agent that follows pheromone trails with flow field as a fallback.
    /// Uses cone vision for pheromone detection in front-left, front, and front-right cells.
    /// Never moves onto obstacle or occupied cells.
    /// </summary>
    public class PheromoneFieldAgent : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float stoppingDistance = 0.1f;
        
        [Header("Pheromone Following")]
        [Tooltip("Whether to follow pheromones when detected")]
        [SerializeField] private bool followPheromones = true;
        [Tooltip("Minimum value of pheromone to be considered valid")]
        [SerializeField] private float minPheromoneThreshold = 0.1f;
        [Tooltip("Which pheromone type this agent responds to (0=Regular, 1=Fast, 2=Strong)")]
        [SerializeField] private int pheromoneTypeIndex = 0;
        [Tooltip("How strongly the agent follows pheromones vs other factors")]
        [Range(0.5f, 5.0f)]
        [SerializeField] private float pheromoneWeight = 2.0f;
        
        [Header("Flow Field Settings")]
        [Tooltip("How much flow field influences movement when following pheromones (lower = more pheromone focus)")]
        [Range(0.0f, 1.0f)]
        [SerializeField] private float flowFieldDesirabilityWithPheromones = 0.2f;
        [Tooltip("Enable interpolation for smoother flow field following")]
        [SerializeField] private bool useFlowFieldInterpolation = true;
        
        [Header("Vision Settings")]
        [Tooltip("Width of the cone vision in degrees")]
        [Range(30f, 180f)]
        [SerializeField] private float coneVisionAngle = 120f;
        [Tooltip("How far the agent can detect pheromones (in grid cells)")]
        [Range(1, 3)]
        [SerializeField] private int visionRange = 1;
        
        [Header("Movement Smoothing")]
        [SerializeField] private float velocitySmoothTime = 0.5f;
        [SerializeField] private float minMoveThreshold = 0.05f;
        [SerializeField] private float directionPersistence = 0.2f;
        
        [Header("Obstacle Avoidance")]
        [Tooltip("How strongly the agent should avoid getting near obstacles")]
        [Range(1.0f, 5.0f)]
        [SerializeField] private float obstacleAvoidanceWeight = 3.0f;
        [Tooltip("Maximum attempts to find a valid path when stuck")]
        [SerializeField] private int maxPathFindingAttempts = 5;
        
        [Header("Debug")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private bool showPheromoneDetection = true;
        [SerializeField] private bool showObstacleAvoidance = true;
        
        // References
        private FlowField.FlowFieldManager flowFieldManager;
        private GridController gridController;
        private GridDataGenerator gridDataGenerator;
        
        // Internal state
        private Vector2 previousDirection = Vector2.zero;
        private Vector3 currentVelocity = Vector3.zero;
        private bool isMoving = true;
        private bool isFollowingPheromone = false;
        private int stuckCounter = 0;
        private Vector2Int lastGridPosition;
        
        private void Start()
        {
            // Get required references
            flowFieldManager = FindObjectOfType<FlowField.FlowFieldManager>();
            if (flowFieldManager == null)
            {
                Debug.LogError("PheromoneFieldAgent requires a FlowFieldManager in the scene");
                enabled = false;
                return;
            }
            
            gridController = flowFieldManager.GridController;
            if (gridController != null)
            {
                gridDataGenerator = gridController.GetComponent<GridDataGenerator>();
            }
            
            if (gridController == null || gridDataGenerator == null)
            {
                Debug.LogError("PheromoneFieldAgent requires GridController and GridDataGenerator");
                enabled = false;
                return;
            }
            
            // Initialize last position
            lastGridPosition = gridController.WorldToGridCoords(transform.position);
        }
        
        private void Update()
        {
            if (!isMoving)
                return;
                
            // EMERGENCY CHECK: First, ensure we're not inside an obstacle
            EmergencyObstacleRecovery();
            
            // First check for pheromones in the vision cone
            Vector2 moveDirection;
            Vector2Int gridCoords = gridController.WorldToGridCoords(transform.position);
            
            // Check if we've moved to a new grid cell
            if (gridCoords != lastGridPosition)
            {
                stuckCounter = 0;
                lastGridPosition = gridCoords;
            }
            
            if (followPheromones && DetectPheromonesInCone(gridCoords, out moveDirection))
            {
                // Follow pheromones with minimal flow field influence
                FollowDirection(moveDirection, true);
                isFollowingPheromone = true;
            }
            else
            {
                // Fall back to flow field when no pheromones detected
                FollowFlowField();
                isFollowingPheromone = false;
            }
        }
        
        private bool DetectPheromonesInCone(Vector2Int currentCell, out Vector2 preferredDirection)
        {
            preferredDirection = Vector2.zero;
            
            // Get agent's forward direction in grid space
            Vector3 worldForward = transform.forward;
            Vector2 agentDirection = new Vector2(worldForward.x, worldForward.z).normalized;
            
            // For tracking the best cell and its pheromone value
            float highestPheromoneValue = minPheromoneThreshold;
            Vector2Int bestCell = currentCell;
            bool foundPheromone = false;
            
            // Check cells in vision range
            for (int distanceStep = 1; distanceStep <= visionRange; distanceStep++)
            {
                // Check cells at increasing distances
                for (int x = -distanceStep; x <= distanceStep; x++)
                {
                    for (int y = -distanceStep; y <= distanceStep; y++)
                    {
                        // Skip center cell and cells not at the current distance step
                        if ((x == 0 && y == 0) || Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != distanceStep)
                            continue;
                        
                        Vector2Int checkPos = currentCell + new Vector2Int(x, y);
                        
                        // Skip invalid cells or obstacles
                        if (!IsValidAndUnblockedCell(checkPos.x, checkPos.y))
                            continue;
                            
                        // Get direction to this cell
                        Vector2 dirToCell = new Vector2(x, y).normalized;
                        
                        // Check if the cell is within our vision cone
                        float angle = Vector2.Angle(agentDirection, dirToCell);
                        if (angle > coneVisionAngle * 0.5f)
                            continue; // Cell is outside our vision cone
                            
                        // Check the cell for pheromones
                        GridCell cell = gridController.GetCell(checkPos.x, checkPos.y);
                        if (cell == null || cell.pheromones == null || cell.pheromones.Length <= pheromoneTypeIndex)
                            continue;
                            
                        float pheromoneValue = cell.pheromones[pheromoneTypeIndex];
                        
                        // If the cell has sufficient pheromones and it's the highest value yet
                        if (pheromoneValue > highestPheromoneValue)
                        {
                            highestPheromoneValue = pheromoneValue;
                            bestCell = checkPos;
                            foundPheromone = true;
                        }
                    }
                }
            }
            
            // If we found a cell with pheromones, set that as our preferred direction
            if (foundPheromone)
            {
                Vector2Int dirVector = bestCell - currentCell;
                preferredDirection = new Vector2(dirVector.x, dirVector.y).normalized;
                return true;
            }
            
            // No suitable pheromones found
            return false;
        }
        
        private void FollowDirection(Vector2 direction, bool isPheromoneDirection)
        {
            if (direction == Vector2.zero)
                return;
                
            // If following pheromones, blend with flow field by a small factor
            if (isPheromoneDirection && flowFieldDesirabilityWithPheromones > 0)
            {
                Vector2Int gridCoords = gridController.WorldToGridCoords(transform.position);
                GridCell cell = gridController.GetCell(gridCoords.x, gridCoords.y);
                if (cell != null && cell.flowDirection != Vector2.zero)
                {
                    // Blend pheromone direction with flow field
                    direction = Vector2.Lerp(direction * pheromoneWeight, cell.flowDirection, flowFieldDesirabilityWithPheromones);
                    direction.Normalize();
                }
            }
            
            // Apply direction persistence for smoother turns
            if (previousDirection != Vector2.zero)
            {
                direction = Vector2.Lerp(previousDirection, direction, 1f - directionPersistence);
                direction.Normalize();
            }
            
            // Apply obstacle avoidance
            direction = ApplyObstacleAvoidance(direction);
            
            previousDirection = direction;
            
            // Convert direction to world space and move
            MoveInDirection(new Vector3(direction.x, 0, direction.y));
        }
        
        private void FollowFlowField()
        {
            // Get current position and grid coordinates
            Vector3 currentPosition = transform.position;
            Vector2Int gridCoords = gridController.WorldToGridCoords(currentPosition);
            
            // Check if we're in a valid grid cell
            if (!gridController.IsValidCell(gridCoords.x, gridCoords.y))
                return;
                
            Vector2 flowDirection;
            
            // Get flow direction (with or without interpolation)
            if (useFlowFieldInterpolation)
            {
                flowDirection = GetInterpolatedFlowDirection(currentPosition);
            }
            else
            {
                GridCell cell = gridController.GetCell(gridCoords.x, gridCoords.y);
                flowDirection = cell != null ? cell.flowDirection : Vector2.zero;
            }
            
            // If we have a valid direction, follow it
            if (flowDirection != Vector2.zero)
            {
                FollowDirection(flowDirection, false);
            }
            else
            {
                // If stuck with no valid flow direction, try to find any valid direction
                AttemptToFindValidPathWhenStuck();
            }
        }
        
        // Modify GetInterpolatedFlowDirection
        private Vector2 GetInterpolatedFlowDirection(Vector3 worldPosition)
        {
            // This uses a simplified version of the interpolation from FlowFieldAgent
            Vector2Int gridPos = gridController.WorldToGridCoords(worldPosition);
            
            // SAFETY CHECK - If current position is in an obstacle, return zero direction
            if (!IsValidAndUnblockedCell(gridPos.x, gridPos.y))
            {
                return Vector2.zero;
            }
            
            // Get grid dimensions to check boundaries
            int gridWidth = gridDataGenerator.GetGridWidth();
            int gridHeight = gridDataGenerator.GetGridHeight();
            
            // Check if position is valid
            if (gridPos.x < 0 || gridPos.x >= gridWidth - 1 || gridPos.y < 0 || gridPos.y >= gridHeight - 1)
            {
                // Near grid edge, fall back to non-interpolated direction
                GridCell cell = gridController.GetCell(gridPos.x, gridPos.y);
                return cell != null ? cell.flowDirection : Vector2.zero;
            }
            
            // Get the positions of the four surrounding grid cells
            Vector2Int bl = gridPos;                                // Bottom Left
            Vector2Int br = new Vector2Int(gridPos.x + 1, gridPos.y);     // Bottom Right
            Vector2Int tl = new Vector2Int(gridPos.x, gridPos.y + 1);     // Top Left
            Vector2Int tr = new Vector2Int(gridPos.x + 1, gridPos.y + 1); // Top Right
            
            // Check if any of these cells are obstacles - if so, use only direct cell
            if (!IsValidAndUnblockedCell(bl.x, bl.y) || 
                !IsValidAndUnblockedCell(br.x, br.y) || 
                !IsValidAndUnblockedCell(tl.x, tl.y) || 
                !IsValidAndUnblockedCell(tr.x, tr.y))
            {
                // Don't interpolate near obstacles - just use the current cell's flow direction
                GridCell cell = gridController.GetCell(gridPos.x, gridPos.y);
                return cell != null ? cell.flowDirection : Vector2.zero;
            }
            
            // Get the cells
            GridCell blCell = gridDataGenerator.GetCell(bl.x, bl.y);
            GridCell brCell = gridDataGenerator.GetCell(br.x, br.y);
            GridCell tlCell = gridDataGenerator.GetCell(tl.x, tl.y);
            GridCell trCell = gridDataGenerator.GetCell(tr.x, tr.y);
            
            // If any cell is missing, return the flow direction of the current cell
            if (blCell == null || brCell == null || tlCell == null || trCell == null)
            {
                GridCell cell = gridController.GetCell(gridPos.x, gridPos.y);
                return cell != null ? cell.flowDirection : Vector2.zero;
            }
            
            // Get world positions and flow directions
            Vector3 blWorld = blCell.worldPosition;
            Vector3 brWorld = brCell.worldPosition;
            Vector3 tlWorld = tlCell.worldPosition;
            
            Vector2 blDir = blCell.flowDirection;
            Vector2 brDir = brCell.flowDirection;
            Vector2 tlDir = tlCell.flowDirection;
            Vector2 trDir = trCell.flowDirection;
            
            // Calculate interpolation factors
            float cellWidth = Vector3.Distance(blWorld, brWorld);
            float cellHeight = Vector3.Distance(blWorld, tlWorld);
            
            if (cellWidth <= 0 || cellHeight <= 0)
            {
                GridCell cell = gridController.GetCell(gridPos.x, gridPos.y);
                return cell != null ? cell.flowDirection : Vector2.zero;
            }
            
            float fracX = Mathf.Clamp01((worldPosition.x - blWorld.x) / cellWidth);
            float fracY = Mathf.Clamp01((worldPosition.z - blWorld.z) / cellHeight);
            
            // Handle zero directions
            if (blDir == Vector2.zero) blDir = GetFirstValidDirection(brDir, tlDir, trDir);
            if (brDir == Vector2.zero) brDir = GetFirstValidDirection(blDir, trDir, tlDir);
            if (tlDir == Vector2.zero) tlDir = GetFirstValidDirection(blDir, trDir, brDir);
            if (trDir == Vector2.zero) trDir = GetFirstValidDirection(tlDir, brDir, blDir);
            
            // If all directions are still zero, return zero
            if (blDir == Vector2.zero && brDir == Vector2.zero && tlDir == Vector2.zero && trDir == Vector2.zero)
                return Vector2.zero;
                
            // Apply bilinear interpolation
            Vector2 bottom = Vector2.Lerp(blDir, brDir, fracX);
            Vector2 top = Vector2.Lerp(tlDir, trDir, fracX);
            Vector2 interpolated = Vector2.Lerp(bottom, top, fracY);
            
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
        
        private Vector2 ApplyObstacleAvoidance(Vector2 direction)
        {
            if (direction == Vector2.zero)
                return Vector2.zero;
                
            Vector2Int currentCell = gridController.WorldToGridCoords(transform.position);
            
            // Check the cell we would move into based on our direction
            Vector2Int targetCellOffset = GetGridOffsetFromDirection(direction);
            Vector2Int targetCell = currentCell + targetCellOffset;
            
            // If the target cell is blocked, we need to find an alternative direction
            if (!IsValidAndUnblockedCell(targetCell.x, targetCell.y))
            {
                // Check all 8 neighboring cells to find valid ones
                List<Vector2> validDirections = new List<Vector2>();
                
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        // Skip current cell
                        if (x == 0 && y == 0)
                            continue;
                            
                        Vector2Int neighborCell = currentCell + new Vector2Int(x, y);
                        
                        // If this neighboring cell is valid and not blocked
                        if (IsValidAndUnblockedCell(neighborCell.x, neighborCell.y))
                        {
                            // Calculate a score for this direction
                            // Higher score means closer to our desired direction
                            Vector2 neighborDirection = new Vector2(x, y).normalized;
                            float similarity = Vector2.Dot(direction, neighborDirection);
                            
                            // Higher weight means this direction is better
                            float weight = similarity + 1.0f; // Range 0-2, higher is better
                            
                            // Add this direction to our list of possibilities
                            for (int i = 0; i < Mathf.CeilToInt(weight * 3); i++) // Add multiple copies based on weight
                            {
                                validDirections.Add(neighborDirection);
                            }
                        }
                    }
                }
                
                // If we found valid directions, choose one weighted by similarity to original direction
                if (validDirections.Count > 0)
                {
                    // Pick a random valid direction, weighted by the times we added it
                    direction = validDirections[Random.Range(0, validDirections.Count)];
                    
                    // Count this as being stuck if we had to change direction
                    stuckCounter++;
                }
                else
                {
                    // No valid directions - we're truly stuck
                    // Just return zero vector to stop moving
                    direction = Vector2.zero;
                    stuckCounter += 2; // Count as being more stuck
                }
            }
            else
            {
                // We're not stuck since the target cell is valid
                stuckCounter = Mathf.Max(0, stuckCounter - 1);
            }
            
            return direction;
        }
        
        private Vector2Int GetGridOffsetFromDirection(Vector2 direction)
        {
            // Convert a continuous direction to grid cell offset
            int x = Mathf.RoundToInt(direction.x);
            int y = Mathf.RoundToInt(direction.y);
            
            // Ensure we don't get (0,0) for small but non-zero directions
            if (x == 0 && y == 0 && direction.sqrMagnitude > 0.01f)
            {
                x = direction.x > 0 ? 1 : (direction.x < 0 ? -1 : 0);
                y = direction.y > 0 ? 1 : (direction.y < 0 ? -1 : 0);
            }
            
            return new Vector2Int(x, y);
        }
        
        private bool IsValidAndUnblockedCell(int x, int y)
        {
            if (!gridController.IsValidCell(x, y))
                return false;
                
            GridCell cell = gridController.GetCell(x, y);
            return cell != null && !cell.flags.isObstacle && !cell.flags.isOccupied;
        }
        
        private void AttemptToFindValidPathWhenStuck()
        {
            if (stuckCounter > maxPathFindingAttempts)
            {
                // We're very stuck - just stop moving for this frame
                return;
            }
            
            // If we're here, we have no flow direction and need to try something
            Vector2Int currentCell = gridController.WorldToGridCoords(transform.position);
            List<Vector2Int> validNeighbors = new List<Vector2Int>();
            
            // Check all 8 neighboring cells
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    // Skip current cell
                    if (x == 0 && y == 0)
                        continue;
                        
                    Vector2Int neighborCell = currentCell + new Vector2Int(x, y);
                    
                    // If neighbor is valid and not blocked
                    if (IsValidAndUnblockedCell(neighborCell.x, neighborCell.y))
                    {
                        validNeighbors.Add(neighborCell);
                    }
                }
            }
            
            // If we have valid neighbors, move toward one of them
            if (validNeighbors.Count > 0)
            {
                // Pick a random valid neighbor
                Vector2Int targetCell = validNeighbors[Random.Range(0, validNeighbors.Count)];
                Vector2 direction = new Vector2(targetCell.x - currentCell.x, targetCell.y - currentCell.y).normalized;
                
                // Move in that direction
                MoveInDirection(new Vector3(direction.x, 0, direction.y));
                stuckCounter++;
            }
        }
        
        private void MoveInDirection(Vector3 direction)
        {
            if (direction == Vector3.zero)
                return;
                
            // Calculate target velocity
            Vector3 targetVelocity = direction.normalized * moveSpeed;
            
            // Add repulsion from nearby obstacles
            targetVelocity = ApplyObstacleRepulsion(targetVelocity);
            
            // Apply velocity smoothing
            Vector3 smoothedVelocity = Vector3.SmoothDamp(
                currentVelocity,
                targetVelocity,
                ref currentVelocity,
                velocitySmoothTime
            );
            
            // Apply minimum threshold
            if (smoothedVelocity.magnitude < minMoveThreshold)
            {
                smoothedVelocity = Vector3.zero;
                return;
            }
            
            // Create the proposed new position
            Vector3 newPosition = transform.position + smoothedVelocity * Time.deltaTime;
            
            // ENHANCED VERIFICATION: Check intermediate positions too
            // This prevents "tunneling" through obstacles due to frame rate or speed
            Vector3 moveVector = newPosition - transform.position;
            float moveDistance = moveVector.magnitude;
            Vector3 moveDir = moveVector.normalized;
            
            // Improved step size calculation based on grid cell size to prevent tunneling
            float cellSize = 1.0f; // Adjust based on your grid size
            float stepSize = Mathf.Min(cellSize * 0.4f, moveDistance * 0.5f);
            bool pathClear = true;
            
            // Only do intermediate checks for longer movements
            if (moveDistance > cellSize * 0.5f)
            {
                for (float dist = stepSize; dist < moveDistance - stepSize; dist += stepSize)
                {
                    Vector3 checkPos = transform.position + moveDir * dist;
                    Vector2Int checkGridPos = gridController.WorldToGridCoords(checkPos);
                    
                    if (!IsValidAndUnblockedCell(checkGridPos.x, checkGridPos.y))
                    {
                        pathClear = false;
                        break;
                    }
                }
            }
            
            // Final destination check
            Vector2Int newGridPos = gridController.WorldToGridCoords(newPosition);
            bool destinationValid = IsValidAndUnblockedCell(newGridPos.x, newGridPos.y);
            
            // Move only if the path and destination are clear
            if (pathClear && destinationValid)
            {
                transform.position = newPosition;
                
                // Apply rotation to face direction of movement
                if (smoothedVelocity.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(smoothedVelocity);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        rotationSpeed * Time.deltaTime
                    );
                }
            }
            else
            {
                // Path or destination is blocked - apply the emergency obstacle avoidance
                currentVelocity = Vector3.zero;
                stuckCounter++;
                
                // If we're getting repeatedly stuck, try the emergency handling
                if (stuckCounter >= 5)
                {
                    EmergencyObstacleRecovery();
                }
            }
        }
        
        // New method to add repulsion forces from obstacles
        private Vector3 ApplyObstacleRepulsion(Vector3 baseVelocity)
        {
            Vector2Int currentCell = gridController.WorldToGridCoords(transform.position);
            Vector2 repulsionForce = Vector2.zero;
            
            // Check all cells in a 3x3 grid around us
            int repulsionRange = 2; // Can be adjusted for stronger avoidance
            
            for (int x = -repulsionRange; x <= repulsionRange; x++)
            {
                for (int y = -repulsionRange; y <= repulsionRange; y++)
                {
                    // Skip our own cell
                    if (x == 0 && y == 0)
                        continue;
                        
                    Vector2Int checkPos = currentCell + new Vector2Int(x, y);
                    
                    // Check if this is an obstacle
                    if (!gridController.IsValidCell(checkPos.x, checkPos.y))
                        continue;
                        
                    GridCell cell = gridController.GetCell(checkPos.x, checkPos.y);
                    if (cell == null)
                        continue;
                        
                    // If this is an obstacle or occupied cell, add repulsion
                    if (cell.flags.isObstacle || cell.flags.isOccupied)
                    {
                        // Calculate distance and direction vector from obstacle
                        Vector3 obstaclePos = cell.worldPosition;
                        Vector3 toObstacle = obstaclePos - transform.position;
                        toObstacle.y = 0; // Ensure we're only considering horizontal plane
                        
                        // Distance to obstacle center
                        float distance = toObstacle.magnitude;
                        
                        // Skip obstacles that are too far
                        float repulsionRadius = 2.0f; // Adjust based on your grid scale
                        if (distance > repulsionRadius)
                            continue;
                            
                        // Direction AWAY from the obstacle
                        Vector2 repulsionDir = new Vector2(-toObstacle.x, -toObstacle.z).normalized;
                        
                        // Strength drops off with distance (stronger when closer)
                        // Power of 2 gives a stronger repulsion as we get very close
                        float repulsionStrength = (1.0f - (distance / repulsionRadius));
                        repulsionStrength = repulsionStrength * repulsionStrength * obstacleAvoidanceWeight;
                        
                        // Add to total repulsion force
                        repulsionForce += repulsionDir * repulsionStrength;
                    }
                }
            }
            
            // Apply repulsion to velocity
            Vector3 adjustedVelocity = baseVelocity;
            
            if (repulsionForce.magnitude > 0)
            {
                // Convert 2D repulsion to 3D and add to velocity
                Vector3 repulsion3D = new Vector3(repulsionForce.x, 0, repulsionForce.y);
                
                // Blend original direction with repulsion direction
                adjustedVelocity = Vector3.Lerp(
                    baseVelocity,
                    repulsion3D.normalized * baseVelocity.magnitude,
                    Mathf.Clamp01(repulsionForce.magnitude * 0.5f)
                );
                
                // If we're very close to an obstacle, give repulsion higher priority
                if (repulsionForce.magnitude > 1.0f)
                {
                    adjustedVelocity = repulsion3D.normalized * baseVelocity.magnitude;
                }
            }
            
            return adjustedVelocity;
        }
        
        // Public API
        
        public void SetMoving(bool moving)
        {
            isMoving = moving;
        }
        
        public void SetPheromoneType(int typeIndex)
        {
            pheromoneTypeIndex = Mathf.Clamp(typeIndex, 0, 2);
        }
        
        public void SetPheromoneFollowing(bool enabled)
        {
            followPheromones = enabled;
        }
        
        public bool IsFollowingPheromone()
        {
            return isFollowingPheromone;
        }
        
        // Debug visualization
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !showGizmos || !enabled)
                return;
                
            Vector3 position = transform.position;
            
            // Draw agent sphere - color based on navigation mode
            Gizmos.color = isFollowingPheromone ? Color.magenta : Color.yellow;
            Gizmos.DrawWireSphere(position, 0.3f);
            
            // Draw direction
            Vector3 forward = transform.forward * 1.5f;
            Gizmos.color = isFollowingPheromone ? Color.red : Color.cyan;
            Gizmos.DrawLine(position, position + forward);
            Gizmos.DrawSphere(position + forward, 0.1f);
            
            if (showObstacleAvoidance)
            {
                // Draw stuckCounter information
                if (stuckCounter > 0)
                {
                    Gizmos.color = Color.red;
                    Vector3 textPos = position + Vector3.up * 1.5f;
                    UnityEditor.Handles.Label(textPos, $"Stuck: {stuckCounter}");
                }
                
                // Draw blocked cells around us
                Vector2Int currentCell = gridController.WorldToGridCoords(position);
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        Vector2Int checkPos = currentCell + new Vector2Int(x, y);
                        if (!IsValidAndUnblockedCell(checkPos.x, checkPos.y))
                        {
                            GridCell cell = gridController.GetCell(checkPos.x, checkPos.y);
                            if (cell != null)
                            {
                                Gizmos.color = Color.red;
                                Gizmos.DrawCube(cell.worldPosition, new Vector3(0.8f, 0.2f, 0.8f));
                            }
                        }
                    }
                }
            }
            
            // Draw vision cone if enabled
            if (showPheromoneDetection && gridController != null)
            {
                // Get current grid position
                Vector2Int gridPos = gridController.WorldToGridCoords(position);
                
                // Draw cone visualization
                Vector3 worldForward = transform.forward;
                Vector2 forwardDir = new Vector2(worldForward.x, worldForward.z).normalized;
                
                // Draw cone lines
                float halfConeAngle = coneVisionAngle * 0.5f * Mathf.Deg2Rad;
                Vector2 leftDir = RotateVector(forwardDir, -halfConeAngle);
                Vector2 rightDir = RotateVector(forwardDir, halfConeAngle);
                
                Vector3 leftLine = new Vector3(leftDir.x, 0, leftDir.y) * visionRange * 2f;
                Vector3 rightLine = new Vector3(rightDir.x, 0, rightDir.y) * visionRange * 2f;
                
                Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.5f);
                Gizmos.DrawLine(position, position + leftLine);
                Gizmos.DrawLine(position, position + rightLine);
                
                // Draw arc for the cone
                int segments = 20;
                Vector3 prevPoint = position + leftLine;
                
                for (int i = 1; i <= segments; i++)
                {
                    float angle = -halfConeAngle + (i * (2 * halfConeAngle) / segments);
                    Vector2 dir = RotateVector(forwardDir, angle);
                    Vector3 point = position + new Vector3(dir.x, 0, dir.y) * visionRange * 2f;
                    Gizmos.DrawLine(prevPoint, point);
                    prevPoint = point;
                }
                
                // Draw cells in vision range
                for (int distanceStep = 1; distanceStep <= visionRange; distanceStep++)
                {
                    for (int x = -distanceStep; x <= distanceStep; x++)
                    {
                        for (int y = -distanceStep; y <= distanceStep; y++)
                        {
                            // Skip cells not at the current distance step
                            if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != distanceStep)
                                continue;
                                
                            Vector2Int checkPos = gridPos + new Vector2Int(x, y);
                            
                            // Skip invalid cells
                            if (!gridController.IsValidCell(checkPos.x, checkPos.y))
                                continue;
                                
                            // Get direction to this cell
                            Vector2 dirToCell = new Vector2(x, y).normalized;
                            
                            // Check if the cell is within our vision cone
                            float dirAngle = Vector2.Angle(forwardDir, dirToCell);
                            bool inCone = dirAngle <= coneVisionAngle * 0.5f;
                            
                            // Get cell position
                            GridCell cell = gridController.GetCell(checkPos.x, checkPos.y);
                            if (cell == null) continue;
                            
                            Vector3 cellPos = cell.worldPosition;
                            
                            // Skip drawing cells that are obstacles
                            if (cell.flags.isObstacle || cell.flags.isOccupied)
                            {
                                Gizmos.color = Color.red;
                                Gizmos.DrawLine(cellPos, cellPos + Vector3.up * 0.5f);
                                continue;
                            }
                            
                            // Draw cell - blue for in cone, gray for outside
                            if (inCone)
                            {
                                // Check for pheromones
                                bool hasPheromones = false;
                                float intensity = 0f;
                                
                                if (cell.pheromones != null && cell.pheromones.Length > pheromoneTypeIndex)
                                {
                                    float value = cell.pheromones[pheromoneTypeIndex];
                                    if (value > minPheromoneThreshold)
                                    {
                                        hasPheromones = true;
                                        intensity = Mathf.Clamp01(value / 5f); // Assuming max around 5
                                    }
                                }
                                
                                if (hasPheromones)
                                {
                                    // Pheromone cell - green to red based on intensity
                                    Gizmos.color = Color.Lerp(Color.green, Color.red, intensity);
                                    Gizmos.DrawCube(cellPos, new Vector3(0.8f, 0.05f, 0.8f));
                                }
                                else
                                {
                                    // Regular cell in cone - blue
                                    Gizmos.color = new Color(0.3f, 0.3f, 1f, 0.3f);
                                    Gizmos.DrawWireCube(cellPos, new Vector3(0.8f, 0.05f, 0.8f));
                                }
                            }
                            else
                            {
                                // Cell outside cone - gray
                                Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.1f);
                                Gizmos.DrawWireCube(cellPos, new Vector3(0.8f, 0.05f, 0.8f));
                            }
                        }
                    }
                }
            }
        }
        
        // Helper method to rotate a vector by an angle (in radians)
        private Vector2 RotateVector(Vector2 v, float angle)
        {
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            
            return new Vector2(
                v.x * cos - v.y * sin,
                v.x * sin + v.y * cos
            );
        }
        
        // Add this new method
        private void EmergencyObstacleRecovery()
        {
            Vector2Int currentCell = gridController.WorldToGridCoords(transform.position);
            
            // If we're in a valid cell, no need for emergency recovery
            if (IsValidAndUnblockedCell(currentCell.x, currentCell.y))
                return;
                
            Debug.LogWarning($"Agent {gameObject.name} is inside obstacle! Performing emergency recovery");
            
            // Find the closest valid cell to escape to
            Vector2Int escapeCell = FindClosestValidCell(currentCell, 5);
            
            if (escapeCell != currentCell)
            {
                // We found a valid cell to escape to - immediately teleport there
                GridCell cell = gridController.GetCell(escapeCell.x, escapeCell.y);
                if (cell != null)
                {
                    transform.position = cell.worldPosition;
                    stuckCounter = 0; // Reset stuck counter
                }
            }
        }
        
        private Vector2Int FindClosestValidCell(Vector2Int fromCell, int maxDistance)
        {
            // Simple BFS or DFS could be implemented here to find the closest valid cell
            // For now, let's do a simple outward spiral search
            for (int distance = 1; distance <= maxDistance; distance++)
            {
                for (int x = -distance; x <= distance; x++)
                {
                    for (int y = -distance; y <= distance; y++)
                    {
                        // Skip the center and invalid cells
                        if ((x == 0 && y == 0) || !gridController.IsValidCell(fromCell.x + x, fromCell.y + y))
                            continue;
                        
                        Vector2Int checkCell = fromCell + new Vector2Int(x, y);
                        
                        if (IsValidAndUnblockedCell(checkCell.x, checkCell.y))
                        {
                            return checkCell;
                        }
                    }
                }
            }
            
            return fromCell; // Fallback to original cell if no valid cell found
        }
    }
}