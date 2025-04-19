using UnityEngine;
using System.Collections.Generic;

public class BuildController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridController gridController;
    
    [Header("Build Settings")]
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private GameObject[] buildablePrefabs;
    
    [Header("Input Settings")]
    [SerializeField] private KeyCode rotateKey = KeyCode.R;
    [SerializeField] private KeyCode nextItemKey = KeyCode.N;
    [SerializeField] private KeyCode previousItemKey = KeyCode.P;
    
    private GameObject currentGhost;
    private GameObject currentBuildTargetPrefab;
    private bool isBuildModeActive = false;
    private int currentPrefabIndex = 0;
    private Quaternion currentRotation = Quaternion.identity;
    
    // References to shop UI component
    private ShopPanelUI shopPanelUI;
    
    private bool isGhostTemporarilyHidden = false;
    
    void Start()
    {
        if (gridController == null)
        {
            gridController = FindObjectOfType<GridController>();
            if (gridController == null)
            {
                Debug.LogError("GridController not found. BuildController cannot function.");
                enabled = false;
                return;
            }
        }
        
        // Find shop UI component - including inactive objects
        shopPanelUI = FindObjectOfType<ShopPanelUI>(true); // Include inactive objects
        
        // Set up shop event listeners
        if (shopPanelUI != null)
        {
            Debug.Log("BuildController: Found ShopPanelUI, subscribing to events");
            shopPanelUI.OnShopOpened.AddListener(HandleShopOpened);
            shopPanelUI.OnShopClosed.AddListener(HandleShopClosed);
        }
        else
        {
            Debug.LogWarning("BuildController: ShopPanelUI not found in scene!");
        }
        
        // Set a default build target if available
        if (buildablePrefabs.Length > 0)
        {
            currentBuildTargetPrefab = buildablePrefabs[0];
        }
        
        Debug.Log($"BuildController started. Grid controller reference: {(gridController != null ? "Valid" : "NULL")}");
        Debug.Log($"Available prefabs: {buildablePrefabs.Length}");
    }

    void OnDestroy()
    {
        // Clean up event listeners
        if (shopPanelUI != null)
        {
            shopPanelUI.OnShopOpened.RemoveListener(HandleShopOpened);
            shopPanelUI.OnShopClosed.RemoveListener(HandleShopClosed);
        }
    }
    
    // Event handlers for shop state changes
    public void HandleShopOpened()
    {
        Debug.Log("BuildController: HandleShopOpened called");
        gridController.ShowGrid();
        EnableBuildMode();
    }
    
    public void HandleShopClosed()
    {
        DisableBuildMode();
        gridController.HideGrid();
    }
    
    void Update()
    {
        if (!isBuildModeActive) return;
        
        HandleBuildInput();
        UpdateGhostPosition();
    }
    
    public void EnableBuildMode()
    {
        isBuildModeActive = true;
        gridController.ShowGrid();
        
        if (currentBuildTargetPrefab != null && currentGhost == null)
        {
            CreateGhost(currentBuildTargetPrefab);
        }
    }
    
    public void DisableBuildMode()
    {
        isBuildModeActive = false;
        gridController.HideGrid();
        
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }
    }
    
    public void ToggleBuildMode()
    {
        if (isBuildModeActive)
            DisableBuildMode();
        else
            EnableBuildMode();
    }
    
    public bool IsBuildModeActive()
    {
        return isBuildModeActive;
    }
    
    // Called from ShopPanelUI when pointer enters the UI
    public void HideGhostTemporarily()
    {
        if (currentGhost != null && currentGhost.activeSelf)
        {
            isGhostTemporarilyHidden = true;
            currentGhost.SetActive(false);
        }
    }
    
    // Called from ShopPanelUI when pointer exits the UI
    public void RestoreGhost()
    {
        if (currentGhost != null && isGhostTemporarilyHidden)
        {
            currentGhost.SetActive(true);
            isGhostTemporarilyHidden = false;
        }
    }
    
    void HandleBuildInput()
    {
        // Skip input handling if ghost is temporarily hidden (hovering over UI)
        if (isGhostTemporarilyHidden) return;
        
        // Rotation
        if (Input.GetKeyDown(rotateKey))
        {
            currentRotation *= Quaternion.Euler(0, 90, 0);
            if (currentGhost != null)
            {
                currentGhost.transform.rotation = currentRotation;
            }
        }
        
        // Next/Previous item
        if (Input.GetKeyDown(nextItemKey) && buildablePrefabs.Length > 0)
        {
            currentPrefabIndex = (currentPrefabIndex + 1) % buildablePrefabs.Length;
            currentBuildTargetPrefab = buildablePrefabs[currentPrefabIndex];
            CreateGhost(currentBuildTargetPrefab);
        }
        
        if (Input.GetKeyDown(previousItemKey) && buildablePrefabs.Length > 0)
        {
            currentPrefabIndex = (currentPrefabIndex - 1 + buildablePrefabs.Length) % buildablePrefabs.Length;
            currentBuildTargetPrefab = buildablePrefabs[currentPrefabIndex];
            CreateGhost(currentBuildTargetPrefab);
        }
        
        // Place and Remove objects - CHECK FOR UI INTERACTION FIRST
        if (Input.GetMouseButtonDown(0))
        {
            // Don't place items if clicking on UI elements
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return; // Skip building when clicking UI
            }
            
            Vector2Int hoveredCell = gridController.GetCurrentHoveredCell();
            PlaceItem(hoveredCell.x, hoveredCell.y);
        }
        else if (Input.GetMouseButtonDown(1))
        {
            // Also prevent right-click removal when clicking UI
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            
            Vector2Int hoveredCell = gridController.GetCurrentHoveredCell();
            RemoveItem(hoveredCell.x, hoveredCell.y);
        }
    }
    
    void UpdateGhostPosition()
    {
        if (currentGhost == null) return;
        
        Vector2Int hoveredCell = gridController.GetCurrentHoveredCell();
        Vector3 cellCenter = gridController.GetCellCenterFromTexture(hoveredCell.x, hoveredCell.y);
        
        currentGhost.transform.position = cellCenter;
        
        // Update ghost visibility based on placement validity
        bool isValidPlacement = IsValidPlacement(hoveredCell.x, hoveredCell.y);
        
        // Change ghost material color based on validity
        foreach (Renderer renderer in currentGhost.GetComponentsInChildren<Renderer>())
        {
            // Apply translucent green for valid placement, red for invalid
            renderer.material.color = isValidPlacement ? 
                new Color(0, 1, 0, 0.5f) : 
                new Color(1, 0, 0, 0.5f);
        }
    }
    
    void CreateGhost(GameObject prefab)
    {
        if (currentGhost != null)
        {
            Destroy(currentGhost);
        }
        
        if (prefab == null) return;
        
        currentGhost = Instantiate(prefab);
        currentGhost.name = "BuildGhost";
        
        // Apply ghost material to all renderers
        ApplyGhostMaterial(currentGhost);
        
        // Set rotation
        currentGhost.transform.rotation = currentRotation;
    }
    
    void ApplyGhostMaterial(GameObject obj)
    {
        if (ghostMaterial == null)
        {
            Debug.LogWarning("Ghost material not assigned! Creating a simple translucent material.");
            ghostMaterial = new Material(Shader.Find("Standard"));
            ghostMaterial.color = new Color(0, 1, 0, 0.5f);
            ghostMaterial.SetFloat("_Mode", 3); // Transparent mode
            ghostMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            ghostMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            ghostMaterial.SetInt("_ZWrite", 0);
            ghostMaterial.DisableKeyword("_ALPHATEST_ON");
            ghostMaterial.EnableKeyword("_ALPHABLEND_ON");
            ghostMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            ghostMaterial.renderQueue = 3000;
        }
        
        foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>())
        {
            Material ghostMatInstance = new Material(ghostMaterial);
            renderer.material = ghostMatInstance;
        }
    }
    
    public void SetBuildTarget(StructureData data)
    {
        if (data == null || data.prefab == null)
        {
            Debug.LogError("Invalid StructureData or prefab is null.");
            return;
        }
        
        Debug.Log($"Setting build target to: {data.structureName}");
        currentBuildTargetPrefab = data.prefab;
        
        if (isBuildModeActive)
        {
            CreateGhost(currentBuildTargetPrefab);
        }
    }
    
    public void SetBuildTarget(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("Cannot set null prefab as build target.");
            return;
        }
        
        currentBuildTargetPrefab = prefab;
        
        if (isBuildModeActive)
        {
            CreateGhost(currentBuildTargetPrefab);
        }
    }
    
    private List<Vector2Int> GetStructureFootprint(GameObject obj)
    {
        List<Vector2Int> occupiedCells = new List<Vector2Int>();
        
        // Get the object's bounds in world space
        Renderer renderer = obj.GetComponentInChildren<Renderer>();
        if (renderer == null) return occupiedCells;
        
        // Get bounds in world space (handles rotation correctly)
        Bounds bounds = renderer.bounds;
        
        // Shrink bounds slightly to prevent edge cases
        bounds.Expand(-0.1f);
        
        // Convert to grid coordinates
        Vector2Int bottomLeft = gridController.WorldToGridCoords(bounds.min);
        Vector2Int topRight = gridController.WorldToGridCoords(bounds.max);
        
        // Loop through all cells
        for (int x = bottomLeft.x; x <= topRight.x; x++)
        {
            for (int y = bottomLeft.y; y <= topRight.y; y++)
            {
                if (gridController.IsValidCell(x, y))
                {
                    // Get cell center in world space
                    Vector3 cellCenter = gridController.GetCellCenterFromTexture(x, y);
                    
                    // Only occupy if cell center is within bounds
                    if (bounds.Contains(new Vector3(cellCenter.x, bounds.center.y, cellCenter.z)))
                    {
                        occupiedCells.Add(new Vector2Int(x, y));
                    }
                }
            }
        }
        
        return occupiedCells;
    }
    
    bool IsValidPlacement(int x, int y)
    {
        if (!gridController.IsValidCell(x, y)) return false;
        if (currentBuildTargetPrefab == null) return false;
        
        // Check if shop is actually visible/active in the scene
        bool shopOpen = (shopPanelUI != null && shopPanelUI.gameObject.activeSelf);
        
        if (!shopOpen) return false;
        
        // Create a temporary object to calculate footprint
        GameObject tempObj = Instantiate(currentBuildTargetPrefab, 
            gridController.GetCellCenterFromTexture(x, y), 
            currentRotation);
            
        List<Vector2Int> footprint = GetStructureFootprint(tempObj);
        Destroy(tempObj);
        
        // Check if all cells in footprint are valid for placement
        foreach (Vector2Int cell in footprint)
        {
            if (!gridController.IsValidCell(cell.x, cell.y))
                return false;
                
            GridCell gridCell = gridController.GetCell(cell.x, cell.y);
            if (gridCell == null || !gridCell.flags.isOwned || gridCell.flags.isOccupied || gridCell.flags.isObstacle)
                return false;
        }
        
        return true;
    }
    
    void PlaceItem(int x, int y)
    {
        if (!IsValidPlacement(x, y)) return;
        
        Vector3 cellCenter = gridController.GetCellCenterFromTexture(x, y);
        
        // Create the actual item to place
        GameObject placedItem = Instantiate(currentBuildTargetPrefab, cellCenter, currentRotation);
        placedItem.name = $"Item_{x}_{y}";
        
        // Mark cells as occupied
        List<Vector2Int> footprint = GetStructureFootprint(placedItem);
        foreach (Vector2Int cell in footprint)
        {
            gridController.SetCellOccupied(cell.x, cell.y, true);
        }
        
        // Update grid texture
        gridController.UpdateGridTexture();
    }
    
    void RemoveItem(int x, int y)
    {
        if (!gridController.IsValidCell(x, y)) return;
        
        GridCell cell = gridController.GetCell(x, y);
        if (cell == null || !cell.flags.isOccupied) return;
        
        // Find the object at this position
        string itemName = $"Item_{x}_{y}";
        GameObject placedItem = GameObject.Find(itemName);
        
        if (placedItem != null)
        {
            // Get the footprint before destroying
            List<Vector2Int> footprint = GetStructureFootprint(placedItem);
            
            // Destroy the object
            Destroy(placedItem);
            
            // Mark cells as unoccupied
            foreach (Vector2Int pos in footprint)
            {
                gridController.SetCellOccupied(pos.x, pos.y, false);
            }
            
            // Update grid texture
            gridController.UpdateGridTexture();
        }
    }
}