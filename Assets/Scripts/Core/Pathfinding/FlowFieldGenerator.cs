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
    [Tooltip("Amount of randomness to add to flow directions (0 = none, 1 = max)")]
    [Range(0f, 1f)]
    public float directionRandomness = 0.1f;
    [Tooltip("Maximum angle for random direction variation in degrees")]
    [Range(0f, 180f)]
    public float maxRandomAngle = 45f; // Increased from the default 30 degrees
    [Tooltip("Whether to use weighted diagonal paths (1.4x cost)")]
    public bool useWeightedDiagonals = true;

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

        // 1. Reset all cells
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GridCell cell = gridDataGenerator.GetCell(x, y);
                if (cell != null)
                {
                    cell.integrationCost = int.MaxValue;
                    cell.flowDirection = Vector2Int.zero;
                }
            }
        }

        // 2. Initialize queue
        Queue<GridCell> queue = new Queue<GridCell>();
        GridCell targetCell = gridDataGenerator.GetCell(goal.x, goal.y);
        targetCell.integrationCost = 0;
        queue.Enqueue(targetCell);

        // 3. Dijkstra propagation - Now with optional diagonal movement
        while (queue.Count > 0)
        {
            GridCell current = queue.Dequeue();

            foreach (var neighborInfo in GetNeighborsWithCost(current))
            {
                GridCell neighbor = neighborInfo.cell;
                float moveCost = neighborInfo.cost;
                
                // Check for both obstacles AND occupied cells, and avoid them in pathfinding
                if (neighbor != null && !neighbor.flags.isObstacle && !neighbor.flags.isOccupied)
                {
                    // Add the movement cost - more expensive for diagonals if weighted
                    int newCost = current.integrationCost + Mathf.RoundToInt(moveCost);
                    if (newCost < neighbor.integrationCost)
                    {
                        neighbor.integrationCost = newCost;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        // 4. Compute flow direction with randomness
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GridCell cell = gridDataGenerator.GetCell(x, y);
                if (cell == null || cell.integrationCost == int.MaxValue) 
                    continue;
                
                GridCell lowest = cell;

                foreach (var neighborInfo in GetNeighborsWithCost(cell))
                {
                    GridCell neighbor = neighborInfo.cell;
                    if (neighbor != null && neighbor.integrationCost < lowest.integrationCost)
                        lowest = neighbor;
                }

                if (lowest != cell)
                {
                    // Basic flow direction based on lowest cost neighbor - now using Vector2 (float)
                    Vector2 baseDirection = new Vector2(lowest.x - cell.x, lowest.y - cell.y);
                    baseDirection.Normalize(); // Normalize for consistent magnitude
                    
                    // Add randomness if desired
                    if (directionRandomness > 0)
                    {
                        cell.flowDirection = AddRandomnessToDirection(baseDirection);
                    }
                    else
                    {
                        cell.flowDirection = baseDirection;
                    }
                }
            }
        }
        
        Debug.Log($"Flow field generated with target: {goal}");
    }

    // Helper method to add randomness to direction using continuous Vector2
    private Vector2 AddRandomnessToDirection(Vector2 baseDirection)
    {
        // No randomness for zero direction
        if (baseDirection == Vector2.zero)
            return Vector2.zero;
        
        // Calculate a random angle based on randomness factor
        float randomAngle = ((float)random.NextDouble() * 2f - 1f) * maxRandomAngle * directionRandomness;
        
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

    // New struct to represent a neighbor cell and its movement costt
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

        // Visualization to highlight the randomness
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
                    Vector3 end = start + direction * arrowScale;
                    
                    // Color based on direction randomness intensity
                    Color arrowColoring = arrowColor;
                    
                    // Change color based on the angle of the direction
                    float angle = Mathf.Atan2(cell.flowDirection.y, cell.flowDirection.x) * Mathf.Rad2Deg;
                    // Normalize angle to 0-360
                    angle = (angle + 360) % 360;
                    
                    if (directionRandomness > 0.0f)
                    {
                        // Create a gradient of colors based on the angle
                        float hue = angle / 360f;
                        arrowColoring = Color.HSVToRGB(hue, 0.7f, 0.8f);
                    }
                    
                    Gizmos.color = arrowColoring;
                    Gizmos.DrawLine(start, end);
                    
                    // Draw arrow head
                    Vector3 right = Quaternion.Euler(0, 30, 0) * -direction * arrowScale * 0.4f;
                    Vector3 left = Quaternion.Euler(0, -30, 0) * -direction * arrowScale * 0.4f;
                    Gizmos.DrawLine(end, end + right);
                    Gizmos.DrawLine(end, end + left);
                    
                    // Draw small circle at base for better visibility of high randomness cells
                    if (directionRandomness > 0.5f)
                    {
                        Gizmos.DrawWireSphere(start, 0.1f);
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
}
