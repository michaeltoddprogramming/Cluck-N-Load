using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

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
    [SerializeField] private KeyCode removeModifierKey = KeyCode.LeftControl; // New: Key to hold for removal
    
    [Header("UI References")]
    [SerializeField] private RectTransform itemDeleteIcon; // Reference to your red cross UI element
    
    [Header("Delete Mode Settings")]
    [Tooltip("Position offset from the cursor where the delete icon will appear")]
    [SerializeField] private Vector2 cursorOffset = new Vector2(15f, 15f); // Now exposed in Inspector
    
    [Header("Land Ownership")]
    [SerializeField] private OwnershipController ownershipController;
    [SerializeField] private bool enableLandBuying = true;
    [SerializeField] private KeyCode buyLandKey = KeyCode.LeftShift; // Hold shift to buy land
    private bool isInLandBuyMode = false;
    
    // Add this property for programmatic access
    public Vector2 DeleteIconOffset
    {
        get { return cursorOffset; }
        set { cursorOffset = value; }
    }
    
    private GameObject currentGhost;
    private GameObject currentBuildTargetPrefab;
    private bool isBuildModeActive = false;
    private int currentPrefabIndex = 0;
    private Quaternion currentRotation = Quaternion.identity;
    
    // References to shop UI component
    private ShopPanelUI shopPanelUI;
    
    private bool isGhostTemporarilyHidden = false;
    private bool isDeleteModeActive = false;
    private StructureData currentStructureData;

    
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
        
        // Make sure the delete icon doesn't block raycasts
        if (itemDeleteIcon != null && itemDeleteIcon.GetComponent<Graphic>() != null)
        {
            itemDeleteIcon.GetComponent<Graphic>().raycastTarget = false;
        }
        
        // Find ownership controller if not assigned
        if (ownershipController == null)
            ownershipController = FindObjectOfType<OwnershipController>();
        
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
        
        // Check for delete mode toggle (Ctrl key)
        bool deleteKeyPressed = Input.GetKey(removeModifierKey);
        
        // If delete mode state has changed
        if (deleteKeyPressed != isDeleteModeActive)
        {
            isDeleteModeActive = deleteKeyPressed;
            
            // Toggle visibility of ghost and delete icon
            if (isDeleteModeActive)
            {
                // Entering delete mode
                if (currentGhost != null)
                {
                    currentGhost.SetActive(false);
                }
                
                // Show delete icon
                if (itemDeleteIcon != null)
                {
                    itemDeleteIcon.gameObject.SetActive(true);
                }
            }
            else
            {
                // Exiting delete mode
                if (currentGhost != null && !isGhostTemporarilyHidden)
                {
                    currentGhost.SetActive(true);
                }
                
                // Hide delete icon
                if (itemDeleteIcon != null)
                {
                    itemDeleteIcon.gameObject.SetActive(false);
                }
            }
        }
        
        // Update delete icon position if active
        if (isDeleteModeActive && itemDeleteIcon != null && itemDeleteIcon.gameObject.activeSelf)
        {
            UpdateDeleteIconPosition();
        }
        
        HandleBuildInput();
        
        // Only update ghost position if not in delete mode
        if (!isDeleteModeActive && currentGhost != null)
        {
            UpdateGhostPosition();
        }
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
        
        // Also hide delete icon if visible
        if (itemDeleteIcon != null && isDeleteModeActive)
        {
            itemDeleteIcon.gameObject.SetActive(false);
        }
    }
    
    // Called from ShopPanelUI when pointer exits the UI
    public void RestoreGhost()
    {
        isGhostTemporarilyHidden = false;
        
        // Only show ghost if we're not in delete mode
        if (currentGhost != null && !isDeleteModeActive)
        {
            currentGhost.SetActive(true);
        }
        
        // Restore delete icon if in delete mode
        if (itemDeleteIcon != null && isDeleteModeActive && !isGhostTemporarilyHidden)
        {
            itemDeleteIcon.gameObject.SetActive(true);
        }
    }
    
    void HandleBuildInput()
    {
        // Skip input handling if ghost is temporarily hidden (hovering over UI)
        if (isGhostTemporarilyHidden) return;
        
        // We're in land buying mode if no building is selected
        isInLandBuyMode = enableLandBuying && currentBuildTargetPrefab == null;
        
        // Right-click to cancel selected building
        if (Input.GetMouseButtonDown(1))
        {
            // Don't process if clicking on UI elements
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;
                
            // Cancel the current building selection
            if (currentGhost != null)
            {
                Destroy(currentGhost);
                currentGhost = null;
                currentBuildTargetPrefab = null;
                
                Debug.Log("Cancelled building selection");
                return;
            }
        }
        
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
        
        // Left-click for placement or land buying
        if (Input.GetMouseButtonDown(0))
        {
            // Don't process if clicking on UI elements
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;
                
            // Check if modifier key is pressed for removal
            if (Input.GetKey(removeModifierKey))
            {
                // Try to remove structure by direct click
                if (TryRemoveStructureByRaycast())
                {
                    // Successfully removed structure by direct click
                    return;
                }
                
                // Fallback to grid-based removal if no structure was hit
                Vector2Int hoveredCell = gridController.GetCurrentHoveredCell();
                RemoveItem(hoveredCell.x, hoveredCell.y);
            }
            else if (currentBuildTargetPrefab == null || isInLandBuyMode)
            {
                // Buy land at the clicked position when no building is selected
                if (ownershipController != null)
                {
                    // Get the position under the mouse
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        ownershipController.BuyLandAtPosition(hit.point);
                    }
                }
            }
            else if (currentBuildTargetPrefab != null)
            {
                // Normal placement with Left Click when a building is selected
                Vector2Int hoveredCell = gridController.GetCurrentHoveredCell();
                PlaceItem(hoveredCell.x, hoveredCell.y);
            }
        }
    }
    
    // Public method to change the removal modifier key at runtime
    public void SetRemovalModifierKey(KeyCode newKey)
    {
        removeModifierKey = newKey;
        Debug.Log($"Removal modifier key changed to: {newKey}");
    }
    
    // Get the current removal modifier key
    public KeyCode GetRemovalModifierKey()
    {
        return removeModifierKey;
    }
    
    void UpdateGhostPosition()
    {
        if (currentGhost == null) return;
        
        // Hide ghost if in land buying mode
        if (isInLandBuyMode)
        {
            currentGhost.SetActive(false);
            return;
        }
        else if (!currentGhost.activeSelf && !isGhostTemporarilyHidden && !isDeleteModeActive)
        {
            currentGhost.SetActive(true);
        }
        
        Vector2Int hoveredCell = gridController.GetCurrentHoveredCell();
        
        // Add bounds checking before getting the cell center
        if (!gridController.IsValidCell(hoveredCell.x, hoveredCell.y))
        {
            // Optional: Hide ghost when mouse is outside valid grid area
            currentGhost.SetActive(false);
            return;
        }
        
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
    
    // Check if player can afford this structure
    if (MoneyManager.Instance != null && !MoneyManager.Instance.CanAfford(data.cost))
    {
        Debug.Log($"Cannot afford {data.structureName} (Cost: {data.cost})");
        
        // Optional: Show a message to the player that they can't afford it
        // UIManager.Instance.ShowMessage($"Not enough {MoneyManager.Instance.GetCurrencyName()} to build {data.structureName}");
        return;
    }
    
    Debug.Log($"Setting build target to: {data.structureName}");
    currentBuildTargetPrefab = data.prefab;
    currentStructureData = data;
    
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

        // Check again if the player can afford it (could have changed since selection)
    if (currentStructureData != null && 
        MoneyManager.Instance != null && 
        !MoneyManager.Instance.SpendMoney(currentStructureData.cost))
    {
        Debug.Log("Not enough money to place structure");
        return;
    }

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
    
    // Enhanced method to check if mouse is directly over a placed structure
    private bool TryRemoveStructureByRaycast()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        // Skip this object if it's the ghost to avoid raycast issues
        int layerMask = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
        
        if (Physics.Raycast(ray, out hit, 1000f, layerMask))
        {
            // Skip if we hit the ghost
            if (hit.transform.name == "BuildGhost") return false;
            
            // Debug.Log($"Raycast hit: {hit.transform.name} at distance {hit.distance}");
            
            // Search upward in hierarchy to find the parent structure
            Transform hitTransform = hit.transform;
            while (hitTransform != null)
            {
                // Check if this transform or any parent is a placed structure
                if (hitTransform.name.StartsWith("Item_"))
                {
                    GameObject placedItem = hitTransform.gameObject;
                    // Debug.Log($"Found structure to remove: {placedItem.name}");
                    
                    // Get footprint before destroying
                    List<Vector2Int> footprint = GetStructureFootprint(placedItem);
                    
                    if (footprint.Count == 0)
                    {
                        Debug.LogWarning("Structure has empty footprint, trying alternate method");
                        footprint = GetExtendedStructureFootprint(placedItem);
                    }
                    
                    // Find the grid position - parse from the object name (Item_X_Y)
                    string[] parts = placedItem.name.Split('_');
                    if (parts.Length >= 3 && int.TryParse(parts[1], out int gridX) && int.TryParse(parts[2], out int gridY))
                    {
                        // Make sure to update the grid for all cells occupied by this structure
                        foreach (Vector2Int pos in footprint)
                        {
                            if (gridController.IsValidCell(pos.x, pos.y))
                            {
                                gridController.SetCellOccupied(pos.x, pos.y, false);
                            }
                        }
                        
                        // Destroy the object
                        Destroy(placedItem);
                        
                        // Update grid texture
                        gridController.UpdateGridTexture();
                        
                        return true; // Successfully removed
                    }
                }
                
                // Move up the hierarchy
                hitTransform = hitTransform.parent;
            }
        }
        
        return false; // Nothing found to remove
    }
    
    // Alternative method to get structure footprint that's more thorough
    private List<Vector2Int> GetExtendedStructureFootprint(GameObject obj)
    {
        List<Vector2Int> occupiedCells = new List<Vector2Int>();
        
        // Get all renderers (in case there are multiple parts)
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return occupiedCells;
        
        // Create a combined bounds
        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }
        
        // Add a small margin to ensure we catch all cells
        combinedBounds.Expand(0.1f);
        
        // Convert to grid coordinates
        Vector2Int bottomLeft = gridController.WorldToGridCoords(combinedBounds.min);
        Vector2Int topRight = gridController.WorldToGridCoords(combinedBounds.max);
        
        // Debug info
        Debug.Log($"Structure bounds: min={combinedBounds.min}, max={combinedBounds.max}");
        Debug.Log($"Grid coords: bottomLeft={bottomLeft}, topRight={topRight}");
        
        // Loop through all potentially affected cells
        for (int x = bottomLeft.x - 1; x <= topRight.x + 1; x++)
        {
            for (int y = bottomLeft.y - 1; y <= topRight.y + 1; y++)
            {
                if (gridController.IsValidCell(x, y))
                {
                    // Check if cell is occupied
                    GridCell cell = gridController.GetCell(x, y);
                    if (cell != null && cell.flags.isOccupied)
                    {
                        occupiedCells.Add(new Vector2Int(x, y));
                    }
                }
            }
        }
        
        Debug.Log($"Found {occupiedCells.Count} occupied cells in extended footprint");
        return occupiedCells;
    }
    
    private void UpdateDeleteIconPosition()
    {
        // Get current mouse position
        Vector2 mousePosition = Input.mousePosition;
        
        // Apply offset so icon doesn't cover what we're pointing at
        mousePosition += cursorOffset;
        
        // Set the position of the delete icon to follow cursor
        itemDeleteIcon.position = mousePosition;
        
        // Make sure all graphics in the delete icon hierarchy don't block raycasts
        foreach (Graphic graphic in itemDeleteIcon.GetComponentsInChildren<Graphic>())
        {
            graphic.raycastTarget = false;
        }
    }
    
    // Add this method to provide a public way to cancel the current building
    public void CancelCurrentBuilding()
    {
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }
        
        currentBuildTargetPrefab = null;
    }

    public void HideDeleteIcon()
    {
        itemDeleteIcon.gameObject.SetActive(false);
    }
}