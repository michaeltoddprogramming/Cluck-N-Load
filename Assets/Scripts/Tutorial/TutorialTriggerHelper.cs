using UnityEngine;

/// <summary>
/// Simple integration helper to connect existing game events to simplified tutorial
/// Add this to existing managers to trigger tutorial progression
/// </summary>
public class TutorialTriggerHelper : MonoBehaviour
{
    // Call these methods from your existing systems
    
    public static void TriggerShopOpened()
    {
        if (SimplifiedTutorialManager.Instance != null)
        {
            SimplifiedTutorialManager.Instance.OnShopOpened();
        }
    }
    
    public static void TriggerFarmhouseBuilt()
    {
        if (SimplifiedTutorialManager.Instance != null)
        {
            SimplifiedTutorialManager.Instance.OnFarmhouseBuilt();
        }
    }
    
    public static void TriggerCameraMoved()
    {
        if (SimplifiedTutorialManager.Instance != null)
        {
            SimplifiedTutorialManager.Instance.OnCameraMoved();
        }
    }
}

/// <summary>
/// Add this to your camera controller to detect movement
/// </summary>
public class CameraMovementDetector : MonoBehaviour
{
    private Vector3 lastPosition;
    private bool hasMovedThisSession = false;
    
    private void Start()
    {
        lastPosition = transform.position;
    }
    
    private void Update()
    {
        if (!hasMovedThisSession && Vector3.Distance(transform.position, lastPosition) > 0.1f)
        {
            hasMovedThisSession = true;
            TutorialTriggerHelper.TriggerCameraMoved();
        }
        
        lastPosition = transform.position;
    }
}