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

    private bool isShopOpen = false; // Tracks whether the shop is open
    private BuildController buildController;
    private CameraController cameraController;
    private bool wasGhostActiveBeforeHover = false;

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
            closeButton.onClick.AddListener(() => {
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
        else
        {
            Debug.LogWarning("Close button is not assigned in the inspector!");
        }

        // Cache controller references
        buildController = FindObjectOfType<BuildController>();
        cameraController = FindObjectOfType<CameraController>();
    }

    void PopulateShop()
    {
        if (database == null)
        {
            Debug.LogError("💥 StructureDatabase is NOT assigned in the inspector!");
            return;
        }

        if (database.allStructures == null || database.allStructures.Count == 0)
        {
            Debug.LogWarning("🫠 StructureDatabase is empty. No structures to display.");
            return;
        }

        Debug.Log($"🔍 Populating shop with {database.allStructures.Count} structures...");

        foreach (StructureData data in database.allStructures)
        {
            if (data == null)
            {
                Debug.LogError("🚨 StructureData entry is NULL in the database! Skipping...");
                continue;
            }

            Debug.Log($"✅ Creating UI for structure: {data.structureName}");

            GameObject item = Instantiate(itemPrefab, contentParent);
            if (item == null)
            {
                Debug.LogError("❌ Failed to instantiate itemPrefab!");
                continue;
            }

            StructureItemUI itemUI = item.GetComponent<StructureItemUI>();
            if (itemUI == null)
            {
                Debug.LogError($"🧨 StructureItemUI script is missing on prefab: {item.name}");
                continue;
            }

            itemUI.Setup(data);
        }

        Debug.Log("🎉 Shop population complete!");
    }

    public void OpenShop()
{
    isShopOpen = true;

    // Move THIS UI panel (gameObject) to the top of the hierarchy
    transform.SetAsLastSibling();

    // Show this UI panel
    gameObject.SetActive(true);

    Debug.Log("Shop Panel: OnShopOpened event firing");
    OnShopOpened.Invoke();

    // Let the BuildController know the shop opened
    if (buildController != null)
    {
        buildController.HandleShopOpened();
    }
    else
    {
        Debug.LogWarning("Shop Panel: BuildController not found in scene!");
    }
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