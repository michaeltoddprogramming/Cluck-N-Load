using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameLoopManager : MonoBehaviour
{
    public static GameLoopManager Instance { get; private set; }
    
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private string mainSceneName = "MainScene";
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";
    
    private bool isGameOver = false;
    private List<Structure> activeStructures = new List<Structure>();
    public bool IsGameOver => isGameOver;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DO NOT USE DontDestroyOnLoad - this causes reference issues
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
            
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
    }

    public void RegisterStructure(Structure structure)
    {
        if (!activeStructures.Contains(structure))
        {
            activeStructures.Add(structure);
        }
    }

    public void UnregisterStructure(Structure structure)
    {
        if (activeStructures.Contains(structure))
        {
            activeStructures.Remove(structure);
            
            if (activeStructures.Count == 0 && !isGameOver)
            {
                TriggerGameOver();
            }
        }
    }

    private void TriggerGameOver()
    {
        if (isGameOver) return;
        
        isGameOver = true;
        
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
            
        Debug.Log("Game Over: All structures destroyed!");
    }

    private void RestartGame()
    {
        // Reset instance reference to prevent null issues
        Instance = null;
        
        // Reset time scale
        Time.timeScale = 1f;
        
        // Load the main menu scene IMMEDIATELY
        Debug.Log("Loading main menu scene");
        SceneManager.LoadScene(0);
    }

    // Public method to load any scene
    public void LoadScene(string sceneName)
    {
        // Reset instance reference
        Instance = null;
        
        // Reset time scale
        Time.timeScale = 1f;
        
        // Load the scene
        Debug.Log($"Loading scene: {sceneName}");
        SceneManager.LoadScene(0);
    }
    
    // Load the main scene
    public void LoadMainScene()
    {
        LoadScene(mainSceneName);
    }
    
    // Load the menu scene
    public void LoadMainMenu()
    {
        LoadScene(mainMenuSceneName);
    }
}