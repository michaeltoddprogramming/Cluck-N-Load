using System;
using UnityEngine;

public class GridDataGenerator : MonoBehaviour
{
    public MeshRenderer targetMeshRenderer; // The floor mesh renderer
    public float cellSize = 1f;             // Desired cell size in world units

    public int gridWidth;   // Number of cells along X (computed)
    public int gridHeight;  // Number of cells along Z (computed)

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

        Bounds b = targetMeshRenderer.bounds;
        gridOrigin = new Vector4(b.min.x, b.min.z, 0, 0);   // Use X and Z as our 2D origin.
        gridWorldSize = new Vector4(b.size.x, b.size.z, 0, 0); // Use size.x and size.z.

        // Compute how many cells fit along X and Z based on cellSize:
        gridWidth = Mathf.RoundToInt(b.size.x / cellSize); // Use RoundToInt for better alignment
        gridHeight = Mathf.RoundToInt(b.size.z / cellSize);

        Debug.Log($"Grid size: {gridWidth} x {gridHeight}");

        grid = new GridCell[gridWidth, gridHeight];

        // Initialize each grid cell with its center position and default flags.
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                float cx = b.min.x + (x + 0.5f) * cellSize; // Center the cell along X
                float cz = b.min.z + (y + 0.5f) * cellSize; // Center the cell along Z
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
                    flags = new GridCellFlags { isOwned = false, isOccupied = false, isObstacle = false }
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
