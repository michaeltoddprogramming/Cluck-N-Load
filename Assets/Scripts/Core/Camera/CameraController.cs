using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform cameraTransform;
    public float normalSpeed;
    public float fastSpeed;
    public float movementSpeed;
    public float movementTime;
    public float rotationAmount;
    public Vector3 zoomAmount;

    public Vector3 newPosition;
    public Quaternion newRotation;
    public Vector3 newZoom;

    public Vector3 dragStartPosition;
    public Vector3 dragCurrentPosition;
    public Vector3 rotateStartPosition;
    public Vector3 rotateCurrentPosition;

    private bool controlsTemporarilyDisabled = false;
    private bool mouseControlsDisabled = false;

    void Start()
    {
        newPosition = transform.position;
        newRotation = transform.rotation;
        newZoom = cameraTransform.localPosition;
    }

    void Update()
    {
        
    }

    void LateUpdate() 
    {
        // Always handle keyboard input, regardless of UI hovering
        HandleMovementInput();
        
        // Only handle mouse input if mouse controls are not disabled
        if (!mouseControlsDisabled)
        {
            HandleMouseInput();
        }
        
        // Always apply the movement interpolation to maintain inertia
        ApplyCameraTransforms();
    }
    
    // New method to apply transforms - this will run even when input is disabled
    void ApplyCameraTransforms()
    {
        // Apply the smooth transition/inertia regardless of input state
        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * movementTime);
        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, newZoom, Time.deltaTime * movementTime);
    }

    // New method for selectively disabling only mouse controls
    public void TemporarilyDisableMouseControls(bool disable)
    {
        mouseControlsDisabled = disable;
    }
    
    // Keep original method for backward compatibility, but make it just affect mouse
    public void TemporarilyDisableControls(bool disable)
    {
        mouseControlsDisabled = disable;
    }

    void HandleMouseInput() {
        // Adjust mouse scroll zoom sensitivity
        if (Input.mouseScrollDelta.y != 0) {
            newZoom += Input.mouseScrollDelta.y * zoomAmount * 0.2f; // Reduce sensitivity by scaling down
        }
    
        if (Input.GetMouseButtonDown(1)) {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float entry;
            if (plane.Raycast(ray, out entry)) {
                dragStartPosition = ray.GetPoint(entry);
            }
        }
    
        if (Input.GetMouseButton(1)) {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float entry;
            if (plane.Raycast(ray, out entry)) {
                dragCurrentPosition = ray.GetPoint(entry);
                newPosition = transform.position + dragStartPosition - dragCurrentPosition;
            }
        }
    
        if (Input.GetMouseButtonDown(2)) {
            rotateStartPosition = Input.mousePosition;
        }
    
        if (Input.GetMouseButton(2)) {
            rotateCurrentPosition = Input.mousePosition;
            Vector3 difference = rotateStartPosition - rotateCurrentPosition;
            rotateStartPosition = rotateCurrentPosition;
            newRotation *= Quaternion.Euler(Vector3.up * (-difference.x / 5f));
        }
    }
    
    void HandleMovementInput() {
        Vector3 direction = Vector3.zero;
    
        if (Application.isEditor) {
            float wasdSpeedMultiplier = 50f;
            if (Input.GetKey(KeyCode.W)) direction += transform.forward * wasdSpeedMultiplier;
            if (Input.GetKey(KeyCode.S)) direction -= transform.forward * wasdSpeedMultiplier;
            if (Input.GetKey(KeyCode.A)) direction -= transform.right * wasdSpeedMultiplier;
            if (Input.GetKey(KeyCode.D)) direction += transform.right * wasdSpeedMultiplier;
        } else {
            float edgeThreshold = 20f;
            float maxSpeedMultiplier = 100f;
    
            if (Input.mousePosition.x >= Screen.width - edgeThreshold) {
                float proximity = (Input.mousePosition.x - (Screen.width - edgeThreshold)) / edgeThreshold;
                direction += transform.right * Mathf.Clamp(proximity * maxSpeedMultiplier, 0, maxSpeedMultiplier);
            }
            if (Input.mousePosition.x <= edgeThreshold) {
                float proximity = (edgeThreshold - Input.mousePosition.x) / edgeThreshold;
                direction -= transform.right * Mathf.Clamp(proximity * maxSpeedMultiplier, 0, maxSpeedMultiplier);
            }
    
            if (Input.mousePosition.y >= Screen.height - edgeThreshold) {
                float proximity = (Input.mousePosition.y - (Screen.height - edgeThreshold)) / edgeThreshold;
                direction += transform.forward * Mathf.Clamp(proximity * maxSpeedMultiplier, 0, maxSpeedMultiplier);
            }
            if (Input.mousePosition.y <= edgeThreshold) {
                float proximity = (edgeThreshold - Input.mousePosition.y) / edgeThreshold;
                direction -= transform.forward * Mathf.Clamp(proximity * maxSpeedMultiplier, 0, maxSpeedMultiplier);
            }
        }
    
        if (Input.GetKey(KeyCode.LeftShift)) {
            movementSpeed = fastSpeed;
        } else {
            movementSpeed = normalSpeed;
        }
    
        newPosition += direction * movementSpeed * Time.deltaTime;
    
        if (Input.GetKey(KeyCode.Q)) {
            newRotation *= Quaternion.Euler(Vector3.up * rotationAmount);
        }
        if (Input.GetKey(KeyCode.E)) {
            newRotation *= Quaternion.Euler(Vector3.up * -rotationAmount);
        }
    
        // Adjust keyboard zoom sensitivity
        if (Input.GetKey(KeyCode.Alpha1)) {
            newZoom += zoomAmount * 0.2f; // Reduce sensitivity by scaling down
        }
        if (Input.GetKey(KeyCode.Alpha2)) {
            newZoom -= zoomAmount * 0.2f; // Reduce sensitivity by scaling down
        }
        
        // Remove the transform application from here since it's now in ApplyCameraTransforms
    }
}