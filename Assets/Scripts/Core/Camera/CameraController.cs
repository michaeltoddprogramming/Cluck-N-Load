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
    void Start()
    {
        newPosition = transform.position;
        newRotation = transform.rotation;
        newZoom = cameraTransform.localPosition;
    }

    void Update()
    {
        
    }

    void LateUpdate() {
        HandleMovementInput();
        HandleMouseInput();
    }

    void HandleMouseInput() {
        if(Input.mouseScrollDelta.y != 0) {
            newZoom += Input.mouseScrollDelta.y * zoomAmount;
        }

        if(Input.GetMouseButtonDown(0)) {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float entry;
            if(plane.Raycast(ray, out entry)) {
                dragStartPosition = ray.GetPoint(entry);
            }
        }

        if(Input.GetMouseButton(0)) {
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float entry;
            if(plane.Raycast(ray, out entry)) {
                dragCurrentPosition = ray.GetPoint(entry);
                newPosition = transform.position + dragStartPosition - dragCurrentPosition;
            }
        }

        if(Input.GetMouseButtonDown(2)) {
            rotateStartPosition = Input.mousePosition;
        }

        if(Input.GetMouseButton(2)) {
            rotateCurrentPosition = Input.mousePosition;
            Vector3 difference = rotateStartPosition - rotateCurrentPosition;
            rotateStartPosition = rotateCurrentPosition;
            newRotation *= Quaternion.Euler(Vector3.up * (-difference.x / 5f));
        }
    }

        void HandleMovementInput() {
        Vector3 direction = Vector3.zero;
    
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
        if (Input.GetKey(KeyCode.R)) {
            newZoom += zoomAmount;
        }
        if (Input.GetKey(KeyCode.F)) {
            newZoom -= zoomAmount;
        }
    
        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * movementTime);
        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, newZoom, Time.deltaTime * movementTime);
    }
}
