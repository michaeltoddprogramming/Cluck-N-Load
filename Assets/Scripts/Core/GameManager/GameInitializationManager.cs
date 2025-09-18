using UnityEngine;
using System.Collections;

/// <summary>
/// Ensures proper initialization order of core game systems
/// </summary>
public class GameInitializationManager : MonoBehaviour
{
    [Header("Initialization Order")]
    [SerializeField] private float initializationDelay = 0.5f;
    
    private void Start()
    {
        StartCoroutine(InitializeGameSystems());
    }
    
    private IEnumerator InitializeGameSystems()
    {
        Debug.Log("GameInitializationManager: Starting game systems initialization...");
        
        // Wait a bit for Unity to finish its own initialization
        yield return new WaitForSeconds(initializationDelay);
        
        // Step 1: Ensure MoneyManager is initialized first
        if (MoneyManager.Instance != null)
        {
            Debug.Log("MoneyManager initialized successfully");
        }
        else
        {
            Debug.LogError("MoneyManager failed to initialize!");
        }
        
        yield return new WaitForSeconds(0.1f);
        
        // Step 2: Initialize ShopUIManager 
        if (ShopUIManager.Instance != null)
        {
            Debug.Log("ShopUIManager initialized successfully");
        }
        else
        {
            Debug.LogError("ShopUIManager failed to initialize!");
        }
        
        yield return new WaitForSeconds(0.1f);
        
        // Step 3: Initialize TutorialManager (if needed)
        if (TutorialManager.Instance != null)
        {
            Debug.Log("TutorialManager initialized successfully");
            
            // Force restart tutorial if it's not active
            if (!TutorialManager.Instance.IsTutorialActive())
            {
                Debug.Log("Tutorial not active, restarting...");
                TutorialManager.Instance.StartTutorial();
            }
        }
        else
        {
            Debug.LogError("TutorialManager failed to initialize!");
        }
        
        yield return new WaitForSeconds(0.1f);
        
        // Step 4: CheatManager initializes last (if present)
        if (CheatManager.Instance != null)
        {
            Debug.Log("CheatManager initialized (optional)");
        }
        
        Debug.Log("GameInitializationManager: All systems initialized!");
    }
}