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

        if (icon != null)
            icon.sprite = structure.icon;
        else
            Debug.LogWarning("Icon Image is not assigned!");

        if (nameText != null)
            nameText.text = structure.structureName;
        else
            Debug.LogWarning("Name Text is not assigned!");

        // Display cost if we have a cost text component
        if (costText != null)
            costText.text = $"{structure.cost} Gold";

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners(); // Prevent stacking listeners
            selectButton.onClick.AddListener(() => SelectStructure());
        }
        else
            Debug.LogWarning("Select Button is not assigned!");
            
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
        if (data != null)
        {
            Debug.Log($"Selected structure: {data.structureName}");
            // Pass the StructureData to the BuildController
            BuildController controller = FindObjectOfType<BuildController>();
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
        }
    }
}