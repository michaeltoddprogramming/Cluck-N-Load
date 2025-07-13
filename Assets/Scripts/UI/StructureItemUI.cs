using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StructureItemUI : MonoBehaviour
{
    public Image icon;
    public TMP_Text nameText;
    public TMP_Text costText; // Add this if you want to display cost
    public Button selectButton;

    private StructureData data;

    // Setup is called when populating the UI from the database
    public void Setup(StructureData structure)
    {
        if (structure == null)
        {
            Debug.LogError("StructureData is null!");
            return;
        }

        data = structure;
        Debug.Log($"Setting up shop item: {structure.structureName} - Cost: {structure.cost}");

        // Set icon if available
        if (icon != null)
            icon.sprite = structure.icon;
        else
            Debug.LogWarning($"Icon component is missing for {structure.structureName}!");

        // Set name text if available
        if (nameText != null)
        {
            nameText.text = structure.structureName;
            Debug.Log($"Set name text to: {structure.structureName}");
        }
        else
            Debug.LogWarning($"Name text component is missing for {structure.structureName}!");

        // Display cost if we have a cost text component
        if (costText != null)
        {
            costText.text = $"{structure.cost} Gold";
            Debug.Log($"Set cost text to: {structure.cost} Gold");
        }
        else
            Debug.LogWarning($"Cost text component is missing for {structure.structureName}!");

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners(); // Prevent stacking listeners
            selectButton.onClick.AddListener(() => SelectStructure());
        }
        else
            Debug.LogWarning($"Select button component is missing for {structure.structureName}!");

        // Check affordability when setting up
        UpdateAffordability();
        
        // Subscribe to money changes
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged += OnMoneyChanged;
        }
        else
        {
            Debug.LogWarning("MoneyManager.Instance is null when setting up shop item!");
        }
    }

    public void SelectStructure()
    {
        if (data != null)
        {
            // Pass the StructureData to the BuildController
            BuildController controller = FindFirstObjectByType<BuildController>();
            if (controller != null)
            {
                controller.SetBuildTarget(data);
            }
            else
            {
                Debug.LogError("BuildController not found in scene!");
            }
        }
        else
        {
            Debug.LogError("StructureData is null when selecting structure!");
        }
    }

    private void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveAllListeners();
            
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnMoneyChanged -= OnMoneyChanged;
    }
    
    private void OnMoneyChanged(int newAmount)
    {
        UpdateAffordability();
    }

    // Update affordability based on current money
    public void UpdateAffordability()
    {
        // Make sure we have data and button
        if (data == null || selectButton == null || MoneyManager.Instance == null)
            return;
                
        // Change the appearance based on whether the player can afford this structure
        bool canAfford = MoneyManager.Instance.CanAfford(data.cost);
                         
        // Visual feedback through button interactability
        selectButton.interactable = canAfford;
        
        // Visual feedback through cost text color (if available)
        if (costText != null)
        {
            costText.color = canAfford ? Color.white : Color.red;
            
            // Add a "Cannot Afford" text or symbol for extra clarity
            if (!canAfford)
            {
                costText.text = $"Cost: {data.cost} Gold (Cannot Afford!)";
            }
            else
            {
                costText.text = $"Cost: {data.cost} Gold";
            }
        }
        
        // Optional: You can also gray out or modify the icon
        if (icon != null)
        {
            icon.color = canAfford ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.8f);
        }
    }
}