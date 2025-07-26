using UnityEngine;
using System;
using TMPro;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }
    
    [SerializeField] private int startingMoney = 800;
    [SerializeField] private string currencyName = "Gold";
    [SerializeField] private bool resetMoneyOnStart = true;
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI moneyText;

    private int _currentMoney;
    
    public event Action<int> OnMoneyChanged;
    
    private void Awake()
    {
        // Singleton pattern
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
        
        // Reset money if option is checked
        if (resetMoneyOnStart)
        {
            PlayerPrefs.DeleteKey("PlayerMoney");
        }
        
        LoadMoney();
    }
    
    private void Start()
    {
        UpdateMoneyUI();
        }
    
    private void LoadMoney()
    {
        // Load saved money or use starting amount
        if (PlayerPrefs.HasKey("PlayerMoney"))
        {
            _currentMoney = PlayerPrefs.GetInt("PlayerMoney");
            }
        else
        {
            _currentMoney = startingMoney;
            }
            
        // Notify listeners of initial amount
        OnMoneyChanged?.Invoke(_currentMoney);
    }
    
    private void UpdateMoneyUI()
    {
        if (moneyText != null)
        {
            moneyText.text = $"{_currentMoney} {currencyName}";
        }
    }
    
    public int GetCurrentMoney()
    {
        return _currentMoney;
    }
    
    public string GetCurrencyName()
    {
        return currencyName;
    }
    
    public bool CanAfford(int cost)
    {
        return _currentMoney >= cost;
    }
    
    public bool SpendMoney(int amount)
    {
        if (!CanAfford(amount)) {
            // Play insufficient funds sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayInsufficientFundsSound();
            }
            return false;
        }
            
        _currentMoney -= amount;
        // Play money spend sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMoneySpendSound();
        }
        UpdateMoneyUI();
        SaveMoney();
        OnMoneyChanged?.Invoke(_currentMoney);
        return true;
    }
    
    public void AddMoney(int amount)
    {
        _currentMoney += amount;
        UpdateMoneyUI();
        SaveMoney();
        OnMoneyChanged?.Invoke(_currentMoney);
    }
    
    private void SaveMoney()
    {
        PlayerPrefs.SetInt("PlayerMoney", _currentMoney);
        PlayerPrefs.Save();
    }
    
    public void ResetMoney()
    {
        _currentMoney = startingMoney;
        UpdateMoneyUI();
        SaveMoney();
        OnMoneyChanged?.Invoke(_currentMoney);
        }
}