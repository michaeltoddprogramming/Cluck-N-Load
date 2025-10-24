using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages the main game loop, game state, and game over conditions.
/// Integrates with your existing GameEventManager system.
/// </summary>
/// 
/// 

public class GameLoopManager : MonoBehaviour
{
    public bool IsStructureRegistered(Structure structure)
    {
        return allStructures.Contains(structure);
    }
    public void OnQuitButton()
    {
    #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

    [Header("UI")]
    [SerializeField] private GameObject gameOverPanel;
    public static GameLoopManager Instance { get; private set; }

    [Header("Game State")]
    [SerializeField] private bool isGameOver = false;
    [SerializeField] private bool isPaused = false;

    [Header("Structure Tracking")]
    [SerializeField] private List<Structure> allStructures = new List<Structure>();
    [SerializeField] private int totalStructuresBuilt = 0;

    [Header("Game Over Conditions")]
    [SerializeField] private bool checkFarmHouseDestruction = false;
    [SerializeField] private bool checkAllStructuresDestroyed = true;

    [Header("Game Over")]
    [SerializeField] private AudioClip gameOverSound;
    [SerializeField] private AudioSource gameOverAudioSource;

    [SerializeField] private StructureDatabase structureDatabase;

    public System.Action OnGameOver;
    
    private int previousDay = -1;
    private HashSet<string> announcedUnlockedStructures = new HashSet<string>();
    
    private Coroutine waitForNightManagerCoroutine;

    public bool IsGameOver => isGameOver;
    public bool IsPaused => isPaused;
    public int TotalStructuresBuilt => totalStructuresBuilt;
    public int ActiveStructuresCount => allStructures.Count;

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
        }
    }

    private void CleanNullStructures()
    {
        // Efficiently remove null structures without LINQ
        for (int i = allStructures.Count - 1; i >= 0; i--)
        {
            if (allStructures[i] == null || !allStructures[i])
            {
                allStructures.RemoveAt(i);
            }
        }
    }

    private void Start()
    {
        // Always reset game state when Start is called to ensure clean state for new games
        isGameOver = false;
        isPaused = false;
        checkFarmHouseDestruction = true; // Enable farmhouse destruction check by default
        checkAllStructuresDestroyed = true;
        
        // Don't clear structures here as they might be loaded from save data
        // allStructures.Clear();
        // totalStructuresBuilt = 0;

        // Debug: Check if there's a leftover SelectedSaveSlot key
        if (PlayerPrefs.HasKey("SelectedSaveSlot"))
        {
        }
        else
        {
        }

        TutorialManager.Instance?.StartTutorial();
        TutorialManager.Instance?.Trigger(TutorialTrigger.GameStarted);

        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.OnStructurePlaced.AddListener(RegisterStructure);
            GameEventManager.Instance.OnStructureDestroyed.AddListener(UnregisterStructure);
        }

        if (PlayerPrefs.HasKey("SelectedSaveSlot"))
        {
            int slot = PlayerPrefs.GetInt("SelectedSaveSlot", 0);
            GameSaveData saveData = GameSaveHelper.LoadFromSlot(slot);

            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.ResetMoney();
                if (saveData != null)
                {
                    MoneyManager.Instance.AddMoney(saveData.money - MoneyManager.Instance.GetCurrentMoney());

                    if (TutorialManager.Instance != null)
                    {
                        TutorialManager.Instance.EndTutorial();
                    }
                }
                else
                {
                    // If no save data found, this is effectively a new game, so keep tutorial running
                }
            }

            if (InventoryManager.Instance != null && saveData != null)
            {
                InventoryManager.Instance.AddItem("Sunflower", saveData.sunflowerAmount - InventoryManager.Instance.GetItemCount("Sunflower"));
                InventoryManager.Instance.AddItem("Wheat", saveData.wheatAmount - InventoryManager.Instance.GetItemCount("Wheat"));
                InventoryManager.Instance.AddItem("Carrots", saveData.carrotsAmount - InventoryManager.Instance.GetItemCount("Carrots"));
            }

            if (NightManager.Instance != null && saveData != null)
            {
                NightManager.Instance.Days = saveData.day;
                NightManager.Instance.SetSeason(saveData.season);
                NightManager.Instance.SetLastSurvivalRewardDay(saveData.day); // Prevent duplicate survival rewards

                // Always start in the morning when loading a save
                NightManager.Instance.Hours = 7;
                NightManager.Instance.Minutes = 0;
                
                // Initialize announced structures for loaded game
                InitializeAnnouncedStructures();
            }

            if (saveData != null)
            {
                LoadStructures(saveData.structures);
            }
            else
            {
                Debug.LogWarning("saveData is null in GameLoopManager.Start(), skipping structure load.");
            }

            PlayerPrefs.DeleteKey("SelectedSaveSlot");
        }
        else
        {
            MoneyManager.Instance?.ResetMoney();
            
            // Initialize announced structures for fresh game
            InitializeAnnouncedStructures();
        }



        if (NightManager.Instance != null)
            NightManager.Instance.OnDayChanged += OnDayChanged;
        else
            waitForNightManagerCoroutine = StartCoroutine(WaitForNightManager());
    }

    private IEnumerator WaitForNightManager()
    {
        while (NightManager.Instance == null)
            yield return null;
        NightManager.Instance.OnDayChanged += OnDayChanged;
    }

    private void LoadStructures(List<StructureSaveData> structureSaves)
    {
        foreach (var save in structureSaves)
        {
            StructureData structureData = structureDatabase.GetStructureByName(save.type);
            if (structureData == null)
            {
                Debug.LogWarning($"Structure type {save.type} not found in database.");
                continue;
            }
            GameObject prefab = structureData.prefab;
            if (prefab == null)
            {
                Debug.LogWarning($"Prefab for {save.type} not found.");
                continue;
            }
            GameObject obj = Instantiate(prefab, save.position, save.rotation);
            Structure structure = obj.GetComponent<Structure>();
            if (structure != null)
            {
                structure.structureData = structureData;
                structure.ApplyDamage(structure.GetMaxHealth() - save.health);

                if (structure is AnimalStructure animal)
                {
                    animal.SetAnimalCount(save.animalCount);
                    animal.SetProductionState(save.isProducing, save.productReady, save.productionProgress);
                }
                else if (structure is CropStructure crop)
                {
                    crop.SetCropState(save.cropType, save.isGrowing, save.cropReady);
                }
                else if (structure is BarracksStructure barracks)
                {
                    barracks.ClearBarracksArmy();
                    barracks.SpawnArmyAnimals(save.armyAnimalCount); 
                }

            }
        }
    }

    public void RegisterStructure(Structure structure)
    {
        if (structure == null || !structure) return;

        // Clean null references efficiently without LINQ
        CleanNullStructures();

        if (allStructures.Contains(structure))
        {
            Debug.LogWarning($"[GameLoopManager] Attempted to register structure twice: {structure.name}", structure);
            return;
        }

        allStructures.Add(structure);
        totalStructuresBuilt++;
        string structureName = structure != null ? structure.name : "(destroyed object)";
        GameEventManager.Instance?.OnStructurePlaced?.Invoke(structure);
    }
    public void UnregisterStructure(Structure structure)
    {
        if (structure == null || !structure) return;

        // Clean null references efficiently without LINQ
        CleanNullStructures();

        if (allStructures.Contains(structure))
        {
            string structureName = structure != null ? structure.name : "(destroyed object)";
            allStructures.Remove(structure);
            GameEventManager.Instance?.OnStructureDestroyed?.Invoke(structure);
            CheckGameOverConditions();
        }
    }

