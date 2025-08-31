using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ShopPanelUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static ShopPanelUI Instance { get; private set; } // Singleton instance

    public GameObject itemPrefab; // Your StructureItem prefab
    public Transform contentParent; // The "StructureList" object
    public StructureDatabase database; // Your ScriptableObject
    [SerializeField] private Button closeButton; // Reference to the close button

    // Events for opening/closing shop
    public UnityEvent OnShopOpened = new UnityEvent();
    public UnityEvent OnShopClosed = new UnityEvent();

    [Header("Performance Settings")]
    [SerializeField] private bool enableAnimations = true; // Can disable for potato devices
    [SerializeField] private bool poolStructureItems = true; // Object pooling for performance
    [SerializeField] private int maxVisibleItems = 20; // Limit visible items for performance

    private bool isShopOpen = false; // Tracks whether the shop is open
    private BuildController buildController;
    private CameraController cameraController;
    private bool wasGhostActiveBeforeHover = false;

    // private bool navBarChange = true; // if user chose something else in the nav bar
    private char currNav = 'C'; // current nav bar selection

    [SerializeField] private GameObject comingSoonPanel; // Panel for upcoming features 

    private void Awake()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = 100; // Ensure Shop UI is on top
        }
        // Ensure only one instance of ShopPanelUI exists
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple instances of ShopPanelUI detected! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        PopulateShop();

        // Link the close button to the ShopUIManager's CloseShop method
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() =>
            {
                if (ShopUIManager.Instance != null)
                {
                    ShopUIManager.Instance.CloseShop();
                }
                else
                {
                    CloseShop(); // Fallback if ShopUIManager isn't available
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

        switch (num)
        {
            case 0:
                currNav = 'C'; // animals
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
            case 4:
                currNav = 'D'; // decoration
                break;
            default:
                Debug.LogWarning("Invalid nav bar selection: " + num);
                return;
        }

        if (tempNav != currNav)
        {
            PopulateShop(currNav);
        }
    }

    public void PopulateShop(char display = 'C')
    {
        int currentDay = NightManager.Instance != null ? NightManager.Instance.Days : 0;

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

        foreach (StructureData data in database.allStructures)
        {
            if (data == null) continue;

            // Skip farmhouse if already placed
            if (data.structureName.ToLower().Contains("farm house") && ShopUIManager.Instance != null && ShopUIManager.Instance.IsFarmHousePlaced)
                continue;

            bool showItem = false;
            switch (display)
            {
                case 'C': showItem = data.type == StructureType.Animal; break;
                case 'A': showItem = data.type == StructureType.Barracks; break;
                case 'S': showItem = data.type == StructureType.Defense; break;
                case 'P': showItem = data.type == StructureType.CropPlot || data.type == StructureType.Silo; break;
                case 'D': showItem = data.type == StructureType.Decoration; break;
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

            itemUI.Setup(data);
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

        // Move THIS UI panel (gameObject) to the top of the hierarchy
        transform.SetAsLastSibling();

        // Show this UI panel
        gameObject.SetActive(true);

        OnShopOpened.Invoke();

        // Let the BuildController know the shop opened
        if (buildController == null)
        {
            // Try to find it again in case it wasn't available earlier
            buildController = FindFirstObjectByType<BuildController>();
        }

        if (buildController != null)
        {
            buildController.HandleShopOpened();
        }
        // Remove the warning as it's not critical - shop can work without BuildController
    }

    public void CloseShop()
    {

        isShopOpen = false;

        // Make sure to re-enable controls before deactivating the panel
        // This ensures controls are restored even if OnPointerExit doesn't trigger
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

        gameObject.SetActive(false);
        OnShopClosed.Invoke();
    }

    public bool IsShopOpen()
    {
        return gameObject.activeSelf;
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
}