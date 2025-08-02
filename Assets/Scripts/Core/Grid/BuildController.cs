using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using FarmDefender.Core.AI.FlowField;
using System.Collections;
using UnityEngine.VFX;

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

    [Header("UI References")]
    [SerializeField] private RectTransform itemDeleteIcon;
    [SerializeField] public GameObject dustPoof;

    [Header("Delete Mode Settings")]
    [Tooltip("Position offset from the cursor where the delete icon will appear")]
    [SerializeField] private Vector2 cursorOffset = new Vector2(15f, 15f);

    [Header("Land Ownership")]
    [SerializeField] private bool enableLandBuying = true;

    [Header("Synergy Visualization")]
    [SerializeField] private GameObject rangeIndicatorPrefab;
    [SerializeField] private Material validSynergyMaterial;
    [SerializeField] private Material potentialSynergyMaterial;
    [SerializeField] private Material invalidSynergyMaterial;
    [SerializeField] private float synergyIndicatorHeight = 0.1f;
    [SerializeField] private GameObject synergyLineRendererPrefab;
    [SerializeField] private Canvas worldSpaceCanvas;

    [Header("Performance Settings")]
    [SerializeField] private bool enableSynergyVisuals = true; // Allow disabling for potato devices
    [SerializeField] private int maxSynergyLines = 10; // Limit lines for performance
    private List<GameObject> synergyIndicators = new List<GameObject>();
    private List<LineRenderer> synergyLines = new List<LineRenderer>();
    private List<GameObject> activeSynergyLines = new List<GameObject>(); // Track active synergy line GameObjects
    private Dictionary<LineRenderer, SynergyTooltip> lineTooltips = new Dictionary<LineRenderer, SynergyTooltip>();

    private bool isHousePlaced = false;

    private class SynergyTooltip
    {
        public GameObject tooltipObject;
        public string synergyType;
        public string description;
        public Color lineColor;
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
            gridController = FindFirstObjectByType<GridController>();
            if (gridController == null)
            {
                Debug.LogError("GridController not found. BuildController cannot function.");
                enabled = false;
                return;
            }
        }

        if (flowFieldManager == null)
            flowFieldManager = FindFirstObjectByType<FlowFieldManager>();

        if (ownershipController == null)
            ownershipController = FindFirstObjectByType<OwnershipController>();

        if (gridMonitor == null)
            gridMonitor = FindFirstObjectByType<GridMonitor>();

        if (gridMonitor == null)
            Debug.LogWarning("GridMonitor not found. Grid changes won't be centrally tracked.");

        shopPanelUI = FindFirstObjectByType<ShopPanelUI>(FindObjectsInactive.Include);
        if (shopPanelUI != null)
        {
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

        // Performance optimization: Skip frame if too many synergy lines
        if (enableSynergyVisuals && activeSynergyLines.Count > maxSynergyLines)
        {
            // Remove excess synergy lines
            while (activeSynergyLines.Count > maxSynergyLines)
            {
                var lineToRemove = activeSynergyLines[activeSynergyLines.Count - 1];
                activeSynergyLines.RemoveAt(activeSynergyLines.Count - 1);
                if (lineToRemove != null)
                    Destroy(lineToRemove);
            }
        }

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
        ClearSynergyVisualization();
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
        // Make sure to copy the structure data for proper synergy visualization
        currentStructureData = structure.structureData;
        currentRotation = originalRotation;

        if (currentBuildTargetPrefab == null)
        {
            Debug.LogWarning($"No prefab assigned to {structure.GetStructureName()}'s StructureData. Cannot create ghost.");
            CancelMove();
            return;
        }

        CreateGhost(currentBuildTargetPrefab);

        // Copy component data for special structure types
        if (structure is BarracksStructure barracks)
        {
            // Add BarracksStructure component if missing on ghost
            BarracksStructure ghostBarracks = currentGhost.GetComponent<BarracksStructure>() ??
                                             currentGhost.AddComponent<BarracksStructure>();

            // FIX: Copy via reflection since TargetAnimalType is read-only
            var targetField = typeof(BarracksStructure).GetField("targetAnimalType",
                              System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (targetField != null)
            {
                string value = barracks.TargetAnimalType;
                targetField.SetValue(ghostBarracks, value);
            }

            // Copy the distance values which are likely writable
            ghostBarracks.synergyMinDist = barracks.synergyMinDist;
            ghostBarracks.synergyMaxDist = barracks.synergyMaxDist;
        }
        else if (structure is AnimalStructure animal)
        {
            // Add AnimalStructure component if missing on ghost
            AnimalStructure ghostAnimal = currentGhost.GetComponent<AnimalStructure>() ??
                                         currentGhost.AddComponent<AnimalStructure>();

            // FIX: Use proper property or method instead of direct field access
            // Option 1: If there's a SetAnimalType method
            if (ghostAnimal.GetType().GetMethod("SetAnimalType") != null)
            {
                ghostAnimal.GetType().GetMethod("SetAnimalType").Invoke(
                    ghostAnimal, new object[] { animal.GetAnimalType });
            }
            // Option 2: Use reflection to set private field
            else
            {
                var field = typeof(AnimalStructure).GetField("animalType",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    var value = animal.GetAnimalType;
                    field.SetValue(ghostAnimal, value);
                }
            }
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

        UpdateSynergyVisualization();
    }

    private void UpdateSynergyVisualization()
    {
        // Modified condition to include move mode
        if (currentGhost == null || (!isBuildModeActive && !isMoveModeActive) || isDeleteModeActive || currentStructureData == null)
            return;

        ClearSynergyVisualization();

        switch (currentStructureData.type)
        {
            case StructureType.Silo:
                ShowSiloSynergyPreview();
                break;
            case StructureType.CropPlot:
                ShowCropSynergyPreview();
                break;
            case StructureType.Animal:
                ShowAnimalSynergyPreview();
                break;
            case StructureType.Barracks:
                ShowBarracksSynergyPreview();
                break;
        }
    }

    private void ShowSiloSynergyPreview()
    {
        // Show range indicators
        CreateRangeIndicator(currentGhost.transform.position, 15f, potentialSynergyMaterial, "Animal Synergy Range");
        CreateRangeIndicator(currentGhost.transform.position, 10f, potentialSynergyMaterial, "Crop Synergy Range");

        // Find animals in range
        AnimalStructure[] animals = FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None);
        foreach (var animal in animals)
        {
            float sqrDistance = (currentGhost.transform.position - animal.transform.position).sqrMagnitude;
            if (sqrDistance <= 15f * 15f) // 225f
            {
                CreateSynergyLine(currentGhost.transform.position, animal.transform.position, Color.green, "Silo-Animal");
            }
        }

        // Find crops in range with improved detection
        CropStructure[] crops = FindObjectsByType<CropStructure>(FindObjectsSortMode.None);
        if (crops.Length == 0)
        {
            // Alternative search method if no crops found through FindObjectsByType
            var allStructures = FindObjectsByType<Structure>(FindObjectsSortMode.None);
            foreach (var structure in allStructures)
            {
                if (structure is CropStructure)
                {
                    CropStructure crop = structure as CropStructure;
                    float sqrDistance = (currentGhost.transform.position - crop.transform.position).sqrMagnitude;
                    if (sqrDistance <= 10f * 10f) // 100f
                    {
                        // Make crop lines more visible with a bright color
                        CreateSynergyLine(currentGhost.transform.position, crop.transform.position, Color.green, "Silo-Crop");
                    }
                }
            }
        }
        else
        {
            foreach (var crop in crops)
            {
                if (crop == null)
                {
                    Debug.LogWarning("Null crop structure in crops array!");
                    continue;
                }

                float sqrDistance = (currentGhost.transform.position - crop.transform.position).sqrMagnitude;
                if (sqrDistance <= 10f * 10f) // 100f
                {
                    // Changed from Color.yellow to something more visible
                    CreateSynergyLine(currentGhost.transform.position, crop.transform.position, Color.green, "Silo-Crop");
                }
            }
        }
    }

    private void ShowAnimalSynergyPreview()
    {
        SiloStructure[] silos = FindObjectsByType<SiloStructure>(FindObjectsSortMode.None);
        foreach (var silo in silos)
        {
            float sqrDistance = (currentGhost.transform.position - silo.transform.position).sqrMagnitude;
            if (sqrDistance <= 15f * 15f) // 225f
            {
                CreateSynergyLine(currentGhost.transform.position, silo.transform.position, Color.green, "Silo-Animal");
            }
        }

        BarracksStructure[] barracks = FindObjectsByType<BarracksStructure>(FindObjectsSortMode.None);
        foreach (var barrack in barracks)
        {
            AnimalStructure animalStructure = currentGhost.GetComponent<AnimalStructure>();
            if (animalStructure != null && barrack.TargetAnimalType == animalStructure.GetAnimalType.ToString())
            {
                float sqrDistance = (currentGhost.transform.position - barrack.transform.position).sqrMagnitude;
                if (sqrDistance <= barrack.synergyMaxDist * barrack.synergyMaxDist)
                {
                    // Calculate actual distance only when needed for color comparison
                    float distance = Mathf.Sqrt(sqrDistance);
                    Color lineColor = distance <= barrack.synergyMinDist ? Color.red : Color.green;
                    string synergyType = $"Barracks-{animalStructure.GetAnimalType}";
                    CreateSynergyLine(currentGhost.transform.position, barrack.transform.position, lineColor, synergyType);
                }
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

        AnimalStructure[] animals = FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None);
        foreach (var animal in animals)
        {
            if (animal.GetAnimalType.ToString() == targetType)
            {
                float sqrDistance = (currentGhost.transform.position - animal.transform.position).sqrMagnitude;
                float synergyMaxDistSqr = barracksStructure.synergyMaxDist * barracksStructure.synergyMaxDist;
                if (sqrDistance <= synergyMaxDistSqr)
                {
                    // For color comparison, still need actual distance
                    float distance = Mathf.Sqrt(sqrDistance);
                    Color lineColor = distance <= barracksStructure.synergyMinDist ? Color.red : Color.green;
                    string synergyType = $"Barracks-{targetType}";
                    CreateSynergyLine(currentGhost.transform.position, animal.transform.position, lineColor, synergyType);
                }
            }
        }
    }
    private void ShowCropSynergyPreview()
    {
        // Find silos in range (reverse of the silo->crop relationship)
        SiloStructure[] silos = FindObjectsByType<SiloStructure>(FindObjectsSortMode.None);
        foreach (var silo in silos)
        {
            if (silo == null) continue;

            float sqrDistance = (currentGhost.transform.position - silo.transform.position).sqrMagnitude;
            if (sqrDistance <= 10f * 10f) // 100f
            {
                CreateSynergyLine(currentGhost.transform.position, silo.transform.position, Color.green, "Silo-Crop");
            }
        }
    }

    private GameObject CreateRangeIndicator(Vector3 position, float radius, Material material, string label = "")
    {
        if (rangeIndicatorPrefab == null)
        {
            GameObject indicator = new GameObject($"SynergyRange_{label}");
            indicator.transform.position = new Vector3(position.x, position.y + synergyIndicatorHeight, position.z);

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

            synergyIndicators.Add(indicator);
            return indicator;
        }
        else
        {
            GameObject indicator = Instantiate(rangeIndicatorPrefab,
                new Vector3(position.x, position.y + synergyIndicatorHeight, position.z),
                Quaternion.Euler(90, 0, 0));

            float diameter = radius * 2;
            indicator.transform.localScale = new Vector3(diameter, diameter, 1);

            Renderer renderer = indicator.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material = material;

            synergyIndicators.Add(indicator);
            return indicator;
        }
    }

    private LineRenderer CreateSynergyLine(Vector3 start, Vector3 end, Color color, string synergyType = "")
    {
        GameObject lineObj;
        if (synergyLineRendererPrefab != null)
            lineObj = Instantiate(synergyLineRendererPrefab);
        else
        {
            lineObj = new GameObject("SynergyLine_" + synergyType);
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.startWidth = 0.15f;
            lr.endWidth = 0.15f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
        }

        // Raise lines slightly above ground to ensure visibility
        start.y += 0.15f;
        end.y += 0.15f;

        LineRenderer line = lineObj.GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startColor = color;
        line.endColor = color;

        // Create simple floating text at the midpoint of the line
        ShowSynergyText((start + end) / 2f, GetBonusSummary(synergyType, color), color);

        synergyLines.Add(line);
        activeSynergyLines.Add(lineObj); // Track the GameObject for performance management
        return line;
    }

    // Simple floating text method (no Canvas required)
    private void ShowSynergyText(Vector3 position, string text, Color color)
    {
        if (string.IsNullOrEmpty(text)) return;

        // Create a GameObject for the text
        GameObject textObj = new GameObject("SynergyText");
        textObj.transform.position = new Vector3(position.x, position.y + 1f, position.z);

        // Add TextMesh component
        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.color = color;
        textMesh.fontSize = 32;
        textMesh.characterSize = 0.1f;
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;

        // Add outline for better visibility
        MeshRenderer renderer = textObj.GetComponent<MeshRenderer>();
        renderer.material.shader = Shader.Find("GUI/Text Shader");

        // Create a simple billboard script to make text face camera
        textObj.AddComponent<SimpleBillboard>();

        // Add to synergy indicators for cleanup
        synergyIndicators.Add(textObj);

    }

    // New method to create visible bonus labels
    private void CreateVisibleBonusLabel(Vector3 start, Vector3 end, Color color, string synergyType)
    {
        if (worldSpaceCanvas == null)
        {
            Debug.LogError("Cannot create bonus label: worldSpaceCanvas is null!");
            return;
        }

        // Get bonus text
        string bonusText = GetBonusSummary(synergyType, color);
        if (string.IsNullOrEmpty(bonusText)) return;

        // Log for debugging
        try
        {
            // Create a new TextMeshProUGUI component in a way that works reliably
            GameObject labelObj = new GameObject($"BonusLabel_{synergyType}_{Random.Range(0, 1000)}");
            labelObj.transform.SetParent(worldSpaceCanvas.transform, false);

            // Create a panel first (white background)
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(labelObj.transform, false);

            // Add panel components
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(1f, 1f, 1f, 0.7f); // Semi-transparent white
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(100, 30);

            // Create text as child of panel
            GameObject textObj = new GameObject("BonusText");
            textObj.transform.SetParent(panel.transform, false);

            // Add text components
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = bonusText;
            text.fontSize = 12;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(5, 5);
            textRect.offsetMax = new Vector2(-5, -5);

            // Position the label at the midpoint of the line
            Vector3 midpoint = (start + end) / 2f;
            midpoint.y += 0.75f; // Position above the line
            labelObj.transform.position = midpoint;

            // Set a larger, more visible scale
            labelObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            // Make it always face the camera
            labelObj.AddComponent<LookAtCamera>();

            // Add to synergy indicators for cleanup
            synergyIndicators.Add(labelObj);

        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create bonus label: {e.Message}\n{e.StackTrace}");
        }
    }

    // Helper method for shortened bonus text
    private string GetBonusSummary(string synergyType, Color color)
    {
        if (color == Color.green || color == Color.magenta)
        {
            if (synergyType.Contains("Silo-Animal"))
                return "+20% Food Efficiency";
            if (synergyType.Contains("Silo-Crop"))
                return "+50% Yield";
            if (synergyType.Contains("Barracks"))
                return "20% Discount on Recruitment";
        }
        else if (color == Color.yellow)
        {
            if (synergyType.Contains("Barracks"))
                return "Discount";
            return "Bonus";
        }
        else if (color == Color.red)
        {
            return "No Discount on Recruitment";
        }
        return "";
    }

    private string GetSynergyDescription(string synergyType, Color color)
    {
        if (color == Color.green)
        {
            if (synergyType.Contains("Silo-Animal"))
                return "Silo Effect: Animals use 20% less food";
            if (synergyType.Contains("Silo-Crop"))
                return "Silo Effect: Crops produce 50% more harvest";
            if (synergyType.Contains("Barracks"))
                return "Optimal Barracks Synergy: Maximum recruitment discount";
            return "Optimal Synergy: Maximum bonus";
        }
        else if (color == Color.yellow)
        {
            if (synergyType.Contains("Barracks"))
                return "Partial Barracks Synergy: Reduced recruitment cost";
            return "Partial Synergy: Some bonus applied";
        }
        else if (color == Color.red)
        {
            return "Conflict: These buildings compete for resources";
        }
        return "Building Connection";
    }

    private void ClearSynergyVisualization()
    {
        foreach (var indicator in synergyIndicators)
        {
            if (indicator != null) Destroy(indicator);
        }
        synergyIndicators.Clear();

        foreach (var line in synergyLines)
        {
            if (line != null)
            {
                if (lineTooltips.ContainsKey(line) && lineTooltips[line].tooltipObject != null)
                {
                    Destroy(lineTooltips[line].tooltipObject);
                }
                if (line.gameObject != null)
                {
                    Destroy(line.gameObject);
                }
            }
        }
        lineTooltips.Clear();
        synergyLines.Clear();
        activeSynergyLines.Clear(); // Clear the performance tracking list
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

        // Add this line to show synergy lines during movement
        UpdateSynergyVisualization();
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
            return;
        }

        currentBuildTargetPrefab = data.prefab;
        currentStructureData = data;

        // Enable build mode and create new ghost
        EnableBuildMode();
        // Force create new ghost even if one already exists
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

        // Enable build mode and create new ghost
        EnableBuildMode();
        // Force create new ghost even if one already exists
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
            if (gridCell == null || !gridCell.flags.isOwned || gridCell.flags.isObstacle) return false;

            if (gridCell.flags.isOccupied && isMoveModeActive && originalFootprint != null && !originalFootprint.Contains(cell))
                return false;
        }
        return true;
    }

    void PlaceItem(int x, int y)
    {
        if (!IsValidPlacement(x, y)) return;

        if (currentStructureData != null && MoneyManager.Instance != null && !MoneyManager.Instance.SpendMoney(currentStructureData.cost))
        {
            return;
        }

        Vector3 cellCenter = gridController.GetCellCenterFromTexture(x, y);
        GameObject placedItem = Instantiate(currentBuildTargetPrefab, cellCenter, currentRotation);
        placedItem.name = $"Item_{x}_{y}";
        Structure structure = placedItem.GetComponent<Structure>();

        if (structure.GetStructureName() == "Crop Plot")
        {
            TutorialManager.Instance.CheckStep4();
        }
        if (structure.GetStructureName() == "Silo")
        {
            TutorialManager.Instance.CheckStep6();
        }
        if (structure.GetStructureName() == "Chicken Coop")
        {
            TutorialManager.Instance.CheckStep8();
        }
        if (structure.GetStructureName() == "Chicken Barrack")
        {
            TutorialManager.Instance.CheckStep12();
        }

        // --- Play particle effect on placement ---
        if (dustPoof != null)
        {
            Vector3 effectPosition;
            Vector3 posMult;
            float totalMult;

            if (structure != null)
            {
                if (structure.GetStructureName() == "Cow Barn")
                {
                    effectPosition = placedItem.transform.position + new Vector3(0, 4f, 0); // Center of building, 1 unit above
                    posMult = new Vector3(1.5f, 1.5f, 1.5f); // more position spread
                    totalMult = 1.5f; // more bubbles
                }
                else if (structure.GetStructureName() == "Goat Pen")
                {
                    effectPosition = placedItem.transform.position + new Vector3(0, 4f, 0); // Center of building, 1 unit above
                    posMult = new Vector3(1.2f, 1.2f, 1.2f); // more position spread
                    totalMult = 1.2f; // more bubbles
                }
                else if (structure.GetStructureName() == "Farm House")
                {
                    TutorialManager.Instance.CheckStep3();


                    isHousePlaced = true;
                    effectPosition = placedItem.transform.position + new Vector3(0, 4f, 0); // Center of building, 1 unit above
                    posMult = new Vector3(1.2f, 1.2f, 1.2f); // more position spread
                    totalMult = 1.2f; // more bubbles
                    TutorialManager.Instance?.Trigger(TutorialTrigger.BuiltFarmHouse);

                }
                else
                {
                    effectPosition = placedItem.transform.position + new Vector3(0, 1f, 0); // Center of building, 1 unit above
                    posMult = new Vector3(1f, 1f, 1f); // more position spread
                    totalMult = 1f; // more bubbles
                }
            }
            else
            {
                effectPosition = placedItem.transform.position + new Vector3(0, 3f, 0); // Center of building, 1 unit above
                posMult = new Vector3(2f, 2f, 2f); // more position spread
                totalMult = 2f; // more bubbles
            }


            GameObject effect = Instantiate(dustPoof, effectPosition, Quaternion.identity);


            VisualEffect ps = effect.GetComponent<VisualEffect>();
            if (ps != null)
                ps.SetVector3("posMult", posMult);
            ps.SetFloat("totalMult", totalMult);
            ps.Play();
            Destroy(effect, 3f); // Destroy after 3 seconds (adjust as needed)

            Debug.Log("Dust effect position: " + effect.transform.position);
        }
        // --- End particle effect ---

        if (structure != null)
        {
            structure.SetAllowSelectionAndUI(false);
            StartCoroutine(EnableSelectionAfterRelease(structure));

            SiloStructure silo = structure as SiloStructure;
            if (silo != null)
            {
                InventoryManager.Instance.RegisterSilo(silo);
                TutorialManager.Instance?.Trigger(TutorialTrigger.BuiltSilo); // <-- Add this line
                
            }
        }

        List<Vector2Int> footprint = GetStructureFootprint(placedItem);
        foreach (Vector2Int cell in footprint)
            gridController.SetCellOccupied(cell.x, cell.y, true);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPlaceSound();

        // // Notify tutorial system about structure placement
        // TutorialConditionTracker tutorialTracker = FindFirstObjectByType<TutorialConditionTracker>();
        // if (tutorialTracker != null)
        // {
        //     Structure placedStructure = placedItem.GetComponent<Structure>();
        //     if (placedStructure != null && placedStructure.structureData != null)
        //     {
        //         string structureName = placedStructure.structureData.structureName ?? placedStructure.name;
        //         tutorialTracker.OnStructurePlaced(placedStructure.structureData.type, structureName);
        //     }
        // }

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
        Debug.Log($"Structure name: =====================================================");
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

            Debug.Log($"Structure name: '{structure.GetStructureName()}'");

            if (structure.GetStructureName().ToLower().Contains("farm house"))
            {
                isHousePlaced = false;
            }

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

                        if(structure.GetStructureName().ToLower().Contains("farm house"))
                        {
                            isHousePlaced = false;
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

            Plane gridPlane = new Plane(Vector3.up, new Vector3(0, gridHeight, 0));
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
    }

    public KeyCode GetRemovalModifierKey()
    {
        return removeModifierKey;
    }

    public void HideDeleteIcon()
    {
        itemDeleteIcon.gameObject.SetActive(false);
    }

    public Vector2 DeleteIconOffset
    {
        get => cursorOffset;
        set => cursorOffset = value;
    }
    
    public bool IsHousePlaced()
    {
        return isHousePlaced;
    }
}