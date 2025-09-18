using UnityEngine;

/// <summary>
/// Debug utility to help identify what's broken in the game initialization
/// </summary>
public class GameStartDebugger : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(DebugGameState());
    }
    
    private System.Collections.IEnumerator DebugGameState()
    {
        yield return new WaitForSeconds(2f); // Wait for initialization
        
        Debug.Log("=== GAME STATE DEBUG ===");
        
        // Check MoneyManager
        if (MoneyManager.Instance != null)
        {
            Debug.Log($"MoneyManager: OK - Current Money: {MoneyManager.Instance.GetCurrentMoney()}");
        }
        else
        {
            Debug.LogError("MoneyManager: MISSING!");
        }
        
        // Check TutorialManager
        if (TutorialManager.Instance != null)
        {
            Debug.Log($"TutorialManager: OK - Active: {TutorialManager.Instance.IsTutorialActive()}");
        }
        else
        {
            Debug.LogError("TutorialManager: MISSING!");
        }
        
        // Check ShopUIManager
        if (ShopUIManager.Instance != null)
        {
            Debug.Log($"ShopUIManager: OK - Shop Open: {ShopUIManager.Instance.IsShopOpen()}");
        }
        else
        {
            Debug.LogError("ShopUIManager: MISSING!");
        }
        
        // Check CheatManager
        if (CheatManager.Instance != null)
        {
            Debug.Log($"CheatManager: OK - God Mode: {CheatManager.Instance.IsGodModeActive()}");
        }
        else
        {
            Debug.Log("CheatManager: Not present (this is OK)");
        }
        
        Debug.Log("=== END DEBUG ===");
    }
}