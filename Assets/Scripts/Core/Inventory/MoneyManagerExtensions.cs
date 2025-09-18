using UnityEngine;

/// <summary>
/// Extensions for MoneyManager to handle cheat integration safely
/// </summary>
public static class MoneyManagerExtensions
{
    /// <summary>
    /// Safely check if cheats are active without causing errors during initialization
    /// </summary>
    public static bool IsCheatModeActive()
    {
        try
        {
            // Multiple safety checks to prevent initialization issues
            if (CheatManager.Instance == null) return false;
            if (CheatManager.Instance.gameObject == null) return false;
            if (!CheatManager.Instance.gameObject.activeInHierarchy) return false;
            if (!CheatManager.Instance.enabled) return false;
            
            return CheatManager.Instance.IsGodModeActive() || CheatManager.Instance.IsUnlimitedBuildingActive();
        }
        catch (System.Exception ex)
        {
            // Log the exception for debugging but don't break the game
            Debug.Log($"CheatManager check failed (this is safe): {ex.Message}");
            return false;
        }
    }
}