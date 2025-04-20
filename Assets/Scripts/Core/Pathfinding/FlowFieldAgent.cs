using UnityEngine;

public class FlowFieldAgent : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 5f;
    public float arrivalThreshold = 0.1f;
    public float pathRefreshRate = 0.1f; // How often to check for path updates
    
    [Header("Pathfinding Behavior")]
    [Tooltip("Whether agent can move through non-owned territory")]
    public bool canTravelThroughNonOwnedCells = true;
    [Tooltip("Whether agent should slow down in non-owned territory")]
    public bool slowInNonOwnedTerritory = true;
    [Tooltip("Speed multiplier when in non-owned territory (if slowInNonOwnedTerritory is true)")]
    [Range(0.1f, 1.0f)]
    public float nonOwnedSpeedMultiplier = 0.5f;

    [Header("Debug")]
    public bool showDebugInfo = false;

    // References found at runtime
    private GridController gridController;
    private FlowFieldGenerator flowFieldGenerator;
    private Vector2Int currentCellCoord;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private float pathUpdateTimer = 0f;
    private bool isInitialized = false;

    // Public methods to set references
    public void SetGridController(GridController controller)
    {
        gridController = controller;
        CheckInitialization();
    }

    public void SetFlowFieldGenerator(FlowFieldGenerator generator)
    {
        flowFieldGenerator = generator;
        CheckInitialization();
    }

    private void CheckInitialization()
    {
        // Check if we now have both required components
        if (gridController != null && flowFieldGenerator != null && !isInitialized)
        {
            isInitialized = true;
            InitializePosition();
        }
    }

    private void Awake()
    {
        // Try to immediately find references
        FindRequiredComponents();
    }

    private void FindRequiredComponents()
    {
        if (gridController == null)
            gridController = FindObjectOfType<GridController>();
            
        if (flowFieldGenerator == null)
            flowFieldGenerator = FindObjectOfType<FlowFieldGenerator>();

        isInitialized = (gridController != null && flowFieldGenerator != null);

        if (!isInitialized && (gridController == null || flowFieldGenerator == null))
        {
            Debug.LogWarning($"[FlowFieldAgent] {gameObject.name}: Missing required components. Will retry.", this);
            // We'll retry in the first Update call
        }
    }

    private void Start()
    {
        // If we found our components in Awake, initialize the cell coordinates
        if (isInitialized)
        {
            InitializePosition();
        }
    }

    private void InitializePosition()
    {
        currentCellCoord = gridController.WorldToGridCoords(transform.position);
        
        // If we're not in a valid grid cell, try to find the nearest valid one
        if (!gridController.IsValidCell(currentCellCoord.x, currentCellCoord.y))
        {
            Debug.LogWarning($"[FlowFieldAgent] {gameObject.name}: Initialized outside valid grid! Attempting to find nearest valid cell.", this);
            FindNearestValidCell();
        }
    }

    private void FindNearestValidCell()
    {
        // Simple algorithm to find the nearest valid cell
        int maxSearchRadius = 10;
        
        for (int radius = 1; radius <= maxSearchRadius; radius++)
        {
            // Search in expanding square pattern
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    // Only check cells at the current radius (perimeter)
                    if (Mathf.Abs(x) == radius || Mathf.Abs(y) == radius)
                    {
                        int testX = currentCellCoord.x + x;
                        int testY = currentCellCoord.y + y;
                        
                        if (gridController.IsValidCell(testX, testY))
                        {
                            currentCellCoord = new Vector2Int(testX, testY);
                            Vector3 validPos = gridController.GetCellCenterFromTexture(testX, testY);
                            
                            // Maintain y position (height) but use valid x,z
                            transform.position = new Vector3(validPos.x, transform.position.y, validPos.z);
                            
                            Debug.Log($"[FlowFieldAgent] {gameObject.name}: Moved to nearest valid cell at {currentCellCoord}", this);
                            return;
                        }
                    }
                }
            }
        }
        
        Debug.LogError($"[FlowFieldAgent] {gameObject.name}: Failed to find any valid cells nearby!", this);
    }

    private void Update()
    {
        // Keep trying to find references if we don't have them yet
        if (!isInitialized)
        {
            FindRequiredComponents();
            
            if (isInitialized)
            {
                InitializePosition();
            }
            else
            {
                return; // Skip the rest of update until initialized
            }
        }

        // Path update timing
        pathUpdateTimer += Time.deltaTime;
        if (pathUpdateTimer >= pathRefreshRate)
        {
            pathUpdateTimer = 0f;
            UpdatePathFollowing();
        }
        
        // Movement handling (every frame)
        if (isMoving)
        {
            MoveTowardTarget();
        }
    }

    private void UpdatePathFollowing()
    {
        // Update current cell coordinates
        currentCellCoord = gridController.WorldToGridCoords(transform.position);
        
        // Get current cell and its flow direction
        GridCell currentCell = gridController.GetCell(currentCellCoord.x, currentCellCoord.y);
        
        if (currentCell == null)
            return;
            
        // Check if we have a valid flow direction and can move there
        if (currentCell.flowDirection != Vector2.zero && currentCell.integrationCost != int.MaxValue)
        {
            // Get the world position of the current cell
            Vector3 currentPos = gridController.GetCellCenterFromTexture(currentCellCoord.x, currentCellCoord.y);
            
            // Calculate the target position using the continuous flow direction
            float cellSize = gridController.GetCellSize();
            Vector3 targetDirection = new Vector3(currentCell.flowDirection.x, 0, currentCell.flowDirection.y);
            Vector3 worldTargetPos = currentPos + targetDirection * cellSize;
            
            // Convert to grid coordinates for validity check
            Vector2Int nextCellCoords = gridController.WorldToGridCoords(worldTargetPos);
            
            if (gridController.IsValidCell(nextCellCoords.x, nextCellCoords.y))
            {
                GridCell nextCell = gridController.GetCell(nextCellCoords.x, nextCellCoords.y);
                
                // Only proceed if we can travel through non-owned territories, or if the cell is owned
                if (canTravelThroughNonOwnedCells || (nextCell != null && nextCell.flags.isOwned))
                {
                    // Set the target position, preserving the agent's height
                    targetPosition = new Vector3(worldTargetPos.x, transform.position.y, worldTargetPos.z);
                    isMoving = true;
                }
            }
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
        
        // Calculate speed based on territory
        float currentSpeed = moveSpeed;
        
        // If we should slow down in non-owned territory, check current cell
        if (slowInNonOwnedTerritory)
        {
            GridCell currentCell = gridController.GetCell(currentCellCoord.x, currentCellCoord.y);
            if (currentCell != null && !currentCell.flags.isOwned)
            {
                currentSpeed *= nonOwnedSpeedMultiplier;
            }
        }
        
        // Move toward target
        transform.position += movementDirection * currentSpeed * Time.deltaTime;
        
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
}