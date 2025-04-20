using UnityEngine;
using TMPro;

public class MoneyDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private string format = "{0} {1}"; // {0} = amount, {1} = currency name
    
    private void Start()
    {
        if (MoneyManager.Instance != null)
        {
            // Subscribe to money change events
            MoneyManager.Instance.OnMoneyChanged += UpdateMoneyDisplay;
            
            // Initial display
            UpdateMoneyDisplay(MoneyManager.Instance.GetCurrentMoney());
        }
        else
        {
            Debug.LogWarning("MoneyManager not found in scene!");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe when destroyed
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged -= UpdateMoneyDisplay;
        }
    }
    
    private void UpdateMoneyDisplay(int amount)
    {
        if (moneyText != null)
        {
            string currencyName = MoneyManager.Instance.GetCurrencyName();
            moneyText.text = string.Format(format, amount, currencyName);
        }
    }
}