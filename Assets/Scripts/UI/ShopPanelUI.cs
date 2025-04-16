using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShopPanelUI : MonoBehaviour
{
    public GameObject itemPrefab; // Your StructureItem prefab
    public Transform contentParent; // The "StructureList" object
    public StructureDatabase database; // Your ScriptableObject

    void Start()
    {
        PopulateShop();
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

}