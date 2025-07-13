using UnityEngine;

public class TutorialCameraController : MonoBehaviour
{
    private Vector3 lastCameraPosition;
    private bool hasMovedCamera = false;
    
    private void Start()
    {
        if (Camera.main != null)
        {
            lastCameraPosition = Camera.main.transform.position;
        }
    }
    
    private void Update()
    {
        if (hasMovedCamera || Camera.main == null) return;
        
        // Check if camera has moved significantly
        float distance = Vector3.Distance(Camera.main.transform.position, lastCameraPosition);
        if (distance > 0.5f) // Threshold for "moved"
        {
            hasMovedCamera = true;
            
            // Notify tutorial system
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.OnConditionMet(TutorialCondition.CameraMoved);
            }
        }
    }
}
