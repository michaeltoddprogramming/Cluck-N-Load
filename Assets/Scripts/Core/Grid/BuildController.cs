using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.VFX;


public class BuildController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridController gridController;
    // [SerializeField] private FlowFieldManager flowFieldManager;
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

    [Header("UI References")]
    [SerializeField] private RectTransform itemDeleteIcon;
    [SerializeField] public GameObject dustPoof;
    [SerializeField] private CanvasGroup buildControlsPanelGroup;

    [Header("Delete Mode Settings")]
    [SerializeField] private Vector2 cursorOffset = new Vector2(15f, 15f);
    [SerializeField] private float moneyBackAfterDeletePercentage = 0.5f;



    [Header("Synergy Visualization")]
    [SerializeField] private GameObject rangeIndicatorPrefab;
    [SerializeField] private Material validSynergyMaterial;
    [SerializeField] private Material potentialSynergyMaterial;
    [SerializeField] private Material invalidSynergyMaterial;
    [SerializeField] private float synergyIndicatorHeight = 0.1f;
    [SerializeField] private GameObject synergyLineRendererPrefab;
    [SerializeField] private Canvas worldSpaceCanvas;

    [Header("Performance Settings")]
    [SerializeField] private bool enableSynergyVisuals = true;
    [SerializeField] private int maxSynergyLines = 10;
    [SerializeField] private bool snapGhostToNearestValidCellWhenOutside = true;

    private List<GameObject> synergyIndicators = new List<GameObject>();
    private List<LineRenderer> synergyLines = new List<LineRenderer>();
    private List<GameObject> activeSynergyLines = new List<GameObject>();
    private bool isHousePlaced;

    private GameObject currentGhost;
    private GameObject currentBuildTargetPrefab;
    private Structure movingStructure;
    private Vector3 originalPosition;
    private bool personallyHidden = false;
    private Quaternion originalRotation;
    private List<Vector2Int> originalFootprint;
    private bool isBuildModeActive;
    private bool isMoveModeActive;
    private bool isDeleteModeActive;
    private bool isGhostTemporarilyHidden;
    private int currentPrefabIndex;
    private Quaternion currentRotation = Quaternion.identity;
    private ShopPanelUI shopPanelUI;
    private StructureData currentStructureData;

    [SerializeField] private GridDataGenerator gridDataGenerator;



    void Start()
    {
        gridController = gridController ?? FindFirstObjectByType<GridController>();
        if (gridController == null)
        {
            Debug.LogError("GridController not found. BuildController cannot function.");
            enabled = false;
            return;
        }

        // ADD THIS - Initialize gridDataGenerator
        if (gridDataGenerator == null)
        {
            gridDataGenerator = FindFirstObjectByType<GridDataGenerator>();
            if (gridDataGenerator == null)
            {
                Debug.LogError("GridDataGenerator not found. BuildController cannot function.");
                enabled = false;
                return;
            }
        }

        if (ownershipController == null)
            ownershipController = FindFirstObjectByType<OwnershipController>();

        if (gridMonitor == null)
            gridMonitor = FindFirstObjectByType<GridMonitor>();

        shopPanelUI = FindFirstObjectByType<ShopPanelUI>(FindObjectsInactive.Include);
        if (shopPanelUI != null)
        {
            shopPanelUI.OnShopOpened.AddListener(HandleShopOpened);
            shopPanelUI.OnShopClosed.AddListener(HandleShopClosed);
        }
        if (buildablePrefabs.Length > 0) currentBuildTargetPrefab = buildablePrefabs[0];
        if (itemDeleteIcon != null) itemDeleteIcon.GetComponent<Graphic>().raycastTarget = false;
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
        // Prevent infinite loops
        if (isBuildModeActive) return;
        
        Debug.Log("HandleShopOpened called - showing grid and enabling build mode");
        gridController.ShowGrid();
        EnableBuildMode();
    }

    public void HandleShopClosed()
    {
        Debug.Log("HandleShopClosed called - disabling build mode and hiding grid");
        DisableBuildMode();
        gridController.HideGrid();
    }

    void Update()
    {
        // Debug: Check if we're getting input but build mode isn't active
        if (Input.GetKey(removeModifierKey) && (!isBuildModeActive && !isMoveModeActive))
        {
            Debug.Log("Ctrl pressed but build mode is not active. BuildMode: " + isBuildModeActive + ", MoveMode: " + isMoveModeActive);
        }
        
        if (!isBuildModeActive && !isMoveModeActive)
        {
            // Reset delete mode if build mode is not active
            if (isDeleteModeActive)
            {
                isDeleteModeActive = false;
                if (itemDeleteIcon != null) itemDeleteIcon.gameObject.SetActive(false);
            }
            return;
        }
        
        if (enableSynergyVisuals && activeSynergyLines.Count > maxSynergyLines)
        {
            while (activeSynergyLines.Count > maxSynergyLines)
            {
                var lineToRemove = activeSynergyLines[activeSynergyLines.Count - 1];
                activeSynergyLines.RemoveAt(activeSynergyLines.Count - 1);
                if (lineToRemove != null) Destroy(lineToRemove);
            }
        }

        bool deleteKeyPressed = Input.GetKey(removeModifierKey);
        if (deleteKeyPressed != isDeleteModeActive)
        {
            isDeleteModeActive = deleteKeyPressed;
            Debug.Log($"Delete mode changed to: {isDeleteModeActive}");
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
        if (!isDeleteModeActive && currentGhost != null && !isMoveModeActive) UpdateGhostPosition();
        if (isMoveModeActive && currentGhost != null) UpdateGhostPositionForMove();
    }

    public void EnableBuildMode()
    {
        isBuildModeActive = true;
        isMoveModeActive = false;
        gridController.ShowGrid();
        if (currentBuildTargetPrefab != null && currentGhost == null)
            CreateGhost(currentBuildTargetPrefab);

        if (buildControlsPanelGroup != null)
        {
            buildControlsPanelGroup.alpha = 1f;
            buildControlsPanelGroup.interactable = true;
            buildControlsPanelGroup.blocksRaycasts = true;
        }
        else
        {
            Debug.LogWarning("BuildControlsPanel reference is null!");
        }
    }

    public void DisableBuildMode()
    {
        ClearSynergyVisualization();
        
        // IMPORTANT: If a structure is being moved, place it immediately at current position
        if (movingStructure != null)
        {
            Debug.Log($"DisableBuildMode: Force-placing moving structure {movingStructure.name} at current position due to night transition");
            
            // Get current ghost position (where the structure would be placed)
            Vector3 currentPosition = currentGhost != null ? currentGhost.transform.position : originalPosition;
            Vector2Int gridCoords = gridController.WorldToGridCoords(currentPosition);
            
            // Check if current position is valid, if not use original position as fallback
            if (IsValidPlacement(gridCoords.x, gridCoords.y))
            {
                // Place at current ghost position
                movingStructure.transform.position = currentPosition;
                movingStructure.transform.rotation = currentRotation;
                movingStructure.gameObject.SetActive(true);
                
                // Update grid occupancy for new position
                List<Vector2Int> newFootprint = GetStructureFootprint(movingStructure.gameObject);
                foreach (Vector2Int cell in newFootprint) 
                {
                    gridController.SetCellOccupied(cell.x, cell.y, true);
                }
                
                movingStructure.RegisterWithGrid();
                Debug.Log($"Structure placed at new position: {currentPosition}");
            }
            else
            {
                // Fallback to original position if current position is invalid
                Debug.LogWarning($"Current position invalid, restoring to original position");
                movingStructure.transform.position = originalPosition;
                movingStructure.transform.rotation = originalRotation;
                movingStructure.gameObject.SetActive(true);
                movingStructure.RegisterWithGrid();
            }
            
            movingStructure = null;
        }
        
        isBuildModeActive = false;
        isMoveModeActive = false;
        gridController.HideGrid();
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }
        if (itemDeleteIcon != null)
            itemDeleteIcon.gameObject.SetActive(false);

        if (buildControlsPanelGroup != null)
        {
            buildControlsPanelGroup.alpha = 0f;
            buildControlsPanelGroup.interactable = false;
            buildControlsPanelGroup.blocksRaycasts = false;
        }
    }

    public void ToggleBuildMode()
    {
        if (isBuildModeActive) DisableBuildMode();
        else EnableBuildMode();
    }

    public bool IsBuildModeActive() => isBuildModeActive;
    public bool IsDeleteModeActive() => isDeleteModeActive;

    public void ToggleMoveMode()
    {
        if (isMoveModeActive) CancelMove();
        else
        {
            isMoveModeActive = true;
            isBuildModeActive = false;
            if (currentGhost != null)
            {
                Destroy(currentGhost);
                currentGhost = null;
            }
        }
    }

    public void StartMoveModeForStructure(Structure structure)
    {
        if (structure == null) return;
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
        currentStructureData = structure.structureData;
        currentRotation = originalRotation;

        if (currentBuildTargetPrefab == null)
        {
            CancelMove();
            return;
        }

        CreateGhost(currentBuildTargetPrefab);
        if (structure is BarracksStructure barracks)
        {
            BarracksStructure ghostBarracks = currentGhost.GetComponent<BarracksStructure>() ?? currentGhost.AddComponent<BarracksStructure>();
            var targetField = typeof(BarracksStructure).GetField("targetAnimalType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            targetField?.SetValue(ghostBarracks, barracks.TargetAnimalType);
            ghostBarracks.synergyMinDist = barracks.synergyMinDist;
            ghostBarracks.synergyMaxDist = barracks.synergyMaxDist;
        }
        else if (structure is AnimalStructure animal)
        {
            AnimalStructure ghostAnimal = currentGhost.GetComponent<AnimalStructure>() ?? currentGhost.AddComponent<AnimalStructure>();
            var field = typeof(AnimalStructure).GetField("animalType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(ghostAnimal, animal.GetAnimalType);
        }
        structure.UnregisterFromGrid();
        structure.gameObject.SetActive(false);
        gridController.ShowGrid();
    }

    public void HideGhostTemporarily()
    {
        if (currentGhost != null && currentGhost.activeSelf)
        {
            isGhostTemporarilyHidden = true;
            currentGhost.SetActive(false);
        }
        if (itemDeleteIcon != null && isDeleteModeActive) itemDeleteIcon.gameObject.SetActive(false);
    }

    public void RestoreGhost()
    {
        isGhostTemporarilyHidden = personallyHidden = false;
        if (currentGhost != null && !isDeleteModeActive) currentGhost.SetActive(true);
        if (itemDeleteIcon != null && isDeleteModeActive && !isGhostTemporarilyHidden) itemDeleteIcon.gameObject.SetActive(true);
    }

    void HandleBuildInput()
    {
        if (isGhostTemporarilyHidden) return;


        if (Input.GetKeyDown(moveKey) && !isDeleteModeActive)
        {
            ToggleMoveMode();
            return;
        }

        if (isMoveModeActive)
        {
            if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                if (movingStructure == null) SelectStructureToMove();
                else
                {
                    Vector2Int hoveredCell = GetGridCellUnderCursor(true);
                    PlaceMovedStructure(hoveredCell.x, hoveredCell.y);
                }
            }
            else if (Input.GetMouseButtonDown(1)) CancelMove();
            else if (Input.GetKeyDown(rotateKey))
            {
                currentRotation *= Quaternion.Euler(0, 90, 0);
                if (currentGhost != null) currentGhost.transform.rotation = currentRotation;
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(1) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) CancelCurrentBuilding();
            else if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                if (isDeleteModeActive)
                {
                    Debug.Log("Left click detected in delete mode");
                    if (TryRemoveStructureByRaycast()) 
                    {
                        Debug.Log("Structure removed by raycast");
                        return;
                    }
                    Vector2Int hoveredCell = gridController.GetCurrentHoveredCell();
                    Debug.Log($"Attempting to remove at grid cell: {hoveredCell}");
                    RemoveItem(hoveredCell.x, hoveredCell.y);
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
                if (currentGhost != null) currentGhost.transform.rotation = currentRotation;
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

        // New: Right-click to deselect
        if (Input.GetMouseButtonDown(1) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            SelectionManager selectionManager = FindFirstObjectByType<SelectionManager>();
            if (selectionManager != null)
            {
                selectionManager.DeselectCurrent();
            }
        }
    }

    void SelectStructureToMove()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int layerMask = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, layerMask))
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
                        return;
                    }
                }
                hitTransform = hitTransform.parent;
            }
        }
    }

    void PlaceMovedStructure(int x, int y)
    {
        if (!IsValidPlacement(x, y)) return;
        Vector3 cellCenter = gridController.GetCellCenterFromTexture(x, y);
        movingStructure.transform.position = cellCenter;
        movingStructure.transform.rotation = currentRotation;
        movingStructure.gameObject.SetActive(true);
        List<Vector2Int> newFootprint = GetStructureFootprint(movingStructure.gameObject);
        foreach (Vector2Int cell in newFootprint) gridController.SetCellOccupied(cell.x, cell.y, true);
        movingStructure.RegisterWithGrid();
        AudioManager.Instance?.PlayPlaceSound();
        gridController.UpdateGridTexture();
        if (gridMonitor != null && newFootprint.Count > 0) gridMonitor.NotifyMultipleCellsChanged(newFootprint, GridChangeType.Structural);
        movingStructure = null;
        isMoveModeActive = false;
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }
        DisableBuildMode();
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
        ClearSynergyVisualization();
        gridController.HideGrid();
        DisableBuildMode();
    }

    void UpdateGhostPosition()
    {
        if (currentGhost == null) return;
        if (!currentGhost.activeSelf && !isGhostTemporarilyHidden && !isDeleteModeActive) currentGhost.SetActive(true);

        Vector2Int hoveredCell = GetGridCellUnderCursor(true);
        bool hoveredValid = gridController.IsValidCell(hoveredCell.x, hoveredCell.y);

        // If the hover cell is outside the logical grid, optionally snap to the nearest valid cell
        int useX = hoveredCell.x;
        int useY = hoveredCell.y;
        if (!hoveredValid)
        {
            if (snapGhostToNearestValidCellWhenOutside && gridController.TextureWidth > 0 && gridController.TextureHeight > 0)
            {
                useX = Mathf.Clamp(hoveredCell.x, 0, gridController.TextureWidth - 1);
                useY = Mathf.Clamp(hoveredCell.y, 0, gridController.TextureHeight - 1);
                hoveredValid = gridController.IsValidCell(useX, useY);
            }
            else
            {
                currentGhost.SetActive(false);
                return;
            }
        }

        Vector3 cellCenter = gridController.GetCellCenterFromTexture(useX, useY);
        currentGhost.transform.position = cellCenter;
        bool isValidPlacement = IsValidPlacement(useX, useY);
        bool canAfford = currentStructureData != null && MoneyManager.Instance != null && MoneyManager.Instance.CanAfford(currentStructureData.cost);

        // Update ghost color based on affordability and placement
        foreach (Renderer renderer in currentGhost.GetComponentsInChildren<Renderer>())
        {
            if (!canAfford)
            {
                // Dim and color red for unaffordable
                renderer.material.color = new Color(1f, 0.2f, 0.2f, 0.3f);  // Red and semi-transparent
            }
            else if (!isValidPlacement)
            {
                renderer.material.color = new Color(1f, 0f, 0f, 0.5f);  // Red for invalid placement
            }
            else
            {
                renderer.material.color = new Color(0f, 1f, 0f, 0.5f);  // Green for valid and affordable
            }
        }
        UpdateSynergyVisualization();
    }

    public void UpdateGhostAffordability(bool canAfford)
    {
        if (currentGhost != null)
        {
            // Force update the ghost color without moving it
            UpdateGhostPosition();
        }
    }

    private void UpdateSynergyVisualization()
    {
        if (currentGhost == null || (!isBuildModeActive && !isMoveModeActive) || isDeleteModeActive || currentStructureData == null) return;
        ClearSynergyVisualization();
        switch (currentStructureData.type)
        {
            case StructureType.Silo: ShowSiloSynergyPreview(); break;
            case StructureType.CropPlot: ShowCropSynergyPreview(); break;
            case StructureType.Animal: ShowAnimalSynergyPreview(); break;
            case StructureType.Barracks: ShowBarracksSynergyPreview(); break;
        }
    }

    private void ShowSiloSynergyPreview()
    {
        CreateRangeIndicator(currentGhost.transform.position, 15f, potentialSynergyMaterial, "Animal Synergy Range");
        CreateRangeIndicator(currentGhost.transform.position, 10f, potentialSynergyMaterial, "Crop Synergy Range");
        foreach (var animal in FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None))
            if ((currentGhost.transform.position - animal.transform.position).sqrMagnitude <= 225f)
                CreateSynergyLine(currentGhost.transform.position, animal.transform.position, Color.green, "Silo-Animal");;
        foreach (var crop in FindObjectsByType<CropStructure>(FindObjectsSortMode.None))
            if (crop != null && (currentGhost.transform.position - crop.transform.position).sqrMagnitude <= 100f)
                CreateSynergyLine(currentGhost.transform.position, crop.transform.position, Color.green, "Silo-Crop");
    }

    private void ShowAnimalSynergyPreview()
    {
        foreach (var silo in FindObjectsByType<SiloStructure>(FindObjectsSortMode.None))
            if ((currentGhost.transform.position - silo.transform.position).sqrMagnitude <= 225f)
                CreateSynergyLine(currentGhost.transform.position, silo.transform.position, Color.green, "Silo-Animal");
        AnimalStructure animalStructure = currentGhost.GetComponent<AnimalStructure>();
        if (animalStructure != null)
            foreach (var barrack in FindObjectsByType<BarracksStructure>(FindObjectsSortMode.None))
                if (barrack.TargetAnimalType == animalStructure.GetAnimalType.ToString())
                {
                    float sqrDistance = (currentGhost.transform.position - barrack.transform.position).sqrMagnitude;
                    if (sqrDistance <= barrack.synergyMaxDist * barrack.synergyMaxDist)
                    {
                        float distance = Mathf.Sqrt(sqrDistance);
                        Color lineColor = distance <= barrack.synergyMinDist ? Color.red : Color.green;
                        CreateSynergyLine(currentGhost.transform.position, barrack.transform.position, lineColor, $"Barracks-{animalStructure.GetAnimalType}");
                    }
                }
    }

    private void ShowBarracksSynergyPreview()
    {
        BarracksStructure barracksStructure = currentGhost.GetComponent<BarracksStructure>();
        if (barracksStructure == null) return;
        string targetType = barracksStructure.TargetAnimalType;
        CreateRangeIndicator(currentGhost.transform.position, barracksStructure.synergyMinDist, validSynergyMaterial, "Optimal Synergy Range");
        CreateRangeIndicator(currentGhost.transform.position, barracksStructure.synergyMaxDist, potentialSynergyMaterial, "Maximum Synergy Range");
        foreach (var animal in FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None))
            if (animal.GetAnimalType.ToString() == targetType)
            {
                float sqrDistance = (currentGhost.transform.position - animal.transform.position).sqrMagnitude;
                if (sqrDistance <= barracksStructure.synergyMaxDist * barracksStructure.synergyMaxDist)
                {
                    float distance = Mathf.Sqrt(sqrDistance);
                    Color lineColor = distance <= barracksStructure.synergyMinDist ? Color.red : Color.green;
                    CreateSynergyLine(currentGhost.transform.position, animal.transform.position, lineColor, $"Barracks-{targetType}");
                }
            }
    }

    private void ShowCropSynergyPreview()
    {
        foreach (var silo in FindObjectsByType<SiloStructure>(FindObjectsSortMode.None))
            if (silo != null && (currentGhost.transform.position - silo.transform.position).sqrMagnitude <= 100f)
                CreateSynergyLine(currentGhost.transform.position, silo.transform.position, Color.green, "Silo-Crop");
    }

    private GameObject CreateRangeIndicator(Vector3 position, float radius, Material material, string label = "")
    {
        GameObject indicator = rangeIndicatorPrefab == null ? new GameObject($"SynergyRange_{label}") : Instantiate(rangeIndicatorPrefab, new Vector3(position.x, position.y + synergyIndicatorHeight, position.z), Quaternion.Euler(90, 0, 0));
        indicator.transform.position = new Vector3(position.x, position.y + synergyIndicatorHeight, position.z);
        if (rangeIndicatorPrefab == null)
        {
            MeshFilter meshFilter = indicator.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = indicator.AddComponent<MeshRenderer>();
            Mesh mesh = new Mesh();
            int segments = 64;
            Vector3[] vertices = new Vector3[segments + 1];
            int[] triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;
            for (int i = 0; i < segments; i++)
            {
                float angle = 2 * Mathf.PI * i / segments;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % segments + 1;
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;
            meshRenderer.material = material;
        }
        else
        {
            float diameter = radius * 2;
            indicator.transform.localScale = new Vector3(diameter, diameter, 1);
            Renderer renderer = indicator.GetComponent<Renderer>();
            if (renderer != null) renderer.material = material;
        }
        synergyIndicators.Add(indicator);
        return indicator;
    }

    private LineRenderer CreateSynergyLine(Vector3 start, Vector3 end, Color color, string synergyType = "")
    {
        GameObject lineObj = synergyLineRendererPrefab != null ? Instantiate(synergyLineRendererPrefab) : new GameObject("SynergyLine_" + synergyType);
        if (synergyLineRendererPrefab == null) lineObj.AddComponent<LineRenderer>().material = new Material(Shader.Find("Sprites/Default"));
        start.y += 0.15f;
        end.y += 0.15f;
        LineRenderer line = lineObj.GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = line.endWidth = 0.15f;
        line.startColor = line.endColor = color;
        ShowSynergyText((start + end) / 2f, GetBonusSummary(synergyType, color), color);
        synergyLines.Add(line);
        activeSynergyLines.Add(lineObj);
        return line;
    }

    private void ShowSynergyText(Vector3 position, string text, Color color)
    {
        if (string.IsNullOrEmpty(text)) return;
        GameObject textObj = new GameObject("SynergyText");
        textObj.transform.position = new Vector3(position.x, position.y + 1f, position.z);
        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.color = color;
        textMesh.fontSize = 32;
        textMesh.characterSize = 0.1f;
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;
        MeshRenderer renderer = textObj.GetComponent<MeshRenderer>();
        renderer.material.shader = Shader.Find("GUI/Text Shader");

        // Create a simple billboard script to make text face camera
        // textObj.AddComponent<SimpleBillboard>();

        // Add to synergy indicators for cleanup
        synergyIndicators.Add(textObj);
    }

    private string GetBonusSummary(string synergyType, Color color)
    {
        if (color == Color.green)
        {
            if (synergyType.Contains("Silo-Animal")) return "+20% Food Efficiency";
            if (synergyType.Contains("Silo-Crop")) return "+50% Yield";
            if (synergyType.Contains("Barracks")) return "20% Discount on Recruitment";
        }
        else if (color == Color.red) return "No Discount on Recruitment";
        return "";
    }

    private void ClearSynergyVisualization()
    {
        foreach (var indicator in synergyIndicators) if (indicator != null) Destroy(indicator);
        synergyIndicators.Clear();
        foreach (var line in synergyLines) if (line != null && line.gameObject != null) Destroy(line.gameObject);
        synergyLines.Clear();
        activeSynergyLines.Clear();
    }

    void UpdateGhostPositionForMove()
    {
        if (currentGhost == null || movingStructure == null) return;
        Vector2Int hoveredCell = GetGridCellUnderCursor(true);
        bool hoveredValid = gridController.IsValidCell(hoveredCell.x, hoveredCell.y);

        int useX = hoveredCell.x;
        int useY = hoveredCell.y;
        if (!hoveredValid)
        {
            if (snapGhostToNearestValidCellWhenOutside && gridController.TextureWidth > 0 && gridController.TextureHeight > 0)
            {
                useX = Mathf.Clamp(hoveredCell.x, 0, gridController.TextureWidth - 1);
                useY = Mathf.Clamp(hoveredCell.y, 0, gridController.TextureHeight - 1);
                hoveredValid = gridController.IsValidCell(useX, useY);
            }
            else
            {
                currentGhost.SetActive(false);
                return;
            }
        }
        Vector3 cellCenter = gridController.GetCellCenterFromTexture(useX, useY);
        currentGhost.transform.position = cellCenter;
        currentGhost.SetActive(true);
        bool isValidPlacement = IsValidPlacement(useX, useY);
        foreach (Renderer renderer in currentGhost.GetComponentsInChildren<Renderer>())
            renderer.material.color = isValidPlacement ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
        UpdateSynergyVisualization();
    }

    void CreateGhost(GameObject prefab)
    {
        if (currentGhost != null) Destroy(currentGhost);
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
            ghostMaterial = new Material(Shader.Find("Standard"))
            {
                color = new Color(0, 1, 0, 0.5f)
            };
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
            renderer.material = new Material(ghostMaterial);
    }

    public void SetBuildTarget(StructureData data)
    {
        if (data == null || data.prefab == null) return;
        currentBuildTargetPrefab = data.prefab;
        currentStructureData = data;
        EnableBuildMode();
        CreateGhost(currentBuildTargetPrefab);
        // Note: Ghost will be updated in UpdateGhostPosition() to reflect affordability
    }

    public void SetBuildTarget(GameObject prefab)
    {
        if (prefab == null) return;
        currentBuildTargetPrefab = prefab;
        EnableBuildMode();
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
            for (int y = bottomLeft.y; y <= topRight.y; y++)
                if (gridController.IsValidCell(x, y))
                {
                    Vector3 cellCenter = gridController.GetCellCenterFromTexture(x, y);
                    if (bounds.Contains(new Vector3(cellCenter.x, bounds.center.y, cellCenter.z)))
                        occupiedCells.Add(new Vector2Int(x, y));
                }
        return occupiedCells;
    }

        bool IsValidPlacement(int x, int y)
    {
        // Add null checks FIRST
        if (gridController == null || gridDataGenerator == null) 
        {
            Debug.LogError("GridController or GridDataGenerator is null in IsValidPlacement");
            return false;
        }
        
        // STRICT bounds checking - prevent placement outside logical grid
        if (x < 0 || x >= gridDataGenerator.GetGridWidth() || 
            y < 0 || y >= gridDataGenerator.GetGridHeight())
        {
            Debug.LogWarning($"Placement blocked: ({x},{y}) is outside logical grid bounds (0,0) to ({gridDataGenerator.GetGridWidth()-1},{gridDataGenerator.GetGridHeight()-1})");
            return false;
        }
        
        // Unlimited building mode bypasses most restrictions but still respects grid bounds
        if (CheatManager.Instance != null && CheatManager.Instance.IsUnlimitedBuildingActive())
        {
            return gridController.IsValidCell(x, y);
        }
        
        // Get the structure's footprint and check ALL cells
        if (currentGhost != null)
        {
            List<Vector2Int> footprint = GetStructureFootprint(currentGhost);
            
            foreach (Vector2Int cell in footprint)
            {
                // Check if each cell in footprint is valid
                if (!gridController.IsValidCell(cell.x, cell.y))
                    return false;
                    
                GridCell gridCell = gridDataGenerator.GetCell(cell.x, cell.y);
                if (gridCell == null) return false;
                
                // Check if any cell in footprint is occupied
                if (gridCell.flags.isOccupied) return false;
                
                // Check if any cell in footprint is not owned
                if (!gridCell.flags.isOwned) return false;
            }
        }
        else
        {
            // Fallback: check single cell if no ghost exists
            GridCell cell = gridDataGenerator.GetCell(x, y);
            if (cell == null) return false;
            
            if (cell.flags.isOccupied) return false;
            if (!cell.flags.isOwned) return false;
        }
        
        // Check money
        if (currentStructureData != null && MoneyManager.Instance != null && !MoneyManager.Instance.CanAfford(currentStructureData.cost)) 
            return false;
        
        return true;
    }

    void PlaceItem(int x, int y)
    {
        if (!IsValidPlacement(x, y) || (currentStructureData != null && MoneyManager.Instance != null && !MoneyManager.Instance.SpendMoney(currentStructureData.cost))) return;

        // Prevent placing more than one farmhouse
        if (currentStructureData != null && currentStructureData.structureName.ToLower().Contains("farm house"))
        {
            if (ShopUIManager.Instance != null && ShopUIManager.Instance.IsFarmHousePlaced)
            {
                Debug.Log("Cannot place more than one Farmhouse!");
                return;  // Block placement
            }
        }

        // New: Check tutorial restrictions
        if (currentStructureData != null && !IsStructureAllowedInCurrentTutorialStep(currentStructureData))
        {
            Debug.Log("Cannot place this structure yet - follow the tutorial!");
            return;
        }

        Vector3 cellCenter = gridController.GetCellCenterFromTexture(x, y);
        GameObject placedItem = Instantiate(currentBuildTargetPrefab, cellCenter, currentRotation);
        Structure structure = placedItem.GetComponent<Structure>();
        placedItem.name = $"Item_{x}_{y}";

        // Disable farmhouse in shop after placement
        if (structure != null && structure.GetStructureName().ToLower().Contains("farm house"))
        {
            ShopUIManager.Instance?.OnFarmHousePlaced();
        }

        if (dustPoof != null)
        {
            Vector3 effectPosition = placedItem.transform.position + (structure != null && structure.GetStructureName() == "Cow Barn" ? new Vector3(0, 4f, 0) : new Vector3(0, 1f, 0));
            float totalMult = structure != null && structure.GetStructureName() == "Cow Barn" ? 1.5f : 1f;
            GameObject effect = Instantiate(dustPoof, effectPosition, Quaternion.identity);
            VisualEffect ps = effect.GetComponent<VisualEffect>();
            if (ps != null)
            {
                ps.SetVector3("posMult", new Vector3(totalMult, totalMult, totalMult));
                ps.SetFloat("totalMult", totalMult);
                ps.Play();
            }
            Destroy(effect, 3f);
        }
        if (structure != null)
        {
            structure.SetAllowSelectionAndUI(false);
            StartCoroutine(EnableSelectionAfterRelease(structure));
            HandleTutorialTriggers(structure);
        }
        List<Vector2Int> footprint = GetStructureFootprint(placedItem);
        foreach (Vector2Int cell in footprint)
        {
            gridController.SetCellOccupied(cell.x, cell.y, true);
        }
        AudioManager.Instance?.PlayPlaceSound();
        gridController.UpdateGridTexture();
        if (gridMonitor != null && footprint.Count > 0) gridMonitor.NotifyMultipleCellsChanged(footprint, GridChangeType.Structural);

        // Hide the info card immediately after placement to prevent UI conflicts
        ItemHoverPanel.Instance?.HideImmediate();
    }

    private bool IsStructureAllowedInCurrentTutorialStep(StructureData data)
    {
        // Check if "Unlock All Buildings" cheat is active
        if (CheatManager.Instance != null && CheatManager.Instance.IsUnlockAllBuildsActive())
        {
            return true; // Cheat overrides all restrictions
        }
        
        if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive()) return true;

        string currentStepId = TutorialManager.Instance.GetCurrentStepId();
        string structureName = data.structureName.ToLower();

        // Same logic as in ShopPanelUI
        switch (currentStepId)
        {
            case "build_farmhouse":
                return structureName.Contains("farm house");
            case "build_crop_plot":
                return structureName.Contains("crop plot");
            case "build_silo":
                return structureName.Contains("silo");
            case "build_chicken_coop":
                return structureName.Contains("chicken coop");
            case "build_chicken_barracks":
                return structureName.Contains("chicken barrack");
            default:
                return false;
        }
    }

    private void HandleTutorialTriggers(Structure structure)
    {
        if (TutorialManager.Instance == null) return;
        string name = structure.GetStructureName().ToLower();
        if (structure is BarracksStructure barracks)
        {
            string targetAnimalType = barracks.TargetAnimalType.ToLower();
            TutorialTrigger barracksType = targetAnimalType switch
            {
                "chicken" => TutorialTrigger.BuiltChickenBarracks,
                "cow" => TutorialTrigger.BuiltCowBarracks,
                "sheep" => TutorialTrigger.BuiltSheepBarracks,
                "goat" => TutorialTrigger.BuiltGoatBarracks,
                "pig" => TutorialTrigger.BuiltPigBarracks,
                _ => TutorialTrigger.None
            };
            // Only trigger if the step isn't already completed (prevents reset on movement)
            if (barracksType != TutorialTrigger.None && !TutorialManager.Instance.GetCompletedStepIds().Contains($"build_{targetAnimalType}_barracks"))
            {
                TutorialManager.Instance.Trigger(barracksType);
            }
            // Trigger re-search for all barracks when a new barracks is built
            BarracksStructure.UpdateAllNearbyChickenCoops();
            return;
        }
        if (structure is AnimalStructure animal)
        {
            // Add trigger for animal structures like chicken coop
            string animalType = animal.GetAnimalType.ToString().ToLower();
            TutorialTrigger animalTrigger = animalType switch
            {
                "chicken" => TutorialTrigger.BuiltChickenCoop,
                "cow" => TutorialTrigger.BuiltCowPen,
                "sheep" => TutorialTrigger.BuiltSheepPen,
                "goat" => TutorialTrigger.BuiltGoatPen,
                "pig" => TutorialTrigger.BuiltPigPen,
                _ => TutorialTrigger.None
            };
            if (animalTrigger != TutorialTrigger.None && !TutorialManager.Instance.GetCompletedStepIds().Contains($"build_{animalType}_coop"))
            {
                TutorialManager.Instance.Trigger(animalTrigger);
            }
            // Trigger re-search for all barracks when a new animal structure is built
            BarracksStructure.UpdateAllNearbyChickenCoops();
            return;
        }
        TutorialTrigger trigger = name switch
        {
            var n when n.Contains("silo") || n.Contains("storage") => TutorialTrigger.BuiltSilo,
            var n when n.Contains("farm house") || n.Contains("farmhouse") => TutorialTrigger.BuiltFarmHouse,
            var n when n.Contains("crop") || n.Contains("plot") => TutorialTrigger.BuiltCropPlot,
            _ => TutorialTrigger.None
        };
        // Only trigger if the step isn't already completed (prevents reset on movement)
        if (trigger != TutorialTrigger.None && !TutorialManager.Instance.GetCompletedStepIds().Contains($"build_{name.Replace(" ", "").ToLower()}"))
        {
            TutorialManager.Instance.Trigger(trigger);
        }
        if (name.Contains("farm house") || name.Contains("farmhouse")) isHousePlaced = true;
    }

    public void OnStructureBuilt(Structure structure)
    {
        if (structure is AnimalStructure)
        {
            // Trigger re-search for all barracks when a new animal structure is built
            BarracksStructure.UpdateAllNearbyChickenCoops();
        }
        HandleTutorialTriggers(structure);
    }

    private IEnumerator EnableSelectionAfterRelease(Structure structure)
    {
        while (Input.GetMouseButton(0)) yield return null;
        if (structure != null) structure.SetAllowSelectionAndUI(true);
    }

    void RemoveItem(int x, int y)
    {
        Debug.Log($"RemoveItem called at ({x}, {y})");
        
        if (!gridController.IsValidCell(x, y)) 
        {
            Debug.Log($"Invalid cell at ({x}, {y})");
            return;
        }
        
        GridCell cell = gridController.GetCell(x, y);
        if (cell == null) 
        {
            Debug.Log($"Cell is null at ({x}, {y})");
            return;
        }
        
        if (!cell.flags.isOccupied) 
        {
            Debug.Log($"Cell at ({x}, {y}) is not occupied");
            return;
        }
        
        // Try to find the item by name first
        string itemName = $"Item_{x}_{y}";
        GameObject placedItem = GameObject.Find(itemName);
        Debug.Log($"Looking for item with name: {itemName}, found: {placedItem != null}");
        
        // If not found by exact name, try to find any structure that occupies this cell
        if (placedItem == null)
        {
            Debug.Log($"Item not found by name, searching for structures occupying cell ({x}, {y})");
            Structure[] allStructures = FindObjectsByType<Structure>(FindObjectsSortMode.None);
            foreach (Structure structure in allStructures)
            {
                List<Vector2Int> footprint = GetStructureFootprint(structure.gameObject);
                if (footprint.Contains(new Vector2Int(x, y)))
                {
                    placedItem = structure.gameObject;
                    Debug.Log($"Found structure {structure.gameObject.name} occupying cell ({x}, {y})");
                    break;
                }
            }
        }
        
        if (placedItem != null)
        {
            Structure structure = placedItem.GetComponent<Structure>();
            // Prevent deletion of farmhouse
            if (structure != null && structure.GetStructureName().ToLower().Contains("farm house"))
            {
                Debug.Log("Cannot delete Farmhouse - it is indestructible!");
                return;  // Skip deletion
            }

            Debug.Log($"Removing structure: {placedItem.name}");
            List<Vector2Int> footprint = GetStructureFootprint(placedItem);
            
            // Get the structure data for money back calculation
            StructureData structureData = structure?.structureData;
            if (structureData != null)
            {
                int moneyBack = (int)(structureData.cost * moneyBackAfterDeletePercentage);
                MoneyManager.Instance.AddMoney(moneyBack);
                Debug.Log($"Added money back after deleting. cost {structureData.cost} percentage: {moneyBackAfterDeletePercentage} money gained: {moneyBack}");
            }
            else
            {
                Debug.Log("No structure data found for money back calculation");
            }
            
            Destroy(placedItem);
            AudioManager.Instance?.PlayRemoveSound();
            foreach (Vector2Int pos in footprint) gridController.SetCellOccupied(pos.x, pos.y, false);
            gridController.UpdateGridTexture();
            if (gridMonitor != null && footprint.Count > 0) gridMonitor.NotifyMultipleCellsChanged(footprint, GridChangeType.Structural);
        }
        else
        {
            Debug.Log($"No structure found at cell ({x}, {y})");
        }
    }

    private bool TryRemoveStructureByRaycast()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Debug.Log("Performing raycast for structure removal");
        
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log($"Raycast hit: {hit.collider.gameObject.name}");
            
            // Look for a Structure component in the hit object or its parents
            Transform currentTransform = hit.collider.transform;
            Structure structure = null;
            
            while (currentTransform != null)
            {
                structure = currentTransform.GetComponent<Structure>();
                if (structure != null)
                {
                    Debug.Log($"Found structure: {structure.gameObject.name}");
                    break;
                }
                currentTransform = currentTransform.parent;
            }
            
            if (structure != null)
            {
                // Prevent deletion of farmhouse
                if (structure.GetStructureName().ToLower().Contains("farm house"))
                {
                    Debug.Log("Cannot delete Farmhouse - it is indestructible!");
                    return true;  // Return true to indicate "handled" without deleting
                }

                Debug.Log($"Attempting to remove structure by raycast: {structure.gameObject.name}");
                
                // Get the structure's footprint and remove it
                List<Vector2Int> footprint = GetStructureFootprint(structure.gameObject);
                
                // Get money back
                StructureData structureData = structure.structureData;
                if (structureData != null)
                {
                    int moneyBack = (int)(structureData.cost * moneyBackAfterDeletePercentage);
                    MoneyManager.Instance.AddMoney(moneyBack);
                    Debug.Log($"Added money back after deleting by raycast. cost {structureData.cost} money gained: {moneyBack}");
                }
                
                // Remove from grid
                foreach (Vector2Int pos in footprint) 
                {
                    gridController.SetCellOccupied(pos.x, pos.y, false);
                }
                
                // Destroy the structure
                Destroy(structure.gameObject);
                AudioManager.Instance?.PlayRemoveSound();
                gridController.UpdateGridTexture();
                
                if (gridMonitor != null && footprint.Count > 0) 
                {
                    gridMonitor.NotifyMultipleCellsChanged(footprint, GridChangeType.Structural);
                }
                
                Debug.Log("Structure successfully removed by raycast");
                return true;
            }
            else
            {
                Debug.Log("No structure component found in raycast hit");
            }
        }
        else
        {
            Debug.Log("Raycast missed - no objects hit");
        }
        return false;
    }

    private List<Vector2Int> GetExtendedStructureFootprint(GameObject obj)
    {
        List<Vector2Int> occupiedCells = new List<Vector2Int>();
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return occupiedCells;
        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) combinedBounds.Encapsulate(renderers[i].bounds);
        combinedBounds.Expand(0.1f);
        Vector2Int bottomLeft = gridController.WorldToGridCoords(combinedBounds.min);
        Vector2Int topRight = gridController.WorldToGridCoords(combinedBounds.max);
        for (int x = bottomLeft.x - 1; x <= topRight.x + 1; x++)
            for (int y = bottomLeft.y - 1; y <= topRight.y + 1; y++)
                if (gridController.IsValidCell(x, y))
                {
                    GridCell cell = gridController.GetCell(x, y);
                    if (cell != null && cell.flags.isOccupied) occupiedCells.Add(new Vector2Int(x, y));
                }
        return occupiedCells;
    }

    private void UpdateDeleteIconPosition()
    {
        Vector2 mousePosition = Input.mousePosition;
        itemDeleteIcon.position = new Vector2(mousePosition.x + cursorOffset.x, mousePosition.y + cursorOffset.y);
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
        ClearSynergyVisualization(); // <-- Add this line
        currentBuildTargetPrefab = null;
    }

    private Vector2Int GetGridCellUnderCursor(bool ignoreStructures = false)
    {
        Vector2Int hoveredCell = gridController.GetCurrentHoveredCell();
        if (ignoreStructures && !isDeleteModeActive)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float gridHeight = 0f;
            if (gridController.TextureHeight > 0)
                for (int x = 0; x < gridController.TextureWidth; x++)
                    for (int y = 0; y < gridController.TextureHeight; y++)
                    {
                        GridCell cell = gridController.GetCell(x, y);
                        if (cell != null)
                        {
                            gridHeight = cell.worldPosition.y;
                            break;
                        }
                    }
            Plane gridPlane = new Plane(Vector3.up, new Vector3(0, gridHeight, 0));
            if (gridPlane.Raycast(ray, out float distance))
            {
                Vector3 hitPoint = ray.GetPoint(distance);
                hoveredCell = gridController.WorldToGridCoords(hitPoint);
            }
            else
            {
                // Fallback: Use the last known hovered cell if raycast fails
                Debug.LogWarning("Raycast to grid plane failed - using last hovered cell");
            }
        }
        
        if (gridController.TextureWidth > 0 && gridController.TextureHeight > 0)
        {
            hoveredCell.x = Mathf.Clamp(hoveredCell.x, 0, gridController.TextureWidth - 1);
            hoveredCell.y = Mathf.Clamp(hoveredCell.y, 0, gridController.TextureHeight - 1);
        }
        return hoveredCell;
    }

    public void SetRemovalModifierKey(KeyCode newKey) => removeModifierKey = newKey;
    public KeyCode GetRemovalModifierKey() => removeModifierKey;
    public void HideDeleteIcon() => itemDeleteIcon.gameObject.SetActive(false);
    public Vector2 DeleteIconOffset { get => cursorOffset; set => cursorOffset = value; }
    public bool IsHousePlaced() => isHousePlaced;
}