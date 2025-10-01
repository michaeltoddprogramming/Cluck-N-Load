using UnityEngine;
using TMPro;

public class ChainCostDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI affordableText;
    [SerializeField] private string costFormat = "Cost: {0} {1}";
    [SerializeField] private string affordableFormat = "Affordable: {0}/{1}";
    
    private RectTransform rectTransform;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // Initially hide the display
        gameObject.SetActive(false);
    }
    
    public void ShowCostDisplay(int totalCost, int affordableCount, int totalCount)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        
        UpdateCostDisplay(totalCost, affordableCount, totalCount);
    }
    
    public void HideCostDisplay()
    {
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }
    
    public void UpdatePosition(Vector2 screenPosition, Vector2 offset)
    {
        if (rectTransform != null)
        {
            rectTransform.position = screenPosition + offset;
        }
    }
    
    private void UpdateCostDisplay(int totalCost, int affordableCount, int totalCount)
    {
        if (costText != null)
        {
            string currencyName = MoneyManager.Instance != null ? 
                MoneyManager.Instance.GetCurrencyName() : "Coins";
            costText.text = string.Format(costFormat, totalCost, currencyName);
            
            // Change cost text color based on affordability
            if (affordableCount < totalCount)
            {
                costText.color = Color.red;
            }
            else
            {
                costText.color = Color.white;
            }
        }
        
        if (affordableText != null)
        {
            affordableText.text = string.Format(affordableFormat, affordableCount, totalCount);
            
            // Change color based on affordability
            if (affordableCount < totalCount)
            {
                affordableText.color = Color.red;
            }
            else
            {
                affordableText.color = Color.green;
            }
        }
    }
}