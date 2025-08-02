using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the main game loop, game state, and game over conditions.
/// Integrates with your existing GameEventManager system.
/// </summary>
/// 
/// 

public class GameLoopManager : MonoBehaviour
{

    // Helper to check if a structure is already registered
    public bool IsStructureRegistered(Structure structure)
    {
        return allStructures.Contains(structure);
    }

    // Call this from your Game Over panel's Quit button

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


    // Events
    public System.Action OnGameOver;

    // Properties
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

    private void Start()
    {
        // Initialize game state
        isGameOver = false;
        isPaused = false;

        TutorialManager.Instance?.StartTutorial();
        TutorialManager.Instance?.Trigger(TutorialTrigger.GameStarted);
        
        // Subscribe to GameEventManager events if it exists
        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.OnStructurePlaced.AddListener(RegisterStructure);
            GameEventManager.Instance.OnStructureDestroyed.AddListener(UnregisterStructure);
        }
    }

    /// <summary>
    /// Register a structure when it's built
    /// </summary>
    public void RegisterStructure(Structure structure)
    {
        if (structure == null || !structure) return;

        // Clean up null/destroyed entries before adding
        allStructures.RemoveAll(s => s == null || !s);

        // Debug: log if double registration is attempted
        if (allStructures.Contains(structure))
        {
            Debug.LogWarning($"[GameLoopManager] Attempted to register structure twice: {structure.name}", structure);
            return;
        }

        allStructures.Add(structure);
        totalStructuresBuilt++;
        string structureName = structure != null ? structure.name : "(destroyed object)";
        Debug.Log($"Structure registered: {structureName}. Total: {allStructures.Count}");
        // Fire event through GameEventManager if available
        GameEventManager.Instance?.OnStructurePlaced?.Invoke(structure);
    }

    /// <summary>
    /// Unregister a structure when it's destroyed
    /// </summary>
    public void UnregisterStructure(Structure structure)
    {
        if (structure == null || !structure) return;

        // Clean up null/destroyed entries before removing
        allStructures.RemoveAll(s => s == null || !s);

        if (allStructures.Contains(structure))
        {
            string structureName = structure != null ? structure.name : "(destroyed object)";
            allStructures.Remove(structure);
            Debug.Log($"Structure unregistered: {structureName}. Remaining: {allStructures.Count}");
            // Fire event through GameEventManager if available
            GameEventManager.Instance?.OnStructureDestroyed?.Invoke(structure);
            // Check game over conditions
            CheckGameOverConditions();
        }
    }

   private void CheckGameOverConditions()
{
    if (isGameOver) return;

    bool shouldGameOver = false;

    // Clean up null/destroyed entries before checking
    allStructures.RemoveAll(s => s == null || !s);

    // Only trigger game over if ALL structures are destroyed
    if (checkAllStructuresDestroyed && allStructures.Count == 0 && totalStructuresBuilt > 0)
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
        
        OnGameOver?.Invoke();
        
        // Use your existing GameEventManager
        GameEventManager.Instance?.OnGamePaused?.Invoke();

        // Show Game Over UI if assigned
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
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
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.OnStructurePlaced.RemoveListener(RegisterStructure);
            GameEventManager.Instance.OnStructureDestroyed.RemoveListener(UnregisterStructure);
        }
    }

    // Debug methods for testing
    [ContextMenu("Trigger Game Over")]
    public void Debug_TriggerGameOver() => TriggerGameOver();

    [ContextMenu("Print Structure Count")]
    public void Debug_PrintStructureCount()
    {
        Debug.Log($"Active Structures: {allStructures.Count}, Total Built: {totalStructuresBuilt}");
    }
}
