using UnityEngine;
using TMPro;
using UnityEngine.UI;



public class RepairItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI structureNameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Button repairButton;
    private CanvasGroup canvasGroup; 

    private GameObject thisBuilding;
    private int currentMoney;
    private int repairCost;

    public System.Action<RepairItem> OnRepaired;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    void Update()
    {
        Debug.Log("Current Money: " + currentMoney + ", Repair Cost: " + repairCost);
        Debug.Log("Repair Button Interactable: " + (repairButton != null ? repairButton.interactable.ToString() : "null"));
    }


    public void Initialize(GameObject building, string structureName, int cost)
    {
        structureNameText.text = structureName;
        repairCost = cost;
        costText.text = $"{cost}"; 
        thisBuilding = building;       

        MoneyManager.Instance.OnMoneyChanged += UpdateButtonState; 

        UpdateButtonState(MoneyManager.Instance.GetCurrentMoney());
    }

    public void repairBuilding()
    {
        if(thisBuilding.GetComponent<Structure>().Repair())
        {
            MoneyManager.Instance.SpendMoney(repairCost);
            OnRepaired?.Invoke(this);
        }
    }

    private void UpdateButtonState(int money)
    {
        currentMoney = money;
        if (repairButton != null)
        {
            repairButton.interactable = currentMoney >= repairCost;
        }

        if(costText != null)
        {
            costText.color = currentMoney >= repairCost ? Color.white : Color.red;
        }
    }

    private void OnDestroy()
    {
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnMoneyChanged -= UpdateButtonState;
    }

    
}
