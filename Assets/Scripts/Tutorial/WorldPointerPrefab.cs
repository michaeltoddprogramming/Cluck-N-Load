using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Script for world pointer prefab - points to 3D world objects during tutorial
/// </summary>
public class WorldPointerPrefab : MonoBehaviour
{
    [Header("Components")]
    public Image pointerImage;
    public TextMeshProUGUI labelText;
    public Animator animator;
    
    [Header("Settings")]
    public float bobSpeed = 2f;
    public float bobHeight = 0.5f;
    public Vector3 offset = Vector3.up;
    
    private Transform targetTransform;
    private Vector3 originalLocalPosition;
    private Camera mainCamera;
    
    private void Awake()
    {
        AutoAssignComponents();
        originalLocalPosition = transform.localPosition;
        mainCamera = Camera.main;
    }
    
    private void AutoAssignComponents()
    {
        if (pointerImage == null)
            pointerImage = GetComponentInChildren<Image>();
            
        if (labelText == null)
            labelText = GetComponentInChildren<TextMeshProUGUI>();
            
        if (animator == null)
            animator = GetComponent<Animator>();
    }
    
    private void Update()
    {
        if (targetTransform != null && mainCamera != null)
        {
            // Convert world position to screen position
            Vector3 worldPos = targetTransform.position + offset;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
            
            // Update position
            transform.position = screenPos;
            
            // Add bobbing animation
            float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.localPosition = originalLocalPosition + Vector3.up * bob;
            
            // Hide if behind camera
            bool isBehind = screenPos.z < 0;
            gameObject.SetActive(!isBehind);
        }
    }
    
    public void SetTarget(Transform target, string label = "")
    {
        targetTransform = target;
        if (labelText != null && !string.IsNullOrEmpty(label))
        {
            labelText.text = label;
        }
        gameObject.SetActive(target != null);
    }
    
    public void ClearTarget()
    {
        targetTransform = null;
        gameObject.SetActive(false);
    }
}
