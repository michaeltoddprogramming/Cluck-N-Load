using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI sunflowerText;
    [SerializeField] private TextMeshProUGUI wheatText;
    [SerializeField] private TextMeshProUGUI carrotsText;
    [SerializeField] public int totalPerSilo = 100;
    [SerializeField] public int totalCapacity = 0;
    [SerializeField] public int currCapacity = 0;
    private List<SiloStructure> silos = new List<SiloStructure>();


    // Event that fires whenever inventory changes
    public System.Action OnInventoryChanged;

    private Dictionary<string, int> inventory = new Dictionary<string, int>();
    private Dictionary<string, TextMeshProUGUI> itemTextComponents = new Dictionary<string, TextMeshProUGUI>();


    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize inventory with crop types
        inventory["Sunflower"] = 20;
        inventory["Wheat"] = 20;
        inventory["Carrots"] = 20;
        Debug.Log("Inventory initialized: Sunflower=20, Wheat=20, Carrots=20");
    }

    private void Update()
    {
        totalCapacity = GetTotalSiloCapacity();
        currCapacity = GetCurrentSiloCapacity();
    }


    private void Start()
    {
        if (sunflowerText != null) itemTextComponents["Sunflower"] = sunflowerText;
        if (wheatText != null) itemTextComponents["Wheat"] = wheatText;
        if (carrotsText != null) itemTextComponents["Carrots"] = carrotsText;

        CropStructure.UpdateAllCropSynergies();

        UpdateAllUI();
    }

    // --- Silo Management ---
    public void RegisterSilo(SiloStructure silo)
    {
        if (!silos.Contains(silo))
        {
            silos.Add(silo);
            OnInventoryChanged?.Invoke();
            // CropStructure.OnPlaced();
            CropStructure.UpdateAllCropSynergies();
        }
    }

    public void UnregisterSilo(SiloStructure silo)
    {
        if (silos.Contains(silo))
        {
            silos.Remove(silo);
            OnInventoryChanged?.Invoke();
            CropStructure.UpdateAllCropSynergies();
        }
    }

    public void AddItem(string itemName, int amount)
    {
        if (!inventory.ContainsKey(itemName))
        {
            inventory[itemName] = 0;
        }
        inventory[itemName] += amount;
        Debug.Log($"Added {amount} {itemName} to inventory. New total: {inventory[itemName]}");

        // Update UI
        UpdateItemUI(itemName);

        // Notify listeners
        OnInventoryChanged?.Invoke();
    }

    public bool HasItem(string itemName, int amount)
    {
        if (!inventory.ContainsKey(itemName))
        {
            return false;
        }
        return inventory[itemName] >= amount;
    }

    public void RemoveItem(string itemName, int amount)
    {
        if (HasItem(itemName, amount))
        {
            inventory[itemName] -= amount;
            Debug.Log($"Removed {amount} {itemName} from inventory. New total: {inventory[itemName]}");

            // Update UI
            UpdateItemUI(itemName);

            // Notify listeners
            OnInventoryChanged?.Invoke();
        }
        else
        {
            Debug.LogWarning($"Cannot remove {amount} {itemName}: Not enough in inventory!");
        }
    }

    public int GetItemCount(string itemName)
    {
        return inventory.ContainsKey(itemName) ? inventory[itemName] : 0;
    }

    private void UpdateItemUI(string itemName)
    {
        if (itemTextComponents.ContainsKey(itemName) && itemTextComponents[itemName] != null)
        {
            itemTextComponents[itemName].text = inventory[itemName].ToString();
            Debug.Log($"Updated UI for {itemName}: {inventory[itemName]}");
        }
        else
        {
            Debug.LogWarning($"No TextMeshProUGUI assigned for {itemName}");
        }
    }

    private void UpdateAllUI()
    {
        foreach (var item in inventory)
        {
            UpdateItemUI(item.Key);
        }
    }

    public int GetTotalSiloCapacity()
    {
        totalCapacity = silos.Count * totalPerSilo;
        return totalCapacity;
    }

    public int GetCurrentSiloCapacity()
    {
        currCapacity = inventory["Sunflower"] + inventory["Wheat"] + inventory["Carrots"];
        return currCapacity;
    }

    public bool canHarvest(int harvestAmount)
    {
        if (currCapacity < totalCapacity && currCapacity + harvestAmount <= totalCapacity)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public int[] getInventory(string[] crops)
    {
        int[] amounts = new int[crops.Length];

        for (int k = 0; k < crops.Length; k++)
        {
            string crop = crops[k];

            switch (crop)
            {
                case "s":
                    amounts[k] = GetItemCount("Sunflower");
                    break;
                case "w":
                    amounts[k] = GetItemCount("Wheat");
                    break;
                case "c":
                    amounts[k] = GetItemCount("Carrots");
                    break;
                default:
                    Debug.LogError("THis crop type does not exist!!!!");
                    amounts[k] = -10;
                    break;
            }
        }

        // Debug.Log("------------------------------------------------------" + amounts);

        return amounts;
        

    }

}