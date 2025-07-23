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

        // Set icon if available
        if (icon != null)
            icon.sprite = structure.icon;

        // Set name text if available
        if (nameText != null)
        {
            nameText.text = structure.structureName;
        }

        // Display cost if we have a cost text component
        if (costText != null)
        {
            costText.text = $"{structure.cost}";
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners(); // Prevent stacking listeners
            selectButton.onClick.AddListener(() => SelectStructure());
        }
        else
        {
            // If no button is assigned, try to find one on this GameObject or its children
            selectButton = GetComponentInChildren<Button>();
            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => SelectStructure());
            }
        }

        // Check affordability when setting up
        UpdateAffordability();
        
        // Subscribe to money changes
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged += OnMoneyChanged;
        }
    }

    public void SelectStructure()
    {
        if (data == null)
        {
            Debug.LogError("StructureData is null when selecting structure!");
            return;
        }

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
                costText.text = $"{data.cost} (Cannot Afford!)";
            }
            else
            {
                costText.text = $"{data.cost}";
            }
        }
        
        // Optional: You can also gray out or modify the icon
        if (icon != null)
        {
            icon.color = canAfford ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.8f);
        }
    }
}