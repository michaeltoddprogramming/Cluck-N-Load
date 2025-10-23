using UnityEngine;
public class CameraController : MonoBehaviour
{
    [Header("Camera References")]
    public Transform cameraTransform;

    [Header("Movement Settings")]
    public float normalSpeed = 30f;
    public float fastSpeed = 60f;
    public float keyboardSpeedMultiplier = 3f;
    public float movementTime = 5f;  // S

    [Header("Rotation Settings")]
    public float rotationAmount = 15f;
    public bool lockRotationDuringMovement = false;  

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

    private Vector3 newPosition;
    private Quaternion newRotation;
    private Vector3 newZoom;

    private Vector3 dragStartPosition;
    private Vector3 dragCurrentPosition;
    private Vector3 rotateStartPosition;
    private Vector3 rotateCurrentPosition;
    private bool isMoving = false;
    private float currentSpeed;

    private bool mouseControlsDisabled = false;

    [SerializeField] private CursorManager cursor;
    [SerializeField] public bool devCamera;
    
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        
        newPosition = transform.position;

        Vector3 startEuler = transform.rotation.eulerAngles;
        newRotation = Quaternion.Euler(fixedPitch, startEuler.y, 0);

        newZoom = cameraTransform.localPosition + zoomAmount * -10f;
        currentSpeed = normalSpeed;

        SetCameraBoundaries();
    }

    public void TemporarilyDisableMouseControls(bool disable)
    {
        mouseControlsDisabled = disable;
    }

    void SetCameraBoundaries()
    {
        Collider terrainCollider = GameObject.FindGameObjectWithTag("Terrain")?.GetComponent<Collider>();
        if (terrainCollider != null)
        {
            Bounds bounds = terrainCollider.bounds;

            minX = bounds.min.x + 5f;
            maxX = bounds.max.x - 5f;
            minZ = bounds.min.z + 5f;
            maxZ = bounds.max.z - 5f;

        }
    }

    void LateUpdate()
    {
        isMoving = false;

        HandleKeyboardMovement();
        HandleMouseMovement();

        if (!isMoving || !lockRotationDuringMovement)
        {
            HandleRotation();
        }
        HandleZoom();
        EnforceBoundaries();
        ApplySmoothTransition();
    }

    void ApplySmoothTransition()
    {
        transform.position = Vector3.Lerp(transform.position, newPosition, Time.unscaledDeltaTime * movementTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.unscaledDeltaTime * movementTime);
        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, newZoom, Time.unscaledDeltaTime * movementTime);
    }

    void HandleKeyboardMovement()
    {
        Vector3 direction = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) direction += transform.forward;
        if (Input.GetKey(KeyCode.S)) direction -= transform.forward;
        if (Input.GetKey(KeyCode.A)) direction -= transform.right;
        if (Input.GetKey(KeyCode.D)) direction += transform.right;

        currentSpeed = Input.GetKey(KeyCode.LeftShift) ? fastSpeed : normalSpeed;

        if (direction.magnitude > 0)
        {
            newPosition += direction.normalized * currentSpeed * keyboardSpeedMultiplier * Time.unscaledDeltaTime;
            isMoving = true;
        }
    }

    void HandleMouseMovement()
    {
        if (!devCamera)
        {
            if (mouseControlsDisabled) return;

            float edgeThreshold = 20f;
            Vector3 direction = Vector3.zero;

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

            if (direction.magnitude > 0)
            {
                newPosition += direction.normalized * currentSpeed * Time.unscaledDeltaTime;
            }

        }

        if (Input.GetMouseButtonDown(1))
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (plane.Raycast(ray, out float entry))
            {
                dragStartPosition = ray.GetPoint(entry);
            }
        }

        if (Input.GetMouseButton(1))
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (plane.Raycast(ray, out float entry))
            {
                dragCurrentPosition = ray.GetPoint(entry);
                newPosition = transform.position + dragStartPosition - dragCurrentPosition;
                isMoving = true;
            }
        }
    }
    void HandleRotation()
    {

        float rotationInput = 0f;
        
        if (Input.GetKey(KeyCode.Q))
        {
            rotationInput += rotationAmount;
        }

        if (Input.GetKey(KeyCode.E))
        {
            rotationInput -= rotationAmount;
        }
        
        if (rotationInput != 0f)
        {
            Vector3 currentEuler = newRotation.eulerAngles;
            currentEuler.y += rotationInput * Time.unscaledDeltaTime * 30f; // 30f scales it to reasonable speed
            newRotation = Quaternion.Euler(currentEuler.x, currentEuler.y, currentEuler.z);
        }

        if (Input.GetMouseButtonDown(2))
        {
            rotateStartPosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(2))
        {
            rotateCurrentPosition = Input.mousePosition;
            Vector3 difference = rotateStartPosition - rotateCurrentPosition;
            rotateStartPosition = rotateCurrentPosition;

            newRotation *= Quaternion.Euler(Vector3.up * (-difference.x / 5f));
        }
    }

    void HandleZoom()
    {

        if (!mouseControlsDisabled)
        {
            float scroll = Input.mouseScrollDelta.y;

            if (scroll != 0)
            {
                if (scroll > 0)
                {
                    cursor.zoom(true);
                }
                else if (scroll < 0)
                {
                    cursor.zoom(false);
                }

                newZoom += scroll * zoomAmount * 0.6f;
            }
        }

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

        if (!Input.GetKey(KeyCode.Alpha1) && !Input.GetKey(KeyCode.Alpha2))
        {
            cursor.resetCursor();
        }
    }

    void EnforceBoundaries()
    {
        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);

        Vector3 currentEuler = newRotation.eulerAngles;
        newRotation = Quaternion.Euler(fixedPitch, currentEuler.y, 0);
        float distance = Mathf.Sqrt(newZoom.y * newZoom.y + newZoom.z * newZoom.z);
        distance = Mathf.Clamp(distance, minZoom, maxZoom);
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

