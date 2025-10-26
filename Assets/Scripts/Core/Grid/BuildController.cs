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
    [SerializeField] private Material ghostErrorMaterial; // Red material for unaffordable ghost chain links
    [SerializeField] private GameObject[] buildablePrefabs;

    [Header("Input Settings")]
    [SerializeField] private KeyCode rotateKey = KeyCode.R;
    [SerializeField] private KeyCode nextItemKey = KeyCode.N;
    [SerializeField] private KeyCode previousItemKey = KeyCode.P;
    [SerializeField] private KeyCode removeModifierKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode moveKey = KeyCode.M;

    [Header("UI References")]
    [SerializeField] private RectTransform itemDeleteIcon;
    [SerializeField] private ChainCostDisplay chainCostDisplay;
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
    private bool wasShopOpenBeforeGhost = false;
    private bool isHidingShopForGhost = false;
    private Vector3 originalPosition;
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

    // Defence chain build mode state
    private bool isDefenceChainModeActive = false;
    private bool isDefenceChainDragging = false; // Track if user is actively dragging
    private Vector2Int initialDefenceCell;
    private Vector2Int lastDefenceHoverCell = Vector2Int.one * -1; // Track last hovered cell
    private List<GameObject> defenceGhostChain = new List<GameObject>();

    [SerializeField] private GridDataGenerator gridDataGenerator;

    //list of buildings for repair
    private BuildingManager buildingManager;



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

        // Subscribe to structure destruction events to update synergies
        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.OnStructureDestroyed.AddListener(HandleStructureDestroyed);
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

        // Unsubscribe from structure destruction events
        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.OnStructureDestroyed.RemoveListener(HandleStructureDestroyed);
        }
    }

    public void HandleShopOpened()
    {
        // Prevent infinite loops
        if (isBuildModeActive) return;

        gridController.ShowGrid();
        EnableBuildMode();
    }

    public void HandleShopClosed()
    {
        // Clear tutorial highlighting when shop closes
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.CleanupShopHighlights();
        }

        // If we're intentionally hiding the shop for ghost creation, don't disable build mode
        if (isHidingShopForGhost)
        {
            isHidingShopForGhost = false;
            return;
        }

        DisableBuildMode();
        gridController.HideGrid();
    }

    public void HandleStructureDestroyed(Structure destroyedStructure)
    {
        // When a structure is destroyed, recalculate all synergies
        // since distances between remaining structures may have changed
        UpdateAllSynergies();
    }

    void Update()
    {
        // Debug: Check if we're getting input but build mode isn't active
        if (Input.GetKey(removeModifierKey) && (!isBuildModeActive && !isMoveModeActive))
        {
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
        if (isDefenceChainModeActive)
        {
            UpdateDefenceGhostChain();
        }
        else
        {
            if (!isDeleteModeActive && currentGhost != null && !isMoveModeActive) UpdateGhostPosition();
            if (isMoveModeActive && currentGhost != null) UpdateGhostPositionForMove();
        }
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

        // Clean up defense chain mode
        if (isDefenceChainModeActive)
        {
            CancelDefenceChain();
        }

        // IMPORTANT: If a structure is being moved, place it immediately at current position
        if (movingStructure != null)
        {
            // Get current ghost position (where the structure would be placed)
            Vector3 currentPosition = currentGhost != null ? currentGhost.transform.position : originalPosition;
            Vector2Int gridCoords = gridController.WorldToGridCoords(currentPosition);

            // Check if current position is valid (skip money check since we're moving)
            if (IsValidPlacement(gridCoords.x, gridCoords.y, checkMoney: false))
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
            // If we had a shop open before the ghost, restore it when disabling build mode
            bool shouldRestoreShop = wasShopOpenBeforeGhost;

            Destroy(currentGhost);
            currentGhost = null;

            if (shouldRestoreShop && ShopUIManager.Instance != null)
            {
                ShopUIManager.Instance.OpenShop();
            }
            wasShopOpenBeforeGhost = false;
            isHidingShopForGhost = false;
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
    public bool IsMoveModeActive() => isMoveModeActive;
    public bool IsDeleteModeActive() => isDeleteModeActive;
    public bool HasActiveGhost() => currentGhost != null;

    public void ToggleMoveMode()
    {
        if (isMoveModeActive) CancelMove();
        else
        {
            // Check if it's night time or paused - prevent move mode
            NightManager nightManager = NightManager.Instance;
            if (nightManager != null && (!nightManager.IsDay || nightManager.getIsPaused()))
            {
                return;
            }
            
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
        
        // Check if it's night time or paused - prevent move mode
        NightManager nightManager = NightManager.Instance;
        if (nightManager != null && (!nightManager.IsDay || nightManager.getIsPaused()))
        {
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
        
        // IMPORTANT: For defense structures, manually unregister from defense registry
        DefenseStructure defenseStructure = structure.GetComponent<DefenseStructure>();
        if (defenseStructure != null)
        {
            var unregisterMethod = typeof(DefenseStructure).GetMethod("UnregisterFromRegistry", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            unregisterMethod?.Invoke(defenseStructure, null);
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
        isGhostTemporarilyHidden = false;
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
            // Handle defense chain mode
            if (isDefenceChainModeActive)
            {
                // Mouse button held down = dragging
                if (Input.GetMouseButton(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    if (!isDefenceChainDragging)
                    {
                        // Just started dragging
                        isDefenceChainDragging = true;
                    }
                }
                // Mouse button released
                else if (Input.GetMouseButtonUp(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    
                    // Check if user dragged (creating a chain of 2+) or just clicked (single fence)
                    if (isDefenceChainDragging && defenceGhostChain.Count >= 2)
                    {
                        // User dragged and created a chain - place all
                        FinalizeDefenceChain();
                    }
                    else
                    {
                        // Single click (no drag or only 1 fence) - place just one fence at initial position
                        PlaceSingleDefenceStructure();
                    }
                }
                // Right click to cancel chain (works during drag or before placement)
                if (Input.GetMouseButtonDown(1) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    CancelDefenceChain();
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(1) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) CancelCurrentBuilding();
                else if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    if (isDeleteModeActive)
                    {
                        // Debug.Log("Left click detected in delete mode");
                        if (TryRemoveStructureByRaycast())
                        {
                            // Debug.Log("Structure removed by raycast");
                            return;
                        }
                        Vector2Int hoveredCell = gridController.GetCurrentHoveredCell();
                        // Debug.Log($"Attempting to remove at grid cell: {hoveredCell}");
                        RemoveItem(hoveredCell.x, hoveredCell.y);
                    }
                    // Check if this is a defense type that should start chain mode
                    else if (currentBuildTargetPrefab != null && IsDefenceType(currentStructureData))
                    {
                        Vector2Int hoveredCell = GetGridCellUnderCursor(true);
                        
                        // Start chain building on mouse down (drag will follow)
                        StartDefenceChain(hoveredCell);
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

    // Helper: Is this a defence type structure?
    private bool IsDefenceType(StructureData data)
    {
        if (data == null)
        {
            return false;
        }

        string structureName = data.structureName.ToLower();

        // Add your defense structure type checks here
        bool isDefense = structureName.Contains("fence") ||
                        structureName.Contains("wall") ||
                        structureName.Contains("turret") ||
                        structureName.Contains("barrier") ||
                        structureName.Contains("barricade") ||
                        structureName.Contains("defense") ||
                        structureName.Contains("defence") ||
                        structureName.Contains("hay") ||
                        structureName.Contains("bale");

        return isDefense;
    }

    // Start defence chain mode
    private void StartDefenceChain(Vector2Int startCell)
    {
        
        // Validate the starting cell
        if (!IsValidPlacement(startCell.x, startCell.y))
        {
            return;
        }

        // Check if player can afford at least one structure
        if (currentStructureData != null && MoneyManager.Instance != null)
        {
            if (!MoneyManager.Instance.CanAfford(currentStructureData.cost))
            {
                // Play insufficient funds sound
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayInsufficientFundsSound();
                }
                return;
            }
        }

        // Start chain mode
        isDefenceChainModeActive = true;
        isDefenceChainDragging = false;
        initialDefenceCell = startCell;
        lastDefenceHoverCell = Vector2Int.one * -1;
        ClearDefenceGhostChain();
        
        // Hide the main ghost during chain building
        if (currentGhost != null) 
        {
            currentGhost.SetActive(false);
        }
    }

    // Cancel defence chain mode (don't place anything)
    private void CancelDefenceChain()
    {
        
        isDefenceChainModeActive = false;
        isDefenceChainDragging = false;
        ClearDefenceGhostChain();
        
        // Restore the main ghost
        if (currentGhost != null) 
        {
            currentGhost.SetActive(true);
        }
    }

    // Place a single defense structure (for single click without drag)
    private void PlaceSingleDefenceStructure()
    {
        
        if (currentStructureData == null || MoneyManager.Instance == null)
        {
            CancelDefenceChain();
            return;
        }

        // Check if player can afford one structure
        int singleCost = currentStructureData.cost;
        if (!MoneyManager.Instance.CanAfford(singleCost))
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayInsufficientFundsSound();
            }
            CancelDefenceChain();
            return;
        }

        // Spend money and place at initial cell
        MoneyManager.Instance.SpendMoney(singleCost);

        if (IsValidPlacementForChain(initialDefenceCell.x, initialDefenceCell.y))
        {
            PlaceItemWithoutMoneyCheck(initialDefenceCell.x, initialDefenceCell.y, playSound: true);

            // Tutorial trigger for single fence placement
            HandleChainTutorialTrigger(1, false);

            // Update connectors after placement
            StartCoroutine(DelayedConnectorRebuild());
        }
        else
        {
            Debug.LogWarning($"PlaceSingleDefenceStructure: Cannot place at {initialDefenceCell} - invalid position");
        }

        // Clean up chain mode
        isDefenceChainModeActive = false;
        isDefenceChainDragging = false;
        ClearDefenceGhostChain();
        if (currentGhost != null) currentGhost.SetActive(true);
    }

    // Finalize defence chain: place real objects
    private void FinalizeDefenceChain()
    {
        if (!isDefenceChainModeActive || defenceGhostChain.Count == 0)
        {
            CancelDefenceChain();
            return;
        }

        // Tutorial validation for chain length
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
        {
            string currentStepId = TutorialManager.Instance.GetCurrentStepId();
            if (currentStepId == "build_wall_chain")
            {
                int currentHayBales = CountHayBales();
                int willHave = currentHayBales + defenceGhostChain.Count;
                
                if (willHave < 10)
                {
                    // Allow it but provide feedback
                }
            }
        }

        // Check if player can afford all remaining structures
        int totalCost = defenceGhostChain.Count * (currentStructureData?.cost ?? 0);
        if (currentStructureData != null && MoneyManager.Instance != null && !MoneyManager.Instance.CanAfford(totalCost))
        {
            // Play insufficient funds sound for defence chain
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayInsufficientFundsSound();
            }
            return;
        }

        // Spend money for entire chain
        if (currentStructureData != null && MoneyManager.Instance != null)
        {
            MoneyManager.Instance.SpendMoney(totalCost);
        }

        // Place all structures in the chain (suppress sound for each individual placement)
        int placedCount = 0;
        foreach (GameObject ghost in defenceGhostChain)
        {
            Vector2Int gridCoords = gridController.WorldToGridCoords(ghost.transform.position);

            // Check if this position is still valid before placing
            if (IsValidPlacementForChain(gridCoords.x, gridCoords.y))
            {
                PlaceItemWithoutMoneyCheck(gridCoords.x, gridCoords.y, playSound: false); // Suppress sound for individual placements
                placedCount++;
            }
            else
            {
                Debug.LogWarning($"FinalizeDefenceChain: Cannot place structure at {gridCoords} - position no longer valid");
            }
        }

        // Play build sound once after all structures are placed
        if (placedCount > 0)
        {
            AudioManager.Instance?.PlayPlaceSound();
        }

        // Tutorial trigger for chain completion
        if (placedCount > 0)
        {
            HandleChainTutorialTrigger(placedCount, false); // Multiple structures, was completed
        }

        // Update connectors for all defense structures after chain placement
        if (placedCount > 0)
        {
            // Start coroutine to rebuild connectors after a short delay
            StartCoroutine(DelayedConnectorRebuild());
        }

        // Clean up
        CancelDefenceChain();
        if (currentGhost != null) currentGhost.SetActive(true);
    }

    // Coroutine to rebuild connectors after a delay (ensures all structures are properly registered)
    private IEnumerator DelayedConnectorRebuild()
    {
        // Wait a couple frames to ensure all defense structures are properly registered
        yield return null;
        yield return null;

        DefenseStructure.RebuildAllConnectors();
    }

    // Update ghost chain as mouse moves
    private void UpdateDefenceGhostChain()
    {
        Vector2Int hoveredCell = GetGridCellUnderCursor(true);

        if (hoveredCell != lastDefenceHoverCell && hoveredCell != initialDefenceCell)
        {
            lastDefenceHoverCell = hoveredCell;
            CreateDefenceGhostChain(initialDefenceCell, hoveredCell);
        }
    }

    // Create ghost chain between two cells
    private void CreateDefenceGhostChain(Vector2Int start, Vector2Int end)
    {
        ClearDefenceGhostChain();

        List<Vector2Int> cellsBetween = GetCellsBetween(start, end);

        // Check tutorial restrictions for chain length
        int maxChainLength = GetMaxChainLengthForTutorial();
        
        // Calculate budget limit
        int currentMoney = MoneyManager.Instance != null ? MoneyManager.Instance.GetCurrentMoney() : 0;
        int structureCost = currentStructureData != null ? currentStructureData.cost : 0;
        int affordableCount = structureCost > 0 ? currentMoney / structureCost : cellsBetween.Count;
        
        int validGhostCount = 0;
        
        // Include ALL cells (including the start cell) since we're now using click-and-drag
        for (int i = 0; i < cellsBetween.Count && i < maxChainLength; i++)
        {
            Vector2Int cell = cellsBetween[i];
            if (IsValidPlacementForChain(cell.x, cell.y))
            {
                Vector3 worldPos = gridController.GetCellCenterFromTexture(cell.x, cell.y);
                
                // Create new ghost object
                GameObject ghost = Instantiate(currentBuildTargetPrefab, worldPos, currentRotation);
                
                // IMPORTANT: Disable DefenseStructure component on ghost chain objects to prevent connector creation
                DefenseStructure defenseComponent = ghost.GetComponent<DefenseStructure>();
                if (defenseComponent != null)
                {
                    defenseComponent.enabled = false;
                }
                
                // Determine if this ghost is affordable
                bool isAffordable = validGhostCount < affordableCount;
                
                // Apply appropriate material based on affordability
                if (isAffordable)
                {
                    ApplyGhostMaterial(ghost);
                }
                else
                {
                    ApplyGhostErrorMaterial(ghost);
                }
                
                defenceGhostChain.Add(ghost);
                validGhostCount++;
            }
        }
        
        // Update cost display if there are any ghosts in chain
        if (validGhostCount > 0)
        {
            int totalCost = validGhostCount * structureCost;
            UpdateChainCostDisplay(totalCost, affordableCount, validGhostCount);
        }
    }

    // Clear all ghost chain objects
    private void ClearDefenceGhostChain()
    {
        foreach (GameObject ghost in defenceGhostChain)
        {
            if (ghost != null)
            {
                Destroy(ghost);
            }
        }
        defenceGhostChain.Clear();
        
        // Hide the cost display when clearing chain
        if (chainCostDisplay != null)
        {
            chainCostDisplay.HideCostDisplay();
        }
    }

    // Get ghost object from pool or create new one
    // Stepped diagonal algorithm - moves in staircase pattern
    private List<Vector2Int> GetCellsBetween(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        cells.Add(start); // Always include start cell

        int currentX = start.x;
        int currentY = start.y;

        int deltaX = end.x - start.x;
        int deltaY = end.y - start.y;

        int remainingX = Mathf.Abs(deltaX);
        int remainingY = Mathf.Abs(deltaY);

        int stepX = deltaX > 0 ? 1 : -1;
        int stepY = deltaY > 0 ? 1 : -1;

        while (currentX != end.x || currentY != end.y)
        {
            bool moveX = false;
            bool moveY = false;

            if (currentX != end.x && currentY != end.y)
            {
                // Choose direction based on remaining distance
                // This creates a staircase pattern that prioritizes the longer remaining distance

                if (remainingX >= remainingY)
                {
                    moveX = true;
                }
                else
                {
                    moveY = true;
                }
            }
            else if (currentX != end.x)
            {
                moveX = true;
            }
            else if (currentY != end.y)
            {
                moveY = true;
            }

            if (moveX)
            {
                currentX += stepX;
                remainingX--;
            }
            if (moveY)
            {
                currentY += stepY;
                remainingY--;
            }

            cells.Add(new Vector2Int(currentX, currentY));
        }

        return cells;
    }

    void PlaceMovedStructure(int x, int y)
    {
        if (!IsValidPlacementWithoutMoney(x, y)) return; // Use no-money check for moving existing structures
        Vector3 cellCenter = gridController.GetCellCenterFromTexture(x, y);
        movingStructure.transform.position = cellCenter;
        movingStructure.transform.rotation = currentRotation;
        movingStructure.gameObject.SetActive(true);
        List<Vector2Int> newFootprint = GetStructureFootprint(movingStructure.gameObject);
        foreach (Vector2Int cell in newFootprint) gridController.SetCellOccupied(cell.x, cell.y, true);
        movingStructure.RegisterWithGrid();
        
        // IMPORTANT: For defense structures, force re-registration in the defense registry
        DefenseStructure defenseStructure = movingStructure.GetComponent<DefenseStructure>();
        if (defenseStructure != null)
        {
            // Manually trigger defense structure registration by calling Start via reflection
            // Or use a coroutine to rebuild connectors after a delay
            StartCoroutine(DelayedDefenseStructureUpdate(defenseStructure));
        }
        
        AudioManager.Instance?.PlayPlaceSound();
        gridController.UpdateGridTexture();
        if (gridMonitor != null && newFootprint.Count > 0) gridMonitor.NotifyMultipleCellsChanged(newFootprint, GridChangeType.Structural);

        // IMPORTANT: Recalculate all synergies after moving a structure
        UpdateAllSynergies();

        AnimalStructure animal = movingStructure.GetComponent<AnimalStructure>();
        if (animal != null)
        {
            animal.OnMoved();
        }

        movingStructure = null;
        isMoveModeActive = false;
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }
        currentBuildTargetPrefab = null; // Clear the build target to prevent shop from entering build mode
        currentStructureData = null; // Also clear the structure data
        DisableBuildMode();
    }

    // Coroutine to handle defense structure re-registration and connector updates after moving
    private IEnumerator DelayedDefenseStructureUpdate(DefenseStructure defenseStructure)
    {
        // Wait a frame to ensure position is finalized
        yield return null;
        
        
        // Use reflection to call the private RegisterInRegistry method
        var registerMethod = typeof(DefenseStructure).GetMethod("RegisterInRegistry", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (registerMethod != null)
        {
            registerMethod.Invoke(defenseStructure, null);
        }
        else
        {
            Debug.LogError("Could not find RegisterInRegistry method");
        }
        
        // Wait another frame for registration to complete
        yield return null;
        
        // Now rebuild all connectors
        DefenseStructure.RebuildAllConnectors();
    }

    // Coroutine to rebuild connectors after moving a fence (ensures proper registration)
    private IEnumerator DelayedConnectorRebuildAfterMove()
    {
        // Wait a couple frames to ensure the moved defense structure is properly registered
        yield return null;
        yield return null;

        DefenseStructure.RebuildAllConnectors();
    }

    private void UpdateAllSynergies()
    {
        // Update all animal synergies (for silo food efficiency)
        AnimalStructure[] allAnimals = FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None);
        foreach (AnimalStructure animal in allAnimals)
        {
            if (animal != null)
            {
                animal.updateSiloSynergy();
            }
        }

        // Update all crop synergies (for silo harvest bonuses)
        CropStructure.UpdateAllCropSynergies();

        // Update barracks synergies if needed (they calculate discounts based on nearby animals)
        BarracksStructure[] allBarracks = FindObjectsByType<BarracksStructure>(FindObjectsSortMode.None);
        foreach (BarracksStructure barracks in allBarracks)
        {
            if (barracks != null)
            {
                // Barracks synergy is calculated on-demand when recruiting, so no need to cache
                // But we could trigger a UI update if the barracks UI is open
            }
        }
    }

    public void CancelMove()
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
        currentBuildTargetPrefab = null; // Clear the build target to prevent shop from entering build mode
        currentStructureData = null; // Also clear the structure data
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
        
        // Use different validation logic for move mode vs build mode
        bool isValidPlacement;
        bool canAfford = true; // Default to true for move mode
        
        if (isMoveModeActive)
        {
            // For moving existing structures, don't check money
            isValidPlacement = IsValidPlacementWithoutMoney(useX, useY);
            canAfford = true; // Always "affordable" when moving existing structures
        }
        else
        {
            // For building new structures, check both placement and money
            isValidPlacement = IsValidPlacement(useX, useY);
            canAfford = currentStructureData != null && MoneyManager.Instance != null && MoneyManager.Instance.CanAfford(currentStructureData.cost);
        }

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

    // private void ShowSiloSynergyPreview()
    // {
    //     Debug.Log("here is the distance for the crop synergy: " + currentStructureData.cropSiloSynergyRange + " the current structure is: " + currentStructureData.structureName);
    //     // Debug.Log("here is the distance for animal synergy: " + currentStructureData.cropSiloSynergyRange);
    //     CreateRangeIndicator(currentGhost.transform.position, currentStructureData.siloSynergyRange, potentialSynergyMaterial, "Animal Synergy Range");
    //     CreateRangeIndicator(currentGhost.transform.position, currentStructureData.cropSiloSynergyRange, potentialSynergyMaterial, "Crop Synergy Range");
    //     // CreateRangeIndicator(currentGhost.transform.position, 15f, potentialSynergyMaterial, "Animal Synergy Range");
    //     // CreateRangeIndicator(currentGhost.transform.position, 10f, potentialSynergyMaterial, "Crop Synergy Range");
    //     foreach (var animal in FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None))
    //         // if ((currentGhost.transform.position - animal.transform.position).sqrMagnitude <= 225f)
    //         if ((currentGhost.transform.position - animal.transform.position).sqrMagnitude <= currentStructureData.siloSynergyRange * currentStructureData.siloSynergyRange)
    //             CreateSynergyLine(currentGhost.transform.position, animal.transform.position, Color.green, "Silo-Animal"); ;
    //     foreach (var crop in FindObjectsByType<CropStructure>(FindObjectsSortMode.None))
    //         // if (crop != null && (currentGhost.transform.position - crop.transform.position).sqrMagnitude <= 100f)
    //         if (crop != null && (currentGhost.transform.position - crop.transform.position).sqrMagnitude <= currentStructureData.cropSiloSynergyRange * currentStructureData.cropSiloSynergyRange)
    //             CreateSynergyLine(currentGhost.transform.position, crop.transform.position, Color.green, "Silo-Crop");
    // }

    // private void ShowAnimalSynergyPreview()
    // {
    //     foreach (var silo in FindObjectsByType<SiloStructure>(FindObjectsSortMode.None))
    //     {
    //         float range = silo.structureData.siloSynergyRange;
    //         if ((currentGhost.transform.position - silo.transform.position).sqrMagnitude <= range * range)
    //             CreateSynergyLine(currentGhost.transform.position, silo.transform.position, Color.green, "Silo-Animal");
    //     }
    //     AnimalStructure animalStructure = currentGhost.GetComponent<AnimalStructure>();
    //     if (animalStructure != null)
    //         foreach (var barrack in FindObjectsByType<BarracksStructure>(FindObjectsSortMode.None))
    //             if (barrack.TargetAnimalType == animalStructure.GetAnimalType.ToString())
    //             {
    //                 float sqrDistance = (currentGhost.transform.position - barrack.transform.position).sqrMagnitude;
    //                 if (sqrDistance <= barrack.synergyMaxDist * barrack.synergyMaxDist)
    //                 {
    //                     float distance = Mathf.Sqrt(sqrDistance);
    //                     Color lineColor = distance <= barrack.synergyMinDist ? Color.red : Color.green;
    //                     CreateSynergyLine(currentGhost.transform.position, barrack.transform.position, lineColor, $"Barracks-{animalStructure.GetAnimalType}");
    //                 }
    //             }
    // }

    // private void ShowBarracksSynergyPreview()
    // {
    //     BarracksStructure barracksStructure = currentGhost.GetComponent<BarracksStructure>();
    //     if (barracksStructure == null) return;
    //     string targetType = barracksStructure.TargetAnimalType;
    //     CreateRangeIndicator(currentGhost.transform.position, barracksStructure.synergyMinDist, validSynergyMaterial, "Optimal Synergy Range");
    //     CreateRangeIndicator(currentGhost.transform.position, barracksStructure.synergyMaxDist, potentialSynergyMaterial, "Maximum Synergy Range");
    //     foreach (var animal in FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None))
    //         if (animal.GetAnimalType.ToString() == targetType)
    //         {
    //             float sqrDistance = (currentGhost.transform.position - animal.transform.position).sqrMagnitude;
    //             if (sqrDistance <= barracksStructure.synergyMaxDist * barracksStructure.synergyMaxDist)
    //             {
    //                 float distance = Mathf.Sqrt(sqrDistance);
    //                 Color lineColor = distance <= barracksStructure.synergyMinDist ? Color.red : Color.green;
    //                 CreateSynergyLine(currentGhost.transform.position, animal.transform.position, lineColor, $"Barracks-{targetType}");
    //             }
    //         }
    // }

    // private void ShowCropSynergyPreview()
    // {
    //     foreach (var silo in FindObjectsByType<SiloStructure>(FindObjectsSortMode.None))
    //         // if (silo != null && (currentGhost.transform.position - silo.transform.position).sqrMagnitude <= 100f)
    //         if (silo != null && (currentGhost.transform.position - silo.transform.position).sqrMagnitude <=  currentStructureData.cropSiloSynergyRange * currentStructureData.cropSiloSynergyRange)
    //             CreateSynergyLine(currentGhost.transform.position, silo.transform.position, Color.green, "Silo-Crop");
    // }

    private void ShowSiloSynergyPreview()
{

    // Draw circles still in world space
    CreateRangeIndicator(currentGhost.transform.position, currentStructureData.siloSynergyRange * gridController.GetCellSize(), potentialSynergyMaterial, "Animal Synergy Range");
    CreateRangeIndicator(currentGhost.transform.position, currentStructureData.cropSiloSynergyRange * gridController.GetCellSize(), potentialSynergyMaterial, "Crop Synergy Range");

    Vector2Int ghostCell = gridController.WorldToGridCoords(currentGhost.transform.position);

    foreach (var animal in FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None))
    {
        Vector2Int animalCell = gridController.WorldToGridCoords(animal.transform.position);
        if (GridDistance(ghostCell, animalCell) <= currentStructureData.siloSynergyRange)
            CreateSynergyLine(currentGhost.transform.position, animal.transform.position, Color.green, "Silo-Animal");
    }

    foreach (var crop in FindObjectsByType<CropStructure>(FindObjectsSortMode.None))
    {
        Vector2Int cropCell = gridController.WorldToGridCoords(crop.transform.position);
        if (GridDistance(ghostCell, cropCell) <= currentStructureData.cropSiloSynergyRange)
            CreateSynergyLine(currentGhost.transform.position, crop.transform.position, Color.green, "Silo-Crop");
    }
}

private void ShowAnimalSynergyPreview()
{
    Vector2Int ghostCell = gridController.WorldToGridCoords(currentGhost.transform.position);

    foreach (var silo in FindObjectsByType<SiloStructure>(FindObjectsSortMode.None))
    {
        Vector2Int siloCell = gridController.WorldToGridCoords(silo.transform.position);
        if (GridDistance(ghostCell, siloCell) <= silo.structureData.siloSynergyRange)
            CreateSynergyLine(currentGhost.transform.position, silo.transform.position, Color.green, "Silo-Animal");
    }

    AnimalStructure animalStructure = currentGhost.GetComponent<AnimalStructure>();
    if (animalStructure != null)
    {
        foreach (var barrack in FindObjectsByType<BarracksStructure>(FindObjectsSortMode.None))
        {
            if (barrack.TargetAnimalType == animalStructure.GetAnimalType.ToString())
            {
                Vector2Int barrackCell = gridController.WorldToGridCoords(barrack.transform.position);
                int dist = GridDistance(ghostCell, barrackCell);

                if (dist <= barrack.synergyMaxDist)
                {
                    Color lineColor = dist <= barrack.synergyMinDist ? Color.red : Color.green;
                    CreateSynergyLine(currentGhost.transform.position, barrack.transform.position, lineColor, $"Barracks-{animalStructure.GetAnimalType}");
                }
            }
        }
    }
}

private void ShowBarracksSynergyPreview()
{
    BarracksStructure barracksStructure = currentGhost.GetComponent<BarracksStructure>();
    if (barracksStructure == null) return;

    string targetType = barracksStructure.TargetAnimalType;

    CreateRangeIndicator(currentGhost.transform.position, barracksStructure.synergyMinDist * gridController.GetCellSize(), validSynergyMaterial, "Optimal Synergy Range");
    CreateRangeIndicator(currentGhost.transform.position, barracksStructure.synergyMaxDist * gridController.GetCellSize(), potentialSynergyMaterial, "Maximum Synergy Range");

    Vector2Int ghostCell = gridController.WorldToGridCoords(currentGhost.transform.position);

    foreach (var animal in FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None))
    {
        if (animal.GetAnimalType.ToString() == targetType)
        {
            Vector2Int animalCell = gridController.WorldToGridCoords(animal.transform.position);
            int dist = GridDistance(ghostCell, animalCell);

            if (dist <= barracksStructure.synergyMaxDist)
            {
                Color lineColor = dist <= barracksStructure.synergyMinDist ? Color.red : Color.green;
                CreateSynergyLine(currentGhost.transform.position, animal.transform.position, lineColor, $"Barracks-{targetType}");
            }
        }
    }
}

private void ShowCropSynergyPreview()
{
    Vector2Int ghostCell = gridController.WorldToGridCoords(currentGhost.transform.position);

    foreach (var silo in FindObjectsByType<SiloStructure>(FindObjectsSortMode.None))
    {
        Vector2Int siloCell = gridController.WorldToGridCoords(silo.transform.position);
        if (GridDistance(ghostCell, siloCell) <= currentStructureData.cropSiloSynergyRange)
            CreateSynergyLine(currentGhost.transform.position, silo.transform.position, Color.green, "Silo-Crop");
    }
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

        textObj.transform.position = new Vector3(position.x, position.y + 5f, position.z);

        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.color = color;
        textMesh.fontSize = 50;        // internal font size
        textMesh.characterSize = 0.2f;   // world-space scale

        // textMesh.characterSize = 0.1f;
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;
        MeshRenderer renderer = textObj.GetComponent<MeshRenderer>();
        renderer.material.shader = Shader.Find("GUI/Text Shader");

        // Create a simple billboard script to make text face camera
        textObj.AddComponent<SimpleBillboard>();

        // Add to synergy indicators for cleanup
        synergyIndicators.Add(textObj);
    }

    private string GetBonusSummary(string synergyType, Color color)
    {
        if (color == Color.green)
        {
            if (synergyType.Contains("Silo-Animal")) return "20% Less Food Needed";
            if (synergyType.Contains("Silo-Crop")) return "+50% Harvest";
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

    private int GridDistance(Vector2Int a, Vector2Int b)
{
    int dx = a.x - b.x;
    int dy = a.y - b.y;
    return Mathf.RoundToInt(Mathf.Sqrt(dx * dx + dy * dy));
}








    // private void ClearSynergyVisualization()
    // {
    //     foreach (var indicator in synergyIndicators) if (indicator != null) Destroy(indicator);
    //     synergyIndicators.Clear();
    //     foreach (var line in synergyLines) if (line != null && line.gameObject != null) Destroy(line.gameObject);
    //     synergyLines.Clear();
    //     activeSynergyLines.Clear();
    // }


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
        bool isValidPlacement = IsValidPlacementWithoutMoney(useX, useY); // Use no-money check for move mode
        foreach (Renderer renderer in currentGhost.GetComponentsInChildren<Renderer>())
            renderer.material.color = isValidPlacement ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
        UpdateSynergyVisualization();
    }

    void CreateGhost(GameObject prefab)
    {
        if (currentGhost != null) Destroy(currentGhost);
        if (prefab == null) return;

        // Clear tutorial highlighting when building item is selected
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.CleanupShopHighlights();
        }

        // Store whether shop was open before creating ghost
        if (ShopUIManager.Instance != null && ShopUIManager.Instance.IsShopOpen())
        {
            wasShopOpenBeforeGhost = true;
            isHidingShopForGhost = true; // Set flag before closing shop
            ShopUIManager.Instance.CloseShop();
        }

        currentGhost = Instantiate(prefab);
        currentGhost.name = "BuildGhost";

        // IMPORTANT: Disable DefenseStructure component on ghost objects to prevent connector creation
        DefenseStructure defenseComponent = currentGhost.GetComponent<DefenseStructure>();
        if (defenseComponent != null)
        {
            defenseComponent.enabled = false;
        }

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

    void ApplyGhostErrorMaterial(GameObject obj)
    {
        if (ghostErrorMaterial == null)
        {
            // Create red error material if not assigned
            ghostErrorMaterial = new Material(Shader.Find("Standard"))
            {
                color = new Color(1, 0, 0, 0.5f) // Red and semi-transparent
            };
            ghostErrorMaterial.SetFloat("_Mode", 3);
            ghostErrorMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            ghostErrorMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            ghostErrorMaterial.SetInt("_ZWrite", 0);
            ghostErrorMaterial.DisableKeyword("_ALPHATEST_ON");
            ghostErrorMaterial.EnableKeyword("_ALPHABLEND_ON");
            ghostErrorMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            ghostErrorMaterial.renderQueue = 3000;
        }
        foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>())
            renderer.material = new Material(ghostErrorMaterial);
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

        // Special handling for DefenseStructure - they should only occupy a single cell
        DefenseStructure defenseStructure = obj.GetComponent<DefenseStructure>();
        if (defenseStructure != null)
        {
            Vector2Int gridPos = gridController.WorldToGridCoords(obj.transform.position);
            occupiedCells.Add(gridPos);
            return occupiedCells;
        }

        // Default behavior for other structures
        Renderer renderer = obj.GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            Debug.LogWarning($"No renderer found for object {obj.name}");
            return occupiedCells;
        }

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
                    {
                        occupiedCells.Add(new Vector2Int(x, y));
                    }
                }
            }

        }
        return occupiedCells;
    }

    // Cached footprint data to avoid expensive instantiate/destroy operations
    private List<Vector2Int> cachedStructureFootprint = null;
    private GameObject cachedPrefabReference = null;
    private Quaternion cachedRotation;

    // Get structure footprint at a specific grid position (optimized for defence chains)
    private List<Vector2Int> GetStructureFootprintAtPosition(int x, int y)
    {
        List<Vector2Int> relativeFootprint = GetCachedStructureFootprint();
        List<Vector2Int> absoluteFootprint = new List<Vector2Int>();

        foreach (Vector2Int cell in relativeFootprint)
        {
            absoluteFootprint.Add(new Vector2Int(x + cell.x, y + cell.y));
        }

        return absoluteFootprint;
    }

    // Get cached relative footprint to avoid expensive instantiate operations
    private List<Vector2Int> GetCachedStructureFootprint()
    {
        // Check if cache is still valid
        if (cachedStructureFootprint == null ||
            cachedPrefabReference != currentBuildTargetPrefab ||
            cachedRotation != currentRotation)
        {
            RecalculateStructureFootprintCache();
        }

        return cachedStructureFootprint;
    }

    // Calculate and cache the structure footprint once per prefab/rotation combo
    private void RecalculateStructureFootprintCache()
    {
        cachedStructureFootprint = new List<Vector2Int>();
        cachedPrefabReference = currentBuildTargetPrefab;
        cachedRotation = currentRotation;

        if (currentBuildTargetPrefab == null)
        {
            Debug.LogWarning("Cannot calculate footprint cache - no prefab selected");
            return;
        }

        // Temporarily instantiate to get accurate bounds
        GameObject tempObj = Instantiate(currentBuildTargetPrefab);
        tempObj.transform.rotation = currentRotation;
        Renderer renderer = tempObj.GetComponentInChildren<Renderer>();

        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            bounds.Expand(-0.1f);

            // Calculate relative to origin (0,0)
            Vector2Int bottomLeft = gridController.WorldToGridCoords(bounds.min);
            Vector2Int topRight = gridController.WorldToGridCoords(bounds.max);

            // Store as relative coordinates (offset from structure center)
            for (int x = bottomLeft.x; x <= topRight.x; x++)
            {
                for (int y = bottomLeft.y; y <= topRight.y; y++)
                {
                    if (gridController.IsValidCell(x, y))
                    {
                        Vector3 cellCenter = gridController.GetCellCenterFromTexture(x, y);
                        if (bounds.Contains(new Vector3(cellCenter.x, bounds.center.y, cellCenter.z)))
                        {
                            cachedStructureFootprint.Add(new Vector2Int(x, y));
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"No renderer found on prefab {currentBuildTargetPrefab.name}");
            cachedStructureFootprint.Add(Vector2Int.zero); // Default single cell
        }

        DestroyImmediate(tempObj);
        Debug.Log($"Cached footprint for {currentBuildTargetPrefab.name}: {cachedStructureFootprint.Count} cells");
    }

    bool IsValidPlacement(int x, int y, bool checkMoney = true)
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
            Debug.LogWarning($"Placement blocked: ({x},{y}) is outside logical grid bounds (0,0) to ({gridDataGenerator.GetGridWidth() - 1},{gridDataGenerator.GetGridHeight() - 1})");
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

        // Check money only if requested (skip when moving structures)
        if (checkMoney && currentStructureData != null && MoneyManager.Instance != null && !MoneyManager.Instance.CanAfford(currentStructureData.cost))
            return false;

        return true;
    }

    void PlaceItem(int x, int y)
    {
        if (!IsValidPlacement(x, y)) return;
        
        // Check if we can afford the structure
        if (currentStructureData != null && MoneyManager.Instance != null && !MoneyManager.Instance.SpendMoney(currentStructureData.cost))
        {
            // Play insufficient funds sound when can't afford building
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayInsufficientFundsSound();
            }
            // NEW: Show notification for insufficient funds
            if (NotificationManager.Instance != null && currentStructureData != null)
            {
                NotificationManager.ShowError("Not Enough Money!", $"Need ${currentStructureData.cost} to build {currentStructureData.structureName}");
            }
            return;
        }

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

        if(currentStructureData != null && currentStructureData.structureName.ToLower().Contains("farm house"))
        {
            // Debug.LogWarning("Placing Farm House this will not be added to the building list");
            //do not add it to the buidling manager
        }
        else
        {
            BuildingManager.Instance?.addBuilding(placedItem);
        }

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

        // IMPORTANT: Recalculate all synergies after placing a new structure
        UpdateAllSynergies();

        // Hide the info card immediately after placement to prevent UI conflicts
        ItemHoverPanel.Instance?.HideImmediate();

        // NEW: Reopen shop after placing item, except for defense structures (walls, hay bales, etc.)
        if (wasShopOpenBeforeGhost && structure != null && !IsDefenceType(currentStructureData))
        {
            // Clear the ghost and exit build mode first, then reopen shop
            if (currentGhost != null)
            {
                Destroy(currentGhost);
                currentGhost = null;
            }
            currentBuildTargetPrefab = null;
            isBuildModeActive = false;
            gridController.HideGrid();

            // Now reopen the shop
            if (ShopUIManager.Instance != null)
            {
                ShopUIManager.Instance.OpenShop();
            }

            wasShopOpenBeforeGhost = false;
            isHidingShopForGhost = false;
        }
    }

    // Place item without money check (used for defence chains where money is already spent)
    private void PlaceItemWithoutMoneyCheck(int x, int y, bool playSound = true)
    {
        
        if (currentBuildTargetPrefab == null)
        {
            Debug.LogError($"PlaceItemWithoutMoneyCheck: currentBuildTargetPrefab is null! Cannot place structure at ({x}, {y})");
            return;
        }

        // Use chain-specific validation instead of the ghost-based one
        if (!IsValidPlacementForChain(x, y))
        {
            Debug.LogWarning($"PlaceItemWithoutMoneyCheck: Invalid placement at ({x}, {y})");
            return;
        }

        

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

        if (structure != null && !structure.GetStructureName().ToLower().Contains("farm house"))
        {
            BuildingManager.Instance?.addBuilding(placedItem);
            // Debug.Log($"Added {placedItem.name} to BuildingManager list");
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
        
        // Only play sound if requested (to avoid multiple sounds in chain building)
        if (playSound)
        {
            AudioManager.Instance?.PlayPlaceSound();
        }
        
        gridController.UpdateGridTexture();
        if (gridMonitor != null && footprint.Count > 0) gridMonitor.NotifyMultipleCellsChanged(footprint, GridChangeType.Structural);

        // IMPORTANT: Recalculate all synergies after placing a new structure
        UpdateAllSynergies();

        // Hide the info card immediately after placement to prevent UI conflicts
        ItemHoverPanel.Instance?.HideImmediate();

        // NEW: Reopen shop after placing item, except for defense structures (walls, hay bales, etc.)
        if (wasShopOpenBeforeGhost && structure != null && !IsDefenceType(currentStructureData))
        {
            // Clear the ghost and exit build mode first, then reopen shop
            if (currentGhost != null)
            {
                Destroy(currentGhost);
                currentGhost = null;
            }
            currentBuildTargetPrefab = null;
            isBuildModeActive = false;
            gridController.HideGrid();

            // Now reopen the shop
            if (ShopUIManager.Instance != null)
            {
                ShopUIManager.Instance.OpenShop();
            }

            wasShopOpenBeforeGhost = false;
            isHidingShopForGhost = false;
        }
    }

    // Check placement validity without money check
    private bool IsValidPlacementWithoutMoney(int x, int y)
    {
        Debug.Log($"IsValidPlacementWithoutMoney at ({x}, {y})");

        // Add null checks FIRST
        if (gridController == null || gridDataGenerator == null)
        {
            Debug.LogError("GridController or GridDataGenerator is null in IsValidPlacementWithoutMoney");
            return false;
        }

        // Check if coordinates are within grid bounds
        if (x < 0 || x >= gridDataGenerator.GetGridWidth() ||
            y < 0 || y >= gridDataGenerator.GetGridHeight())
        {
            Debug.LogWarning($"Coordinates ({x}, {y}) are outside grid bounds");
            return false;
        }

        // Check ownership first for efficiency
        if (CheatManager.Instance != null && CheatManager.Instance.IsUnlimitedBuildingActive())
        {
            Debug.Log("Unlimited building cheat is active - skipping ownership check");
            return true;
        }

        // Check if any cell in the structure footprint is invalid
        if (currentGhost != null)
        {
            List<Vector2Int> footprint = GetStructureFootprint(currentGhost);
            foreach (Vector2Int cell in footprint)
            {
                if (!gridController.IsValidCell(cell.x, cell.y)) return false;

                GridCell gridCell = gridController.GetCell(cell.x, cell.y);
                if (gridCell == null) return false;

                if (gridCell.flags.isOccupied) return false;
                if (!gridCell.flags.isOwned) return false;
            }
        }
        else
        {
            // Fallback for single cell placement
            GridCell cell = gridController.GetCell(x, y);
            if (cell == null) return false;

            if (cell.flags.isOccupied) return false;
            if (!cell.flags.isOwned) return false;
        }

        return true;
    }

    // Check placement validity for chain building (single cell, no money check)
    private bool IsValidPlacementForChain(int x, int y)
    {
        // Add null checks FIRST
        if (gridController == null || gridDataGenerator == null)
        {
            Debug.LogError("GridController or GridDataGenerator is null in IsValidPlacementForChain");
            return false;
        }

        // Check if coordinates are within grid bounds
        if (x < 0 || x >= gridDataGenerator.GetGridWidth() ||
            y < 0 || y >= gridDataGenerator.GetGridHeight())
        {
            return false;
        }

        // Check ownership first for efficiency
        if (CheatManager.Instance != null && CheatManager.Instance.IsUnlimitedBuildingActive())
        {
            return true;
        }

        // Check single cell placement (no footprint calculation needed for chain)
        GridCell cell = gridController.GetCell(x, y);
        if (cell == null) return false;

        if (cell.flags.isOccupied) return false;
        if (!cell.flags.isOwned) return false;

        return true;
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
            case "build_first_wall":
            case "build_first_hay_bale":
            case "build_wall_chain":
                // Allow any defense structure during wall tutorial, plus keep previous buildings available
                return structureName.Contains("fence") || structureName.Contains("wall") || 
                       structureName.Contains("barrier") || structureName.Contains("defense") ||
                       structureName.Contains("defence") || structureName.Contains("hay") ||
                       structureName.Contains("bale") || structureName.Contains("farm house") ||
                       structureName.Contains("chicken") || structureName.Contains("crop") ||
                       structureName.Contains("silo") || structureName.Contains("barracks");
            default:
                return false;
        }
    }

    private int CountHayBales()
    {
        int count = 0;
        DefenseStructure[] allDefenseStructures = FindObjectsByType<DefenseStructure>(FindObjectsSortMode.None);
        
        foreach (DefenseStructure defenseStructure in allDefenseStructures)
        {
            if (defenseStructure != null && !defenseStructure.name.Contains("ghost"))
            {
                string structureName = defenseStructure.GetStructureName().ToLower();
                if (structureName.Contains("hay") || structureName.Contains("bale"))
                {
                    count++;
                }
            }
        }
        
        return count;
    }

    private void HandleTutorialTriggers(Structure structure)
    {
        string name = structure.GetStructureName().ToLower();
        Debug.Log($"[HandleTutorialTriggers] Structure placed: {name}");

        // Handle barracks structures
        if (structure is BarracksStructure barracks)
        {
            string targetAnimalType = barracks.TargetAnimalType.ToLower();
            
            // Notify simplified tutorial directly
            if (targetAnimalType == "chicken")
            {
                TutorialTriggerHelper.TriggerChickenBarracksBuilt();
            }
            
            // Trigger re-search for all barracks when a new barracks is built
            BarracksStructure.UpdateAllNearbyChickenCoops();
            return;
        }
        
        // Handle animal structures (coops)
        if (structure is AnimalStructure animal)
        {
            string animalType = animal.GetAnimalType.ToString().ToLower();
            
            // Notify simplified tutorial directly
            if (animalType == "chicken")
            {
                TutorialTriggerHelper.TriggerChickenCoopBuilt();
            }
            
            // Trigger re-search for all barracks when a new animal structure is built
            BarracksStructure.UpdateAllNearbyChickenCoops();
            return;
        }
        
        // Check if this is a DefenseStructure (wall/fence)
        if (structure is DefenseStructure)
        {
            // Hay bale tutorial triggers are handled by HandleChainTutorialTrigger
            // in CancelDefenceChain and FinalizeDefenceChain methods
            return;
        }
        
        // Handle other structures (farmhouse, crop plot, silo)
        if (name.Contains("farm house") || name.Contains("farmhouse"))
        {
            Debug.Log($"[HandleTutorialTriggers] Farmhouse placed - triggering helper");
            TutorialTriggerHelper.TriggerFarmhouseBuilt();
            isHousePlaced = true;
        }
        else if (name.Contains("crop") || name.Contains("plot"))
        {
            Debug.Log($"[HandleTutorialTriggers] Crop plot placed - triggering helper");
            TutorialTriggerHelper.TriggerCropPlotBuilt();
        }
        else if (name.Contains("silo") || name.Contains("storage"))
        {
            Debug.Log($"[HandleTutorialTriggers] Silo placed - triggering helper");
            TutorialTriggerHelper.TriggerSiloBuilt();
        }
    }

    private void HandleChainTutorialTrigger(int placedCount, bool wasCanceled)
    {
        if (TutorialManager.Instance == null) return;
        
        // Only handle hay bale tutorial triggers
        if (currentStructureData == null || !currentStructureData.structureName.ToLower().Contains("hay")) return;
        
        Debug.Log($"HandleChainTutorialTrigger: placedCount={placedCount}, wasCanceled={wasCanceled}");
        
        var completedSteps = TutorialManager.Instance.GetCompletedStepIds();
        int totalHayBales = CountHayBales();
        
        
        // First hay bale trigger (when user places their first hay bale(s))
        // With click-and-drag, they'll likely place multiple at once
        if (!completedSteps.Contains("build_first_hay_bale") && placedCount > 0)
        {
            TutorialManager.Instance.Trigger(TutorialTrigger.BuiltFirstHayBale);
        }
        // Chain building completion (need 10+ total hay bales)
        if (totalHayBales >= 10 && !completedSteps.Contains("build_wall_chain"))
        {
            TutorialManager.Instance.Trigger(TutorialTrigger.Built10HayBales);
        }
        else if (!completedSteps.Contains("build_wall_chain"))
        {
            int needed = 10 - totalHayBales;
        }
    }

    private int GetMaxChainLengthForTutorial()
    {
        // Check if in tutorial and return appropriate chain length limit
        if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive())
            return int.MaxValue; // No limit outside tutorial

        string currentStepId = TutorialManager.Instance.GetCurrentStepId();
        
        switch (currentStepId)
        {
            case "build_first_hay_bale":
                return 5; // Allow small chain so user can see ghosts and practice canceling
                
            case "build_wall_chain":
                // Allow up to 9 additional structures to reach total of 10
                int currentHayBales = CountHayBales();
                int needed = 10 - currentHayBales;
                return Mathf.Max(1, needed); // At least 1, up to what's needed for 10 total
                
            default:
                return int.MaxValue; // No limit for other tutorial steps
        }
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
                AudioManager.Instance?.PlayErrorSound();
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

            //remove building from the building manager except for farmhouse
            if (!structure.GetStructureName().ToLower().Contains("farm house"))
            {
                BuildingManager.Instance?.removeBuilding(placedItem);
                // Debug.Log($"Removed {placedItem.name} from BuildingManager list");
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
                    AudioManager.Instance?.PlayErrorSound();
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

                BuildingManager.Instance?.removeBuilding(structure.gameObject);

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

    private void UpdateChainCostDisplay(int totalCost, int affordableCount, int totalCount)
    {
        if (chainCostDisplay != null)
        {
            Vector2 mousePosition = Input.mousePosition;
            chainCostDisplay.UpdatePosition(mousePosition, cursorOffset);
            chainCostDisplay.ShowCostDisplay(totalCost, affordableCount, totalCount);
        }
    }

    private void ClearGhost()
    {
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }
        // Reset the shop flags when ghost is cleared
        wasShopOpenBeforeGhost = false;
        isHidingShopForGhost = false;
    }

    public void CancelCurrentBuilding()
    {
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;

            // Restore shop if it was open before creating the ghost
            if (wasShopOpenBeforeGhost && ShopUIManager.Instance != null)
            {
                ShopUIManager.Instance.OpenShop();
                wasShopOpenBeforeGhost = false;
            }
        }
        // Reset flags
        wasShopOpenBeforeGhost = false;
        isHidingShopForGhost = false;

        ClearSynergyVisualization(); // <-- Add this line
        currentBuildTargetPrefab = null;
        currentStructureData = null; // Also clear structure data for consistency
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
    
    // Method to exit build mode (useful for tutorial completion)
    public void ExitBuildMode()
    {
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }
        currentBuildTargetPrefab = null;
        isBuildModeActive = false;
        gridController.HideGrid();
        Debug.Log("Exited build mode");
    }
}