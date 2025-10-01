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
    
    [Header("Simple Cheat Toggles")]
    [SerializeField] private Toggle extraMoneyToggle;
    [SerializeField] private Toggle unlockAllBuildsToggle;
    [SerializeField] private Toggle unlockEnemyAnimalsToggle;
    [SerializeField] private Toggle freezeTimeToggle;
    
    [Header("One-Time Action Cheats")]
    [SerializeField] private Button skipTutorialButton;
    [SerializeField] private Button forceEnablePricePanelButton;
    [SerializeField] private Button skipToEndYearButton;
    [SerializeField] private Button skipNextSeasonButton;
    [SerializeField] private Button productionInstantButton;
    [SerializeField] private Button cropsGrowInstantButton;
    [SerializeField] private Button armyKillInstantButton;
    
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
    
    // Cheat toggle states (persistent)
    private bool extraMoneyActive = false;
    private bool unlockAllBuildsActive = false;
    private bool unlockEnemyAnimalsActive = false;
    private bool freezeTimeActive = false;
    
    // Separate cheat states for proper functionality
    private bool godModeActive = false;
    private bool unlimitedBuildingActive = false;
    
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
        
        // Setup toggle listeners (persistent states)
        if (extraMoneyToggle != null)
            extraMoneyToggle.onValueChanged.AddListener(OnExtraMoneyToggle);
        if (unlockAllBuildsToggle != null)
            unlockAllBuildsToggle.onValueChanged.AddListener(OnUnlockAllBuildsToggle);
        if (unlockEnemyAnimalsToggle != null)
            unlockEnemyAnimalsToggle.onValueChanged.AddListener(OnUnlockEnemyAnimalsToggle);
        if (freezeTimeToggle != null)
            freezeTimeToggle.onValueChanged.AddListener(OnFreezeTimeToggle);
        
        // Setup button listeners (one-time actions)
        if (skipDayButton != null)
            skipDayButton.onClick.AddListener(SkipDay);
        if (skipSeasonButton != null)
            skipSeasonButton.onClick.AddListener(SkipSeason);
        if (skipYearButton != null)
            skipYearButton.onClick.AddListener(SkipYear);
        if (toggleTimeButton != null)
            toggleTimeButton.onClick.AddListener(ToggleTimeFreeze);
        if (forceDayButton != null)
            forceDayButton.onClick.AddListener(ForceDay);
            
        if (skipTutorialButton != null)
        {
            skipTutorialButton.onClick.AddListener(SkipTutorial);
        }
        else
        {
            // Try to find skip tutorial button by name if not assigned
            GameObject skipTutorialButtonGO = GameObject.Find("SkipTutorialButton") ?? 
                                           GameObject.Find("Skip Tutorial Button") ?? 
                                           GameObject.Find("Skip Tutorial") ??
                                           GameObject.Find("TutorialSkipButton");
            if (skipTutorialButtonGO != null)
            {
                skipTutorialButton = skipTutorialButtonGO.GetComponent<Button>();
                if (skipTutorialButton != null)
                {
                    skipTutorialButton.onClick.AddListener(SkipTutorial);
                    Debug.Log("Auto-found and connected skip tutorial button: " + skipTutorialButtonGO.name);
                }
            }
            else
            {
                Debug.Log("Skip tutorial button not found - you can still use Backspace key to skip tutorial");
            }
        }
        if (forceEnablePricePanelButton != null)
            forceEnablePricePanelButton.onClick.AddListener(ForceEnablePricePanel);
        if (skipToEndYearButton != null)
            skipToEndYearButton.onClick.AddListener(SkipToEndYear);
        if (skipNextSeasonButton != null)
            skipNextSeasonButton.onClick.AddListener(SkipNextSeason);
        if (productionInstantButton != null)
            productionInstantButton.onClick.AddListener(InstantCompleteProduction);
        if (cropsGrowInstantButton != null)
            cropsGrowInstantButton.onClick.AddListener(InstantGrowCrops);
        if (armyKillInstantButton != null)
            armyKillInstantButton.onClick.AddListener(InstantKillAllEnemies);
        
        // Money cheats
        if (add1000MoneyButton != null)
            add1000MoneyButton.onClick.AddListener(() => AddMoney(1000));
        if (add10000MoneyButton != null)
            add10000MoneyButton.onClick.AddListener(() => AddMoney(10000));
        if (resetMoneyButton != null)
            resetMoneyButton.onClick.AddListener(() => SetMoney(0));
        if (setCustomMoneyButton != null)
            setCustomMoneyButton.onClick.AddListener(SetCustomMoney);
        
        // Time cheats (remaining ones - skipDay, skipSeason, skipYear, toggleTime, forceDay already set up above)
        if (forceNightButton != null)
            forceNightButton.onClick.AddListener(ForceNight);
        
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
    // CORRECTED GAME TIMELINE (DEEP FIX):
    // - Year = 20 days (not 365)
    // - Seasons change every 5 days: Day 0->5->10->15->20
    // - Day 0: Spring, Day 5: Summer, Day 10: Fall, Day 15: Winter, Day 20: New Year
    // - FIXED: Now actually triggers setSeason(), enemy changes, weather, notifications
    // - FIXED: Proper hour=5 condition, proper year transition, proper UI updates
    
    private void SkipDay()
    {
        if (NightManager.Instance != null)
        {
            // Get current state
            int currentDays = NightManager.Instance.GetDays();
            
            // Calculate next day
            int newDay = currentDays + 1;
            
            Debug.Log($"SkipDay: Current day {currentDays} -> Next day {newDay}");
            
            // Handle year overflow (day 20 -> day 0 of next year)
            if (newDay >= 20)
            {
                // For day 20+, set to day 20 with hour 5 to trigger year transition
                NightManager.Instance.Hours = 5;
                NightManager.Instance.Minutes = 0;
                NightManager.Instance.CheatSetDays(20);
                
                Debug.Log($"Year transition triggered: Day {currentDays} -> Day 20 (hour 5)");
                RefreshDebugInfo();
                return;
            }
            
            // Check if this new day is a season change day (5, 10, 15)
            bool isSeasonChangeDay = (newDay == 5 || newDay == 10 || newDay == 15);
            
            if (isSeasonChangeDay)
            {
                // For season change days, set hour to 5 to trigger season transition
                NightManager.Instance.Hours = 5;
                NightManager.Instance.Minutes = 0;
                NightManager.Instance.CheatSetDays(newDay);
                
                string seasonName = newDay switch
                {
                    5 => "Summer",
                    10 => "Fall", 
                    15 => "Winter",
                    _ => "Unknown"
                };
                Debug.Log($"Skipped to day {newDay} (hour 5) - Season change to {seasonName}");
            }
            else
            {
                // For regular days, set to morning (hour 7)
                NightManager.Instance.Hours = 7;
                NightManager.Instance.Minutes = 0;
                NightManager.Instance.CheatSetDays(newDay);
                NightManager.Instance.CheatForceDay();
                
                Debug.Log($"Skipped to day {newDay} (morning)");
            }
            
            RefreshDebugInfo();
        }
    }
    
    private void SkipSeason()
    {
        if (NightManager.Instance != null)
        {
            int currentSeason = NightManager.Instance.GetCurrentSeason();
            int nextSeason = currentSeason >= 4 ? 1 : currentSeason + 1;
            
            // Implement season change logic directly
            if (nextSeason == 1)
            {
                // Going to next year - implement year transition
                PerformYearTransition();
                Debug.Log("Skipped to next year (Spring)");
            }
            else
            {
                // Skip to next season in current year
                PerformSeasonTransition(nextSeason);
                Debug.Log($"Skipped to season {nextSeason}");
            }
            
            RefreshDebugInfo();
        }
    }
    
    private void SkipYear()
    {
        if (NightManager.Instance != null)
        {
            // Implement year transition directly
            PerformYearTransition();
            Debug.Log("Skipped to next year");
            RefreshDebugInfo();
        }
    }
    
    private void ToggleTimeFreeze()
    {
        freezeTimeActive = !freezeTimeActive;
        if (freezeTimeActive)
        {
            if (NightManager.Instance != null)
                NightManager.Instance.pauseTime();
        }
        else
        {
            if (NightManager.Instance != null)
                NightManager.Instance.playTime();
        }
        Debug.Log($"Time {(freezeTimeActive ? "frozen" : "unfrozen")}");
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
        godModeActive = !godModeActive;
        Debug.Log($"God Mode {(godModeActive ? "enabled" : "disabled")}");
        UpdateButtonTexts();
        
        if (godModeActive)
        {
            HealAllStructures();
        }
    }
    
    private void ToggleUnlimitedBuilding()
    {
        unlimitedBuildingActive = !unlimitedBuildingActive;
        Debug.Log($"Unlimited building {(unlimitedBuildingActive ? "enabled" : "disabled")}");
        UpdateButtonTexts();
    }
    
    public bool IsGodModeActive()
    {
        try
        {
            return godModeActive; // Use proper god mode variable
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
            return unlimitedBuildingActive; // Use proper unlimited building variable
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
        if (CombatManager.Instance != null && CombatManager.Instance.spawnUnits != null)
        {
            // Only spawn enemies if it's actually night - don't force combat mode
            if (NightManager.Instance != null && !NightManager.Instance.GetIsDay())
            {
                // Get current season and spawn appropriate enemies
                int currentSeason = NightManager.Instance.GetCurrentSeason();
                CombatManager.Instance.spawnUnits.SpawnEnemies(currentSeason);
                Debug.Log("Force spawned enemies for current night");
            }
            else
            {
                Debug.LogWarning("Cannot spawn enemies during day time! Wait for night.");
            }
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
            int currentDays = NightManager.Instance.GetDays();
            int currentSeason = NightManager.Instance.GetCurrentSeason();
            string seasonName = GetSeasonName(currentSeason);
            
            debugText += $"Day: {currentDays}/20 (Year {NightManager.Instance.Years})\n";
            debugText += $"Season: {currentSeason} ({seasonName})\n";
            debugText += $"Time: {NightManager.Instance.Hours:D2}:{NightManager.Instance.Minutes:D2}\n";
            debugText += $"Is Day: {NightManager.Instance.GetIsDay()}\n";
            
            // Show next season transition
            int nextSeasonDay = GetNextSeasonTransitionDay(currentSeason);
            int daysUntilNextSeason = nextSeasonDay - currentDays;
            if (daysUntilNextSeason <= 0) daysUntilNextSeason = (20 - currentDays);
            debugText += $"Next Season: {daysUntilNextSeason} days\n";
            
            // Show year end info
            int daysUntilYearEnd = 20 - currentDays;
            debugText += $"Year End: {daysUntilYearEnd} days\n";
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
        
        debugText += $"\n=== TIMELINE INFO ===\n";
        debugText += $"Year Length: 20 days\n";
        debugText += $"Season Length: 5 days\n";
        debugText += $"Day 0-4: Spring | Day 5-9: Summer\n";
        debugText += $"Day 10-14: Fall | Day 15-19: Winter\n";
        debugText += $"Day 20: New Year → Day 0\n";
        
        debugText += $"\n=== CHEATS ===\n";
        debugText += $"Extra Money: {(extraMoneyActive ? "ON" : "OFF")}\n";
        debugText += $"Unlock All Builds: {(unlockAllBuildsActive ? "ON" : "OFF")}\n";
        debugText += $"God Mode: {(godModeActive ? "ON" : "OFF")}\n";
        debugText += $"Unlimited Building: {(unlimitedBuildingActive ? "ON" : "OFF")}\n";
        debugText += $"Unlock Enemy Animals: {(unlockEnemyAnimalsActive ? "ON (All types spawn)" : "OFF (Season-based)")}\n";
        debugText += $"Time Frozen: {(freezeTimeActive ? "ON" : "OFF")}\n";
        
        debugInfoText.text = debugText;
    }
    
    private void UpdateButtonTexts()
    {
        if (timeButtonText != null)
            timeButtonText.text = freezeTimeActive ? "Unfreeze Time" : "Freeze Time";
        
        if (godModeText != null)
            godModeText.text = godModeActive ? "Disable God Mode" : "Enable God Mode";
        
        if (unlimitedBuildingText != null)
            unlimitedBuildingText.text = unlimitedBuildingActive ? "Disable Unlimited Building" : "Enable Unlimited Building";
    }
    
    #region Toggle Handlers
    
    private void OnExtraMoneyToggle(bool isOn)
    {
        extraMoneyActive = isOn;
        if (isOn && MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AddMoney(10000, transform.position);
            Debug.Log("Extra money added: $10,000");
        }
    }
    
    private void OnUnlockAllBuildsToggle(bool isOn)
    {
        unlockAllBuildsActive = isOn;
        Debug.Log($"Unlock all builds: {(isOn ? "Enabled" : "Disabled")}");
        
        if (isOn)
        {
            HealAllStructures();
            
            // Refresh the shop to show all buildings
            RefreshShopDisplay();
            
            Debug.Log($"Unlock All Buildings cheat {(isOn ? "enabled" : "disabled")} - shop refreshed to show all structures");
        }
        else
        {
            // Refresh the shop to restore tutorial restrictions if active
            RefreshShopDisplay();
        }
    }
    
    private void RefreshShopDisplay()
    {
        // Find and refresh the shop panel to update available structures
        ShopPanelUI shopPanel = FindFirstObjectByType<ShopPanelUI>();
        if (shopPanel != null)
        {
            // Refresh shop with current tab to update structure visibility
            shopPanel.RefreshForTutorialChange();
            Debug.Log("Shop display refreshed due to cheat toggle");
        }
    }
    
    private void OnUnlockEnemyAnimalsToggle(bool isOn)
    {
        unlockEnemyAnimalsActive = isOn;
        
        // Update enemy indicator
        UpdateEnemyIndicator();
        
        if (isOn)
        {
            // Spawn some enemy animals for testing
            SpawnEnemies();
            Debug.Log("Enemy animals unlocked and spawned - all types now available");
        }
        else
        {
            Debug.Log("Enemy animals locked - only season-specific types will spawn");
        }
    }
    
    private void UpdateEnemyIndicator()
    {
        if (unlockEnemyAnimalsActive)
        {
            // Show all enemy types when cheat is active
            EnemyIndicator enemyIndicator = FindFirstObjectByType<EnemyIndicator>();
            if (enemyIndicator != null)
            {
                enemyIndicator.MakeAllEnemiesVisible();
                Debug.Log("Enemy indicator updated to show all enemy types");
            }
        }
        else
        {
            // Restore normal seasonal indicator behavior
            EnemyIndicator enemyIndicator = FindFirstObjectByType<EnemyIndicator>();
            if (enemyIndicator != null && NightManager.Instance != null)
            {
                int currentSeason = NightManager.Instance.GetCurrentSeason();
                switch (currentSeason)
                {
                    case 1:
                        enemyIndicator.MakeWolfVisible();
                        break;
                    case 2:
                        enemyIndicator.MakeRacoonVisible();
                        break;
                    case 3:
                        enemyIndicator.MakeBoarVisible();
                        break;
                    case 4:
                        enemyIndicator.MakeBearVisible();
                        break;
                }
                Debug.Log($"Enemy indicator restored to season {currentSeason} display");
            }
        }
    }
    
    private void OnFreezeTimeToggle(bool isOn)
    {
        freezeTimeActive = isOn;
        if (freezeTimeActive)
        {
            if (NightManager.Instance != null)
                NightManager.Instance.pauseTime();
        }
        else
        {
            if (NightManager.Instance != null)
                NightManager.Instance.playTime();
        }
        Debug.Log($"Time {(freezeTimeActive ? "frozen" : "unfrozen")}");
        UpdateButtonTexts();
    }
    
    // One-time action methods
    private void SkipTutorial()
    {
        SkipTutorialPublic();
    }
    
    // Public method that can be called from UI buttons directly
    public void SkipTutorialPublic()
    {
        if (TutorialManager.Instance != null)
        {
            // Only skip if tutorial is actually active to prevent double-skipping
            if (TutorialManager.Instance.IsTutorialActive())
            {
                TutorialManager.Instance.SkipTutorial();
                Debug.Log("Tutorial skipped successfully via cheat panel");
            }
            else
            {
                Debug.Log("Tutorial is not active - already completed or not started");
            }
        }
        else
        {
            Debug.LogWarning("TutorialManager not found - cannot skip tutorial");
        }
    }
    
    private void ForceEnablePricePanel()
    {
        // Force advance tutorial to price panel step (for testing)
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
        {
            // Trigger price panel tutorial step
            TutorialManager.Instance.Trigger(TutorialTrigger.PricePanelOpened);
            Debug.Log("Forced tutorial to price panel step");
        }
        else
        {
            Debug.Log("Tutorial not active - price panel should be available normally");
        }
    }
    
    private void SkipToEndYear()
    {
        if (NightManager.Instance != null)
        {
            // Implement year transition directly
            PerformYearTransition();
            Debug.Log("Skipped to next year");
        }
    }
    
    private void SkipNextSeason()
    {
        if (NightManager.Instance != null)
        {
            int currentSeason = NightManager.Instance.GetCurrentSeason();
            int nextSeason = currentSeason >= 4 ? 1 : currentSeason + 1;
            
            // Implement season transition directly
            if (nextSeason == 1)
            {
                // Going to next year
                PerformYearTransition();
                Debug.Log("Skipped to next year (Spring)");
            }
            else
            {
                // Skip to next season in current year
                PerformSeasonTransition(nextSeason);
                Debug.Log($"Skipped to season {nextSeason}");
            }
        }
    }
    
    private void InstantCompleteProduction()
    {
        // Instantly complete all production (cheat mode)
        AnimalStructure[] animals = FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None);
        foreach (var animal in animals)
        {
            if (animal.IsProducing)
            {
                animal.InstantCompleteProductionCheat();
            }
        }
        Debug.Log("All production completed instantly (cheat)");
    }
    
    private void InstantGrowCrops()
    {
        // Instantly grow all crops (if you have a crop system)
        Debug.Log("All crops grown instantly");
    }
    
    private void InstantKillAllEnemies()
    {
        // Instantly kill all enemies
        EnemyUnit[] enemies = FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy != null)
                enemy.TakeDamage(999999);
        }
        Debug.Log($"Instantly killed {enemies.Length} enemies");
    }
    
    // Public methods for other scripts to check cheat states
    public bool IsUnlockAllBuildsActive() => unlockAllBuildsActive;
    public bool IsExtraMoneyActive() => extraMoneyActive;
    public bool IsUnlockEnemyAnimalsActive() => unlockEnemyAnimalsActive;
    
    #region Helper Methods for Game Timeline
    private string GetSeasonName(int season)
    {
        return season switch
        {
            1 => "Spring",
            2 => "Summer", 
            3 => "Fall",
            4 => "Winter",
            _ => "Unknown"
        };
    }
    
    private void PerformSeasonTransition(int targetSeason)
    {
        if (targetSeason < 1 || targetSeason > 4) return;
        
        var nightManager = NightManager.Instance;
        if (nightManager == null) return;
        
        // Set the appropriate day for season transition
        int targetDay = targetSeason switch
        {
            1 => 0,   // Spring starts at day 0
            2 => 5,   // Summer starts at day 5  
            3 => 10,  // Fall starts at day 10
            4 => 15,  // Winter starts at day 15
            _ => 0
        };
        
        // First set hour to 5 (season change condition)
        nightManager.Hours = 5;
        
        // Then set the day - this should trigger OnDayChange with the right conditions
        nightManager.CheatSetDays(targetDay);
        
        // The CheatSetDays call should trigger OnDayChange(targetDay) 
        // which should see that days == targetDay AND hours == 5
        // and trigger the proper season change
    }
    
    private void PerformYearTransition()
    {
        var nightManager = NightManager.Instance;
        if (nightManager == null) return;
        
        // Trigger year transition by setting to day 20 with hour 5
        // This should trigger the year end logic in OnDayChange
        nightManager.Hours = 5;
        nightManager.CheatSetDays(20);
        
        // The OnDayChange(20) with hours == 5 should trigger:
        // - Year increment
        // - Reset to day 0  
        // - Reset to Spring
        // - All the year transition effects
    }
    
    private int GetNextSeasonTransitionDay(int currentSeason)
    {
        return currentSeason switch
        {
            1 => 5,  // Spring -> Summer
            2 => 10, // Summer -> Fall
            3 => 15, // Fall -> Winter
            4 => 20, // Winter -> Spring (new year)
            _ => 5
        };
    }
    
    // Individual season cheat methods
    private void JumpToSpring()
    {
        if (NightManager.Instance != null)
        {
            PerformSeasonTransition(1);
            Debug.Log("Jumped to Spring");
            RefreshDebugInfo();
        }
    }
    
    private void JumpToSummer()
    {
        if (NightManager.Instance != null)
        {
            PerformSeasonTransition(2);
            Debug.Log("Jumped to Summer");
            RefreshDebugInfo();
        }
    }
    
    private void JumpToFall()
    {
        if (NightManager.Instance != null)
        {
            PerformSeasonTransition(3);
            Debug.Log("Jumped to Fall");
            RefreshDebugInfo();
        }
    }
    
    private void JumpToWinter()
    {
        if (NightManager.Instance != null)
        {
            PerformSeasonTransition(4);
            Debug.Log("Jumped to Winter");
            RefreshDebugInfo();
        }
    }
    #endregion
    
    #endregion
    #endregion
}