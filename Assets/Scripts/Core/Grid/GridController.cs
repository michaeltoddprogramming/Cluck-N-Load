using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GridController : MonoBehaviour
{
    [Header("Terrain & Overlay Settings")]
    [SerializeField] private GameObject terrainSource;
    [SerializeField] private Material gridOverlayMaterial;
    [SerializeField] private GameObject ghostPrefab;
    
    // (The array of itemPrefabs is still here if you want to use keyboard-based cycling)
    [SerializeField] private GameObject[] itemPrefabs;  // Array of item prefabs
    private int currentPrefabIndex = 0;  // Index of the current prefab
    private GameObject currentItemPrefab => itemPrefabs[currentPrefabIndex];

    // NEW: Build target selected from the UI
    private GameObject currentBuildTargetPrefab;

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
    
        // Set a default build target if none is selected
        if (itemPrefabs.Length > 0)
        {
            currentBuildTargetPrefab = itemPrefabs[0];
            ReplaceGhostWithPrefab(currentBuildTargetPrefab);
        }
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
        // Existing keyboard input code for cycling if needed:
        if (Input.GetKeyDown(KeyCode.N))
        {
            currentPrefabIndex = (currentPrefabIndex + 1) % itemPrefabs.Length;
            // When cycling with keys, update the build target too.
            currentBuildTargetPrefab = currentItemPrefab;
            ReplaceGhostWithPrefab(currentBuildTargetPrefab);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            currentPrefabIndex = (currentPrefabIndex - 1 + itemPrefabs.Length) % itemPrefabs.Length;
            currentBuildTargetPrefab = currentItemPrefab;
            ReplaceGhostWithPrefab(currentBuildTargetPrefab);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            currentRotation *= Quaternion.Euler(0, 90, 0);
            if (ghostInstance != null)
                ghostInstance.transform.rotation = currentRotation;
        }
    }

    // NEW: Replaces ghost using the provided prefab
    void ReplaceGhostWithPrefab(GameObject prefab)
    {
        if (ghostInstance != null)
            Destroy(ghostInstance);

        ghostInstance = Instantiate(prefab);
        ApplyGhostMaterial(ghostInstance);
        ghostInstance.transform.rotation = currentRotation;
        hasGhost = true;
    }

               void UpdateGhostInstance()
        {
            // Get reference to ShopUIManager instead of ShopPanelUI
            ShopUIManager shopManager = ShopUIManager.Instance;
            
            // Hide the ghost if the shop is closed or no build target is set
            if (shopManager == null || !shopManager.IsShopOpen() || currentBuildTargetPrefab == null)
            {
                if (ghostInstance != null)
                {
                    Destroy(ghostInstance);
                    ghostInstance = null;
                    hasGhost = false;
                }
                return;
            }
            
            // Create the ghost if it doesn't exist
            if (ghostInstance == null)
            {
                ReplaceGhostWithPrefab(currentBuildTargetPrefab);
            }
        
            // Update ghost position based on mouse hover
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

    // NEW: This method now accepts a StructureData and uses its prefab as the build target.
    public void SetBuildTarget(StructureData data)
    {
        if (data == null || data.prefab == null)
        {
            Debug.LogError("Invalid StructureData or prefab is null.");
            return;
        }

        Debug.Log($"Setting build target to: {data.structureName}");
        currentBuildTargetPrefab = data.prefab;
        ReplaceGhostWithPrefab(currentBuildTargetPrefab);
    }   

    private List<Vector2Int> GetOccupiedCellsFromBounds(GameObject obj)
    {
        List<Vector2Int> occupiedCells = new List<Vector2Int>();

        // Get the object's bounds in world space
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return occupiedCells;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        // Define the corners of the bounds
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        // Loop through every cell that intersects with the bounds
        Vector2Int bottomLeft = WorldToGridCoords(min);
        Vector2Int topRight = WorldToGridCoords(max);

        for (int x = bottomLeft.x; x <= topRight.x; x++)
        {
            for (int y = bottomLeft.y; y <= topRight.y; y++)
            {
                if (IsValidCell(x, y))
                {
                    occupiedCells.Add(new Vector2Int(x, y));
                }
            }
        }

        return occupiedCells;
    }

    private List<Vector2Int> GetOccupiedCellsFromMeshBounds(GameObject obj)
    {
        var occupiedCells = new List<Vector2Int>();
        
        // Get renderer (which properly accounts for rotation)
        var renderer = obj.GetComponentInChildren<Renderer>();
        if (renderer == null) return occupiedCells;
        
        // Get bounds in world space (already handles rotation correctly)
        Bounds bounds = renderer.bounds;
        
        // Shrink bounds slightly to prevent edge-case cells from being occupied
        bounds.Expand(-0.1f);
        
        // Debug visualization
        Debug.DrawLine(bounds.min, bounds.max, Color.red, 2.0f);
        
        // Convert to grid coordinates
        Vector2Int bottomLeft = WorldToGridCoords(bounds.min);
        Vector2Int topRight = WorldToGridCoords(bounds.max);
        
        // Loop through all cells
        for (int x = bottomLeft.x; x <= topRight.x; x++) {
            for (int y = bottomLeft.y; y <= topRight.y; y++) {
                if (IsValidCell(x, y)) {
                    // Get cell center in world space
                    Vector3 cellCenter = GetCellCenterFromTexture(x, y);
                    
                    // Add a more precise check - only occupy if cell center is within the bounds
                    if (bounds.Contains(new Vector3(cellCenter.x, bounds.center.y, cellCenter.z))) {
                        occupiedCells.Add(new Vector2Int(x, y));
                    }
                }
            }
        }
        
        return occupiedCells;
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
                if (currentBuildTargetPrefab != null)
                    ReplaceGhostWithPrefab(currentBuildTargetPrefab);
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
        if (ShopUIManager.Instance == null || !ShopUIManager.Instance.IsShopOpen())
        {
            Debug.LogWarning("Cannot place structures while the shop is closed!");
            return;
        }
        if (!ShopPanelUI.Instance.IsShopOpen())
        {
            Debug.LogWarning("Cannot place structures while the shop is closed!");
            return;
        }
        if (currentBuildTargetPrefab == null)
            {
                Debug.LogWarning("No build target is set!");
                return;
            }
        if (!IsValidCell(x, y)) return;

        GridCell cell = gridDataGenerator.GetCell(x, y);
        if (!cell.flags.isOwned || cell.flags.isOccupied) return;

        Vector3 cellCenter = GetCellCenterFromTexture(x, y);

        // Create a temporary build item to calculate occupied cells
        GameObject tempItem = Instantiate(currentBuildTargetPrefab, cellCenter, currentRotation);
        List<Vector2Int> cellsToOccupy = GetOccupiedCellsFromMeshBounds(tempItem);

        // Validate that all cells in footprint are free
        foreach (var pos in cellsToOccupy)
        {
            if (!IsValidCell(pos.x, pos.y)) {
                Destroy(tempItem);
                return; // Out of bounds, cancel placement
            }

            GridCell targetCell = gridDataGenerator.GetCell(pos.x, pos.y);
            if (targetCell.flags.isOccupied) {
                Destroy(tempItem);
                return; // Collision with another structure, cancel placement
            }
        }

        // No collision — placement is valid!
        tempItem.name = $"Item_{x}_{y}";

        foreach (var pos in cellsToOccupy)
        {
            GridCell targetCell = gridDataGenerator.GetCell(pos.x, pos.y);
            targetCell.flags.isOccupied = true;
            gridDataGenerator.grid[pos.x, pos.y] = targetCell;
        }

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
        if (placedItem != null)
        {
            List<Vector2Int> occupiedCells = GetOccupiedCellsFromMeshBounds(placedItem);
            foreach (var pos in occupiedCells)
            {
                if (!IsValidCell(pos.x, pos.y)) continue;
                GridCell affectedCell = gridDataGenerator.GetCell(pos.x, pos.y);
                affectedCell.flags.isOccupied = false;
                gridDataGenerator.grid[pos.x, pos.y] = affectedCell;
            }
            Destroy(placedItem);
        }
    }

    bool IsValidCell(int x, int y)
    {
        return x >= 0 && x < gridDataGenerator.GetGridWidth() && y >= 0 && y < gridDataGenerator.GetGridHeight();
    }
}
