// HealthBarFaceCamera.cs
using UnityEngine;

public class HealthBarFaceCamera : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null)
            transform.LookAt(transform.position + Camera.main.transform.forward);
    }
}