private void CheckGameOverConditions()
{
    if (isGameOver) return;

    bool shouldGameOver = false;

    allStructures.RemoveAll(s => s == null || !s);

    // Check if tutorial was skipped by developer using Backspace
    bool devSkippedTutorial = TutorialManager.Instance != null && TutorialManager.Instance.WasTutorialSkippedByDev();

    // End game ONLY if farmhouse is destroyed AND tutorial wasn't dev-skipped
    if (checkFarmHouseDestruction && !devSkippedTutorial)
    {
        bool farmhouseExists = allStructures.Exists(s =>
            s != null &&
            s.GetStructureName().Trim().Equals("Farm House", System.StringComparison.OrdinalIgnoreCase)
        );
        if (!farmhouseExists && totalStructuresBuilt > 0)
        {
            shouldGameOver = true;
            Debug.Log("Game Over: Farmhouse destroyed!");
        }
    }

    // End game if all structures destroyed AND tutorial wasn't dev-skipped
    if (checkAllStructuresDestroyed && !devSkippedTutorial && allStructures.Count == 0 && totalStructuresBuilt > 0)
    {
        shouldGameOver = true;
        Debug.Log("Game Over: All structures destroyed!");
    }

    if (shouldGameOver)
    {
        TriggerGameOver();
    }
}

    public void TriggerGameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        Debug.Log("GAME OVER!");

        // Play game over sound effect
        if (gameOverSound != null)
        {
            if (gameOverAudioSource != null)
                gameOverAudioSource.PlayOneShot(gameOverSound);
            else
                AudioSource.PlayClipAtPoint(gameOverSound, Camera.main.transform.position);
        }

        OnGameOver?.Invoke();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            // Dramatic tween: fade in and scale up
            CanvasGroup cg = gameOverPanel.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = gameOverPanel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            gameOverPanel.transform.localScale = Vector3.one * 0.7f;

            // When enabling the game over panel, set the CanvasGroup to ignore time scale
            cg.blocksRaycasts = true;
            cg.interactable = true;

            LeanTween.scale(gameOverPanel, Vector3.one, 0.7f).setEase(LeanTweenType.easeOutBack);
            LeanTween.alphaCanvas(cg, 1f, 0.7f).setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() =>
                {
                    // Pause the game AFTER the panel is fully visible
                    isPaused = true;
                    Time.timeScale = 0f;
                    GameEventManager.Instance?.OnGamePaused?.Invoke();
                });
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        GameEventManager.Instance?.OnGamePaused?.Invoke();
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        GameEventManager.Instance?.OnGameResumed?.Invoke();
    }

    public void RestartGame()
    {
        isGameOver = false;
        isPaused = false;
        allStructures.Clear();
        totalStructuresBuilt = 0;
        Time.timeScale = 1f;
        
        // Reset game over conditions to their default states
        checkFarmHouseDestruction = true; // Enable farmhouse destruction check for new games
        checkAllStructuresDestroyed = true;
    }
    
    // Method to reset game state when returning to main menu
    public void ResetForNewGame()
    {
        RestartGame();
        
        // Clean up any ongoing coroutines
        if (waitForNightManagerCoroutine != null)
        {
            StopCoroutine(waitForNightManagerCoroutine);
            waitForNightManagerCoroutine = null;
        }
        
        // Hide game over panel if it's showing
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Clean up WaitForNightManager coroutine to prevent memory leaks
        if (waitForNightManagerCoroutine != null)
        {
            StopCoroutine(waitForNightManagerCoroutine);
            waitForNightManagerCoroutine = null;
        }
        
        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.OnStructurePlaced.RemoveListener(RegisterStructure);
            GameEventManager.Instance.OnStructureDestroyed.RemoveListener(UnregisterStructure);
        }
    }

