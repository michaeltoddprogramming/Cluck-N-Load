using UnityEngine;

public class GridController : MonoBehaviour
{
    [Header("Terrain & Overlay Settings")]
    [SerializeField] private GameObject terrainSource;
    [SerializeField] private Material gridOverlayMaterial;
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private GameObject[] itemPrefabs;  // Array of item prefabs
    private int currentPrefabIndex = 0;  // Index of the current prefab
    private GameObject currentItemPrefab => itemPrefabs[currentPrefabIndex];

    private GameObject ghostInstance;
    private Vector2Int currentHoveredCell;
    private GameObject gridOverlayInstance;
    private GhostPlacer ghostPlacer;

    [Header("Grid System References")]
    [SerializeField] private GridDataGenerator gridDataGenerator;

    [Header("Grid Settings")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float lineWidth = 0.02f;
    [SerializeField] private GridColors gridColors;
    [SerializeField] private Color highlightColor = new Color(1, 1, 0, 1);
    [SerializeField] private Color gridLineColor = new Color(0, 1, 0, 0.5f);
    [SerializeField] private float gridLineOpacity = 0.5f;
    // [SerializeField] private float highlightIntensity = 0.7f;


    private Material targetMaterial;
    private MeshRenderer targetRenderer;
    private TextureGenerator textureGenerator;

    [Header("Ghost Settings")]
    [SerializeField] private Material ghostMaterial;

    private GameObject currentGhost;
    private bool hasGhost = false;
    private Quaternion currentRotation = Quaternion.identity;

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

        ghostPlacer = FindObjectOfType<GhostPlacer>();
        if (ghostPlacer == null)
        {
            Debug.LogWarning("GhostPlacer not found. Ghost visual feedback will not be shown.");
        }

        SetUpGridOverlay();
        ApplySettings();
    }

    void Update()
    {
        HandlePrefabSelectionInput();
        ProcessInput();
        UpdateGhostInstance();
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
            // targetMaterial.SetFloat("_HighlightIntensity", highlightIntensity);
        }

        textureGenerator.GenerateGridTexture();
    }

    void HandlePrefabSelectionInput()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            currentPrefabIndex = (currentPrefabIndex + 1) % itemPrefabs.Length;
            ReplaceGhost();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            currentPrefabIndex = (currentPrefabIndex - 1 + itemPrefabs.Length) % itemPrefabs.Length;
            ReplaceGhost();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            currentRotation *= Quaternion.Euler(0, 90, 0);
            if (ghostInstance != null)
                ghostInstance.transform.rotation = currentRotation;
        }
    }

    void ReplaceGhost()
    {
        if (ghostInstance != null)
            Destroy(ghostInstance);

        ghostInstance = Instantiate(currentItemPrefab);  // Instantiate the selected prefab
        ApplyGhostMaterial(ghostInstance);
        ghostInstance.transform.rotation = currentRotation;
    }

    void UpdateGhostInstance()
    {
        if (ghostInstance == null)
        {
            ReplaceGhost();
        }

        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
        {
            Vector3 hitPos = hit.point;
            Vector2Int gridCoords = WorldToGridCoords(hitPos);
            Vector3 center = GetCellCenterFromTexture(gridCoords.x, gridCoords.y);

            ghostInstance.transform.position = center;
        }
    }

    void ApplyGhostMaterial(GameObject obj)
    {
        foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
        {
            renderer.material = ghostMaterial;
        }
    }

    Vector2Int WorldToGridCoords(Vector3 worldPos)
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

            currentHoveredCell = new Vector2Int(cellX, cellY);

            targetMaterial.SetVector("_HoverCell", new Vector4(cellX, cellY, 0, 0));

            if (!hasGhost)
            {
                ReplaceGhost();
            }

            if (currentGhost != null)
            {
                Vector3 ghostPos = GetCellCenterFromTexture(cellX, cellY);
                currentGhost.transform.position = ghostPos;
            }

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
        else
        {
            ghostPlacer?.UpdateGhostAtCell(-1, -1); // Hide ghost if not hovering
        }
    }

    void PlaceItem(int x, int y)
    {
        if (!IsValidCell(x, y)) return;

        GridCell cell = gridDataGenerator.GetCell(x, y);
        if (!cell.flags.isOwned || cell.flags.isOccupied) return;

        Vector3 cellCenter = GetCellCenterFromTexture(x, y);

        GameObject item = Instantiate(currentItemPrefab, cellCenter, currentRotation);  // Instantiate with rotation
        item.name = $"Item_{x}_{y}";

        cell.flags.isOccupied = true;
        gridDataGenerator.grid[x, y] = cell;

        Destroy(currentGhost);
        hasGhost = false;
    }

    Vector3 GetCellCenterFromTexture(int x, int y)
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
