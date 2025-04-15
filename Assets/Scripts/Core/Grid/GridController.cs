using UnityEngine;

public class GridController : MonoBehaviour
{
    [Header("Terrain & Overlay Settings")]
    [SerializeField] private GameObject terrainSource;
    [SerializeField] private Material gridOverlayMaterial;
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private GameObject itemPrefab;

    private GameObject ghostInstance;
    private Vector2Int currentHoveredCell;
    private GameObject gridOverlayInstance;

    [Header("Grid System References")]
    [SerializeField] private GridDataGenerator gridDataGenerator;

    [Header("Grid Settings")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Color gridLineColor = new Color(0, 1, 0, 0.5f);
    [SerializeField] private Color highlightColor = new Color(1, 1, 0, 1);
    [SerializeField] private float gridLineOpacity = 0.5f;
    [SerializeField] private float lineWidth = 0.02f;
    [SerializeField] private float highlightIntensity = 0.7f;

    private Material targetMaterial;
    private MeshRenderer targetRenderer;
    private TextureGenerator textureGenerator;

    [Header("Grid Colors")]
    [SerializeField] private GridColors gridColors;

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
    }

    void Update()
    {
        ProcessInput();
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
            targetMaterial.SetColor("_Color", gridLineColor);
            targetMaterial.SetColor("_HighlightColor", highlightColor);
            targetMaterial.SetFloat("_GridLineOpacity", gridLineOpacity);
            targetMaterial.SetFloat("_LineWidth", lineWidth);
            targetMaterial.SetFloat("_HighlightIntensity", highlightIntensity);
        }

        textureGenerator.GenerateGridTexture();
    }

    void ProcessInput()
    {
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
        {
            Vector4 origin = gridDataGenerator.GetGridOrigin();
            Vector4 worldSize = gridDataGenerator.GetGridWorldSize();
            int gridW = gridDataGenerator.GetGridWidth();
            int gridH = gridDataGenerator.GetGridHeight();

            Vector3 hitPos = hit.point;
            float u = Mathf.InverseLerp(origin.x, origin.x + worldSize.x, hitPos.x);
            float v = Mathf.InverseLerp(origin.y, origin.y + worldSize.y, hitPos.z);
            int cellX = Mathf.FloorToInt(u * gridW);
            int cellY = Mathf.FloorToInt(v * gridH);

            targetMaterial.SetVector("_HoverCell", new Vector4(cellX, cellY, 0, 0));

            if (Input.GetMouseButtonDown(0))
            {
                PlaceItem(cellX, cellY);
                textureGenerator.UpdateTexture();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                RemoveItem(cellX, cellY);
                textureGenerator.UpdateTexture();
            }
        }
    }

    void PlaceItem(int x, int y)
    {
        if (!IsValidCell(x, y)) return;

        GridCell cell = gridDataGenerator.GetCell(x, y);
        if (!cell.flags.isOwned || cell.flags.isOccupied) return;

        // Calculate the center of the cell using the texture map
        Vector3 cellCenter = GetCellCenterFromTexture(x, y);

        // Log the calculated position for debugging
        Debug.Log($"Placing item at cell ({x}, {y}) with calculated position: {cellCenter}");

        // Place the item at the calculated position
        GameObject item = Instantiate(itemPrefab, cellCenter, Quaternion.identity);
        item.name = $"Item_{x}_{y}";

        cell.flags.isOccupied = true;
        gridDataGenerator.grid[x, y] = cell;
    }

    Vector3 GetCellCenterFromTexture(int x, int y)
    {
        // Get grid dimensions and origin
        int gridWidth = gridDataGenerator.GetGridWidth();
        int gridHeight = gridDataGenerator.GetGridHeight();
        Vector4 gridOrigin = gridDataGenerator.GetGridOrigin();
        float cellSize = gridDataGenerator.cellSize;

        // Calculate the center of the cell in texture space
        float textureCellWidth = 1f / gridWidth;
        float textureCellHeight = 1f / gridHeight;
        float textureCenterX = (x + 0.5f) * textureCellWidth;
        float textureCenterY = (y + 0.5f) * textureCellHeight;

        // Map texture space to world space
        float worldX = gridOrigin.x + textureCenterX * gridDataGenerator.GetGridWorldSize().x;
        float worldZ = gridOrigin.y + textureCenterY * gridDataGenerator.GetGridWorldSize().y;

        // Use the cell's Y position for height
        float worldY = gridDataGenerator.GetCell(x, y).worldPosition.y;

        return new Vector3(worldX, worldY, worldZ);
    }

    void RemoveItem(int x, int y)
    {
        if (!IsValidCell(x, y)) return;

        GridCell cell = gridDataGenerator.GetCell(x, y);
        if (!cell.flags.isOwned || !cell.flags.isOccupied) return;

        string itemName = $"Item_{x}_{y}";
        GameObject placedItem = GameObject.Find(itemName);
        if (placedItem != null) Destroy(placedItem);

        cell.flags.isOccupied = false;
        gridDataGenerator.grid[x, y] = cell;
    }

    bool IsValidCell(int x, int y)
    {
        return x >= 0 && x < gridDataGenerator.GetGridWidth() && y >= 0 && y < gridDataGenerator.GetGridHeight();
    }
}
