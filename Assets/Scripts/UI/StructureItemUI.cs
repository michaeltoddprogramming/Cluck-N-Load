using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class StructureItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descriptionText; 
    public Button selectButton;
    public GameObject lockedOverlay; // <-- Add this to your prefab and assign in inspector

    private StructureData data;
    private BuildController buildController;  // Add this field to cache the reference

    private void Start()
    {
        // Cache the BuildController reference
        buildController = FindFirstObjectByType<BuildController>();
    }

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

        if (nameText != null)
            nameText.text = structure.structureName;

        if (costText != null)
            costText.text = $"{structure.cost}";

        if (descriptionText != null)
            descriptionText.text = structure.description;

        int currentDay = NightManager.Instance != null ? NightManager.Instance.Days : 0;
        bool isLocked = structure.unlockDay > currentDay;

        if (lockedOverlay != null)
            lockedOverlay.SetActive(isLocked);

        if (isLocked)
        {
            if (selectButton != null)
                selectButton.interactable = false;
            if (icon != null)
                icon.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);

            // Only update UnlockText in overlay, not the price
            if (lockedOverlay != null)
            {
                var unlockText = lockedOverlay.GetComponentInChildren<TextMeshProUGUI>();
                if (unlockText != null)
                    unlockText.text = $"Unlocks on Day {structure.unlockDay}";
            }
            return;
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => SelectStructure());
        }

        UpdateAffordability();

        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnMoneyChanged += OnMoneyChanged;
    }

    public void SelectStructure()
    {
        if (data == null)
        {
            Debug.LogError("StructureData is null when selecting structure!");
            return;
        }

        BuildController controller = FindFirstObjectByType<BuildController>();
        if (controller != null)
            controller.SetBuildTarget(data);
    }

    private void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveAllListeners();
            
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnMoneyChanged -= OnMoneyChanged;
    }
    
    private void OnMoneyChanged(int newAmount) => UpdateAffordability();

    public void UpdateAffordability()
    {
        if (data == null || selectButton == null || MoneyManager.Instance == null)
            return;
                
        bool canAfford = MoneyManager.Instance.CanAfford(data.cost);
        selectButton.interactable = canAfford;
        
        if (costText != null)
        {
            costText.color = canAfford ? Color.white : Color.red;
            costText.text = canAfford ? $"{data.cost}" : $"{data.cost} (Cannot Afford!)";
        }

        if (icon != null)
            icon.color = canAfford ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.8f);

        // Notify BuildController to update ghost if affordability changed
        if (buildController != null)  // Use the cached reference
        {
            buildController.UpdateGhostAffordability(canAfford);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ItemHoverPanel.Instance != null)
            ItemHoverPanel.Instance.Show(data);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemHoverPanel.Instance != null)
            ItemHoverPanel.Instance.Hide();
    }
}
