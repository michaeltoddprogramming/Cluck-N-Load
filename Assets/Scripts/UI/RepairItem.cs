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

    [Header("Pulse Highlight Settings")]
    [SerializeField] private float pulseScale = 1.2f;
    [SerializeField] private float pulseDuration = 0.5f;
    private bool isPulsing = false;

    public System.Action<RepairItem> OnRepaired;

    private UIHover uiHover;


    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        uiHover = FindFirstObjectByType<UIHover>();
        if (uiHover == null)
            Debug.LogWarning("No UIHover found in the scene!");
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
            AudioManager.Instance?.PlayRepairSound();
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

    public int GetRepairCost()
    {
        return repairCost;
    }

    public void OnRepairButtonHoverEnter()
    {
        if (!repairButton.interactable && !isPulsing)
        {
            if(uiHover != null)
            {
                uiHover.Show("Broke!", "You can't afford repairs!", repairButton.GetComponent<RectTransform>());
            }

            isPulsing = true;
            costText.rectTransform.pivot = new Vector2(0.5f, 0.5f); // ensure pivot center
            LeanTween.scale(costText.gameObject, Vector3.one * pulseScale, pulseDuration)
                .setEaseInOutSine()
                .setLoopPingPong();
        }
    }

    public void OnRepairButtonHoverExit()
    {
        if(uiHover != null)
        {
            uiHover.Hide();
        }

        if (isPulsing)
        {

            isPulsing = false;
            LeanTween.cancel(costText.gameObject);
            costText.transform.localScale = Vector3.one;
        }
    }

    public void OnRepairButtonClick()
    {
        if(repairButton.interactable == false)
        {
            AudioManager.Instance?.PlayInsufficientFundsSound();  
            return;      
        }        
    }
}
