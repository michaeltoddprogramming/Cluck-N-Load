using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera References")]
    public Transform cameraTransform;

    [Header("Movement Settings")]
    public float normalSpeed = 30f;
    public float fastSpeed = 60f;
    public float movementTime = 5f;  // Smoothing factor
    
    [Header("Rotation Settings")]
    public float rotationAmount = 2f;
    public bool lockRotationDuringMovement = false;  // NEW: Lock rotation during movement
    
    [Header("Zoom Settings")]
    public Vector3 zoomAmount = new Vector3(0, -5, 5);
    
    [Header("Boundary Settings")]
    public float minX = -15f;
    public float maxX = 15f;
    public float minZ = -15f;
    public float maxZ = 15f;
    public float minZoom = 10f;
    public float maxZoom = 65f;
   [SerializeField] private float fixedPitch = 0f; 
    
    // Target positions that the camera will move toward
    private Vector3 newPosition;
    private Quaternion newRotation;
    private Vector3 newZoom;
    
    // Tracking for drag and rotate operations
    private Vector3 dragStartPosition;
    private Vector3 dragCurrentPosition;
    private Vector3 rotateStartPosition;
    private Vector3 rotateCurrentPosition;
    
    // Movement tracking
    private bool isMoving = false;
    private float currentSpeed;
    
    // Mouse control disabling (for UI interaction)
    private bool mouseControlsDisabled = false;

    //import cursor manager
    [SerializeField] private CursorManager cursor; 
    [SerializeField] public bool devCamera; 

    void Start()
    {
        // Initialize target positions with current transform values
    newPosition = transform.position;
    
    // Set initial rotation with the fixed pitch
    Vector3 startEuler = transform.rotation.eulerAngles;
    newRotation = Quaternion.Euler(fixedPitch, startEuler.y, 0);
    
    newZoom = cameraTransform.localPosition;
    currentSpeed = normalSpeed;
    
    // Set camera boundaries based on terrain size
    SetCameraBoundaries();
    }

    // Allow other scripts to temporarily disable mouse controls
    public void TemporarilyDisableMouseControls(bool disable)
    {
        mouseControlsDisabled = disable;
    }
    
    // Set camera boundaries based on terrain
    void SetCameraBoundaries()
    {
        Collider terrainCollider = GameObject.FindGameObjectWithTag("Terrain")?.GetComponent<Collider>();
        if (terrainCollider != null)
        {
            Bounds bounds = terrainCollider.bounds;
            
            // Add margins to keep camera from going too close to edge
            minX = bounds.min.x + 5f;
            maxX = bounds.max.x - 5f;
            minZ = bounds.min.z + 5f;
            maxZ = bounds.max.z - 5f;
            
            }
    }

    void LateUpdate()
    {
        // Reset movement flag
        isMoving = false;
        
        // Handle various inputs
        HandleKeyboardMovement();
        HandleMouseMovement();
        
        // Lock rotation during movement if enabled
        if (!isMoving || !lockRotationDuringMovement)
        {
            HandleRotation();
        }
        
        // Always handle zoom (doesn't count as changing angle)
        HandleZoom();
        
        // Ensure the camera stays within boundaries
        EnforceBoundaries();
        
        // Apply all changes with smooth transitions
        ApplySmoothTransition();
    }
    
    // Apply smooth transitions to camera transforms
    void ApplySmoothTransition()
    {
        transform.position = Vector3.Lerp(transform.position, newPosition, Time.unscaledDeltaTime * movementTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.unscaledDeltaTime * movementTime);
        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, newZoom, Time.unscaledDeltaTime * movementTime);
    }
    
    // Handle keyboard movement (WASD)
    void HandleKeyboardMovement()
    {
        Vector3 direction = Vector3.zero;
        
        // WASD controls
        if (Input.GetKey(KeyCode.W)) direction += transform.forward;
        if (Input.GetKey(KeyCode.S)) direction -= transform.forward;
        if (Input.GetKey(KeyCode.A)) direction -= transform.right;
        if (Input.GetKey(KeyCode.D)) direction += transform.right;
        
        // Adjust speed with shift
        currentSpeed = Input.GetKey(KeyCode.LeftShift) ? fastSpeed : normalSpeed;
        
        // Apply movement if there's any direction input
        if (direction.magnitude > 0)
        {
            newPosition += direction.normalized * currentSpeed * Time.unscaledDeltaTime;
            isMoving = true;
        }
    }
    
    // Handle edge-of-screen movement
    void HandleMouseMovement()
    {
        //devcam -> no edge moving
        if (!devCamera)
        {
            if (mouseControlsDisabled) return;

            float edgeThreshold = 20f;
            Vector3 direction = Vector3.zero;

            // Handle screen edge movement
            if (Input.mousePosition.x >= Screen.width - edgeThreshold)
            {
                direction += transform.right;
                isMoving = true;
            }

            if (Input.mousePosition.x <= edgeThreshold)
            {
                direction -= transform.right;
                isMoving = true;
            }

            if (Input.mousePosition.y >= Screen.height - edgeThreshold)
            {
                direction += transform.forward;
                isMoving = true;
            }

            if (Input.mousePosition.y <= edgeThreshold)
            {
                direction -= transform.forward;
                isMoving = true;
            }

            // Apply movement
            if (direction.magnitude > 0)
            {
                newPosition += direction.normalized * currentSpeed * Time.unscaledDeltaTime;
            }

        }

        // Right mouse drag movement
        if (Input.GetMouseButtonDown(1))
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (plane.Raycast(ray, out float entry))
            {
                dragStartPosition = ray.GetPoint(entry);
            }
        }

        if (Input.GetMouseButton(1))
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (plane.Raycast(ray, out float entry))
            {
                dragCurrentPosition = ray.GetPoint(entry);
                newPosition = transform.position + dragStartPosition - dragCurrentPosition;
                isMoving = true;
            }
        }
    }
    // void HandleMouseMovement()
    // {
    //     // Skip if mouse controls are disabled
    //     if (mouseControlsDisabled) return;
        
    //     // Only applies in built game, not editor
    //     if (Application.isEditor) return;

    //     // Only applies in built game, not editor
    //     if (Application.isEditor) return;
        
    //     float edgeThreshold = 20f;
    //     Vector3 direction = Vector3.zero;
        
    //     // Handle screen edge movement
    //     if (Input.mousePosition.x >= Screen.width - edgeThreshold)
    //     {
    //         direction += transform.right;
    //         isMoving = true;
    //     }
        
    //     if (Input.mousePosition.x <= edgeThreshold)
    //     {
    //         direction -= transform.right;
    //         isMoving = true;
    //     }
        
    //     if (Input.mousePosition.y >= Screen.height - edgeThreshold)
    //     {
    //         direction += transform.forward;
    //         isMoving = true;
    //     }
        
    //     if (Input.mousePosition.y <= edgeThreshold)
    //     {
    //         direction -= transform.forward;
    //         isMoving = true;
    //     }
        
    //     // Apply movement
    //     if (direction.magnitude > 0)
    //     {
    //         newPosition += direction.normalized * currentSpeed * Time.unscaledDeltaTime;
    //     }
        
    //     // Right mouse drag movement
    //     if (Input.GetMouseButtonDown(1))
    //     {
    //         // Start drag
    //         Plane plane = new Plane(Vector3.up, Vector3.zero);
    //         Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
    //         if (plane.Raycast(ray, out float entry))
    //         {
    //             dragStartPosition = ray.GetPoint(entry);
    //         }
    //     }
        
    //     if (Input.GetMouseButton(1))
    //     {
    //         // Continue drag
    //         Plane plane = new Plane(Vector3.up, Vector3.zero);
    //         Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
    //         if (plane.Raycast(ray, out float entry))
    //         {
    //             dragCurrentPosition = ray.GetPoint(entry);
    //             newPosition = transform.position + dragStartPosition - dragCurrentPosition;
    //             isMoving = true;
    //         }
    //     }
    // }
    
    // Handle rotation inputs
    void HandleRotation()
    {
        
        // Keyboard rotation
        if (Input.GetKey(KeyCode.Q))
        {
            newRotation *= Quaternion.Euler(Vector3.up * rotationAmount);
        }
        // Keyboard rotation
        if (Input.GetKey(KeyCode.Q))
        {
            newRotation *= Quaternion.Euler(Vector3.up * rotationAmount);
        }
        
        if (Input.GetKey(KeyCode.E))
        {
            newRotation *= Quaternion.Euler(Vector3.up * -rotationAmount);
        }
        
        // Middle mouse button rotation
        if (Input.GetMouseButtonDown(2))
        {
            rotateStartPosition = Input.mousePosition;
        }
        
        if (Input.GetMouseButton(2))
        {
            rotateCurrentPosition = Input.mousePosition;
            Vector3 difference = rotateStartPosition - rotateCurrentPosition;
            rotateStartPosition = rotateCurrentPosition;
            
            // Only allow rotation around Y axis (prevents tilting)
            newRotation *= Quaternion.Euler(Vector3.up * (-difference.x / 5f));
        }
    }
    
    // Handle zoom inputs
    void HandleZoom()
    {

        // Skip mouse wheel zoom if controls are disabled
        if (!mouseControlsDisabled)
        {
            // Mouse wheel zoom
            // if (Input.mouseScrollDelta.y != 0)
            // {                
            //     newZoom += Input.mouseScrollDelta.y * zoomAmount * 0.2f;
            // }

            // Mouse wheel zoom
            float scroll = Input.mouseScrollDelta.y;

            if (scroll != 0)
            {
                if (scroll > 0)
                {
                    //zooming int
                    cursor.zoom(true);
                }
                else if (scroll < 0)
                {
                    // Zooming out
                    cursor.zoom(false);
                }

                newZoom += scroll * zoomAmount * 0.2f;
            }
        }    
        
        // Keyboard zoom
        if (Input.GetKey(KeyCode.Alpha1))
        {
            cursor.zoom(true);
            newZoom += zoomAmount * 0.2f;
        }
        
        if (Input.GetKey(KeyCode.Alpha2))
        {
            cursor.zoom(false);
            newZoom -= zoomAmount * 0.2f;
        }

        // After the zooming stops (i.e., no key pressed), reset the cursor
        if (!Input.GetKey(KeyCode.Alpha1) && !Input.GetKey(KeyCode.Alpha2))
        {
            cursor.resetCursor();
        }
    }
    
    // Enforce camera boundaries and restrictions
    void EnforceBoundaries()
    {
        // Restrict position within terrain bounds
        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);
        
        // IMPORTANT: Always force the fixed pitch - this ensures pitch never changes
        Vector3 currentEuler = newRotation.eulerAngles;
        newRotation = Quaternion.Euler(fixedPitch, currentEuler.y, 0);
        
        // Keep zoom within limits
        float distance = Mathf.Sqrt(newZoom.y * newZoom.y + newZoom.z * newZoom.z);
        distance = Mathf.Clamp(distance, minZoom, maxZoom);
        
        // Maintain the camera angle while adjusting zoom distance
        float currentAngle = Mathf.Atan2(newZoom.y, newZoom.z);
        newZoom.y = distance * Mathf.Sin(currentAngle);
        newZoom.z = distance * Mathf.Cos(currentAngle);
    }
}