private void OnDisable()
{
    if (NightManager.Instance != null)
        NightManager.Instance.OnDayChanged -= OnDayChanged;
}

public void OnDayChanged(int newDay)
{
    // Update previous day
    previousDay = newDay;
    
    if (ShopPanelUI.Instance != null)
        ShopPanelUI.Instance.PopulateShop();
}

private void CheckForNewlyUnlockedStructures(int newDay)
{
    if (structureDatabase == null || structureDatabase.allStructures == null)
        return;
    foreach (StructureData structure in structureDatabase.allStructures)
    {
        if (structure.unlockDay == newDay && !announcedUnlockedStructures.Contains(structure.structureName))
        {
            
            // Mark this structure as announced
            announcedUnlockedStructures.Add(structure.structureName);
            
            // Show badge notification for newly unlocked structure - more dramatic!
            if (NotificationManager.Instance != null)
            {
                NotificationManager.ShowBadge("New Structure Unlocked!", $"{structure.structureName} is now available!", 1f);
            }
            
            // Also trigger the feature unlocked event
            if (GameEventManager.Instance != null)
            {
                GameEventManager.Instance.TriggerFeatureUnlocked($"Structure: {structure.structureName}");
            }
        }
    }
}

public void CheckForNewlyUnlockedStructuresMorning()
{
    int currentDay = NightManager.Instance != null ? NightManager.Instance.Days : 0;
    CheckForNewlyUnlockedStructures(currentDay);
}

