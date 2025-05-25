using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            Debug.LogError("No main camera found for LookAtCamera");
    }
    
    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // Always face the camera
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                            mainCamera.transform.rotation * Vector3.up);
        }
    }
}