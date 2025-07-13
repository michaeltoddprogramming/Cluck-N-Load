using UnityEngine;

/// <summary>
/// Quick verification script to test tutorial system integration
/// Add this to a test object in your scene to verify the tutorial system works
/// </summary>
public class TutorialTestHelper : MonoBehaviour
{
    [Header("Tutorial Testing")]
    [SerializeField] private bool enableTestKeys = true;
    
    private void Update()
    {
        if (!enableTestKeys) return;
        
        // Test tutorial system with keyboard shortcuts (for development only)
        if (Input.GetKeyDown(KeyCode.T))
        {
            // Start tutorial
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.StartTutorial();
                Debug.Log("Tutorial started manually");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Reset tutorial progress
            if (FeatureUnlockManager.Instance != null)
            {
                FeatureUnlockManager.Instance.ResetProgress();
                Debug.Log("Tutorial progress reset");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // Test camera moved condition
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.OnConditionMet(TutorialCondition.CameraMoved);
                Debug.Log("Camera moved condition triggered");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // Test shop opened condition
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.OnConditionMet(TutorialCondition.ShopOpened);
                Debug.Log("Shop opened condition triggered");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // Test structure placed condition
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.OnConditionMet(TutorialCondition.FirstStructurePlaced);
                Debug.Log("First structure placed condition triggered");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            // Test night started condition
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.OnConditionMet(TutorialCondition.NightStarted);
                Debug.Log("Night started condition triggered");
            }
        }
    }
    
    private void OnGUI()
    {
        if (!enableTestKeys) return;
        
        GUI.Label(new Rect(10, 10, 300, 20), "Tutorial Test Keys:");
        GUI.Label(new Rect(10, 30, 300, 20), "T - Start Tutorial");
        GUI.Label(new Rect(10, 50, 300, 20), "R - Reset Progress");
        GUI.Label(new Rect(10, 70, 300, 20), "1 - Camera Moved");
        GUI.Label(new Rect(10, 90, 300, 20), "2 - Shop Opened");
        GUI.Label(new Rect(10, 110, 300, 20), "3 - Structure Placed");
        GUI.Label(new Rect(10, 130, 300, 20), "4 - Night Started");
        
        if (TutorialManager.Instance != null)
        {
            GUI.Label(new Rect(10, 160, 300, 20), $"Tutorial Active: {TutorialManager.Instance.IsActive}");
        }
    }
}
