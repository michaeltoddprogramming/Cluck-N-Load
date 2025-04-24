//BEST VERSION SO FAR
using System.Collections.Generic;
using UnityEngine;

public class FlowFieldGenerator : MonoBehaviour
{
    [Header("Grid References")]
    public GridController gridController;
    public GridDataGenerator gridDataGenerator;

    [Header("Target Settings")]
    public Transform targetTransform; // Target game object for the flow field
    public Vector2Int manualTargetCoord; // Manual target coordinates
    public bool useManualTarget = false; // Toggle between transform and manual target

    [Header("Flow Field Settings")]
    [Tooltip("Whether to use diagonal directions for movement")]
    public bool useDiagonalDirections = true;
    [Tooltip("Amount of randomness to add to flow directions (0 = none, 1 = max")]
    [Range(0f, 1f)]
    public float directionRandomness = 0.1f;
    [Tooltip("Maximum angle for random direction variation in degrees")]
    [Range(0f, 180f)]
    public float maxRandomAngle = 45f; // Increased from the default 30 degrees
    [Tooltip("Whether to use weighted diagonal paths (1.4x cost)")]
    public bool useWeightedDiagonals = true;

    [Header("Enhanced Flow Settings")]
    [Tooltip("Influence of direct path to target (0 = none, 1 = maximum)")]
    [Range(0f, 1f)]
    public float directTargetInfluence = 0.4f;
    [Tooltip("Strength of priority paths near obstacles")]
    [Range(0f, 1f)]
    public float obstaclePriorityStrength = 0.7f;
    [Tooltip("How many cells away from obstacles are affected by priority paths")]
    [Range(1, 5)]
    public int priorityPathRange = 2;
    [Tooltip("Color for priority paths (will blend toward this color near obstacles)")]
    public Color priorityPathColor = Color.white;

    [Header("Stream Influence Settings")]
    [Tooltip("How strongly other cells are influenced by priority streams")]
    [Range(0f, 1f)]
    public float streamInfluenceStrength = 0.5f;
    [Tooltip("Maximum distance for stream influence")]
    [Range(1, 10)]
    public int streamInfluenceRange = 4;

    [Header("Debug Visualization")]
    public bool visualizeFlowField = false;
    [Tooltip("Whether to draw the target center point")]
    public bool visualizeTargetPoint = true;
    public float arrowScale = 0.5f;
    public Color arrowColor = Color.blue;
    [Tooltip("Color for the target center point")]
    public Color targetPointColor = Color.red;

    private Vector2Int currentTargetCoord;
    private bool initialized = false;
    private bool manualTargetChanged = false;
    private System.Random random;
    private Dictionary<Vector2Int, float> flowStrengthMap = new Dictionary<Vector2Int, float>();
    private Dictionary<Vector2Int, float> streamInfluenceMap = new Dictionary<Vector2Int, float>();

    private void Start()
    {
        // Initialize random number generator with a seed based on time
        random = new System.Random(System.DateTime.Now.Millisecond);
        
        // Find references if not assigned
        if (gridController == null)
            gridController = FindObjectOfType<GridController>();
            
        if (gridDataGenerator == null && gridController != null)
            gridDataGenerator = FindObjectOfType<GridDataGenerator>();

        if (gridController == null || gridDataGenerator == null)
        {
            Debug.LogError("Grid references not found. Flow field cannot function.");
            return;
        }

        initialized = true;
        
        // Initial flow field generation
        Invoke("GenerateInitialFlowField", 0.5f);
    }

    private void GenerateInitialFlowField()
    {
        if (!initialized) return;
        
        Vector2Int target = GetTargetCoordinates();
        if (IsValidTarget(target))
        {
            currentTargetCoord = target;
            GenerateFlowField(target);
            Debug.Log($"Generated initial flow field to target: {target}");
        }
        else
        {
            Debug.LogWarning("Could not generate initial flow field: invalid target");
        }
    }

    private void Update()
    {
        if (!initialized) return;

        // Handle manual target changes
        if (manualTargetChanged)
        {
            manualTargetChanged = false;
            if (useManualTarget && IsValidTarget(manualTargetCoord))
            {
                currentTargetCoord = manualTargetCoord;
                GenerateFlowField(currentTargetCoord);
                Debug.Log($"Flow field updated for manual target: {manualTargetCoord}");
            }
        }

        // Update flow field if target has moved or changed
        Vector2Int newTargetCoord = GetTargetCoordinates();
        if (newTargetCoord != currentTargetCoord && IsValidTarget(newTargetCoord))
        {
            currentTargetCoord = newTargetCoord;
            GenerateFlowField(currentTargetCoord);
        }
    }

