using UnityEngine;
using System;
using TMPro;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }
    
    [SerializeField] private int startingMoney = 1200;
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
            Debug.Log($"MoneyManager: Loaded saved money: {_currentMoney}");
        }
        else
        {
            _currentMoney = startingMoney;
            Debug.Log($"MoneyManager: Using starting money: {_currentMoney}");
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
    
    public bool SpendMoney(int amount)
    {
        // Check cheat modes ONLY for unlimited building/god mode
        if (CheatManager.Instance != null && 
            (CheatManager.Instance.IsGodModeActive() || CheatManager.Instance.IsUnlimitedBuildingActive()))
        {
            OnMoneyChanged?.Invoke(_currentMoney);
            return true;
        }
        
        // Original logic
        if (!CanAfford(amount)) 
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayInsufficientFundsSound();
            }
            return false;
        }
            
        _currentMoney -= amount;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMoneySpendSound();
        }
        UpdateMoneyUI();
        SaveMoney();
        OnMoneyChanged?.Invoke(_currentMoney);
        return true;
    }
    
    public bool CanAfford(int amount)
    {
        // Check cheat modes
        if (CheatManager.Instance != null && 
            (CheatManager.Instance.IsGodModeActive() || CheatManager.Instance.IsUnlimitedBuildingActive()))
        {
            return true;
        }
        
        return _currentMoney >= amount;
    }
    
    // ADD THIS MISSING METHOD
    public void AddMoney(int amount)
    {
        _currentMoney += amount;
        UpdateMoneyUI();
        SaveMoney();
        OnMoneyChanged?.Invoke(_currentMoney);
    }
    
    // Keep cheat method separate
    public void CheatSetMoney(int amount)
    {
        _currentMoney = amount;
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
        Debug.Log($"MoneyManager: Reset money to starting amount: {_currentMoney}");
    }
}