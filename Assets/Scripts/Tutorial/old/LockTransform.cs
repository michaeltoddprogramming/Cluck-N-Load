using UnityEngine;

/// <summary>
/// Prevents transform changes during runtime
/// Useful for maintaining UI element sizes despite canvas scaling
/// </summary>
public class LockTransform : MonoBehaviour
{
    [Header("Lock Settings")]
    public bool lockPosition = false;
    public bool lockRotation = false;
    public bool lockScale = true;
    
    private Vector3 lockedPosition;
    private Quaternion lockedRotation;
    private Vector3 lockedScale;
    
    private void Start()
    {
        // Store initial transform values
        lockedPosition = transform.localPosition;
        lockedRotation = transform.localRotation;
        lockedScale = transform.localScale;
    }
    
    private void LateUpdate()
    {
        // Restore locked values if they've changed
        if (lockPosition && transform.localPosition != lockedPosition)
        {
            transform.localPosition = lockedPosition;
        }
        
        if (lockRotation && transform.localRotation != lockedRotation)
        {
            transform.localRotation = lockedRotation;
        }
        
        if (lockScale && transform.localScale != lockedScale)
        {
            transform.localScale = lockedScale;
        }
    }
    
    /// <summary>
    /// Update the locked values (call this when you want to change the "locked" state)
    /// </summary>
    public void UpdateLockedValues()
    {
        lockedPosition = transform.localPosition;
        lockedRotation = transform.localRotation;
        lockedScale = transform.localScale;
    }
}