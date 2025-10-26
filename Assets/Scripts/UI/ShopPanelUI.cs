using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;

public class ShopPanelUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static ShopPanelUI Instance { get; private set; } // Singleton instance

    public GameObject itemPrefab; // Your StructureItem prefab
    public Transform contentParent; // The "StructureList" object
    public RectTransform scrollViewParent; 
    public StructureDatabase database; // Your ScriptableObject
    [SerializeField] private Button closeButton; // Reference to the close button

    // Events for opening/closing shop
    public UnityEvent OnShopOpened = new UnityEvent();
    public UnityEvent OnShopClosed = new UnityEvent();

    [Header("Performance Settings")]

    private bool isShopOpen = false; // Tracks whether the shop is open
    private BuildController buildController;
    private CameraController cameraController;
    private bool wasGhostActiveBeforeHover = false;

    // private bool navBarChange = true; // if user chose something else in the nav bar
    private char currNav = 'C'; // current nav bar selection

    [SerializeField] private GameObject comingSoonPanel; // Panel for upcoming features 

    public UIHoverManager hoverManager;
    
    // Public getter for current tab (for tutorial system)
    public char GetCurrentTab()
    {
        return currNav;
    }
    
    // Get the button GameObject for a specific tab (for tutorial highlighting)
    public GameObject GetTabButton(char tab)
    {
        switch (tab)
        {
            case 'C': return coopTabButton != null ? coopTabButton.gameObject : null;
            case 'A': return armyTabButton != null ? armyTabButton.gameObject : null;
            case 'P': return plantTabButton != null ? plantTabButton.gameObject : null;
            case 'S': return defenseTabButton != null ? defenseTabButton.gameObject : null;
            default:
                Debug.LogWarning($"[ShopPanelUI] Unknown tab character: {tab}");
                return null;
        }
    } 

    [Header("Repair things")]
    public GameObject repairItemPrefab;  
    public GameObject repairNotification;
    public GameObject topSection;

    public TextMeshProUGUI totalRepairCostText;
    public Button repairAllButton;
    public Sprite greyNormalButton;
    private int currentMoney;
    private int totalRepairCost;
    public TextMeshProUGUI filteredText;

    [Header("Tab buttons")]
    [SerializeField] private RectTransform  shopTab;
    [SerializeField] private RectTransform  repairTab;
    
    [Header("Shop Navigation Buttons")]
    [SerializeField] private Button coopTabButton;      // Button for 'C' tab (index 0)
    [SerializeField] private Button armyTabButton;      // Button for 'A' tab (index 1)
    [SerializeField] private Button plantTabButton;     // Button for 'P' tab (index 2)
    [SerializeField] private Button defenseTabButton;   // Button for 'S' tab (index 3)

    [Header("Pulse Highlight Settings")]
    [SerializeField] private float pulseScale = 1.2f;
    [SerializeField] private float pulseDuration = 0.5f;
    private bool isPulsing = false;

    private UIHover uiHover;

    private bool onShop = true;
    private bool showAll = true; 


    private void Awake()
    {
        Canvas canvas = GetComponent<Canvas>();
        // if (canvas != null)
        // {
        //     canvas.sortingOrder = 100; // Ensure Shop UI is on top
        // }
        // Ensure only one instance of ShopPanelUI exists
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple instances of ShopPanelUI detected! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        uiHover = FindFirstObjectByType<UIHover>();
        if (uiHover == null)
            Debug.LogWarning("No UIHover found in the scene!");

        Instance = this;

        hoverManager = FindFirstObjectByType<UIHoverManager>();
    }

    void Start()
    {
        showCurrentFilter();
        PopulateShop();

        // Link the close button to the ShopUIManager's CloseShop method
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners(); // Clear any existing listeners
            closeButton.onClick.AddListener(() =>
            {
                if (ShopUIManager.Instance != null)
                {
                    ShopUIManager.Instance.CloseShop();
                }
            });
        }

        // Cache controller references
        buildController = FindFirstObjectByType<BuildController>();
        cameraController = FindFirstObjectByType<CameraController>();
    }

    public void changeNavBar(int num)
    {
        char tempNav = currNav;
        if(showAll)
        {
            showAll = false;
        }

        switch (num)
        {
            case 0:
                currNav = 'C'; // animals (includes farmhouse)
                break;
            case 1:
                currNav = 'A'; // army
                break;
            case 2:
                currNav = 'P'; // plant
                break;
            case 3:
                currNav = 'S'; // defence 
                break;
            default:
                Debug.LogWarning("Invalid nav bar selection: " + num);
                return;
        }

        showCurrentFilter();

        if (tempNav != currNav)
        {
            // Notify tutorial that tab changed
            if (SimplifiedTutorialManager.Instance != null)
            {
                SimplifiedTutorialManager.Instance.OnShopTabChanged(currNav);
            }
            
            if(onShop)
            {
                PopulateShop(currNav);
            }
            else
            {
                PopulateRepairList();
            }
        }
    }

    public void PopulateShop(char display = 'C')
    {
        int currentDay = NightManager.Instance != null ? NightManager.Instance.Days : 0;

        // Check if shop should be completely empty during current tutorial step
        if (ShouldShopBeEmptyForCurrentTutorialStep())
        {
            // Clear all items and show empty shop
            foreach (Transform child in contentParent)
            {
                Destroy(child.gameObject);
            }
            return;
        }

        if (database == null)
        {
            Debug.LogError("StructureDatabase is not assigned in the inspector!");
            return;
        }

        if (database.allStructures == null || database.allStructures.Count == 0)
        {
            Debug.LogWarning("StructureDatabase is empty. No structures to display.");
            return;
        }

        // Clear previous items
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        int itemsAdded = 0;
        foreach (StructureData data in database.allStructures)
        {
            if (data == null || data.prefab == null) continue;

            // Check if farmhouse is already placed
            if (data.structureName.ToLower().Contains("farm house") && ShopUIManager.Instance != null && ShopUIManager.Instance.IsFarmHousePlaced)
                continue;

            // New: Restrict items based on current tutorial step
            if (!IsStructureAllowedInCurrentTutorialStep(data))
                continue;

            bool showItem = false;
            switch (display)
            {
                case 'C': showItem = data.type == StructureType.Animal || data.type == StructureType.Decoration || data.type == StructureType.Building; break;
                case 'A': showItem = data.type == StructureType.Barracks; break;
                case 'S': showItem = data.type == StructureType.Defense; break;
                case 'P': showItem = data.type == StructureType.CropPlot || data.type == StructureType.Silo; break;
                default: break;
            }

            if (!showItem) continue;

            GameObject item = Instantiate(itemPrefab, contentParent);
            StructureItemUI itemUI = item.GetComponent<StructureItemUI>();
            if (itemUI == null)
            {
                Debug.LogError($"StructureItemUI script is missing on prefab: {item.name}");
                continue;
            }

            try
            {
                itemUI.Setup(data);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ShopPanelUI] Exception calling Setup() for {data.structureName}: {e.Message}\n{e.StackTrace}");
            }
            itemsAdded++;
        }
        
        // After populating, highlight the tutorial target building if tutorial is active
        // Only start coroutine if GameObject is active and enabled
        if ((SimplifiedTutorialManager.Instance != null && SimplifiedTutorialManager.Instance.IsTutorialActive()) ||
            (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive()))
        {
            if (gameObject != null && gameObject.activeInHierarchy && enabled)
            {
                StartCoroutine(HighlightTutorialTargetBuilding());
            }
        }
    }
    
    private System.Collections.IEnumerator HighlightTutorialTargetBuilding()
    {
        // Wait a frame for items to be instantiated
        yield return null;
        
        // Check SimplifiedTutorialManager first
        if (SimplifiedTutorialManager.Instance != null && SimplifiedTutorialManager.Instance.IsTutorialActive())
        {
            // Get allowed buildings from SimplifiedTutorialManager
            List<string> allowedBuildings = SimplifiedTutorialManager.Instance.GetAllowedBuildingNames();
            
            if (allowedBuildings == null || allowedBuildings.Count == 0)
                yield break;
                
            Debug.Log($"[SimplifiedTutorial] Highlighting allowed buildings: {string.Join(", ", allowedBuildings)}");
            
            // Find and highlight allowed building items
            foreach (Transform child in contentParent)
            {
                StructureItemUI itemUI = child.GetComponent<StructureItemUI>();
                if (itemUI != null && itemUI.Data != null)
                {
                    // Check if this building is in the allowed list
                    bool isAllowed = SimplifiedTutorialManager.Instance.IsBuildingAllowed(itemUI.Data.structureName);
                    
                    if (isAllowed)
                    {
                        Debug.Log($"[SimplifiedTutorial] Highlighting allowed building: {itemUI.Data.structureName}");
                        HighlightBuildingItem(child.gameObject);
                    }
                }
            }
            yield break;
        }
        
        // Fallback to old TutorialManager
        if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive())
            yield break;
            
        string currentStepId = TutorialManager.Instance.GetCurrentStepId();
        string targetBuildingName = GetTargetBuildingForTutorialStep(currentStepId);
        
        if (string.IsNullOrEmpty(targetBuildingName))
            yield break;
            
        Debug.Log($"Looking for tutorial target building: {targetBuildingName}");
        
        // Find and highlight the target building item
        foreach (Transform child in contentParent)
        {
            StructureItemUI itemUI = child.GetComponent<StructureItemUI>();
            if (itemUI != null && itemUI.Data != null)
            {
                string itemName = itemUI.Data.structureName.ToLower();
                if (IsTargetBuildingMatch(itemName, targetBuildingName))
                {
                    Debug.Log($"Found and highlighting tutorial target: {itemUI.Data.structureName}");
                    HighlightBuildingItem(child.gameObject);
                    break;
                }
            }
        }
    }
    
    private string GetTargetBuildingForTutorialStep(string stepId)
    {
        switch (stepId)
        {
            case "open_build_shop":
            case "build_farmhouse":
                return "farmhouse";
            case "build_crop_plot":
                return "cropplot";
            case "build_silo":
                return "silo";
            case "build_chicken_coop":
                return "chickencoop";
            case "build_chicken_barracks":
                return "barracks";
            default:
                return "";
        }
    }
    
    private bool IsTargetBuildingMatch(string itemName, string targetName)
    {
        switch (targetName)
        {
            case "farmhouse":
                return itemName.Contains("farm") && itemName.Contains("house");
            case "cropplot":
                return (itemName.Contains("crop") && itemName.Contains("plot")) || itemName.Contains("cropplot");
            case "silo":
                return itemName.Contains("silo");
            case "chickencoop":
                return (itemName.Contains("chicken") && itemName.Contains("coop")) || 
                       itemName.Contains("chickenhouse") || itemName.Contains("hen house");
            case "barracks":
                return itemName.Contains("barracks");
            default:
                return false;
        }
    }
    
    private void HighlightBuildingItem(GameObject itemObject)
    {
        // Clear any existing highlights first
        ClearAllBuildingHighlights();
        
        // Add visual highlighting to this specific item
        AddBuildingItemHighlight(itemObject);
    }
    
    private void ClearAllBuildingHighlights()
    {
        // Remove highlights from all items
        foreach (Transform child in contentParent)
        {
            RemoveBuildingItemHighlight(child.gameObject);
        }
    }
    
    private void AddBuildingItemHighlight(GameObject itemObject)
    {
        Debug.Log($"Adding highlight to building item: {itemObject.name}");
        
        // Method 1: Scale the item slightly to make it stand out
        Transform transform = itemObject.transform;
        transform.localScale = Vector3.one * 1.05f;
        
        // Method 2: Find and modify background images for visual feedback
        Image[] images = itemObject.GetComponentsInChildren<Image>();
        
        foreach (Image img in images)
        {
            if (img != null)
            {
                // Store original color and apply highlight
                Color originalColor = img.color;
                Color highlightColor = new Color(1f, 1f, 0.3f, 1f); // Bright yellow tint
                img.color = Color.Lerp(originalColor, highlightColor, 0.4f);
                
                // Store original color for restoration
                HighlightInfo highlightInfo = img.gameObject.GetComponent<HighlightInfo>();
                if (highlightInfo == null)
                {
                    highlightInfo = img.gameObject.AddComponent<HighlightInfo>();
                }
                highlightInfo.originalColor = originalColor;
                highlightInfo.backgroundImage = img;
                
                Debug.Log($"Applied highlight color to image: {img.name}");
            }
        }
        
        // Method 3: Add a pulsing animation
        StartCoroutine(PulseHighlight(itemObject));
        
        Debug.Log($"Successfully highlighted building item: {itemObject.name}");
    }
    
    private void RemoveBuildingItemHighlight(GameObject itemObject)
    {
        // Reset scale
        Transform transform = itemObject.transform;
        transform.localScale = Vector3.one;
        
        // Restore original colors for all images with highlight info
        HighlightInfo[] highlightInfos = itemObject.GetComponentsInChildren<HighlightInfo>();
        foreach (HighlightInfo info in highlightInfos)
        {
            if (info != null && info.backgroundImage != null)
            {
                info.backgroundImage.color = info.originalColor;
                DestroyImmediate(info); // Clean up the component
            }
        }
    }
    
    private System.Collections.IEnumerator PulseHighlight(GameObject itemObject)
    {
        float pulseSpeed = 2.5f;
        float baseScale = 1.05f; // Already scaled up
        
        while (itemObject != null && itemObject.activeInHierarchy)
        {
            // Very gentle pulsing between 1.05 and 1.07 scale
            float pulse = 0.02f * Mathf.Sin(Time.time * pulseSpeed);
            float currentScale = baseScale + pulse;
            
            itemObject.transform.localScale = Vector3.one * currentScale;
            
            yield return null;
        }
        
        // Reset to normal scale when done
        if (itemObject != null)
        {
            itemObject.transform.localScale = Vector3.one;
        }
    }
    
    // Helper component to store highlight information
    private class HighlightInfo : MonoBehaviour
    {
        public Color originalColor;
        public Image backgroundImage;
    }

    private char GetCorrectTabForCurrentTutorialStep()
    {
        if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive()) 
        {
            return 'C'; // Default to animals tab
        }

        string currentStepId = TutorialManager.Instance.GetCurrentStepId();
        
        // Return the tab that contains the building needed for current tutorial step
        switch (currentStepId)
        {
            case "open_build_shop":
            case "build_farmhouse":
                return 'C'; // Farmhouse is in Animals tab (tab 0)
                
            case "build_crop_plot":
            case "build_silo":
                return 'P'; // Crop Plot and Silo are in Plant tab (tab 2)
                
            case "build_chicken_coop":
                return 'C'; // Chicken Coop is in Animals tab (tab 0)
                
            case "build_chicken_barracks":
                return 'A'; // Barracks are in Army tab (tab 1)
                
            case "build_first_wall":
            case "build_first_hay_bale":
            case "build_wall_chain":
                return 'S'; // Walls are in Defense tab (tab 3)
                
            default:
                return 'C'; // Default to animals tab
        }
    }

    public void EnsureCorrectTabForTutorial()
    {
        if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive()) 
        {
            return; // No change needed if tutorial not active
        }

        char neededTab = GetCorrectTabForCurrentTutorialStep();
        if (currNav != neededTab)
        {
            currNav = neededTab;
            PopulateShop(currNav);
            
            // Also trigger the visual tab switching in the UI
            int tabIndex = neededTab switch 
            {
                'C' => 0, // Animals 
                'A' => 1, // Army
                'P' => 2, // Plant
                'S' => 3, // Defense
                _ => 0
            };
            
            Debug.Log($"Auto-switched to tab {tabIndex} ({neededTab}) for tutorial step: {TutorialManager.Instance.GetCurrentStepId()}");
            
            // Also trigger the UI tab change if we have a tab controller
            TriggerTabUIUpdate(tabIndex);
        }
    }
    
    private void TriggerTabUIUpdate(int tabIndex)
    {
        // Try to find and trigger tab UI elements
        // This will work with common UI patterns for tab controllers
        
        // Extended search patterns for tab buttons
        string[] buttonNames = {
            // Common patterns
            $"Tab{tabIndex}Button", $"TabButton{tabIndex}", $"Tab_{tabIndex}", 
            $"Button{tabIndex}", $"NavButton{tabIndex}", $"CategoryButton{tabIndex}",
            // Shop-specific patterns
            $"ShopTab{tabIndex}", $"Category{tabIndex}", $"Nav{tabIndex}",
            // Index-based patterns  
            $"TabButton ({tabIndex})", $"Tab Button {tabIndex}", $"Button ({tabIndex})"
        };
        
        GameObject foundButton = null;
        
        // First try to find the button
        foreach (string buttonName in buttonNames)
        {
            foundButton = GameObject.Find(buttonName);
            if (foundButton != null)
            {
                Debug.Log($"Found tab button: {buttonName}");
                break;
            }
        }
        
        // If we found a button, click it to trigger both logic and visual state
        if (foundButton != null)
        {
            Button button = foundButton.GetComponent<Button>();
            if (button != null && button.interactable)
            {
                Debug.Log($"Clicking tab button to update visual state: {foundButton.name}");
                button.onClick.Invoke();
                return;
            }
        }
        
        // Fallback: Try to find all tab buttons and manually update their states
        UpdateTabButtonStates(tabIndex);
        
        // Try to find a tab controller component and call it directly
        TryUpdateTabController(tabIndex);
        
        // Still call changeNavBar to ensure content updates
        changeNavBar(tabIndex);
    }
    
    private void TryUpdateTabController(int tabIndex)
    {
        // Look for common tab controller components
        string[] controllerNames = {
            "TabController", "ShopTabController", "NavController", 
            "TabManager", "ShopTabs", "CategoryTabs"
        };
        
        foreach (string controllerName in controllerNames)
        {
            GameObject controller = GameObject.Find(controllerName);
            if (controller != null)
            {
                // Try to find a method to select the tab
                Component[] components = controller.GetComponents<Component>();
                foreach (Component comp in components)
                {
                    var type = comp.GetType();
                    
                    // Look for common tab selection method names
                    var selectMethod = type.GetMethod("SelectTab") ?? 
                                      type.GetMethod("SetActiveTab") ?? 
                                      type.GetMethod("SwitchToTab") ??
                                      type.GetMethod("ActivateTab");
                    
                    if (selectMethod != null)
                    {
                        try
                        {
                            selectMethod.Invoke(comp, new object[] { tabIndex });
                            Debug.Log($"Called {selectMethod.Name}({tabIndex}) on {type.Name}");
                            return;
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"Failed to call {selectMethod.Name}: {e.Message}");
                        }
                    }
                }
            }
        }
    }
    
    private void UpdateTabButtonStates(int activeTabIndex)
    {
        // Try to find and update all tab button states manually
        for (int i = 0; i < 4; i++) // We have 4 tabs (0-3)
        {
            string[] buttonPatterns = {
                $"Tab{i}Button", $"TabButton{i}", $"Button{i}", 
                $"NavButton{i}", $"ShopTab{i}", $"Category{i}"
            };
            
            foreach (string pattern in buttonPatterns)
            {
                GameObject tabButton = GameObject.Find(pattern);
                if (tabButton != null)
                {
                    // Try to update button state visually
                    Button button = tabButton.GetComponent<Button>();
                    if (button != null)
                    {
                        // Update button color or state based on whether it's the active tab
                        ColorBlock colors = button.colors;
                        if (i == activeTabIndex)
                        {
                            // Set as selected/pressed state
                            button.interactable = false; // Temporarily disable to show selected state
                            StartCoroutine(ReEnableButtonAfterFrame(button));
                        }
                        else
                        {
                            // Set as normal state
                            button.interactable = true;
                        }
                        
                        Debug.Log($"Updated tab button {pattern} state: active={i == activeTabIndex}");
                        break; // Found this tab button, move to next index
                    }
                }
            }
        }
    }
    
    private System.Collections.IEnumerator ReEnableButtonAfterFrame(Button button)
    {
        yield return null; // Wait one frame
        if (button != null)
        {
            button.interactable = true;
        }
    }
    
    public void RefreshForTutorialChange()
    {
        // Called when tutorial step changes to ensure shop is showing correct content
        EnsureCorrectTabForTutorial();
        PopulateShop(currNav);
        
        // Also ensure building gets highlighted after refresh
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
        {
            StartCoroutine(HighlightTutorialTargetBuilding());
        }
    }

    private void displayComingSoonPanel(int num)
    {
        if (num == 0 && !comingSoonPanel.activeSelf)
        {
            comingSoonPanel.SetActive(true);
        }
        else if (num == 1 && comingSoonPanel.activeSelf)
        {
            comingSoonPanel.SetActive(false);
        }
    }

    public void OpenShop()
    {
        isShopOpen = true;
        
        // Just trigger events and setup - DON'T call HandleShopOpened here
        OnShopOpened.Invoke();

        // Cache controller references if needed
        if (buildController == null)
        {
            buildController = FindFirstObjectByType<BuildController>();
        }
        
        // Ensure correct tab and highlight tutorial building when shop opens
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
        {
            EnsureCorrectTabForTutorial();
            StartCoroutine(HighlightTutorialTargetBuilding());
        }

        // DON'T call HandleShopOpened here - it's already connected via UnityEvent
    }

    public void CloseShop()
    {
        isShopOpen = false;

        // Clear tutorial highlights when shop closes
        ClearAllBuildingHighlights();
        
        // Hide item hover panel when shop closes
        if (ItemHoverPanel.Instance != null)
        {
            ItemHoverPanel.Instance.HideImmediate();
        }

        // Make sure to re-enable controls before closing
        if (cameraController != null)
        {
            cameraController.TemporarilyDisableMouseControls(false);
        }

        // Re-enable ghost if it was active before
        if (buildController != null && wasGhostActiveBeforeHover)
        {
            buildController.RestoreGhost();
            wasGhostActiveBeforeHover = false;
        }

        OnShopClosed.Invoke();
        
        // DON'T deactivate the gameObject here - ShopUIManager handles it
    }

    public bool IsShopOpen()
    {
        return isShopOpen && gameObject.activeSelf;
    }

    // Called when pointer enters the UI element
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Only react if the shop panel is actually visible in the scene
        if (!gameObject.activeSelf) return;

        // Temporarily disable build ghost and placement
        if (buildController != null && buildController.IsBuildModeActive())
        {
            wasGhostActiveBeforeHover = true;
            buildController.HideGhostTemporarily();
        }

        // Temporarily disable only mouse camera controls
        if (cameraController != null)
        {
            cameraController.TemporarilyDisableMouseControls(true);
        }
    }

    // Called when pointer exits the UI element
    public void OnPointerExit(PointerEventData eventData)
    {
        // Only react if the shop panel is actually visible in the scene
        if (!gameObject.activeSelf) return;

        // Re-enable build ghost and placement if it was active before
        if (buildController != null && wasGhostActiveBeforeHover)
        {
            buildController.RestoreGhost();
            wasGhostActiveBeforeHover = false;
        }

        // Re-enable mouse camera controls
        if (cameraController != null)
        {
            cameraController.TemporarilyDisableMouseControls(false);
        }
    }

    private bool IsStructureAllowedInCurrentTutorialStep(StructureData data)
    {
        // Check if "Unlock All Buildings" cheat is active
        if (CheatManager.Instance != null && CheatManager.Instance.IsUnlockAllBuildsActive())
        {
            Debug.Log($"Unlock All Buildings cheat active - allowing structure: {data.structureName}");
            return true; // Cheat overrides all restrictions
        }
        
        // NEW: Use SimplifiedTutorialManager for shop restrictions
        if (SimplifiedTutorialManager.Instance != null && SimplifiedTutorialManager.Instance.IsTutorialActive())
        {
            // Use the new API to check if building is allowed
            bool allowed = SimplifiedTutorialManager.Instance.IsBuildingAllowed(data.structureName);
            Debug.Log($"[SimplifiedTutorial] Structure '{data.structureName}' allowed: {allowed}");
            return allowed;
        }
        
        // OLD: Fallback to TutorialManager if SimplifiedTutorialManager not available
        if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive()) 
        {
            return true; // Allow all if tutorial not active
        }

        string currentStepId = TutorialManager.Instance.GetCurrentStepId();
        string structureName = data.structureName.ToLower();
        
        Debug.Log($"Tutorial filtering: currentStepId='{currentStepId}', checking structure='{structureName}', type={data.type}");

        // INTUITIVE: Show related buildings that make sense for current tutorial phase
        bool isAllowed = false;
        switch (currentStepId)
        {
            case "open_build_shop":
            case "build_farmhouse":
                // Only allow farmhouse during farmhouse tutorial steps
                isAllowed = structureName.Contains("farm") && structureName.Contains("house");
                break;
                
            case "build_crop_plot":
                // During crop plot step, show both crop plot AND silo since they work together
                isAllowed = ((structureName.Contains("crop") && structureName.Contains("plot")) || 
                            structureName.Contains("cropplot") ||
                            structureName.Contains("silo"));
                break;
                
            case "build_silo":
                // During silo step, show both silo AND crop plot since they work together  
                isAllowed = (structureName.Contains("silo") ||
                            (structureName.Contains("crop") && structureName.Contains("plot")) || 
                            structureName.Contains("cropplot"));
                break;
                
            case "build_chicken_coop":
                // Only allow chicken coop during chicken coop tutorial step
                isAllowed = (structureName.Contains("chicken") && structureName.Contains("coop")) ||
                           structureName.Contains("chickenhouse") || structureName.Contains("hen house");
                break;
                
            case "build_chicken_barracks":
                // Only allow chicken barracks during chicken barracks tutorial step
                isAllowed = (structureName.Contains("chicken") && (structureName.Contains("barracks") || structureName.Contains("barrack"))) ||
                           structureName.ToLower().Contains("chicken barrack");
                break;
                
            // All non-building tutorial steps - no buildings allowed
            case "plant_first_crop":
            case "harvest_first_crops":
            case "price_panel_tutorial":
            case "price_panel_explanation":
            case "buy_chickens":
            case "feed_chickens":
            case "collect_eggs":
            case "recruit_soldiers":
            case "place_flag":
            case "welcome":
            case "melony_movement":
            case "melony_zoom":
            case "melony_rotate":
            case "day_night_panel":
            case "money_explanation":
            case "resources_explanation":
                // No buildings during explanation/action steps - hide shop completely
                isAllowed = false;
                break;
                
            case "build_first_wall":
            case "build_first_hay_bale":
            case "build_wall_chain":
                // During wall tutorial step, show Defense structures (walls/fences) AND keep previous buildings available
                isAllowed = data.type == StructureType.Defense ||
                           data.type == StructureType.Building ||
                           data.type == StructureType.Animal ||
                           data.type == StructureType.Barracks;
                break;
                
            default:
                // Unknown tutorial steps - be cautious and block everything
                isAllowed = false;
                break;
        }
        
        Debug.Log($"Structure '{structureName}' (type: {data.type}) for step '{currentStepId}': {(isAllowed ? "ALLOWED" : "BLOCKED")}");
        return isAllowed;
    }

    private bool ShouldShopBeEmptyForCurrentTutorialStep()
    {
        if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive()) 
        {
            return false; // Shop works normally if tutorial not active
        }

        string currentStepId = TutorialManager.Instance.GetCurrentStepId();
        
        // Steps where shop should be completely empty (no building allowed)
        switch (currentStepId)
        {
            case "plant_first_crop":
            case "harvest_first_crops":
            case "price_panel_tutorial":
            case "price_panel_explanation":
            case "buy_chickens":
            case "feed_chickens":
            case "collect_eggs":
            case "recruit_soldiers":
            case "place_flag":
            case "welcome":
            case "melony_movement":
            case "melony_zoom":
            case "melony_rotate":
            case "day_night_panel":
            case "money_explanation":
            case "resources_explanation":
                return true;
            default:
                return false;
        }
    }

    private void OnEnable()
    {
        BuildingManager.onBuildingAdded.AddListener(UpdateRepairList);
        BuildingManager.onBuildingRemoved.AddListener(UpdateRepairList);

        Structure.OnAnyStructureDamaged.AddListener(UpdateRepairList);

        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnMoneyChanged += OnMoneyChange;
    }

    private void OnDisable()
    {
        BuildingManager.onBuildingAdded.RemoveListener(UpdateRepairList);
        BuildingManager.onBuildingRemoved.RemoveListener(UpdateRepairList);

        Structure.OnAnyStructureDamaged.RemoveListener(UpdateRepairList);

        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnMoneyChanged -= OnMoneyChange;
    }

    private void UpdateRepairList()
    {
        if(!onShop)
        {
            PopulateRepairList();
        }
    }


    public void PopulateRepairList()
    {
        // Clear old items
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);
        // Defensive debug info
        if (repairItemPrefab == null)
        {
            Debug.LogError("repairItemPrefab is not assigned in ShopPanelUI! Repair list cannot be populated.");
            return;
        }
        else
        {
            var comp = repairItemPrefab.GetComponent<RepairItem>();
            if (comp == null)
            {
                Debug.LogError("The assigned repairItemPrefab does not contain a RepairItem component. Please assign the correct prefab to avoid shop items appearing in repair list.");
                // Do not proceed to instantiate an incorrect prefab - abort population to avoid UI mixups
                repairNotification.SetActive(true);
                totalRepairCost = 0;
                totalRepairCostText.text = $"{totalRepairCost}";
                repairAllButton.interactable = false;
                return;
            }
        }

        List<GameObject> brokenBuildings;
        
        if(showAll)
        {
            brokenBuildings = BuildingManager.Instance.getBrokenBuildings('X');
        }
        else
        {
            brokenBuildings = BuildingManager.Instance.getBrokenBuildings(currNav);
        }
        

        if(brokenBuildings == null || brokenBuildings.Count == 0)
        {
            //maybe remove it when there is nothing
            // topSection.SetActive(false);

            //set text
            totalRepairCost = 0;
            totalRepairCostText.text = $"{totalRepairCost}";
            // repairAllButton.image.sprite = greyNormalButton;
            repairAllButton.interactable = false;

            repairNotification.SetActive(true);
            return;
        }
        else
        {
            topSection.SetActive(true);
            
            totalRepairCostText.text = $"{totalRepairCost}";
            repairAllButton.interactable = true;

            repairNotification.SetActive(false);
        }

        totalRepairCost = 0;

        foreach (GameObject building in brokenBuildings)
        {
            totalRepairCost += building.GetComponent<Structure>().GetRepairCost();

            // Instantiate the repair item but only if the prefab contains the RepairItem component
            GameObject item = Instantiate(repairItemPrefab, contentParent);
            if (item == null)
            {
                Debug.LogError("Failed to instantiate repairItemPrefab - null returned");
                continue;
            }

            RepairItem repairItem = item.GetComponent<RepairItem>();
            if (repairItem == null)
            {
                Debug.LogError($"Instantiated prefab '{item.name}' does not contain a RepairItem component. Destroying to avoid UI pollution.");
                Destroy(item);
                continue;
            }

            // Initialize repair item
            string structureName = building.GetComponent<Structure>() != null ? building.GetComponent<Structure>().GetStructureName() : building.name;
            int repairCostLocal = building.GetComponent<Structure>() != null ? building.GetComponent<Structure>().GetRepairCost() : 0;
            repairItem.Initialize(building, structureName, repairCostLocal);

            // Wire up callback safely
            repairItem.OnRepaired += RemoveRepairItem;
        }

        totalRepairCostText.text = $"{totalRepairCost}";

        if(MoneyManager.Instance.GetCurrentMoney() <= totalRepairCost)
        {
            repairAllButton.interactable = false;
            totalRepairCostText.color = Color.red;
        }
        else
        {
            repairAllButton.interactable = true;
            totalRepairCostText.color = Color.white;
        }
    }

    private void OnMoneyChange(int money)
    {
        currentMoney = money;
        if (repairAllButton != null)
        {
            repairAllButton.interactable = currentMoney >= totalRepairCost;
        }

        if(totalRepairCostText != null)
        {
            totalRepairCostText.color = currentMoney >= totalRepairCost ? Color.white : Color.red;
        }
    }

    public void OnRepairButtonHoverEnter()
    {
        if (!repairAllButton.interactable && !isPulsing)
        {
            if(uiHover != null)
            {
                if(totalRepairCost == 0)
                {
                    uiHover.Show("All set!", "Nothing needs repairing.", repairAllButton.GetComponent<RectTransform>());

                }
                else
                {
                    uiHover.Show("Broke!", "You can't afford repairs!", repairAllButton.GetComponent<RectTransform>());
                    isPulsing = true;
                    totalRepairCostText.rectTransform.pivot = new Vector2(0.5f, 0.5f); // ensure pivot center
                    LeanTween.scale(totalRepairCostText.gameObject, Vector3.one * pulseScale, pulseDuration)
                        .setEaseInOutSine()
                        .setLoopPingPong();
                }
            }

        }
    }

    public void OnRepairButtonHoverExit()
    {
        if(uiHover != null)
        {
            uiHover.Hide();
        }
        if (isPulsing)
        {
            // if(uiHover != null)
            // {
            //     uiHover.Hide();
            // }

            isPulsing = false;
            LeanTween.cancel(totalRepairCostText.gameObject);
            totalRepairCostText.transform.localScale = Vector3.one;
        }
    }

    public void OnRepairButtonClick()
    {
        if(repairAllButton.interactable == false && totalRepairCost == 0)
        {
            // if(totalRepairCost == 0)
            // {
                AudioManager.Instance?.PlayErrorSound();  
            // }
            return;      
        }
        else if(repairAllButton.interactable == false)
        {
            hoverManager.PlayErrorFeedbackForGameObject(true, repairAllButton.gameObject);
            // AudioManager.Instance?.PlayInsufficientFundsSound();  
            return;      
        }        
    }


    private void UpdateTopSection()
    {
        if (repairAllButton != null)
        {
            if(totalRepairCost == 0)
            {
                repairAllButton.interactable = false;
            }
            else
            {
                repairAllButton.interactable = currentMoney >= totalRepairCost;
            }
        }

        if(totalRepairCostText != null)
        {
            totalRepairCostText.text = $"{totalRepairCost}";
            totalRepairCostText.color = currentMoney >= totalRepairCost ? Color.white : Color.red;
        }
    }

    private void RemoveRepairItem(RepairItem item)
    {         
        if (item != null)
        {
            CanvasGroup cg = item.GetComponent<CanvasGroup>();


            if (cg != null)
            {

                //scale effect
                LeanTween.alphaCanvas(cg, 0f, 0.3f);
                LeanTween.scale(item.gameObject, Vector3.zero, 0.3f).setEaseInBack().setOnComplete(() =>
                {
                    Destroy(item.gameObject);
                    item.transform.SetParent(null);
                    CheckRepairNotification();
                    totalRepairCost -= item.GetRepairCost();
                    UpdateTopSection();
                });

                // fade effect
                // LeanTween.alphaCanvas(cg, 0f, 0.3f).setOnComplete(() =>
                // {
                //     Destroy(item.gameObject);
                // });
                // CheckRepairNotification();
            }
            else
            {
                Destroy(item.gameObject); 
                item.transform.SetParent(null);
                CheckRepairNotification();
                totalRepairCost -= item.GetRepairCost();
                UpdateTopSection();
            }
            CheckRepairNotification();
        }
    }



    public void repairAllBuildings()
    {
        char type;
        if(showAll)
        {
            type = 'X';
        }
        else
        {
            type = currNav;
        }

        if(BuildingManager.Instance.repairAllBuildings(type))
        {
            MoneyManager.Instance.SpendMoney(totalRepairCost);
            totalRepairCost = 0;
            UpdateTopSection();

            AudioManager.Instance?.PlayRepairSound();
        }        

        repairAnimation();

        PopulateRepairList();
    }

    public void repairAnimation()
    {
        // Get all RepairItem components currently in the list
        RepairItem[] items = contentParent.GetComponentsInChildren<RepairItem>();

        foreach (RepairItem item in items)
        {
            if (item != null)
            {
                CanvasGroup cg = item.GetComponent<CanvasGroup>();

                if (cg != null)
                {
                    // item.transform.SetParent(null);

                    LeanTween.alphaCanvas(cg, 0f, 0.3f);
                    LeanTween.scale(item.gameObject, Vector3.zero, 0.3f).setEaseInBack().setOnComplete(() =>
                    {
                        Destroy(item.gameObject);
                        item.transform.SetParent(null);
                        CheckRepairNotification();
                    });
                }
                else
                {
                    Destroy(item.gameObject); 
                    item.transform.SetParent(null);
                    CheckRepairNotification();
                }
                CheckRepairNotification();
            }
        }
    }

    private void CheckRepairNotification()
    {
        // Show notification if there are no remaining repair items
        repairNotification.SetActive(contentParent.childCount == 0);
    }

    public void SetLayoutForShop()
    {
        onShop = true;
        if(scrollViewParent != null)
        {
            Vector2 size1 = scrollViewParent.sizeDelta;
            size1.y = 534f; 
            scrollViewParent.sizeDelta = size1;
        }

        topSection.SetActive(false);
        repairNotification.SetActive(false);
        Vector2 size = shopTab.sizeDelta;
        size.y = 133;
        shopTab.sizeDelta = size;
        
        Vector2 size2 = repairTab.sizeDelta;
        size2.y = 100;
        repairTab.sizeDelta = size2;
        
        GridLayoutGroup layout = contentParent.GetComponent<GridLayoutGroup>();
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 2;  // 2 columns for shop
        layout.cellSize = new Vector2(350, 400); // adjust size for shop items
        layout.spacing = new Vector2(10, 10);
        layout.padding.top = 0;

        PopulateShop(currNav);
    }

    public void SetLayoutForRepair()
    {
        showAll = true;
        showCurrentFilter();

        onShop = false;
        if(scrollViewParent != null)
        {
            Vector2 size1 = scrollViewParent.sizeDelta;
            size1.y = 469f; 
            scrollViewParent.sizeDelta = size1;
            // Debug.Log($"SetLayoutForRepair: Adjusted scrollViewParent height to {scrollViewParent.sizeDelta}");
        }

        topSection.SetActive(true);
        Vector2 size = repairTab.sizeDelta;
        size.y = 133;
        repairTab.sizeDelta = size;
        
        Vector2 size2 = shopTab.sizeDelta;
        size2.y = 100;
        shopTab.sizeDelta = size2;

        GridLayoutGroup layout = contentParent.GetComponent<GridLayoutGroup>();        
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 1;  // 1 column for repair list
        layout.cellSize = new Vector2(690, 140); // taller for repair items
        layout.spacing = new Vector2(10, 10);
        // layout.padding.top = 50;

        PopulateRepairList();
    }

    public void showAllBuildings()
    {
        if(showAll)
        {
            showAll = false;
            PopulateRepairList();
        }
        else
        {
            showAll = true;
            PopulateRepairList();
        }
        showCurrentFilter();
    }

    public void showCurrentFilter()
    {
        if(showAll)
        {
            filteredText.text = "All";
        }
        else
        {
            switch(currNav)
            {
                case 'C':
                    filteredText.text = "Pens";
                    break;
                case 'A':
                    filteredText.text = "Barracks";
                    break;
                case 'P':
                    filteredText.text = "Resources";
                    break;
                case 'S':
                    filteredText.text = "Walls";
                    break;
            }
        }
        
    }
}

