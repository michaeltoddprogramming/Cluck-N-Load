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

    void Start()
    {
        GridCellColorResolver.Colors = gridColors;
    
        if (gridDataGenerator == null)
            gridDataGenerator = FindObjectOfType<GridDataGenerator>();
    
        if (gridDataGenerator == null)
        {
            Debug.LogError("GridDataGenerator not found in scene.");
            return;
        }
    
        SetUpGridOverlay();
        ApplySettings();
        
        // Hide grid by default
        HideGrid();
    }

    void Update()
    {
        // Only update hover cell if grid is visible
        if (gridOverlayInstance.activeSelf)
        {
            UpdateHoveredCell();
        }
    }

    void UpdateHoveredCell()
    {
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
        {
            Vector2Int gridCoords = WorldToGridCoords(hit.point);
            currentHoveredCell = gridCoords;
            
            if (targetMaterial != null)
            {
                targetMaterial.SetVector("_HoverCell", new Vector4(gridCoords.x, gridCoords.y, 0, 0));
            }
        }
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
        Debug.Log($"GridController: ShowGrid called, gridOverlayInstance={gridOverlayInstance != null}");
        if (gridOverlayInstance != null)
        {
            gridOverlayInstance.SetActive(true);
            Debug.Log("Grid is now visible");
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

    public Vector3 GetCellCenterFromTexture(int x, int y)
    {
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

        float worldY = gridDataGenerator.GetCell(x, y).worldPosition.y;

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
        
        // Update texture to reflect changes
        textureGenerator.UpdateTexture();
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
}