using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameLoopManager : MonoBehaviour
{
    public static GameLoopManager Instance { get; private set; }
    
    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button quitGameButton;
    [SerializeField] private Text gameOverText;
    [SerializeField] private float gameOverDelay = 2.5f; 
    
    [Header("Scene Management")]
    
    private bool isGameOver = false;
    private List<Structure> activeStructures = new List<Structure>();
    
    public bool IsGameOver => isGameOver;

    private void Awake()
    {
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
        // Ensure game over panel starts hidden
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        else
            Debug.LogError("Game Over Panel is not assigned! Game over screen won't appear.", this);
            
        // Setup quit button if it exists
        if (quitGameButton != null)
            quitGameButton.onClick.AddListener(QuitGame);
            
        // Debug logging
        Debug.Log("GameLoopManager initialized. Game over panel: " +
                 (gameOverPanel != null ? "Found" : "MISSING") +
                 ", Quit button: " + (quitGameButton != null ? "Found" : "MISSING"));
    }

    public void RegisterStructure(Structure structure)
    {
        // Don't register null or indestructible structures
        if (structure == null || structure.isIndestructible)
            return;
            
        if (!activeStructures.Contains(structure))
        {
            activeStructures.Add(structure);
            Debug.Log($"Registered structure {structure.name}, total: {activeStructures.Count}");
        }
    }

    public void UnregisterStructure(Structure structure)
    {
        if (structure == null) return;
        
        if (activeStructures.Contains(structure))
        {
            activeStructures.Remove(structure);
            Debug.Log($"Structure destroyed: {structure.name}, remaining: {activeStructures.Count}");
            
            // Force immediate check for game over
            CheckGameOverCondition();
        }
    }
    
        // New method - explicitly check game over condition
        public void CheckGameOverCondition()
        {
            // Remove any null entries that might have been left behind
            activeStructures.RemoveAll(s => s == null);
            
            Debug.Log($"Checking game over condition. Active structures: {activeStructures.Count}");
            
            if (activeStructures.Count == 0 && !isGameOver)
            {
                Debug.Log("GAME OVER CONDITION MET! No structures remaining.");
                TriggerGameOver();
            }
        }
    
        private void TriggerGameOver()
        {
            if (isGameOver) return;
            
            Debug.Log($"TRIGGERING GAME OVER! (with {gameOverDelay} second delay)");
            isGameOver = true;
            
            // Start the delayed game over sequence
            StartCoroutine(DelayedGameOver());
        }
        
        // New coroutine to handle delayed game over
        private IEnumerator DelayedGameOver()
        {
            // Wait for specified delay time
            yield return new WaitForSeconds(gameOverDelay);
            
            Debug.Log("Showing game over UI after delay");
            
            // Show game over UI with forced activation
            if (gameOverPanel != null)
            {
                // Make sure it's active and visible in the hierarchy
                Transform parent = gameOverPanel.transform.parent;
                while (parent != null)
                {
                    parent.gameObject.SetActive(true);
                    parent = parent.parent;
                }
                
                // Show the panel
                gameOverPanel.SetActive(true);
                
                // Display game over text if available
                if (gameOverText != null)
                    gameOverText.text = "GAME OVER\nAll structures destroyed!";
                    
                Debug.Log("Game over panel activated");
            }
            else
            {
                Debug.LogError("Game over panel is missing! Cannot show game over UI.");
            }
            
            // Optional: Pause the game after showing the UI
            Time.timeScale = 0f;
        }

    
    public void QuitGame()
    {
        Debug.Log("Quitting game");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }


    public void ForceGameOver()
    {
        activeStructures.Clear();
        TriggerGameOver();
    }
}