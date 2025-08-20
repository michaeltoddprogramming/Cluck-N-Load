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

    private StructureData data;

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
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Enter");
        if (ItemHoverPanel.Instance != null)
            ItemHoverPanel.Instance.Show(data);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("Exit");
        if (ItemHoverPanel.Instance != null)
            ItemHoverPanel.Instance.Hide();
    }
}
