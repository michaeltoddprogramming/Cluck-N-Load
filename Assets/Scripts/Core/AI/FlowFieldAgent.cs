using UnityEngine;

public class FlowFieldAgent : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 7f;
    public float rotationSpeed = 5f;
    public float arrivalThreshold = 0.1f;
    public float pathRefreshRate = 0.1f;
    
    [Header("Debug")]
    public bool showDebugInfo = false;

    // Internal references
    private GridController gridController;
    private FlowFieldGenerator flowFieldGenerator;
    private Vector2Int currentCellCoord;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private float pathUpdateTimer = 0f;
    private bool isInitialized = false;
    private Vector3 lastPosition;

    public Vector3 velocity { get; private set; }

    private void Awake()
    {
        FindRequiredComponents();
    }

    private void FindRequiredComponents()
    {
        gridController = FindObjectOfType<GridController>();
        flowFieldGenerator = FindObjectOfType<FlowFieldGenerator>();
        isInitialized = (gridController != null && flowFieldGenerator != null);
    }

    private void Start()
    {
        if (isInitialized)
            InitializePosition();
    }

    private void InitializePosition()
    {
        currentCellCoord = gridController.WorldToGridCoords(transform.position);
        
        if (!gridController.IsValidCell(currentCellCoord.x, currentCellCoord.y))
            FindNearestValidCell();
    }

    private void FindNearestValidCell()
    {
        int maxSearchRadius = 5;
        
        for (int radius = 1; radius <= maxSearchRadius; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Abs(x) == radius || Mathf.Abs(y) == radius)
                    {
                        int testX = currentCellCoord.x + x;
                        int testY = currentCellCoord.y + y;
                        
                        if (gridController.IsValidCell(testX, testY))
                        {
                            currentCellCoord = new Vector2Int(testX, testY);
                            Vector3 validPos = gridController.GetCellCenterFromTexture(testX, testY);
                            transform.position = new Vector3(validPos.x, transform.position.y, validPos.z);
                            return;
                        }
                    }
                }
            }
        }
    }

    private void Update()
    {
        // Calculate velocity
        velocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;

        if (!isInitialized)
        {
            FindRequiredComponents();
            if (isInitialized)
                InitializePosition();
            return;
        }

        // Path update timing
        pathUpdateTimer += Time.deltaTime;
        if (pathUpdateTimer >= pathRefreshRate)
        {
            pathUpdateTimer = 0f;
            UpdatePathFollowing();
        }
        
        // Movement handling
        if (isMoving)
            MoveTowardTarget();
    }

    private void UpdatePathFollowing()
    {
        // Update current cell coordinates
        currentCellCoord = gridController.WorldToGridCoords(transform.position);
        GridCell currentCell = gridController.GetCell(currentCellCoord.x, currentCellCoord.y);
        
        if (currentCell == null)
            return;
            
        // Check if we have a valid flow direction
        if (currentCell.flowDirection != Vector2.zero && currentCell.integrationCost != int.MaxValue)
        {
            // Get the world position of the current cell
            Vector3 currentPos = gridController.GetCellCenterFromTexture(currentCellCoord.x, currentCellCoord.y);
            
            // Calculate the target position using the flow direction
            float cellSize = gridController.GetCellSize();
            Vector3 targetDirection = new Vector3(currentCell.flowDirection.x, 0, currentCell.flowDirection.y);
            Vector3 worldTargetPos = currentPos + targetDirection * cellSize;
            
            // Set the target position, preserving the agent's height
            targetPosition = new Vector3(worldTargetPos.x, transform.position.y, worldTargetPos.z);
            isMoving = true;
        }
    }

    private void MoveTowardTarget()
    {
        // Calculate distance to target (ignoring Y for ground movement)
        float distanceToTarget = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(targetPosition.x, 0, targetPosition.z)
        );
        
        // If we're close enough to the target, stop moving
        if (distanceToTarget < arrivalThreshold)
        {
            isMoving = false;
            return;
        }
        
        // Calculate movement direction
        Vector3 movementDirection = (targetPosition - transform.position).normalized;
        
        // Move toward target
        transform.position += movementDirection * moveSpeed * Time.deltaTime;
        
        // Rotate toward movement direction
        if (movementDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugInfo || !Application.isPlaying || !isInitialized)
            return;
            
        // Draw current cell
        Gizmos.color = Color.yellow;
        if (gridController != null && gridController.IsValidCell(currentCellCoord.x, currentCellCoord.y))
        {
            Vector3 cellCenter = gridController.GetCellCenterFromTexture(currentCellCoord.x, currentCellCoord.y);
            Gizmos.DrawWireCube(cellCenter, new Vector3(0.9f, 0.1f, 0.9f));
        }
        
        // Draw target position
        if (isMoving)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(targetPosition, 0.2f);
        }
    }

    public void SetGridController(GridController controller)
    {
        gridController = controller;
    }

    public void SetFlowFieldGenerator(FlowFieldGenerator generator)
    {
        flowFieldGenerator = generator;
    }
}