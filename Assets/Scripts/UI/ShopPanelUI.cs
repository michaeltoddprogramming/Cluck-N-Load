using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShopPanelUI : MonoBehaviour
{
    public static ShopPanelUI Instance { get; private set; } // Singleton instance

    public GameObject itemPrefab; // Your StructureItem prefab
    public Transform contentParent; // The "StructureList" object
    public StructureDatabase database; // Your ScriptableObject
    [SerializeField] private Button closeButton; // Reference to the close button

    private bool isShopOpen = false; // Tracks whether the shop is open

    private void Awake()
    {
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
        gameObject.SetActive(true);
    }

    public void CloseShop()
    {
        isShopOpen = false;
        Debug.Log("CloseShop method called!");
        gameObject.SetActive(false);
    }

    public bool IsShopOpen()
    {
        return isShopOpen;
    }
}