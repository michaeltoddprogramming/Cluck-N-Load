using UnityEngine;

/// <summary>
/// Helper script to test unlock notifications manually
/// </summary>
public class TestUnlockHelper : MonoBehaviour
{
    [ContextMenu("Test Unlock Notification")]
    public void TestUnlockNotification()
    {
        Debug.Log("[TestUnlockHelper] Testing unlock notification...");
        NotificationManager.ShowBadge("New Structure Unlocked!", "Test Building is now available!", 3f);
    }

    [ContextMenu("Force Check Unlocks for Current Day")]
    public void ForceCheckUnlocksForCurrentDay()
    {
        if (GameLoopManager.Instance != null && NightManager.Instance != null)
        {
            int currentDay = NightManager.Instance.Days;
            Debug.Log($"[TestUnlockHelper] Forcing unlock check for day {currentDay}");
            GameLoopManager.Instance.CheckForNewlyUnlockedStructuresMorning();
        }
        else
        {
            Debug.LogWarning("[TestUnlockHelper] GameLoopManager or NightManager not available!");
        }
    }

    [ContextMenu("Force Check Unlocks for Day 5")]
    public void ForceCheckUnlocksForDay5()
    {
        ForceCheckUnlocksForSpecificDay(5);
    }

    [ContextMenu("Force Check Unlocks for Day 10")]
    public void ForceCheckUnlocksForDay10()
    {
        ForceCheckUnlocksForSpecificDay(10);
    }

    private void ForceCheckUnlocksForSpecificDay(int day)
    {
        if (NightManager.Instance != null)
        {
            Debug.Log($"[TestUnlockHelper] Setting day to {day} and checking unlocks...");
            NightManager.Instance.CheatSetDays(day);
            
            if (GameLoopManager.Instance != null)
            {
                GameLoopManager.Instance.CheckForNewlyUnlockedStructuresMorning();
            }
        }
        else
        {
            Debug.LogWarning("[TestUnlockHelper] NightManager not available!");
        }
    }
}
