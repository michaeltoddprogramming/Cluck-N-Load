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
            Debug.LogError("Multiple instances of ShopUIManager detected! Destroying duplicate.");
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
            Debug.LogError("ShopUIManager: shopPanel is not assigned in the Inspector!");
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
    }

    public void ToggleShop()
    {
        // Prevent rapid clicking/double clicks
        if (Time.time - lastClickTime < clickCooldown)
        {
            Debug.Log("ToggleShop called too soon after last click - ignoring");
            return;
        }
        lastClickTime = Time.time;
        
        Debug.Log($"ToggleShop called. Current isVisible: {isVisible}, panel active: {shopPanel != null && shopPanel.activeSelf}");
        
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
        Debug.Log($"OpenShop called. Button interactable: {shopButton.interactable}");
        
        // Don't allow opening shop if it's disabled (nighttime)
        if (!shopButton.interactable)
        {
            Debug.Log("Shop is disabled (nighttime)");
            return;
        }
        
        // Check if already open
        if (shopPanel != null && shopPanel.activeSelf)
        {
            Debug.Log("Shop is already open");
            return;
        }
        
        Debug.Log("Opening shop panel");
        isVisible = true;
        
        // Activate the panel FIRST
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            shopPanel.transform.SetAsLastSibling();
            Debug.Log($"Shop panel activated: {shopPanel.activeSelf}");
        }
        else
        {
            Debug.LogError("Shop panel is null!");
            return;
        }

        // THEN call ShopPanelUI methods
        if (shopPanelUI != null)
        {
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
        Debug.Log("CloseShop called");
        
        // Check if already closed
        if (shopPanel != null && !shopPanel.activeSelf)
        {
            Debug.Log("Shop is already closed");
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
            Debug.Log($"Shop panel deactivated: {shopPanel.activeSelf}");
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
            Debug.LogError($"No prefab assigned to {data.structureName}!");
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

    //disable the shop button
        public void disableShop()
        {
            shopButton.interactable = false;
            shopIcon.color = nightShop;
        }

    //enable the shop button
    public void enableShop()
    {
        shopButton.interactable = true;
        shopIcon.color = dayShop;
    }

    // Resets the shop state to fully closed and ready
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
}