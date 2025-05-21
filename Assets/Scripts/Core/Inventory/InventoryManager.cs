using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

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
        // Set up text component mappings
        if (sunflowerText != null) itemTextComponents["Sunflower"] = sunflowerText;
        if (wheatText != null) itemTextComponents["Wheat"] = wheatText;
        if (carrotsText != null) itemTextComponents["Carrots"] = carrotsText;
        
        // Initialize UI with starting values
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