// using UnityEngine;

// public class CameraController : MonoBehaviour
// {
//     [Header("Camera References")]
//     public Transform cameraTransform;

//     [Header("Movement Settings")]
//     public float normalSpeed = 30f;
//     public float fastSpeed = 60f;
//     public float movementTime = 5f;

//     [Header("Rotation Settings")]
//     public float rotationAmount = 2f;
//     public float mouseSensitivity = 100f;
//     public bool lockRotationDuringMovement = true;

//     [Header("Zoom Settings")]
//     public Vector3 zoomAmount = new Vector3(0, -5, 5);

//     [Header("Boundary Settings")]
//     public float minX = -50f;
//     public float maxX = 50f;
//     public float minZ = -50f;
//     public float maxZ = 50f;
//     public float minZoom = 50f;
//     public float maxZoom = 100f;

//     private Vector3 newPosition;
//     private Quaternion newRotation;
//     private Vector3 newZoom;

//     private Vector3 dragStartPosition;
//     private Vector3 dragCurrentPosition;
//     private Vector3 rotateStartPosition;
//     private Vector3 rotateCurrentPosition;

//     private bool isMoving = false;
//     private float currentSpeed;

//     private bool mouseControlsDisabled = false;

//     private float yaw;
//     private float pitch;

//     void Start()
//     {
//         maxZoom = 500f;

