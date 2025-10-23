using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GridController : MonoBehaviour
{
    [Header("Terrain & Overlay Settings")]
    [SerializeField] private GameObject terrainSource;
    [SerializeField] private Material gridOverlayMaterial;

    [Header("Debug Options")]
    [SerializeField] private bool alwaysShowGrid = false;

    private GameObject gridOverlayInstance;
    private Vector2Int currentHoveredCell;

    [Header("Grid System References")]
    [SerializeField] private GridDataGenerator gridDataGenerator;

    [Header("Grid Settings")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float lineWidth = 0.02f;
    [SerializeField] private GridColors gridColors;
    [SerializeField] private Color highlightColor = new Color(1, 1, 0, 1);
    [SerializeField] private Color gridLineColor = new Color(0, 1, 0, 0.5f);
    [SerializeField] private float gridLineOpacity = 0.5f;

    private Material targetMaterial;
    private MeshRenderer targetRenderer;
    private TextureGenerator textureGenerator;
    private bool textureNeedsUpdate = false;

    // Reusable collections for GetEnemiesInRange - prevents garbage allocation
    private HashSet<EnemyUnit> tempEnemySet = new HashSet<EnemyUnit>();
    private List<EnemyUnit> tempEnemyList = new List<EnemyUnit>();

    public static GridController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        // Force update to new improved grid colors
        gridColors.ownedOccupied = new Color(0.8f, 0.4f, 0.4f, 0.4f); // Light red/pink to indicate occupied
        gridColors.ownedObstacle = new Color(0.6f, 0.3f, 0.0f, 0.4f); // Brown for obstacles
        
        GridCellColorResolver.Colors = gridColors;

        if (gridDataGenerator == null)
            gridDataGenerator = FindFirstObjectByType<GridDataGenerator>();

        if (gridDataGenerator == null)
        {
            Debug.LogError("GridDataGenerator not found in scene.");
            return;
        }

        SetUpGridOverlay();
        ApplySettings();

        HideGrid();
    }

    void Update()
    {
        // Hover highlighting disabled - player doesn't need to see yellow grid highlighting
        // if (gridOverlayInstance.activeSelf && (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0))
        // {
        //     UpdateHoveredCell();
        // }
    }

    private void LateUpdate()
    {
        if (textureNeedsUpdate)
        {
            textureGenerator.UpdateTexture();
            textureNeedsUpdate = false;
        }
    }

    // Improve the UpdateHoveredCell method
    void UpdateHoveredCell()
    {
        // Check if mouse is over UI - if so, don't update hover cell
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
        {
            Vector2Int gridCoords = WorldToGridCoords(hit.point);

            // Only update the hover cell if it's within valid bounds
            if (IsValidCell(gridCoords.x, gridCoords.y))
            {
                currentHoveredCell = gridCoords;

                if (targetMaterial != null)
                {
                    targetMaterial.SetVector("_HoverCell", new Vector4(gridCoords.x, gridCoords.y, 0, 0));
                }
            }
        }
    }

    //gets all enemies in a square area(animal attack radius) around the given world position 
    public List<EnemyUnit> GetEnemiesInRange(Vector3 worldPosition, int blockRadius)
    {
        Vector2Int centerGridPos = WorldToGridCoords(worldPosition);
        
        // Reuse collections to prevent garbage allocation
        tempEnemySet.Clear();
        tempEnemyList.Clear();

        // Define the square search area
        for (int x = -blockRadius; x <= blockRadius; x++)
        {
            for (int y = -blockRadius; y <= blockRadius; y++)
            {
                Vector2Int checkPos = centerGridPos + new Vector2Int(x, y);

                if (!IsValidCell(checkPos.x, checkPos.y)) 
                    continue;

                Vector3 cellCenter = gridDataGenerator.GetWorldPositionFromGridCoords(checkPos);

                // Check for enemies at this cell using OverlapSphere
                Collider[] hits = Physics.OverlapSphere(cellCenter, cellSize * 0.5f);
                for (int i = 0; i < hits.Length; i++) // Use for loop instead of foreach (faster)
                {
                    EnemyUnit enemy = hits[i].GetComponent<EnemyUnit>();
                    if (enemy != null)
                    {
                        tempEnemySet.Add(enemy); // HashSet automatically handles duplicates - O(1) instead of O(n)
                    }
                }
            }
        }

        // Convert HashSet to List for return
        tempEnemyList.AddRange(tempEnemySet);
        return tempEnemyList;
    }

    public List<EnemyUnit> GetEnemiesInRangeSheep(Vector3 worldPosition, int blockRadius)
    {
        Vector2Int centerGridPos = WorldToGridCoords(worldPosition);
        
        // Reuse collections to prevent garbage allocation
        tempEnemySet.Clear();
        tempEnemyList.Clear();

        for (int x = -blockRadius; x <= blockRadius; x++)
        {
            for (int y = -blockRadius; y <= blockRadius; y++)
            {
                Vector2Int checkPos = centerGridPos + new Vector2Int(x, y);

                if (!IsValidCell(checkPos.x, checkPos.y)) 
                    continue;

                Vector3 cellCenter = gridDataGenerator.GetWorldPositionFromGridCoords(checkPos);

                Collider[] hits = Physics.OverlapSphere(cellCenter, cellSize * 0.5f);
                for (int i = 0; i < hits.Length; i++) // Use for loop instead of foreach
                {
                    EnemyUnit enemy = hits[i].GetComponent<EnemyUnit>();
                    if (enemy != null)
                    {
                        tempEnemySet.Add(enemy); // HashSet automatically handles duplicates
                    }
                }
            }
        }

        // Convert to list
        tempEnemyList.AddRange(tempEnemySet);
        return tempEnemyList;
    }

    public List<GridCell> GetCellsInRange(Vector3 worldPos, int radius)
    {
        List<GridCell> cellsInRange = new List<GridCell>();

        Vector2Int centerCoords = WorldToGridCoords(worldPos);

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector2Int checkCoords = centerCoords + new Vector2Int(x, y);
                if (!IsValidCell(checkCoords.x, checkCoords.y))
                    continue;

                // Optionally: check if within radius circle for a more accurate range
                if (x * x + y * y <= radius * radius)
                {
                    cellsInRange.Add(gridDataGenerator.GetCell(checkCoords.x, checkCoords.y));
                }
            }
        }

        // Debug.Log($"Cells in range of {worldPos} with radius {radius}: {cellsInRange.Count}------------------------------------------------------------------------------");

        return cellsInRange;
    }





    void SetUpGridOverlay()
    {
        if (terrainSource == null)
        {
            Debug.LogError("terrainSource is not assigned!");
            return;
        }

        MeshFilter sourceMF = terrainSource.GetComponent<MeshFilter>();
        MeshRenderer sourceMR = terrainSource.GetComponent<MeshRenderer>();

        if (sourceMF == null || sourceMR == null)
        {
            Debug.LogError("terrainSource must have both MeshFilter and MeshRenderer.");
            return;
        }

        gridOverlayInstance = new GameObject("GridOverlayMesh");
        gridOverlayInstance.transform.SetParent(this.transform);
        gridOverlayInstance.transform.position = terrainSource.transform.position + new Vector3(0, 0.01f, 0);
        gridOverlayInstance.transform.rotation = terrainSource.transform.rotation;
        gridOverlayInstance.transform.localScale = terrainSource.transform.localScale;

        MeshFilter overlayMF = gridOverlayInstance.AddComponent<MeshFilter>();
        MeshRenderer overlayMR = gridOverlayInstance.AddComponent<MeshRenderer>();
        overlayMF.sharedMesh = Instantiate(sourceMF.sharedMesh);
        overlayMR.material = gridOverlayMaterial;

        targetRenderer = overlayMR;
        targetMaterial = overlayMR.material;

        gridDataGenerator.targetMeshRenderer = overlayMR;

        textureGenerator = gridOverlayInstance.AddComponent<TextureGenerator>();
        textureGenerator.gridData = gridDataGenerator;
        textureGenerator.SetMeshRenderer(overlayMR);
    }

    public void ApplySettings()
    {
        gridDataGenerator.cellSize = cellSize;
        gridDataGenerator.GenerateGridData();

        if (targetMaterial != null)
        {
            targetMaterial.SetFloat("_GridLineOpacity", gridLineOpacity);
            targetMaterial.SetColor("_HighlightColor", highlightColor);
            targetMaterial.SetFloat("_LineWidth", lineWidth);
            targetMaterial.SetColor("_Color", gridLineColor);
        }

        textureGenerator.GenerateGridTexture();
    }

    // Methods for grid visibility control
    public void ShowGrid()
    {
        if (gridOverlayInstance != null)
        {
            gridOverlayInstance.SetActive(true);
        }
    }

    public void HideGrid()
    {
        if (gridOverlayInstance != null && !alwaysShowGrid)
        {
            gridOverlayInstance.SetActive(false);
        }
    }

    public bool IsGridVisible()
    {
        return gridOverlayInstance != null && gridOverlayInstance.activeSelf;
    }

    // Public utility methods for BuildController to use
    public Vector2Int WorldToGridCoords(Vector3 worldPos)
    {
        Vector4 origin = gridDataGenerator.GetGridOrigin();
        Vector4 worldSize = gridDataGenerator.GetGridWorldSize();
        int gridW = gridDataGenerator.GetGridWidth();
        int gridH = gridDataGenerator.GetGridHeight();

        float u = Mathf.InverseLerp(origin.x, origin.x + worldSize.x, worldPos.x);
        float v = Mathf.InverseLerp(origin.y, origin.y + worldSize.y, worldPos.z);
        int cellX = Mathf.FloorToInt(u * gridW);
        int cellY = Mathf.FloorToInt(v * gridH);

        return new Vector2Int(cellX, cellY);
    }

    // Modify the GetCellCenterFromTexture method with proper bounds checking
    public Vector3 GetCellCenterFromTexture(int x, int y)
    {
        // Safety check to prevent out of bounds errors
        if (!IsValidCell(x, y))
        {
            Debug.LogWarning($"GetCellCenterFromTexture: Attempted to access invalid cell ({x}, {y})");
            return Vector3.zero; // Return a default value
        }

        int gridWidth = gridDataGenerator.GetGridWidth();
        int gridHeight = gridDataGenerator.GetGridHeight();
        Vector4 gridOrigin = gridDataGenerator.GetGridOrigin();
        float cellSize = gridDataGenerator.cellSize;

        float textureCellWidth = 1f / gridWidth;
        float textureCellHeight = 1f / gridHeight;
        float textureCenterX = (x + 0.5f) * textureCellWidth;
        float textureCenterY = (y + 0.5f) * textureCellHeight;

        float worldX = gridOrigin.x + textureCenterX * gridDataGenerator.GetGridWorldSize().x;
        float worldZ = gridOrigin.y + textureCenterY * gridDataGenerator.GetGridWorldSize().y;

        // Get the cell and safely access its height
        GridCell cell = gridDataGenerator.GetCell(x, y);
        float worldY = 0f;
        if (cell != null)
        {
            worldY = cell.worldPosition.y;
        }

        return new Vector3(worldX, worldY, worldZ);
    }

    public bool IsValidCell(int x, int y)
    {
        return x >= 0 && x < gridDataGenerator.GetGridWidth() && y >= 0 && y < gridDataGenerator.GetGridHeight();
    }

    public GridCell GetCell(int x, int y)
    {
        if (!IsValidCell(x, y)) return null;
        return gridDataGenerator.GetCell(x, y);
    }

    public void SetCellOccupied(int x, int y, bool occupied)
    {
        if (!IsValidCell(x, y)) return;

        GridCell cell = gridDataGenerator.GetCell(x, y);
        cell.flags.isOccupied = occupied;
        gridDataGenerator.grid[x, y] = cell;

        // Flag for texture update instead of updating immediately
        textureNeedsUpdate = true;
    }

    public Vector2Int GetCurrentHoveredCell()
    {
        return currentHoveredCell;
    }

    public Material GetTargetMaterial()
    {
        return targetMaterial;
    }

    public void UpdateGridTexture()
    {
        textureGenerator.UpdateTexture();
    }

    // Add this public method to the GridController class:
    public float GetCellSize()
    {
        return cellSize;
    }

    public int TextureWidth
    {
        get => gridDataGenerator != null ? gridDataGenerator.GetGridWidth() : 0;
    }

    public int TextureHeight
    {
        get => gridDataGenerator != null ? gridDataGenerator.GetGridHeight() : 0;
    }
}