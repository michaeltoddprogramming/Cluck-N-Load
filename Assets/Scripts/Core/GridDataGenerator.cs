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
    // We store the lower‑left corner (using X and Z) in _GridOrigin.xy
    private Vector4 gridOrigin;    // (b.min.x, b.min.z, 0, 0)
    // And store the overall width and depth (using b.size.x and b.size.z) in _GridWorldSize.xy
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
        gridWidth = Mathf.FloorToInt(b.size.x / cellSize);
        gridHeight = Mathf.FloorToInt(b.size.z / cellSize);

        Debug.Log($"Grid size: {gridWidth} x {gridHeight}");

        grid = new GridCell[gridWidth, gridHeight];

        // Initialize each grid cell with its center position.
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                float cx = b.min.x + x * cellSize + cellSize / 2f;
                float cz = b.min.z + y * cellSize + cellSize / 2f;
                Vector3 center = new Vector3(cx, b.min.y, cz);

                grid[x, y] = new GridCell
                {
                    x = x,
                    y = y,
                    state = CellState.Empty,
                    worldPosition = center
                };
            }
        }
    }

    // Getters for external access:
    public int GetGridWidth() => gridWidth;
    public int GetGridHeight() => gridHeight;
    public GridCell GetCell(int x, int y) => grid[x, y];
    public MeshRenderer GetTargetMeshRenderer() => targetMeshRenderer;
    public Vector4 GetGridOrigin() => gridOrigin;
    public Vector4 GetGridWorldSize() => gridWorldSize;
}
  
[Serializable]
public struct GridCell
{
    public int x;
    public int y;
    public CellState state;
    public Vector3 worldPosition;
}

public enum CellState
{
    Empty,
    Owned,
    Occupied,
    Unavailable,
    Unknown
}