/// <summary>
/// Return the list of structures that unlock on the supplied day and mark them as announced.
/// This does NOT show per-structure badge notifications (useful for consolidating them into a seasonal modal).
/// </summary>
public string[] GetAndMarkNewlyUnlockedStructures(int day)
{
    var list = new System.Collections.Generic.List<string>();
    if (structureDatabase == null || structureDatabase.allStructures == null)
        return list.ToArray();

    foreach (StructureData structure in structureDatabase.allStructures)
    {
        if (structure.unlockDay == day && !announcedUnlockedStructures.Contains(structure.structureName))
        {
            announcedUnlockedStructures.Add(structure.structureName);
            list.Add(structure.structureName);

            // trigger feature unlocked event for analytics/telemetry
            if (GameEventManager.Instance != null)
            {
                GameEventManager.Instance.TriggerFeatureUnlocked($"Structure: {structure.structureName}");
            }
        }
    }

    return list.ToArray();
}

private void InitializeAnnouncedStructures()
{
    if (structureDatabase == null || structureDatabase.allStructures == null)
        return;
    
    int currentDay = NightManager.Instance != null ? NightManager.Instance.Days : 0;
    
    // Mark all structures that should already be unlocked as announced
    foreach (StructureData structure in structureDatabase.allStructures)
    {
        if (structure.unlockDay <= currentDay)
        {
            announcedUnlockedStructures.Add(structure.structureName);
        }
    }
}

    [ContextMenu("Trigger Game Over")]
    public void Debug_TriggerGameOver() => TriggerGameOver();

    [ContextMenu("Print Structure Count")]
    public void Debug_PrintStructureCount()
    {
    }

    public void OnGameOverRestart()
    {
        // Unpause before restarting
        Time.timeScale = 1f;
        isPaused = false;
        isGameOver = false;

        // Clean up persistent objects (UIManager, AudioManager, etc.)
        CleanupPersistentObjects();

        // Use your transition manager if available
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneWithLoading(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }

    // Add this helper method:
    private void CleanupPersistentObjects()
    {
        string[] persistentNames = {
            "OptionsCanvas 1(Clone)",
            "UIManager",
            "AudioManager",
            "GameLoopManager",
            "GameManager",
            "MoneyManager",
            "ShopUIManager",
            "LeanTween",
            "Debug Updater"
        };

        foreach (string objName in persistentNames)
        {
            var obj = GameObject.Find(objName);
            if (obj != null)
            {
                Destroy(obj);
            }
        }
    }

    public void OnGameOverBackToMenu()
    {
        // Unpause before returning to menu
        Time.timeScale = 1f;
        isPaused = false;

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneWithLoading("MainMenuScene");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
        }
    }
}
