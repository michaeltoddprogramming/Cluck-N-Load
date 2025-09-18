using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class CheatManager : MonoBehaviour
{
    public static CheatManager Instance { get; private set; }
    
    [Header("Cheat Activation")]
    [SerializeField] private KeyCode[] cheatKeys = { KeyCode.C, KeyCode.H, KeyCode.E, KeyCode.A, KeyCode.T };
    [SerializeField] private float keyInputTimeout = 2f;
    
    [Header("Cheat Panel UI")]
    [SerializeField] private GameObject cheatPanel;
    [SerializeField] private Button closeButton;
    
    [Header("Money Cheats")]
    [SerializeField] private Button add1000MoneyButton;
    [SerializeField] private Button add10000MoneyButton;
    [SerializeField] private Button resetMoneyButton;
    [SerializeField] private TMP_InputField customMoneyInput;
    [SerializeField] private Button setCustomMoneyButton;
    
    [Header("Time Cheats")]
    [SerializeField] private Button skipDayButton;
    [SerializeField] private Button skipSeasonButton;
    [SerializeField] private Button skipYearButton;
    [SerializeField] private Button toggleTimeButton;
    [SerializeField] private TextMeshProUGUI timeButtonText;
    [SerializeField] private Button forceNightButton;
    [SerializeField] private Button forceDayButton;
    
    [Header("Resource Cheats")]
    [SerializeField] private Button maxResourcesButton;
    [SerializeField] private Button clearResourcesButton;
    [SerializeField] private TMP_Dropdown resourceDropdown;
    [SerializeField] private TMP_InputField resourceAmountInput;
    [SerializeField] private Button addResourceButton;
    
    [Header("Structure Cheats")]
    [SerializeField] private Button healAllStructuresButton;
    [SerializeField] private Button destroyAllEnemiesButton;
    [SerializeField] private Button godModeButton;
    [SerializeField] private TextMeshProUGUI godModeText;
    [SerializeField] private Button unlimitedBuildingButton;
    [SerializeField] private TextMeshProUGUI unlimitedBuildingText;
    
    [Header("Unit Cheats")]
    [SerializeField] private Button spawnAnimalsButton;
    [SerializeField] private Button spawnEnemiesButton;
    [SerializeField] private Button killAllEnemiesButton;
    [SerializeField] private Button instantGrowthButton;
    
    [Header("Debug Info")]
    [SerializeField] private TextMeshProUGUI debugInfoText;
    [SerializeField] private Button refreshDebugButton;
    
    // Private variables
    private List<KeyCode> inputSequence = new List<KeyCode>();
    private float lastInputTime;
    private bool cheatPanelOpen = false;
    private bool timeFreeze = false;
    private bool godMode = false;
    private bool unlimitedBuilding = false;
    
    // Resource names for dropdown
    private string[] resourceNames = { "Sunflower", "Wheat", "Carrots", "Eggs", "Milk", "Bacon", "Cheese", "Wool" };
    
    private void Awake()
    {
        // Only initialize if this GameObject is explicitly placed in the scene
        // This prevents accidental initialization when other scripts reference it
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("CheatManager: Initialized explicitly");
            // Don't use DontDestroyOnLoad to avoid conflicts with scene management
        }
        else
        {
            Debug.Log("CheatManager: Duplicate instance destroyed");
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // Delay initialization to ensure all core systems are ready
        StartCoroutine(DelayedInitialize());
    }
    
    private IEnumerator DelayedInitialize()
    {
        // Wait for a few frames to ensure all managers are initialized
        yield return new WaitForSeconds(1f);
        
        SetupUI();
        if (cheatPanel != null)
            cheatPanel.SetActive(false);
    }
    
    private void Update()
    {
        HandleCheatKeyInput();
        
        if (cheatPanelOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseCheatPanel();
        }
    }
    
    private void HandleCheatKeyInput()
    {
        if (Time.time - lastInputTime > keyInputTimeout)
        {
            inputSequence.Clear();
        }
        
        foreach (KeyCode key in cheatKeys)
        {
            if (Input.GetKeyDown(key))
            {
                inputSequence.Add(key);
                lastInputTime = Time.time;
                
                if (CheckSequence())
                {
                    ToggleCheatPanel();
                    inputSequence.Clear();
                }
                
                if (inputSequence.Count > cheatKeys.Length)
                {
                    inputSequence.RemoveAt(0);
                }
                break;
            }
        }
    }
    
    private bool CheckSequence()
    {
        if (inputSequence.Count != cheatKeys.Length) return false;
        
        for (int i = 0; i < cheatKeys.Length; i++)
        {
            if (inputSequence[i] != cheatKeys[i]) return false;
        }
        return true;
    }
    
    private void SetupUI()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseCheatPanel);
        
        // Money cheats
        if (add1000MoneyButton != null)
            add1000MoneyButton.onClick.AddListener(() => AddMoney(1000));
        if (add10000MoneyButton != null)
            add10000MoneyButton.onClick.AddListener(() => AddMoney(10000));
        if (resetMoneyButton != null)
            resetMoneyButton.onClick.AddListener(() => SetMoney(0));
        if (setCustomMoneyButton != null)
            setCustomMoneyButton.onClick.AddListener(SetCustomMoney);
        
        // Time cheats
        if (skipDayButton != null)
            skipDayButton.onClick.AddListener(SkipDay);
        if (skipSeasonButton != null)
            skipSeasonButton.onClick.AddListener(SkipSeason);
        if (skipYearButton != null)
            skipYearButton.onClick.AddListener(SkipYear);
        if (toggleTimeButton != null)
            toggleTimeButton.onClick.AddListener(ToggleTimeFreeze);
        if (forceNightButton != null)
            forceNightButton.onClick.AddListener(ForceNight);
        if (forceDayButton != null)
            forceDayButton.onClick.AddListener(ForceDay);
        
        // Resource cheats
        if (maxResourcesButton != null)
            maxResourcesButton.onClick.AddListener(MaxAllResources);
        if (clearResourcesButton != null)
            clearResourcesButton.onClick.AddListener(ClearAllResources);
        if (addResourceButton != null)
            addResourceButton.onClick.AddListener(AddSelectedResource);
        
        // Structure cheats
        if (healAllStructuresButton != null)
            healAllStructuresButton.onClick.AddListener(HealAllStructures);
        if (destroyAllEnemiesButton != null)
            destroyAllEnemiesButton.onClick.AddListener(DestroyAllEnemies);
        if (godModeButton != null)
            godModeButton.onClick.AddListener(ToggleGodMode);
        if (unlimitedBuildingButton != null)
            unlimitedBuildingButton.onClick.AddListener(ToggleUnlimitedBuilding);
        
        // Unit cheats
        if (spawnAnimalsButton != null)
            spawnAnimalsButton.onClick.AddListener(SpawnAnimals);
        if (spawnEnemiesButton != null)
            spawnEnemiesButton.onClick.AddListener(SpawnEnemies);
        if (killAllEnemiesButton != null)
            killAllEnemiesButton.onClick.AddListener(KillAllEnemies);
        if (instantGrowthButton != null)
            instantGrowthButton.onClick.AddListener(InstantGrowthAllCrops);
        
        // Debug
        if (refreshDebugButton != null)
            refreshDebugButton.onClick.AddListener(RefreshDebugInfo);
        
        // Setup dropdown
        if (resourceDropdown != null)
        {
            resourceDropdown.ClearOptions();
            resourceDropdown.AddOptions(new List<string>(resourceNames));
        }
    }
    
    private void ToggleCheatPanel()
    {
        cheatPanelOpen = !cheatPanelOpen;
        if (cheatPanel != null)
        {
            cheatPanel.SetActive(cheatPanelOpen);
            if (cheatPanelOpen)
            {
                RefreshDebugInfo();
                UpdateButtonTexts();
            }
        }
        
        Debug.Log($"Cheat panel {(cheatPanelOpen ? "opened" : "closed")}");
    }
    
    private void CloseCheatPanel()
    {
        cheatPanelOpen = false;
        if (cheatPanel != null)
            cheatPanel.SetActive(false);
    }
    
    #region Money Cheats
    private void AddMoney(int amount)
    {
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AddMoney(amount);
            Debug.Log($"Added {amount} money");
            RefreshDebugInfo();
        }
    }
    
    private void SetMoney(int amount)
    {
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.CheatSetMoney(amount);
            Debug.Log($"Set money to {amount}");
            RefreshDebugInfo();
        }
    }
    
    private void SetCustomMoney()
    {
        if (customMoneyInput != null && int.TryParse(customMoneyInput.text, out int amount))
        {
            SetMoney(amount);
            customMoneyInput.text = "";
        }
    }
    #endregion
    
    #region Time Cheats
    private void SkipDay()
    {
        if (NightManager.Instance != null)
        {
            int currentDays = NightManager.Instance.GetDays();
            NightManager.Instance.CheatSetDays(currentDays + 1);
            Debug.Log("Skipped to next day");
            RefreshDebugInfo();
        }
    }
    
    private void SkipSeason()
    {
        if (NightManager.Instance != null)
        {
            int currentSeason = NightManager.Instance.GetCurrentSeason();
            int nextSeason = currentSeason >= 4 ? 1 : currentSeason + 1;
            NightManager.Instance.SetSeason(nextSeason);
            Debug.Log($"Skipped to season {nextSeason}");
            RefreshDebugInfo();
        }
    }
    
    private void SkipYear()
    {
        if (NightManager.Instance != null)
        {
            int currentDays = NightManager.Instance.GetDays();
            NightManager.Instance.CheatSetDays(currentDays + 20); // Skip full year
            NightManager.Instance.SetSeason(1); // Reset to spring
            Debug.Log("Skipped to next year");
            RefreshDebugInfo();
        }
    }
    
    private void ToggleTimeFreeze()
    {
        timeFreeze = !timeFreeze;
        if (timeFreeze)
        {
            if (NightManager.Instance != null)
                NightManager.Instance.pauseTime();
        }
        else
        {
            if (NightManager.Instance != null)
                NightManager.Instance.playTime();
        }
        Debug.Log($"Time {(timeFreeze ? "frozen" : "unfrozen")}");
        UpdateButtonTexts();
    }
    
    private void ForceNight()
    {
        if (NightManager.Instance != null)
        {
            NightManager.Instance.CheatForceNight();
            Debug.Log("Forced night time");
        }
    }
    
    private void ForceDay()
    {
        if (NightManager.Instance != null)
        {
            NightManager.Instance.CheatForceDay();
            Debug.Log("Forced day time");
        }
    }
    #endregion
    
    #region Resource Cheats
    private void MaxAllResources()
    {
        if (InventoryManager.Instance != null)
        {
            foreach (string resource in resourceNames)
            {
                InventoryManager.Instance.AddItem(resource, 9999);
            }
            Debug.Log("Maxed all resources");
            RefreshDebugInfo();
        }
    }
    
    private void ClearAllResources()
    {
        if (InventoryManager.Instance != null)
        {
            foreach (string resource in resourceNames)
            {
                int currentAmount = InventoryManager.Instance.GetItemCount(resource);
                if (currentAmount > 0)
                {
                    InventoryManager.Instance.RemoveItem(resource, currentAmount);
                }
            }
            Debug.Log("Cleared all resources");
            RefreshDebugInfo();
        }
    }
    
    private void AddSelectedResource()
    {
        if (InventoryManager.Instance != null && resourceDropdown != null && resourceAmountInput != null)
        {
            string selectedResource = resourceNames[resourceDropdown.value];
            if (int.TryParse(resourceAmountInput.text, out int amount))
            {
                InventoryManager.Instance.AddItem(selectedResource, amount);
                Debug.Log($"Added {amount} {selectedResource}");
                resourceAmountInput.text = "";
                RefreshDebugInfo();
            }
        }
    }
    #endregion
    
    #region Structure Cheats
    private void HealAllStructures()
    {
        Structure[] structures = FindObjectsByType<Structure>(FindObjectsSortMode.None);
        foreach (Structure structure in structures)
        {
            structure.CheatSetMaxHealth();
        }
        Debug.Log("Healed all structures");
    }
    
    private void DestroyAllEnemies()
    {
        EnemyUnit[] enemies = FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None);
        foreach (EnemyUnit enemy in enemies)
        {
            Destroy(enemy.gameObject);
        }
        Debug.Log($"Destroyed {enemies.Length} enemies");
    }
    
    private void ToggleGodMode()
    {
        godMode = !godMode;
        Debug.Log($"God mode {(godMode ? "enabled" : "disabled")}");
        UpdateButtonTexts();
        
        if (godMode)
        {
            HealAllStructures();
        }
    }
    
    private void ToggleUnlimitedBuilding()
    {
        unlimitedBuilding = !unlimitedBuilding;
        Debug.Log($"Unlimited building {(unlimitedBuilding ? "enabled" : "disabled")}");
        UpdateButtonTexts();
    }
    
    public bool IsGodModeActive()
    {
        try
        {
            return godMode;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"CheatManager.IsGodModeActive error: {ex.Message}");
            return false;
        }
    }
    
    public bool IsUnlimitedBuildingActive()
    {
        try
        {
            return unlimitedBuilding;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"CheatManager.IsUnlimitedBuildingActive error: {ex.Message}");
            return false;
        }
    }
    
    // Safe method to check if cheat manager is ready
    public static bool IsCheatManagerReady()
    {
        return Instance != null && Instance.gameObject.activeInHierarchy;
    }
    #endregion
    
    #region Unit Cheats
    private void SpawnAnimals()
    {
        BarracksStructure[] barracks = FindObjectsByType<BarracksStructure>(FindObjectsSortMode.None);
        foreach (var barrack in barracks)
        {
            barrack.CheatAddAnimals(5);
        }
        Debug.Log("Added animals to all barracks");
    }
    
    private void SpawnEnemies()
    {
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.StartCombat();
            Debug.Log("Force spawned enemies");
        }
    }
    
    private void KillAllEnemies()
    {
        EnemyUnit[] enemies = FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None);
        foreach (EnemyUnit enemy in enemies)
        {
            Destroy(enemy.gameObject);
        }
        Debug.Log($"Killed {enemies.Length} enemies");
    }
    
    private void InstantGrowthAllCrops()
    {
        CropStructure[] crops = FindObjectsByType<CropStructure>(FindObjectsSortMode.None);
        foreach (CropStructure crop in crops)
        {
            crop.CheatInstantGrowth();
        }
        Debug.Log("All crops instantly grown");
    }
    #endregion
    
    #region Debug Info
    private void RefreshDebugInfo()
    {
        if (debugInfoText == null) return;
        
        string debugText = "=== GAME STATE ===\n";
        
        if (MoneyManager.Instance != null)
            debugText += $"Money: ${MoneyManager.Instance.GetCurrentMoney()}\n";
        
        if (NightManager.Instance != null)
        {
            debugText += $"Day: {NightManager.Instance.GetDays()}\n";
            debugText += $"Season: {NightManager.Instance.GetCurrentSeason()}\n";
            debugText += $"Time: {NightManager.Instance.Hours:D2}:{NightManager.Instance.Minutes:D2}\n";
            debugText += $"Is Day: {NightManager.Instance.GetIsDay()}\n";
        }
        
        if (InventoryManager.Instance != null)
        {
            debugText += "\n=== RESOURCES ===\n";
            foreach (string resource in resourceNames)
            {
                int amount = InventoryManager.Instance.GetItemCount(resource);
                if (amount > 0)
                    debugText += $"{resource}: {amount}\n";
            }
        }
        
        debugText += "\n=== STRUCTURES ===\n";
        Structure[] structures = FindObjectsByType<Structure>(FindObjectsSortMode.None);
        debugText += $"Total Structures: {structures.Length}\n";
        
        EnemyUnit[] enemies = FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None);
        debugText += $"Active Enemies: {enemies.Length}\n";
        
        debugText += $"\n=== CHEATS ===\n";
        debugText += $"God Mode: {(godMode ? "ON" : "OFF")}\n";
        debugText += $"Unlimited Building: {(unlimitedBuilding ? "ON" : "OFF")}\n";
        debugText += $"Time Frozen: {(timeFreeze ? "ON" : "OFF")}\n";
        
        debugInfoText.text = debugText;
    }
    
    private void UpdateButtonTexts()
    {
        if (timeButtonText != null)
            timeButtonText.text = timeFreeze ? "Unfreeze Time" : "Freeze Time";
        
        if (godModeText != null)
            godModeText.text = godMode ? "Disable God Mode" : "Enable God Mode";
        
        if (unlimitedBuildingText != null)
            unlimitedBuildingText.text = unlimitedBuilding ? "Disable Unlimited Building" : "Enable Unlimited Building";
    }
    #endregion
}