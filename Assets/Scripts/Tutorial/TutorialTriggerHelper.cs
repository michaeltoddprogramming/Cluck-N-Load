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

    // Building triggers
    public static void TriggerCropPlotBuilt()
    {
        if (SimplifiedTutorialManager.Instance != null)
        {
            SimplifiedTutorialManager.Instance.OnCropPlotBuilt();
        }
    }

    public static void TriggerSiloBuilt()
    {
        if (SimplifiedTutorialManager.Instance != null)
        {
            SimplifiedTutorialManager.Instance.OnSiloBuilt();
        }
    }

    public static void TriggerChickenCoopBuilt()
    {
        if (SimplifiedTutorialManager.Instance != null)
        {
            SimplifiedTutorialManager.Instance.OnChickenCoopBuilt();
        }
    }

    public static void TriggerChickenBarracksBuilt()
    {
        if (SimplifiedTutorialManager.Instance != null)
        {
            SimplifiedTutorialManager.Instance.OnChickenBarracksBuilt();
        }
    }

    public static void TriggerWallsBuilt()
    {
        if (SimplifiedTutorialManager.Instance != null)
        {
            SimplifiedTutorialManager.Instance.OnWallsBuilt();
        }
    }

    // Crop action triggers
    public static void TriggerCropPlanted()
    {
        if (SimplifiedTutorialManager.Instance != null)
        {
            SimplifiedTutorialManager.Instance.OnCropPlanted();
        }
    }

    public static void TriggerCropHarvested()
    {
        if (SimplifiedTutorialManager.Instance != null)
        {
            SimplifiedTutorialManager.Instance.OnCropHarvested();
        }
    }

    // Chicken action triggers
    public static void TriggerChickensBought()
    {
        if (SimplifiedTutorialManager.Instance != null)
        {
            SimplifiedTutorialManager.Instance.OnChickensBought();
        }
    }

    public static void TriggerChickensFed()
    {
        if (SimplifiedTutorialManager.Instance != null)
        {
            SimplifiedTutorialManager.Instance.OnChickensFed();
        }
    }

    public static void TriggerEggsCollected()
    {
        if (SimplifiedTutorialManager.Instance != null)
        {
            SimplifiedTutorialManager.Instance.OnEggsCollected();
        }
    }

    // Army/Defense action triggers
    public static void TriggerSoldiersRecruited()
    {
        if (SimplifiedTutorialManager.Instance != null)
        {
            SimplifiedTutorialManager.Instance.OnSoldiersRecruited();
        }
    }

    public static void TriggerFlagPlaced()
    {
        if (SimplifiedTutorialManager.Instance != null)
        {
            SimplifiedTutorialManager.Instance.OnFlagPlaced();
        }
    }

    // Repair action trigger
    public static void TriggerAllBuildingsRepaired()
    {
        if (SimplifiedTutorialManager.Instance != null)
        {
            SimplifiedTutorialManager.Instance.OnAllBuildingsRepaired();
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