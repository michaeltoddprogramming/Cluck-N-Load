using System;
using UnityEngine;

public class GridDataGenerator : MonoBehaviour
{
    public MeshRenderer targetMeshRenderer; // The floor mesh renderer
    
    [Header("Grid Size Options")]
    public bool useFixedGridSize = false;    // Toggle between auto and fixed size
    public int fixedGridWidth = 100;         // Fixed width when useFixedGridSize is true
    public int fixedGridHeight = 100;        // Fixed height when useFixedGridSize is true
    public float cellSize = 1f;              // Desired cell size in world units (for auto mode)

    [Space]
    public int gridWidth;   // Number of cells along X (computed or fixed)
    public int gridHeight;  // Number of cells along Z (computed or fixed)

    public GridCell[,] grid;

    public bool IsInitialized { get; private set; } = false;

    // Computed from the target mesh’s bounds.
    private Vector4 gridOrigin;    // (b.min.x, b.min.z, 0, 0)
    private Vector4 gridWorldSize; // (b.size.x, b.size.z, 0, 0)

    void Start()
    {
        GenerateGridData();
        IsInitialized = true;
    }

    public void GenerateGridData()
    {
        if (targetMeshRenderer == null)
        {
            Debug.LogError("No mesh assigned in GridDataGenerator!");
            return;
        }

        Debug.Log($"=== GRID GENERATION DEBUG ===");
        Debug.Log($"useFixedGridSize: {useFixedGridSize}");
        Debug.Log($"fixedGridWidth: {fixedGridWidth}");
        Debug.Log($"fixedGridHeight: {fixedGridHeight}");
        Debug.Log($"cellSize: {cellSize}");

        Bounds b = targetMeshRenderer.bounds;
        Debug.Log($"Terrain bounds: {b.size.x} x {b.size.z}");
        
        gridOrigin = new Vector4(b.min.x, b.min.z, 0, 0);   // Use X and Z as our 2D origin.
        gridWorldSize = new Vector4(b.size.x, b.size.z, 0, 0); // Use size.x and size.z.

        if (useFixedGridSize)
        {
            // Use fixed grid size and keep the original cell size
            gridWidth = fixedGridWidth;
            gridHeight = fixedGridHeight;
            
            // Keep the original cell size - don't change it!
            // Instead, expand the grid area to accommodate 100x100 cells
            
            // Calculate the total world size needed for this grid
            float totalWorldWidth = gridWidth * cellSize;
            float totalWorldHeight = gridHeight * cellSize;
            
            // Center the grid around the terrain center
            Vector3 terrainCenter = targetMeshRenderer.bounds.center;
            
            // Override the grid origin and world size to expand beyond terrain if needed
            gridOrigin = new Vector4(
                terrainCenter.x - totalWorldWidth * 0.5f,  // Center X
                terrainCenter.z - totalWorldHeight * 0.5f, // Center Z
                0, 0
            );
            
            gridWorldSize = new Vector4(totalWorldWidth, totalWorldHeight, 0, 0);
            
            Debug.Log($"Fixed Grid Mode: {gridWidth}x{gridHeight}, keeping cellSize: {cellSize}");
            Debug.Log($"Grid will cover area: {totalWorldWidth}x{totalWorldHeight} world units");
        }
        else
        {
            // Compute how many cells fit along X and Z based on cellSize:
            gridWidth = Mathf.RoundToInt(b.size.x / cellSize); // Use RoundToInt for better alignment
            gridHeight = Mathf.RoundToInt(b.size.z / cellSize);
            
            Debug.Log($"Auto Grid Mode: {gridWidth}x{gridHeight}, using cellSize: {cellSize}");
        }

        Debug.Log($"FINAL GRID SIZE: {gridWidth} x {gridHeight}");
        Debug.Log($"=== END GRID DEBUG ===");

        grid = new GridCell[gridWidth, gridHeight];

        // Initialize each grid cell with its center position and default flags.
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                float cx = gridOrigin.x + (x + 0.5f) * cellSize; // Use gridOrigin instead of bounds
                float cz = gridOrigin.y + (y + 0.5f) * cellSize; // Use gridOrigin.y which is actually Z
                Vector3 center = new Vector3(cx, 0, cz);

                // Dynamically calculate the height (Y position) at the center of each grid cell
                float height = GetTerrainHeightAtPosition(center);

                // Create and populate the grid cell
                grid[x, y] = new GridCell
                {
                    x = x,
                    y = y,
                    worldPosition = new Vector3(center.x, height, center.z),  // Use calculated height
                    height = height,  // Store the height for reference
                    flags = new GridCellFlags { 
                        isOwned = false, 
                        isOccupied = false, 
                        isObstacle = false,
                        isVisible = false  // Start with all cells invisible
                    }
                };
            }
        }
    }

    private float GetTerrainHeightAtPosition(Vector3 position)
    {
        // Use a raycast to determine the terrain height at the given position
        if (Physics.Raycast(new Vector3(position.x, targetMeshRenderer.bounds.max.y + 1f, position.z), Vector3.down, out RaycastHit hit))
        {
            return hit.point.y; // Return the Y position of the terrain
        }

        // Fallback to the average Y bound if no terrain is hit
        return (targetMeshRenderer.bounds.min.y + targetMeshRenderer.bounds.max.y) / 2f;
    }

    // Getters for external access:
    public int GetGridWidth() => gridWidth;
    public int GetGridHeight() => gridHeight;
    public GridCell GetCell(int x, int y) => grid[x, y];
    public MeshRenderer GetTargetMeshRenderer() => targetMeshRenderer;
    public Vector4 GetGridOrigin() => gridOrigin;
    public Vector4 GetGridWorldSize() => gridWorldSize;
}