    // Get target coordinates based on settings
    public Vector2Int GetTargetCoordinates()
    {
        if (useManualTarget)
            return manualTargetCoord;
            
        if (targetTransform != null)
            return gridController.WorldToGridCoords(targetTransform.position);
            
        return Vector2Int.zero;
    }

    // Check if target is valid
    private bool IsValidTarget(Vector2Int coord)
    {
        return gridController.IsValidCell(coord.x, coord.y);
    }

    // Generate the flow field with the specified goal
    public void GenerateFlowField(Vector2Int goal)
    {
        if (!initialized) return;
        
        int gridWidth = gridDataGenerator.GetGridWidth();
        int gridHeight = gridDataGenerator.GetGridHeight();
        
        // Skip if invalid goal
        if (!gridController.IsValidCell(goal.x, goal.y))
        {
            Debug.LogWarning($"Invalid flow field goal: {goal}");
            return;
        }

        // Clear flow strength map and stream influence map
        flowStrengthMap.Clear();
        streamInfluenceMap.Clear();

        // 1. Reset all cells
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GridCell cell = gridDataGenerator.GetCell(x, y);
                if (cell != null)
                {
                    cell.integrationCost = int.MaxValue;
                    cell.flowDirection = Vector2.zero;
                    flowStrengthMap[new Vector2Int(x, y)] = 0f;
                }
            }
        }

        // 2. Initialize queue
        Queue<GridCell> queue = new Queue<GridCell>();
        GridCell targetCell = gridDataGenerator.GetCell(goal.x, goal.y);
        targetCell.integrationCost = 0;
        queue.Enqueue(targetCell);

        // 3. Dijkstra propagation with obstacle avoidance
        while (queue.Count > 0)
        {
            GridCell current = queue.Dequeue();

            foreach (var neighborInfo in GetNeighborsWithCost(current))
            {
                GridCell neighbor = neighborInfo.cell;
                float moveCost = neighborInfo.cost;
                
                // Check for obstacles AND occupied cells
                if (neighbor != null && !neighbor.flags.isObstacle && !neighbor.flags.isOccupied)
                {
                    int newCost = current.integrationCost + Mathf.RoundToInt(moveCost);
                    if (newCost < neighbor.integrationCost)
                    {
                        neighbor.integrationCost = newCost;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        // 4. Identify cells near obstacles for priority paths
        HashSet<Vector2Int> obstacleAdjacentCells = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, float> obstaclePriorities = new Dictionary<Vector2Int, float>();
        
        // First find all obstacles
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GridCell cell = gridDataGenerator.GetCell(x, y);
                if (cell == null) continue;
                
                if (cell.flags.isObstacle || cell.flags.isOccupied)
                {
                    // For each obstacle cell, mark cells in range with priority values
                    for (int dx = -priorityPathRange; dx <= priorityPathRange; dx++)
                    {
                        for (int dy = -priorityPathRange; dy <= priorityPathRange; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;
                            
                            // Skip out of bounds or the obstacle itself
                            if (!gridController.IsValidCell(nx, ny) || (dx == 0 && dy == 0))
                                continue;
                                
                            GridCell nearbyCell = gridDataGenerator.GetCell(nx, ny);
                            if (nearbyCell != null && !nearbyCell.flags.isObstacle && !nearbyCell.flags.isOccupied)
                            {
                                Vector2Int key = new Vector2Int(nx, ny);
                                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                                
                                // Priority decreases with distance from obstacle
                                float priority = 1f - Mathf.Clamp01(distance / priorityPathRange);
                                
                                // Track the highest priority for this cell (in case it's near multiple obstacles)
                                if (!obstaclePriorities.ContainsKey(key) || priority > obstaclePriorities[key])
                                {
                                    obstaclePriorities[key] = priority;
                                }
                            }
                        }
                    }
                }
            }
        }

        // 5. Compute enhanced flow directions with direct target influence and obstacle priority
        HashSet<Vector2Int> priorityStreamCells = new HashSet<Vector2Int>();
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GridCell cell = gridDataGenerator.GetCell(x, y);
                if (cell == null || cell.integrationCost == int.MaxValue) 
                    continue;
                
                Vector2Int cellPos = new Vector2Int(x, y);
                
                // Get base flow direction from integration field
                Vector2 baseDirection = GetBaseFlowDirection(cell);
                
                // Calculate direct path to target
                Vector2 directPath = new Vector2(goal.x - x, goal.y - y);
                if (directPath.magnitude > 0) directPath.Normalize();
                
                // Apply priority path strength if near obstacle
                float priorityFactor = 0f;
                Vector2 finalDirection;
                
                if (obstaclePriorities.TryGetValue(cellPos, out float priority))
                {
                    priorityFactor = priority * obstaclePriorityStrength;
                    
                    // Store the flow strength for visualization
                    flowStrengthMap[cellPos] = priorityFactor;
                    
                    // CRITICAL CHANGE: For high priority paths (near obstacles), 
                    // use pure pathfinding with no interpolation at all
                    if (priorityFactor > 0.5f)
                    {
                        // Pure pathfinding direction - no interpolation
                        finalDirection = baseDirection;
                        
                        // Mark this as a priority stream cell for the second pass
                        priorityStreamCells.Add(cellPos);
                    }
                    else
                    {
                        // For cells further from obstacles, gradually blend based on distance
                        // Normalize the priority factor to the 0-0.5 range to create a smooth transition
                        float blendFactor = directTargetInfluence * (1.0f - (priorityFactor / 0.5f));
                        finalDirection = Vector2.Lerp(baseDirection, directPath, blendFactor);
                    }
                }
                else
                {
                    // Normal behavior for cells not near obstacles
                    finalDirection = Vector2.Lerp(baseDirection, directPath, directTargetInfluence);
                }
                
                // Ensure the direction is normalized
                if (finalDirection.magnitude > 0)
                {
                    finalDirection.Normalize();
                    
                    // Apply randomness if enabled
                    if (directionRandomness > 0)
                    {
                        // Reduce randomness for priority paths
                        float adjustedRandomness = directionRandomness * (1f - priorityFactor);
                        cell.flowDirection = AddRandomnessToDirection(finalDirection, adjustedRandomness);
                    }
                    else
                    {
                        cell.flowDirection = finalDirection;
                    }
                }
            }
        }
        
        // 6. NEW PASS: Interpolate regular cells toward priority streams
        if (priorityStreamCells.Count > 0)
        {
            // First calculate influence factors for all cells based on proximity to streams
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector2Int cellPos = new Vector2Int(x, y);
                    
                    // Skip cells that are already priority streams
                    if (priorityStreamCells.Contains(cellPos))
                        continue;
                    
                    GridCell cell = gridDataGenerator.GetCell(x, y);
                    if (cell == null || cell.integrationCost == int.MaxValue) 
                        continue;
                    
                    // Find the nearest priority stream and calculate influence
                    float minDistance = float.MaxValue;
                    Vector2Int nearestStream = Vector2Int.zero;
                    bool foundStream = false;
                    
                    foreach (var streamPos in priorityStreamCells)
                    {
                        float distance = Vector2.Distance(cellPos, streamPos);
                        if (distance < minDistance && distance <= streamInfluenceRange)
                        {
                            minDistance = distance;
                            nearestStream = streamPos;
                            foundStream = true;
                        }
                    }
                    
                    if (foundStream)
                    {
                        // Calculate influence factor based on distance
                        float influenceFactor = 1.0f - Mathf.Clamp01(minDistance / streamInfluenceRange);
                        influenceFactor *= streamInfluenceStrength;
                        
                        // Store influence for visualization and flow adjustment
                        streamInfluenceMap[cellPos] = influenceFactor;
                        
                        // Get the direction from the nearest priority stream
                        GridCell streamCell = gridDataGenerator.GetCell(nearestStream.x, nearestStream.y);
                        if (streamCell != null && streamCell.flowDirection != Vector2.zero)
                        {
                            // Blend the cell's current direction with the priority stream direction
                            Vector2 interpolatedDirection = Vector2.Lerp(
                                cell.flowDirection,
                                streamCell.flowDirection,
                                influenceFactor
                            );
                            
                            // Update the cell's flow direction
                            if (interpolatedDirection.magnitude > 0)
                            {
                                interpolatedDirection.Normalize();
                                cell.flowDirection = interpolatedDirection;
                            }
                        }
                    }
                }
            }
        }
        
        Debug.Log($"Enhanced flow field generated with target: {goal} and {priorityStreamCells.Count} priority streams");
    }

    // New method to extract base flow direction from integration field
    private Vector2 GetBaseFlowDirection(GridCell cell)
    {
        GridCell lowest = cell;
        
        foreach (var neighborInfo in GetNeighborsWithCost(cell))
        {
            GridCell neighbor = neighborInfo.cell;
            if (neighbor != null && neighbor.integrationCost < lowest.integrationCost)
                lowest = neighbor;
        }
        
        if (lowest != cell)
        {
            Vector2 direction = new Vector2(lowest.x - cell.x, lowest.y - cell.y);
            if (direction.magnitude > 0) direction.Normalize();
            return direction;
        }
        
        return Vector2.zero;
    }

    // Update AddRandomnessToDirection to accept a customized randomness value
    private Vector2 AddRandomnessToDirection(Vector2 baseDirection, float randomnessFactor)
    {
        // No randomness for zero direction
        if (baseDirection == Vector2.zero)
            return Vector2.zero;
        
        // Calculate a random angle based on randomness factor
        float randomAngle = ((float)random.NextDouble() * 2f - 1f) * maxRandomAngle * randomnessFactor;
        
        // Rotate the vector by the random angle
        float cos = Mathf.Cos(randomAngle * Mathf.Deg2Rad);
        float sin = Mathf.Sin(randomAngle * Mathf.Deg2Rad);
        Vector2 rotated = new Vector2(
            baseDirection.x * cos - baseDirection.y * sin,
            baseDirection.x * sin + baseDirection.y * cos
        );
        
        // Normalize to maintain consistent magnitude
        rotated.Normalize();
        
        return rotated;
    }

    // Keep the existing AddRandomnessToDirection overload for backward compatibility
    private Vector2 AddRandomnessToDirection(Vector2 baseDirection)
    {
        return AddRandomnessToDirection(baseDirection, directionRandomness);
    }

    // New struct to represent a neighbor cell and its movement cost
    private struct NeighborInfo
    {
        public GridCell cell;
        public float cost;
        
        public NeighborInfo(GridCell cell, float cost)
        {
            this.cell = cell;
            this.cost = cost;
        }
    }

    // Get neighboring cells for a given cell with cost info
    private List<NeighborInfo> GetNeighborsWithCost(GridCell cell)
    {
        List<NeighborInfo> neighbors = new List<NeighborInfo>();

        // Basic cardinal directions
        Vector2Int[] cardinalDirections = new Vector2Int[]
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        // Diagonal directions
        Vector2Int[] diagonalDirections = new Vector2Int[]
        {
            new Vector2Int(1, 1), new Vector2Int(1, -1), 
            new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        // Add cardinal neighbors
        foreach (var dir in cardinalDirections)
        {
            int nx = cell.x + dir.x;
            int ny = cell.y + dir.y;

            if (gridController.IsValidCell(nx, ny))
                neighbors.Add(new NeighborInfo(gridDataGenerator.GetCell(nx, ny), 1f));
        }

        // Add diagonal neighbors if enabled
        if (useDiagonalDirections)
        {
            foreach (var dir in diagonalDirections)
            {
                int nx = cell.x + dir.x;
                int ny = cell.y + dir.y;

                if (gridController.IsValidCell(nx, ny))
                {
                    // Diagonal movement cost can be weighted (typically √2 ≈ 1.414)
                    float cost = useWeightedDiagonals ? 1.4f : 1f;
                    neighbors.Add(new NeighborInfo(gridDataGenerator.GetCell(nx, ny), cost));
                }
            }
        }

        return neighbors;
    }

    // Manual trigger for flow field generation
    [ContextMenu("Generate Flow Field")]
    public void GenerateFlowFieldManually()
    {
        Vector2Int target = useManualTarget ? manualTargetCoord : GetTargetCoordinates();
        if (IsValidTarget(target))
        {
            currentTargetCoord = target;
            GenerateFlowField(target);
            Debug.Log($"Manually triggered flow field generation to {target}");
        }
        else
        {
            Debug.LogWarning("Invalid target for flow field generation");
        }
    }

    // Called when inspector values change
    private void OnValidate()
    {
        // Trigger an update when manual target is changed in inspector
        if (Application.isPlaying && initialized)
        {
            manualTargetChanged = true;
        }
    }

    // Improved visualization of the flow field
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || gridDataGenerator == null || !gridDataGenerator.IsInitialized)
            return;

        int gridWidth = gridDataGenerator.GetGridWidth();
        int gridHeight = gridDataGenerator.GetGridHeight();

        // Draw current target position if enabled
        if (visualizeTargetPoint)
        {
            Vector2Int target = GetTargetCoordinates();
            if (IsValidTarget(target))
            {
                GridCell targetCell = gridDataGenerator.GetCell(target.x, target.y);
                if (targetCell != null)
                {
                    Gizmos.color = targetPointColor;
                    Gizmos.DrawSphere(targetCell.worldPosition, arrowScale * 0.7f);
                    Gizmos.DrawWireCube(targetCell.worldPosition, new Vector3(1.2f, 0.1f, 1.2f));
                }
            }
        }

        // Only draw flow directions if flow field visualization is enabled
        if (!visualizeFlowField)
            return;

        // Visualization with priority path coloring
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GridCell cell = gridDataGenerator.GetCell(x, y);
                if (cell == null) continue;
                
                if (cell.flowDirection != Vector2.zero && cell.integrationCost != int.MaxValue)
                {
                    Vector3 start = cell.worldPosition;
                    
                    // Now the direction is already a continuous Vector2
                    Vector3 direction = new Vector3(cell.flowDirection.x, 0, cell.flowDirection.y);
                    
                    // Adjust arrow length based on flow strength for priority paths
                    float strength = 1.0f;
                    Vector2Int cellPos = new Vector2Int(x, y);
                    if (flowStrengthMap.TryGetValue(cellPos, out float flowStrength))
                    {
                        // Make priority paths slightly longer
                        strength = 1.0f + flowStrength * 0.5f;
                    }
                    
                    Vector3 end = start + direction * arrowScale * strength;
                    
                    // Base color
                    Color arrowColoring = arrowColor;
                    
                    // Calculate angle-based color if using randomness
                    float angle = Mathf.Atan2(cell.flowDirection.y, cell.flowDirection.x) * Mathf.Rad2Deg;
                    angle = (angle + 360) % 360;
                    
                    if (directionRandomness > 0.0f)
                    {
                        // Create a gradient of colors based on the angle
                        float hue = angle / 360f;
                        arrowColoring = Color.HSVToRGB(hue, 0.7f, 0.8f);
                    }
                    
                    // First check if it's a priority path
                    if (flowStrengthMap.TryGetValue(cellPos, out flowStrength) && flowStrength > 0)
                    {
                        // Make priority paths whiter based on their strength
                        arrowColoring = Color.Lerp(arrowColoring, priorityPathColor, flowStrength);
                    }
                    // Then check if it's influenced by a stream
                    else if (streamInfluenceMap.TryGetValue(cellPos, out float influenceFactor) && influenceFactor > 0)
                    {
                        // Make influenced cells blend toward white based on influence factor
                        arrowColoring = Color.Lerp(arrowColoring, priorityPathColor, influenceFactor * 0.8f);
                    }
                    
                    Gizmos.color = arrowColoring;
                    Gizmos.DrawLine(start, end);
                    
                    // Draw arrow head
                    Vector3 right = Quaternion.Euler(0, 30, 0) * -direction * arrowScale * 0.4f * strength;
                    Vector3 left = Quaternion.Euler(0, -30, 0) * -direction * arrowScale * 0.4f * strength;
                    Gizmos.DrawLine(end, end + right);
                    Gizmos.DrawLine(end, end + left);
                    
                    // Draw small circle at base for priority paths
                    if (flowStrengthMap.TryGetValue(cellPos, out flowStrength) && flowStrength > 0.3f)
                    {
                        float circleSize = 0.1f + flowStrength * 0.1f;
                        Gizmos.DrawWireSphere(start, circleSize);
                    }
                }
            }
        }
    }

    // Public methods to control the manual target
    public void SetManualTarget(Vector2Int newTarget)
    {
        if (IsValidTarget(newTarget))
        {
            manualTargetCoord = newTarget;
            manualTargetChanged = true;
            useManualTarget = true;
        }
    }

    public void SetManualTarget(int x, int y)
    {
        SetManualTarget(new Vector2Int(x, y));
    }

    // Toggle between manual and transform target
    public void ToggleManualTarget(bool useManual)
    {
        useManualTarget = useManual;
        manualTargetChanged = true;
    }

    [ContextMenu("Print Obstacle Stats")]
    public void PrintObstacleStats()
    {
        if (!initialized) return;
        
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

    // Add these public methods to access the flow strength maps
    public Dictionary<Vector2Int, float> GetFlowStrengthMap()
    {
        return flowStrengthMap;
    }

    public Dictionary<Vector2Int, float> GetStreamInfluenceMap()
    {
        return streamInfluenceMap;
    }
}
