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

    private void Start()
    {
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

        // 3. Dijkstra propagation - IMPORTANT: Now works on ALL cells, but treats occupied cells as obstacles
        while (queue.Count > 0)
        {
            GridCell current = queue.Dequeue();

            foreach (var neighbor in GetNeighbors(current))
            {
                // Check for both obstacles AND occupied cells, and avoid them in pathfinding
                if (neighbor != null && !neighbor.flags.isObstacle && !neighbor.flags.isOccupied)
                {
                    int newCost = current.integrationCost + 1;
                    if (newCost < neighbor.integrationCost)
                    {
                        neighbor.integrationCost = newCost;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        // 4. Compute flow direction
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GridCell cell = gridDataGenerator.GetCell(x, y);
                if (cell == null || cell.integrationCost == int.MaxValue) 
                    continue;
                
                GridCell lowest = cell;

                foreach (var neighbor in GetNeighbors(cell))
                {
                    if (neighbor != null && neighbor.integrationCost < lowest.integrationCost)
                        lowest = neighbor;
                }

                if (lowest != cell)
                    cell.flowDirection = new Vector2Int(lowest.x - cell.x, lowest.y - cell.y);
            }
        }
        
        Debug.Log($"Flow field generated with target: {goal}");
    }

    // Get neighboring cells for a given cell
    private List<GridCell> GetNeighbors(GridCell cell)
    {
        List<GridCell> neighbors = new List<GridCell>();

        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        foreach (var dir in directions)
        {
            int nx = cell.x + dir.x;
            int ny = cell.y + dir.y;

            if (gridController.IsValidCell(nx, ny))
                neighbors.Add(gridDataGenerator.GetCell(nx, ny));
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

    // Called when inspector values change
    private void OnValidate()
    {
        // Trigger an update when manual target is changed in inspector
        if (Application.isPlaying && initialized)
        {
            manualTargetChanged = true;
        }
    }

    // Visualize the flow field in the editor
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
                    Gizmos.DrawSphere(targetCell.worldPosition, arrowScale * 0.5f);
                    Gizmos.DrawWireCube(targetCell.worldPosition, new Vector3(1, 0.1f, 1));
                }
            }
        }

        // Only draw flow directions if flow field visualization is enabled
        if (!visualizeFlowField)
            return;
            
        // Draw flow directions
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GridCell cell = gridDataGenerator.GetCell(x, y);
                if (cell == null) continue;
                
                if (cell.flowDirection != Vector2Int.zero && cell.integrationCost != int.MaxValue)
                {
                    Vector3 start = cell.worldPosition;
                    Vector3 direction = new Vector3(cell.flowDirection.x, 0, cell.flowDirection.y).normalized;
                    Vector3 end = start + direction * arrowScale;
                    
                    Gizmos.color = arrowColor;
                    Gizmos.DrawLine(start, end);
                    
                    // Draw arrow head
                    Vector3 right = Quaternion.Euler(0, 30, 0) * -direction * arrowScale * 0.3f;
                    Vector3 left = Quaternion.Euler(0, -30, 0) * -direction * arrowScale * 0.3f;
                    Gizmos.DrawLine(end, end + right);
                    Gizmos.DrawLine(end, end + left);
                }
            }
        }
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
