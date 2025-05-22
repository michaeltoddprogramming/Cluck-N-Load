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

    private void Start()
    {
        // Validate and set up text component mappings
        itemTextComponents["Sunflower"] = sunflowerText ?? throw new System.NullReferenceException("Sunflower TextMeshProUGUI is not assigned.");
        itemTextComponents["Wheat"] = wheatText ?? throw new System.NullReferenceException("Wheat TextMeshProUGUI is not assigned.");
        itemTextComponents["Carrots"] = carrotsText ?? throw new System.NullReferenceException("Carrots TextMeshProUGUI is not assigned.");

        UpdateAllUI();
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
}