//         newPosition = transform.position;
//         Vector3 startEuler = transform.rotation.eulerAngles;
//         yaw = startEuler.y;
//         pitch = startEuler.x;
//         newRotation = Quaternion.Euler(pitch, yaw, 0);
//         newZoom = cameraTransform.localPosition;
//         currentSpeed = normalSpeed;
//         SetCameraBoundaries();
//     }

//     public void TemporarilyDisableMouseControls(bool disable)
//     {
//         mouseControlsDisabled = disable;
//     }

//     void SetCameraBoundaries()
//     {
//         Collider terrainCollider = GameObject.FindGameObjectWithTag("Terrain")?.GetComponent<Collider>();
//         if (terrainCollider != null)
//         {
//             Bounds bounds = terrainCollider.bounds;
//             minX = bounds.min.x + 5f;
//             maxX = bounds.max.x - 5f;
//             minZ = bounds.min.z + 5f;
//             maxZ = bounds.max.z - 5f;
//         }
//     }

//     void LateUpdate()
//     {
//         isMoving = false;
//         HandleKeyboardMovement();
//         HandleMouseMovement();

//         if (!isMoving || !lockRotationDuringMovement)
//         {
//             HandleRotation();
//         }

//         HandleZoom();
//         EnforceBoundaries();
//         ApplySmoothTransition();
//     }

