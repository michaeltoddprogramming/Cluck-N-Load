using UnityEngine;

/// <summary>
/// Debug component to track what's changing transform values
/// Attach to objects that keep changing size unexpectedly
/// </summary>
public class TransformDebugger : MonoBehaviour
{
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 lastScale;
    
    private void Start()
    {
        lastPosition = transform.localPosition;
        lastRotation = transform.localRotation;
        lastScale = transform.localScale;
    }
    
    private void Update()
    {
        if (transform.localPosition != lastPosition)
        {
            Debug.Log($"{gameObject.name} position changed from {lastPosition} to {transform.localPosition}");
            lastPosition = transform.localPosition;
        }
        
        if (transform.localRotation != lastRotation)
        {
            Debug.Log($"{gameObject.name} rotation changed from {lastRotation} to {transform.localRotation}");
            lastRotation = transform.localRotation;
        }
        
        if (transform.localScale != lastScale)
        {
            Debug.Log($"{gameObject.name} scale changed from {lastScale} to {transform.localScale}");
            lastScale = transform.localScale;
        }
    }
}