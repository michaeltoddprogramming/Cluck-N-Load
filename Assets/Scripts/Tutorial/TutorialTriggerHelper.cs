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
    
    // Camera movement triggers - updated for new tutorial steps
    public static void TriggerCameraMovedWASD()
    {
        if (SimplifiedTutorialManager.Instance != null)
        {
            SimplifiedTutorialManager.Instance.OnCameraMovedWASD();
        }
    }
    
    public static void TriggerCameraRotated()
    {
        if (SimplifiedTutorialManager.Instance != null)
        {
            SimplifiedTutorialManager.Instance.OnCameraRotated();
        }
    }
    
    public static void TriggerCameraZoomed()
    {
        if (SimplifiedTutorialManager.Instance != null)
        {
            SimplifiedTutorialManager.Instance.OnCameraZoomed();
        }
    }
    
    public static void TriggerCameraDragged()
    {
        if (SimplifiedTutorialManager.Instance != null)
        {
            SimplifiedTutorialManager.Instance.OnCameraDragged();
        }
    }
}

/// <summary>
/// Add this to your camera controller to detect WASD movement
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
            TutorialTriggerHelper.TriggerCameraMovedWASD();
        }
        
        lastPosition = transform.position;
    }
}