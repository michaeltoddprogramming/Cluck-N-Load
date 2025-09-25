using UnityEngine;
using UnityEngine.UI;

public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager Instance { get; private set; }

    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button closeButton;

    //open shop button
    [SerializeField] private Button shopButton;
    private Color dayShop = Color.white;
    private Color nightShop = Color.grey * 0.9f;
    public Image shopIcon;

    private ShopPanelUI shopPanelUI;
    private bool isVisible = false;

    // Add debounce variables
    private float lastClickTime = 0f;
    private float clickCooldown = 0.1f; // 100ms cooldown

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Check if shopPanel is assigned
        if (shopPanel == null)
        {
            return;
        }

        // Find ShopPanelUI component
        shopPanelUI = shopPanel.GetComponent<ShopPanelUI>();
        if (shopPanelUI == null)
        {
            Debug.LogError("ShopPanelUI component not found on shopPanel!");
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseShop);
        }

        // Set up shop button onClick listener - CLEAR EXISTING LISTENERS FIRST
        if (shopButton != null)
        {
            shopButton.onClick.RemoveAllListeners(); // Clear any existing listeners
            shopButton.onClick.AddListener(ToggleShop);
        }
        else
        {
            Debug.LogError("ShopUIManager: Shop button is null!");
        }

        // Initially hide shop
        shopPanel.SetActive(false);

        // Set initial shop button state based on tutorial
        UpdateShopButtonStateForTutorial();
    }

    private bool IsShopAllowedInTutorial()
    {
        if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive())
        {
            return true; // Shop always available when tutorial not active
        }

        string currentStepId = TutorialManager.Instance.GetCurrentStepId();

        // RESTRICTIVE: Only allow shop access during actual building tutorial steps
        switch (currentStepId)
        {
            case "open_build_shop":
            case "build_farmhouse":
            case "build_crop_plot": 
            case "build_silo":
            case "build_chicken_coop":
            case "build_chicken_barracks":
                // Only during building steps - shop is allowed
                return true;

            // All other tutorial steps - shop should be disabled
            case "welcome":
            case "melony_movement":
            case "melony_zoom":
            case "melony_rotate":
            case "day_night_panel":
            case "money_explanation":
            case "resources_explanation":
            case "time_controls":
            case "season_bonuses":
            case "plant_first_crop":
            case "harvest_first_crops":
            case "price_panel_tutorial":
            case "price_panel_explanation":
            case "buy_chickens":
            case "feed_chickens":
            case "collect_eggs":
            case "recruit_soldiers":
            case "place_flag":
            case "prepare_defense":
            default:
                // All explanation, action, and non-building steps - shop disabled
                return false;
        }
    }

    public void UpdateShopButtonStateForTutorial()
    {
        if (shopButton == null) return;

        // First check if it's night time - if it is, always disable regardless of tutorial state
        bool isNightTime = NightManager.Instance != null && !NightManager.Instance.IsDay;
        
        // Also check if the game is paused - if it is, always disable
        // bool isPaused = NightManager.Instance != null && NightManager.Instance.IsPaused;

        // Only check tutorial conditions if it's daytime
        bool shopAllowed = !isNightTime && IsShopAllowedInTutorial();

        shopButton.interactable = shopAllowed;

        if (shopIcon != null)
        {
            if (shopAllowed)
            {
                shopIcon.color = dayShop;
            }
            else
            {
                shopIcon.color = nightShop;
            }
        }

        // Debug log to help track the issue
        if (isNightTime && shopButton.interactable)
        {
            Debug.LogWarning("Shop button was about to be incorrectly enabled at night!");
        }
    }

    public void OnTutorialStepChanged()
    {
        UpdateShopButtonStateForTutorial();
        
        // Close shop if it's open but not allowed in current tutorial step
        if (isVisible && !IsShopAllowedInTutorial())
        {
            CloseShop();
        }
        
        // Also refresh shop contents if shop is currently open and allowed
        if (shopPanelUI != null && isVisible && IsShopAllowedInTutorial())
        {
            // Refresh shop with correct tab and restrictions for new tutorial step
            shopPanelUI.RefreshForTutorialChange();
        }
    }

    private void Update()
    {
        // Periodically check and update shop button state as fallback
        // Only do this occasionally to avoid performance issues
        if (Time.frameCount % 60 == 0) // Check once per second at 60fps
        {
            UpdateShopButtonStateForTutorial();
        }
    }

    public void ToggleShop()
    {
        // Prevent rapid clicking/double clicks
        if (Time.time - lastClickTime < clickCooldown)
        {
            return;
        }
        lastClickTime = Time.time;

        // NEW: Check if it's night time
        if (NightManager.Instance != null && !NightManager.Instance.IsDay)
        {
            return;
        }

        // Check if shop is allowed during current tutorial step
        if (!IsShopAllowedInTutorial())
        {
            return;
        }


        // Use the actual panel state instead of isVisible flag
        bool shouldOpen = shopPanel != null && !shopPanel.activeSelf;

        if (shouldOpen)
        {
            OpenShop();
        }
        else
        {
            CloseShop();
        }
    }

    public void OpenShop()
    {

        // Don't allow opening shop if it's disabled (nighttime)
        if (!shopButton.interactable)
        {
            return;
        }

        // Check if already open
        if (shopPanel != null && shopPanel.activeSelf)
        {
            return;
        }

        isVisible = true;

        // Activate the panel FIRST
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            shopPanel.transform.SetAsLastSibling();
        }
        else
        {
            return;
        }

        // THEN call ShopPanelUI methods
        if (shopPanelUI != null)
        {
            // Ensure we're on the correct tab for the tutorial BEFORE opening
            shopPanelUI.EnsureCorrectTabForTutorial();
            shopPanelUI.OpenShop();
        }
        else
        {
            Debug.LogError("ShopPanelUI is null!");
        }

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.Trigger(TutorialTrigger.ShopOpened);
    }

    public void CloseShop()
    {

        // Check if already closed
        if (shopPanel != null && !shopPanel.activeSelf)
        {
            return;
        }

        isVisible = false;

        // First notify ShopPanelUI to clean up its state
        if (shopPanelUI != null)
        {
            shopPanelUI.CloseShop();
        }

        // Then deactivate the panel
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }

    public bool IsShopOpen()
    {
        return isVisible;
    }

    public GameObject GetShopPanel()
    {
        return shopPanel;
    }

    public void SetBuildTarget(StructureData data)
    {
        // Only allow building when shop is open
        if (!isVisible)
        {
            return;
        }

        if (data.prefab == null)
        {
            return;
        }

        // Find ghost placer to show placement preview
        GhostPlacer ghostPlacer = FindFirstObjectByType<GhostPlacer>();
        if (ghostPlacer != null)
        {
            ghostPlacer.SetGhostPrefab(data.prefab);
        }
        else
        {
            // For testing, just spawn it at a random-ish position
            Vector3 spawnPosition = new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
            Instantiate(data.prefab, spawnPosition, Quaternion.identity);
        }
    }

    public void enableShop()
    {
        UpdateShopButtonStateForTimeControls();
    }

    public void disableShop()
    {
        shopButton.interactable = false;
        shopIcon.color = nightShop;
        
        // Force close shop if it's currently open (e.g., when night starts)
        if (IsShopOpen())
        {
            CloseShop();
        }
    }

    public void ResetShopState()
    {
        isVisible = false;
        if (shopPanelUI != null)
        {
            shopPanelUI.CloseShop();
        }
        shopPanel.SetActive(false);
    }

    public bool IsFarmHousePlaced { get; private set; } = false;

    public void OnFarmHousePlaced()
    {
        IsFarmHousePlaced = true;
        if (shopPanelUI != null)
            shopPanelUI.PopulateShop();
    }

    public void OnFarmHouseRemoved()
    {
        IsFarmHousePlaced = false;
        if (shopPanelUI != null)
            shopPanelUI.PopulateShop();
    }

    [ContextMenu("Debug: Reset Farmhouse Placed Flag")]
    public void DebugResetFarmhousePlacedFlag()
    {
        Debug.Log($"DEBUG: Resetting IsFarmHousePlaced from {IsFarmHousePlaced} to false");
        IsFarmHousePlaced = false;
        if (shopPanelUI != null)
            shopPanelUI.PopulateShop();
    }
    
        public void UpdateShopButtonStateForTimeControls()
        {
            // Check tutorial state first - this takes highest priority
            if (!IsShopAllowedInTutorial())
            {
                shopButton.interactable = false;
                shopIcon.color = nightShop;
                return;
            }
            
            // If shop is allowed in tutorial (or tutorial not active), handle based on day/night and pause state
            NightManager nightManager = NightManager.Instance;
            if (nightManager != null && nightManager.IsDay) // && !nightManager.IsPaused)
            {
                shopButton.interactable = true;
                shopIcon.color = dayShop;
            }
            else
            {
                shopButton.interactable = false;
                shopIcon.color = nightShop;
            }
        }
}