//     void ApplySmoothTransition()
//     {
//         transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
//         transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * movementTime);
//         cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, newZoom, Time.deltaTime * movementTime);
//     }

//     void HandleKeyboardMovement()
//     {
//         Vector3 direction = Vector3.zero;

//         if (Input.GetKey(KeyCode.W)) direction += transform.forward;
//         if (Input.GetKey(KeyCode.S)) direction -= transform.forward;
//         if (Input.GetKey(KeyCode.A)) direction -= transform.right;
//         if (Input.GetKey(KeyCode.D)) direction += transform.right;

//         currentSpeed = Input.GetKey(KeyCode.LeftShift) ? fastSpeed : normalSpeed;

//         if (direction.magnitude > 0)
//         {
//             newPosition += direction.normalized * currentSpeed * Time.deltaTime;
//             isMoving = true;
//         }
//     }

//     void HandleMouseMovement()
//     {
//         if (mouseControlsDisabled) return;
//         if (Application.isEditor) return;

//         float edgeThreshold = 20f;
//         Vector3 direction = Vector3.zero;

//         if (Input.mousePosition.x >= Screen.width - edgeThreshold) { direction += transform.right; isMoving = true; }
//         if (Input.mousePosition.x <= edgeThreshold) { direction -= transform.right; isMoving = true; }
//         if (Input.mousePosition.y >= Screen.height - edgeThreshold) { direction += transform.forward; isMoving = true; }
//         if (Input.mousePosition.y <= edgeThreshold) { direction -= transform.forward; isMoving = true; }

//         if (direction.magnitude > 0)
//         {
//             newPosition += direction.normalized * currentSpeed * Time.deltaTime;
//         }

//         if (Input.GetMouseButtonDown(1))
//         {
//             Plane plane = new Plane(Vector3.up, Vector3.zero);
//             Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
//             if (plane.Raycast(ray, out float entry))
//             {
//                 dragStartPosition = ray.GetPoint(entry);
//             }
//         }

//         if (Input.GetMouseButton(1))
//         {
//             Plane plane = new Plane(Vector3.up, Vector3.zero);
//             Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
//             if (plane.Raycast(ray, out float entry))
//             {
//                 dragCurrentPosition = ray.GetPoint(entry);
//                 newPosition = transform.position + dragStartPosition - dragCurrentPosition;
//                 isMoving = true;
//             }
//         }
//     }

//     void HandleRotation()
//     {
//         if (Input.GetMouseButtonDown(2))
//         {
//             rotateStartPosition = Input.mousePosition;
//         }

//         if (Input.GetMouseButton(2))
//         {
//             rotateCurrentPosition = Input.mousePosition;
//             Vector3 difference = rotateStartPosition - rotateCurrentPosition;
//             rotateStartPosition = rotateCurrentPosition;

//             yaw += difference.x * mouseSensitivity * Time.deltaTime;
//             pitch += difference.y * mouseSensitivity * Time.deltaTime;
//         }

//         if (Input.GetKey(KeyCode.Q)) yaw -= rotationAmount;
//         if (Input.GetKey(KeyCode.E)) yaw += rotationAmount;

//         newRotation = Quaternion.Euler(pitch, yaw, 0);
//     }

//     void HandleZoom()
//     {
//         if (!mouseControlsDisabled && Input.mouseScrollDelta.y != 0)
//         {
//             newZoom += Input.mouseScrollDelta.y * zoomAmount * 0.2f;
//         }

//         if (Input.GetKey(KeyCode.Alpha1)) newZoom += zoomAmount * 0.2f;
//         if (Input.GetKey(KeyCode.Alpha2)) newZoom -= zoomAmount * 0.2f;
//     }

//     void EnforceBoundaries()
//     {
//         newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
//         newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);

//         float distance = Mathf.Sqrt(newZoom.y * newZoom.y + newZoom.z * newZoom.z);
//         distance = Mathf.Clamp(distance, minZoom, maxZoom);
//         float currentAngle = Mathf.Atan2(newZoom.y, newZoom.z);
//         newZoom.y = distance * Mathf.Sin(currentAngle);
//         newZoom.z = distance * Mathf.Cos(currentAngle);
//     }
// }

