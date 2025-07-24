using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the main game loop, game state, and game over conditions.
/// Integrates with your existing GameEventManager system.
/// </summary>
public class GameLoopManager : MonoBehaviour
{// Call this from your Game Over panel's Quit button

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
    [SerializeField] private bool checkFarmHouseDestruction = true;
    [SerializeField] private bool checkAllStructuresDestroyed = false;

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
        if (structure == null) return;

        if (!allStructures.Contains(structure))
        {
            allStructures.Add(structure);
            totalStructuresBuilt++;
            
            Debug.Log($"Structure registered: {structure.name}. Total: {allStructures.Count}");
            
            // Fire event through GameEventManager if available
            GameEventManager.Instance?.OnStructurePlaced?.Invoke(structure);
        }
    }

    /// <summary>
    /// Unregister a structure when it's destroyed
    /// </summary>
    public void UnregisterStructure(Structure structure)
    {
        if (structure == null) return;

        if (allStructures.Contains(structure))
        {
            allStructures.Remove(structure);
            
            Debug.Log($"Structure unregistered: {structure.name}. Remaining: {allStructures.Count}");
            
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

        // Check if Farm House was destroyed (main game over condition)
        if (checkFarmHouseDestruction)
        {
            bool hasFarmHouse = false;
            foreach (var structure in allStructures)
            {
                // Check if it's a main building/farm house
                if (structure.name.ToLower().Contains("farmhouse") || 
                    structure.name.ToLower().Contains("farm house") ||
                    structure.name.ToLower().Contains("mainbuilding") ||
                    (structure.structureData != null && structure.structureData.type == StructureType.Building))
                {
                    hasFarmHouse = true;
                    break;
                }
            }
            
            if (!hasFarmHouse && totalStructuresBuilt > 0)
            {
                shouldGameOver = true;
                Debug.Log("Game Over: Farm House destroyed!");
            }
        }

        // Check if all structures destroyed
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
