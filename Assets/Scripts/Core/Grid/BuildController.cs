using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using FarmDefender.Core.AI.FlowField;
using System.Collections;

public class BuildController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridController gridController;
    [SerializeField] private FlowFieldManager flowFieldManager;
    [SerializeField] private OwnershipController ownershipController;
    [SerializeField] private GridMonitor gridMonitor;

    [Header("Build Settings")]
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private GameObject[] buildablePrefabs;

    [Header("Input Settings")]
    [SerializeField] private KeyCode rotateKey = KeyCode.R;
    [SerializeField] private KeyCode nextItemKey = KeyCode.N;
    [SerializeField] private KeyCode previousItemKey = KeyCode.P;
    [SerializeField] private KeyCode removeModifierKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode moveKey = KeyCode.M;
    [SerializeField] private KeyCode buyLandKey = KeyCode.LeftShift;

    [Header("UI References")]
    [SerializeField] private RectTransform itemDeleteIcon;

    [Header("Delete Mode Settings")]
    [Tooltip("Position offset from the cursor where the delete icon will appear")]
    [SerializeField] private Vector2 cursorOffset = new Vector2(15f, 15f);

    [Header("Land Ownership")]
    [SerializeField] private bool enableLandBuying = true;

    public Vector2 DeleteIconOffset
    {
        get => cursorOffset;
        set => cursorOffset = value;
    }

    private GameObject currentGhost;
    private GameObject currentBuildTargetPrefab;
    private Structure movingStructure;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private List<Vector2Int> originalFootprint;
    private bool isBuildModeActive = false;
    private bool isMoveModeActive = false;
    private bool isDeleteModeActive = false;
    private bool isInLandBuyMode = false;
    private bool isGhostTemporarilyHidden = false;
    private int currentPrefabIndex = 0;
    private Quaternion currentRotation = Quaternion.identity;
    private ShopPanelUI shopPanelUI;
    private StructureData currentStructureData;
    private bool isSelectedStructure = false;

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

        if (flowFieldManager == null)
            flowFieldManager = FindObjectOfType<FlowFieldManager>();

        if (ownershipController == null)
            ownershipController = FindObjectOfType<OwnershipController>();

        if (gridMonitor == null)
            gridMonitor = FindObjectOfType<GridMonitor>();

        if (gridMonitor == null)
            Debug.LogWarning("GridMonitor not found. Grid changes won't be centrally tracked.");

        shopPanelUI = FindObjectOfType<ShopPanelUI>(true);
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

        if (buildablePrefabs.Length > 0)
            currentBuildTargetPrefab = buildablePrefabs[0];

        if (itemDeleteIcon != null && itemDeleteIcon.GetComponent<Graphic>() != null)
            itemDeleteIcon.GetComponent<Graphic>().raycastTarget = false;

        Debug.Log($"BuildController started. Grid controller reference: {(gridController != null ? "Valid" : "NULL")}");
        Debug.Log($"Available prefabs: {buildablePrefabs.Length}");
    }

    void OnDestroy()
    {
        if (shopPanelUI != null)
        {
            shopPanelUI.OnShopOpened.RemoveListener(HandleShopOpened);
            shopPanelUI.OnShopClosed.RemoveListener(HandleShopClosed);
        }
    }

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
        if (!isBuildModeActive && !isMoveModeActive) return;

        bool deleteKeyPressed = Input.GetKey(removeModifierKey);
        if (deleteKeyPressed != isDeleteModeActive)
        {
            isDeleteModeActive = deleteKeyPressed;
            if (isDeleteModeActive)
            {
                if (currentGhost != null) currentGhost.SetActive(false);
                if (itemDeleteIcon != null) itemDeleteIcon.gameObject.SetActive(true);
            }
            else
            {
                if (currentGhost != null && !isGhostTemporarilyHidden) currentGhost.SetActive(true);
                if (itemDeleteIcon != null) itemDeleteIcon.gameObject.SetActive(false);
            }
        }

        if (isDeleteModeActive && itemDeleteIcon != null && itemDeleteIcon.gameObject.activeSelf)
            UpdateDeleteIconPosition();

        HandleBuildInput();
        if (!isDeleteModeActive && currentGhost != null && !isMoveModeActive)
            UpdateGhostPosition();
        if (isMoveModeActive && currentGhost != null)
            UpdateGhostPositionForMove();
    }

    public void EnableBuildMode()
    {
        isBuildModeActive = true;
        isMoveModeActive = false;
        gridController.ShowGrid();
        if (currentBuildTargetPrefab != null && currentGhost == null)
            CreateGhost(currentBuildTargetPrefab);
        if (flowFieldManager != null)
            flowFieldManager.SetBuildModeActive(true);
    }

    public void DisableBuildMode()
    {
        isBuildModeActive = false;
        isMoveModeActive = false;
        gridController.HideGrid();
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }
        movingStructure = null;
        if (flowFieldManager != null)
        {
            flowFieldManager.SetBuildModeActive(false);
            Debug.Log("Build mode deactivated - notified flow field manager");
        }
        if (itemDeleteIcon != null)
            itemDeleteIcon.gameObject.SetActive(false);
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

    public void ToggleMoveMode()
    {
        if (isMoveModeActive)
        {
            CancelMove();
        }
        else
        {
            isMoveModeActive = true;
            isBuildModeActive = false;
            if (currentGhost != null)
            {
                Destroy(currentGhost);
                currentGhost = null;
            }
            Debug.Log("Entered Move Mode: Click a structure to select it for moving.");
        }
    }

    public void StartMoveModeForStructure(Structure structure)
    {
        if (structure == null)
        {
            Debug.LogWarning("Cannot start move mode: Structure is null");
            return;
        }

        isMoveModeActive = true;
        isBuildModeActive = false;
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }

        movingStructure = structure;
        originalPosition = structure.transform.position;
        originalRotation = structure.transform.rotation;
        originalFootprint = GetStructureFootprint(structure.gameObject);
        currentBuildTargetPrefab = structure.structureData?.prefab;
        currentRotation = originalRotation;

        if (currentBuildTargetPrefab == null)
        {
            Debug.LogWarning($"No prefab assigned to {structure.GetStructureName()}'s StructureData. Cannot create ghost.");
            CancelMove();
            return;
        }

        CreateGhost(currentBuildTargetPrefab);
        structure.UnregisterFromGrid();
        structure.gameObject.SetActive(false);
        gridController.ShowGrid();
        Debug.Log($"Started move mode for {structure.GetStructureName()}.");
    }

    public void HideGhostTemporarily()
    {
        if (currentGhost != null && currentGhost.activeSelf)
        {
            isGhostTemporarilyHidden = true;
            currentGhost.SetActive(false);
        }
        if (itemDeleteIcon != null && isDeleteModeActive)
            itemDeleteIcon.gameObject.SetActive(false);
    }

    public void RestoreGhost()
    {
        isGhostTemporarilyHidden = false;
        if (currentGhost != null && !isDeleteModeActive)
            currentGhost.SetActive(true);
        if (itemDeleteIcon != null && isDeleteModeActive && !isGhostTemporarilyHidden)
            itemDeleteIcon.gameObject.SetActive(true);
    }

    void HandleBuildInput()
    {
        if (isGhostTemporarilyHidden) return;

        isInLandBuyMode = enableLandBuying && currentBuildTargetPrefab == null;

        if (Input.GetKeyDown(moveKey) && !isDeleteModeActive)
        {
            ToggleMoveMode();
            return;
        }

        if (isMoveModeActive)
        {
            if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                if (movingStructure == null)
                {
                    SelectStructureToMove();
                }
                else
                {
                    Vector2Int hoveredCell = GetGridCellUnderCursor(true);
                    PlaceMovedStructure(hoveredCell.x, hoveredCell.y);
                }
            }
            else if (Input.GetMouseButtonDown(1))
            {
                CancelMove();
            }
            else if (Input.GetKeyDown(rotateKey))
            {
                currentRotation *= Quaternion.Euler(0, 90, 0);
                if (currentGhost != null)
                    currentGhost.transform.rotation = currentRotation;
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(1) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                CancelCurrentBuilding();
            }
            else if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                if (isDeleteModeActive)
                {
                    if (TryRemoveStructureByRaycast()) return;
                    Vector2Int hoveredCell = gridController.GetCurrentHoveredCell();
                    RemoveItem(hoveredCell.x, hoveredCell.y);
                }
                else if (currentBuildTargetPrefab == null || isInLandBuyMode)
                {
                    if (ownershipController != null)
                    {
                        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        if (Physics.Raycast(ray, out RaycastHit hit))
                            ownershipController.BuyLandAtPosition(hit.point);
                    }
                }
                else if (currentBuildTargetPrefab != null)
                {
                    Vector2Int hoveredCell = GetGridCellUnderCursor(true);
                    PlaceItem(hoveredCell.x, hoveredCell.y);
                }
            }
            else if (Input.GetKeyDown(rotateKey))
            {
                currentRotation *= Quaternion.Euler(0, 90, 0);
                if (currentGhost != null)
                    currentGhost.transform.rotation = currentRotation;
            }
            else if (Input.GetKeyDown(nextItemKey) && buildablePrefabs.Length > 0)
            {
                currentPrefabIndex = (currentPrefabIndex + 1) % buildablePrefabs.Length;
                currentBuildTargetPrefab = buildablePrefabs[currentPrefabIndex];
                CreateGhost(currentBuildTargetPrefab);
            }
            else if (Input.GetKeyDown(previousItemKey) && buildablePrefabs.Length > 0)
            {
                currentPrefabIndex = (currentPrefabIndex - 1 + buildablePrefabs.Length) % buildablePrefabs.Length;
                currentBuildTargetPrefab = buildablePrefabs[currentPrefabIndex];
                CreateGhost(currentBuildTargetPrefab);
            }
        }
    }

    void SelectStructureToMove()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        int layerMask = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
        if (Physics.Raycast(ray, out hit, 1000f, layerMask))
        {
            Transform hitTransform = hit.transform;
            while (hitTransform != null)
            {
                if (hitTransform.name.StartsWith("Item_"))
                {
                    Structure structure = hitTransform.GetComponent<Structure>();
                    if (structure != null)
                    {
                        movingStructure = structure;
                        originalPosition = structure.transform.position;
                        originalRotation = structure.transform.rotation;
                        originalFootprint = GetStructureFootprint(structure.gameObject);
                        currentBuildTargetPrefab = structure.structureData?.prefab;
                        currentRotation = originalRotation;
                        CreateGhost(currentBuildTargetPrefab);
                        structure.UnregisterFromGrid();
                        structure.gameObject.SetActive(false);
                        Debug.Log($"Selected {structure.GetStructureName()} for moving.");
                        return;
                    }
                }
                hitTransform = hitTransform.parent;
            }
        }
        Debug.Log("No structure selected for moving.");
    }

    void PlaceMovedStructure(int x, int y)
    {
        if (!IsValidPlacement(x, y)) return;

        Vector3 cellCenter = gridController.GetCellCenterFromTexture(x, y);
        movingStructure.transform.position = cellCenter;
        movingStructure.transform.rotation = currentRotation;
        movingStructure.gameObject.SetActive(true);

        List<Vector2Int> newFootprint = GetStructureFootprint(movingStructure.gameObject);
        foreach (Vector2Int cell in newFootprint)
            gridController.SetCellOccupied(cell.x, cell.y, true);

        movingStructure.RegisterWithGrid();
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPlaceSound();
        gridController.UpdateGridTexture();
        if (gridMonitor != null && newFootprint.Count > 0)
            gridMonitor.NotifyMultipleCellsChanged(newFootprint, GridChangeType.Structural);

        movingStructure = null;
        isMoveModeActive = false;
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }
        DisableBuildMode();
        Debug.Log("Structure moved successfully.");
    }

    void CancelMove()
    {
        if (movingStructure != null)
        {
            movingStructure.transform.position = originalPosition;
            movingStructure.transform.rotation = originalRotation;
            movingStructure.gameObject.SetActive(true);
            movingStructure.RegisterWithGrid();
            movingStructure = null;
        }
        isMoveModeActive = false;
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }
        gridController.HideGrid();
        DisableBuildMode();
        Debug.Log("Move cancelled.");
    }

    void UpdateGhostPosition()
    {
        if (currentGhost == null) return;

        if (isInLandBuyMode)
        {
            currentGhost.SetActive(false);
            return;
        }
        else if (!currentGhost.activeSelf && !isGhostTemporarilyHidden && !isDeleteModeActive)
        {
            currentGhost.SetActive(true);
        }

        Vector2Int hoveredCell = GetGridCellUnderCursor(true);
        if (!gridController.IsValidCell(hoveredCell.x, hoveredCell.y))
        {
            currentGhost.SetActive(false);
            return;
        }

        Vector3 cellCenter = gridController.GetCellCenterFromTexture(hoveredCell.x, hoveredCell.y);
        currentGhost.transform.position = cellCenter;

        bool isValidPlacement = IsValidPlacement(hoveredCell.x, hoveredCell.y);
        foreach (Renderer renderer in currentGhost.GetComponentsInChildren<Renderer>())
            renderer.material.color = isValidPlacement ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
    }

    void UpdateGhostPositionForMove()
    {
        if (currentGhost == null || movingStructure == null) return;

        Vector2Int hoveredCell = GetGridCellUnderCursor(true);
        if (!gridController.IsValidCell(hoveredCell.x, hoveredCell.y))
        {
            currentGhost.SetActive(false);
            return;
        }

        Vector3 cellCenter = gridController.GetCellCenterFromTexture(hoveredCell.x, hoveredCell.y);
        currentGhost.transform.position = cellCenter;
        currentGhost.SetActive(true);

        bool isValidPlacement = IsValidPlacement(hoveredCell.x, hoveredCell.y);
        foreach (Renderer renderer in currentGhost.GetComponentsInChildren<Renderer>())
            renderer.material.color = isValidPlacement ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
    }

    void CreateGhost(GameObject prefab)
    {
        if (currentGhost != null)
            Destroy(currentGhost);

        if (prefab == null) return;

        currentGhost = Instantiate(prefab);
        currentGhost.name = "BuildGhost";
        ApplyGhostMaterial(currentGhost);
        currentGhost.transform.rotation = currentRotation;
    }

    void ApplyGhostMaterial(GameObject obj)
    {
        if (ghostMaterial == null)
        {
            Debug.LogWarning("Ghost material not assigned! Creating a simple translucent material.");
            ghostMaterial = new Material(Shader.Find("Standard"));
            ghostMaterial.color = new Color(0, 1, 0, 0.5f);
            ghostMaterial.SetFloat("_Mode", 3);
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

        if (MoneyManager.Instance != null && !MoneyManager.Instance.CanAfford(data.cost))
        {
            Debug.Log($"Cannot afford {data.structureName} (Cost: {data.cost})");
            return;
        }

        Debug.Log($"Setting build target to: {data.structureName}");
        currentBuildTargetPrefab = data.prefab;
        currentStructureData = data;

        if (isBuildModeActive && !isMoveModeActive)
            CreateGhost(currentBuildTargetPrefab);
    }

    public void SetBuildTarget(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("Cannot set null prefab as build target.");
            return;
        }

        currentBuildTargetPrefab = prefab;

        if (isBuildModeActive && !isMoveModeActive)
            CreateGhost(currentBuildTargetPrefab);
    }

    private List<Vector2Int> GetStructureFootprint(GameObject obj)
    {
        List<Vector2Int> occupiedCells = new List<Vector2Int>();
        Renderer renderer = obj.GetComponentInChildren<Renderer>();
        if (renderer == null) return occupiedCells;

        Bounds bounds = renderer.bounds;
        bounds.Expand(-0.1f);

        Vector2Int bottomLeft = gridController.WorldToGridCoords(bounds.min);
        Vector2Int topRight = gridController.WorldToGridCoords(bounds.max);

        for (int x = bottomLeft.x; x <= topRight.x; x++)
        {
            for (int y = bottomLeft.y; y <= topRight.y; y++)
            {
                if (gridController.IsValidCell(x, y))
                {
                    Vector3 cellCenter = gridController.GetCellCenterFromTexture(x, y);
                    if (bounds.Contains(new Vector3(cellCenter.x, bounds.center.y, cellCenter.z)))
                        occupiedCells.Add(new Vector2Int(x, y));
                }
            }
        }

        return occupiedCells;
    }

    bool IsValidPlacement(int x, int y)
    {
        if (!gridController.IsValidCell(x, y) || currentBuildTargetPrefab == null) return false;
        bool shopOpen = (shopPanelUI != null && shopPanelUI.gameObject.activeSelf && !isMoveModeActive);
        if (!shopOpen && !isMoveModeActive) return false;

        GameObject tempObj = Instantiate(currentBuildTargetPrefab, gridController.GetCellCenterFromTexture(x, y), currentRotation);
        List<Vector2Int> footprint = GetStructureFootprint(tempObj);
        Destroy(tempObj);

        foreach (Vector2Int cell in footprint)
        {
            if (!gridController.IsValidCell(cell.x, cell.y)) return false;
            GridCell gridCell = gridController.GetCell(cell.x, cell.y);
            if (gridCell == null || !gridCell.flags.isOwned || (gridCell.flags.isOccupied && !originalFootprint.Contains(cell)) || gridCell.flags.isObstacle)
                return false;
        }
        return true;
    }

    void PlaceItem(int x, int y)
    {
        if (!IsValidPlacement(x, y)) return;

        if (currentStructureData != null && MoneyManager.Instance != null && !MoneyManager.Instance.SpendMoney(currentStructureData.cost))
        {
            Debug.Log("Not enough money to place structure");
            return;
        }

        Vector3 cellCenter = gridController.GetCellCenterFromTexture(x, y);
        GameObject placedItem = Instantiate(currentBuildTargetPrefab, cellCenter, currentRotation);
        placedItem.name = $"Item_{x}_{y}";

        Structure structure = placedItem.GetComponent<Structure>();
        if (structure != null)
        {
            structure.SetAllowSelectionAndUI(false);
            StartCoroutine(EnableSelectionAfterRelease(structure));

            SiloStructure silo = structure as SiloStructure;
            if (silo != null)
                InventoryManager.Instance.RegisterSilo(silo);
        }

        List<Vector2Int> footprint = GetStructureFootprint(placedItem);
        foreach (Vector2Int cell in footprint)
            gridController.SetCellOccupied(cell.x, cell.y, true);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPlaceSound();

        gridController.UpdateGridTexture();
        if (gridMonitor != null && footprint.Count > 0)
            gridMonitor.NotifyMultipleCellsChanged(footprint, GridChangeType.Structural);
    }

    private IEnumerator EnableSelectionAfterRelease(Structure structure)
    {
        while (Input.GetMouseButton(0))
            yield return null;
        if (structure != null)
            structure.SetAllowSelectionAndUI(true);
    }

    void RemoveItem(int x, int y)
    {
        if (!gridController.IsValidCell(x, y)) return;
        GridCell cell = gridController.GetCell(x, y);
        if (cell == null || !cell.flags.isOccupied) return;

        string itemName = $"Item_{x}_{y}";
        GameObject placedItem = GameObject.Find(itemName);
        if (placedItem != null)
        {
            Structure structure = placedItem.GetComponent<Structure>();
            if (structure is SiloStructure silo)
                InventoryManager.Instance.UnregisterSilo(silo);

            List<Vector2Int> footprint = GetStructureFootprint(placedItem);
            Destroy(placedItem);
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayRemoveSound();

            foreach (Vector2Int pos in footprint)
                gridController.SetCellOccupied(pos.x, pos.y, false);

            gridController.UpdateGridTexture();
            if (gridMonitor != null && footprint.Count > 0)
                gridMonitor.NotifyMultipleCellsChanged(footprint, GridChangeType.Structural);
        }
    }

    private bool TryRemoveStructureByRaycast()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        int layerMask = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
        if (Physics.Raycast(ray, out hit, 1000f, layerMask))
        {
            if (hit.transform.name == "BuildGhost") return false;

            Transform hitTransform = hit.transform;
            while (hitTransform != null)
            {
                if (hitTransform.name.StartsWith("Item_"))
                {
                    GameObject placedItem = hitTransform.gameObject;
                    List<Vector2Int> footprint = GetStructureFootprint(placedItem);
                    if (footprint.Count == 0)
                    {
                        Debug.LogWarning("Structure has empty footprint, trying alternate method");
                        footprint = GetExtendedStructureFootprint(placedItem);
                    }

                    string[] parts = placedItem.name.Split('_');
                    if (parts.Length >= 3 && int.TryParse(parts[1], out int gridX) && int.TryParse(parts[2], out int gridY))
                    {
                        Structure structure = placedItem.GetComponent<Structure>();
                        if (structure is SiloStructure silo)
                            InventoryManager.Instance.UnregisterSilo(silo);

                        foreach (Vector2Int pos in footprint)
                        {
                            if (gridController.IsValidCell(pos.x, pos.y))
                                gridController.SetCellOccupied(pos.x, pos.y, false);
                        }

                        Destroy(placedItem);
                        if (AudioManager.Instance != null)
                            AudioManager.Instance.PlayRemoveSound();

                        gridController.UpdateGridTexture();
                        if (gridMonitor != null && footprint.Count > 0)
                            gridMonitor.NotifyMultipleCellsChanged(footprint, GridChangeType.Structural);

                        return true;
                    }
                }
                hitTransform = hitTransform.parent;
            }
        }
        return false;
    }

    private List<Vector2Int> GetExtendedStructureFootprint(GameObject obj)
    {
        List<Vector2Int> occupiedCells = new List<Vector2Int>();
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return occupiedCells;

        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            combinedBounds.Encapsulate(renderers[i].bounds);

        combinedBounds.Expand(0.1f);
        Vector2Int bottomLeft = gridController.WorldToGridCoords(combinedBounds.min);
        Vector2Int topRight = gridController.WorldToGridCoords(combinedBounds.max);

        for (int x = bottomLeft.x - 1; x <= topRight.x + 1; x++)
        {
            for (int y = bottomLeft.y - 1; y <= topRight.y + 1; y++)
            {
                if (gridController.IsValidCell(x, y))
                {
                    GridCell cell = gridController.GetCell(x, y);
                    if (cell != null && cell.flags.isOccupied)
                        occupiedCells.Add(new Vector2Int(x, y));
                }
            }
        }

        Debug.Log($"Found {occupiedCells.Count} occupied cells in extended footprint");
        return occupiedCells;
    }

    private void UpdateDeleteIconPosition()
    {
        Vector2 mousePosition = Input.mousePosition;
        mousePosition += cursorOffset;
        itemDeleteIcon.position = mousePosition;
        foreach (Graphic graphic in itemDeleteIcon.GetComponentsInChildren<Graphic>())
            graphic.raycastTarget = false;
    }

    public void CancelCurrentBuilding()
    {
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }
        currentBuildTargetPrefab = null;
    }

    private Vector2Int GetGridCellUnderCursor(bool ignoreStructures = false)
    {
        Vector2Int hoveredCell = gridController.GetCurrentHoveredCell();
        if (ignoreStructures && !isDeleteModeActive)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float gridHeight = 0f;
            if (gridController != null && gridController.TextureHeight > 0)
            {
                for (int x = 0; x < gridController.TextureWidth; x++)
                {
                    for (int y = 0; y < gridController.TextureHeight; y++)
                    {
                        GridCell cell = gridController.GetCell(x, y);
                        if (cell != null)
                        {
                            gridHeight = cell.worldPosition.y;
                            break;
                        }
                    }
                }
            }

            Plane gridPlane = new Plane(Vector3.up, new Vector3(0, gridHeight, 0)); // Fixed typo
            float distance;
            if (gridPlane.Raycast(ray, out distance))
            {
                Vector3 hitPoint = ray.GetPoint(distance);
                hoveredCell = gridController.WorldToGridCoords(hitPoint);
            }
        }
        return hoveredCell;
    }

    public void SetRemovalModifierKey(KeyCode newKey)
    {
        removeModifierKey = newKey;
        Debug.Log($"Removal modifier key changed to: {newKey}");
    }

    public KeyCode GetRemovalModifierKey()
    {
        return removeModifierKey;
    }

    public void HideDeleteIcon()
    {
        itemDeleteIcon.gameObject.SetActive(false);
    }
}