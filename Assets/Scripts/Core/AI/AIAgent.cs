using System.Linq;
using UnityEngine;
using FarmDefender.Core.AI.FlowField;

public class AIAgent : MonoBehaviour
{
    public enum EnemyType { Regular = 0, Fast = 1, Strong = 2 }
    public EnemyType enemyType = EnemyType.Regular;
    public float moveSpeed = 5f;
    public float pheromoneFollowWeight = 1.0f;
    public float flowFieldWeight = 0.5f;
    public float randomSteer = 0.1f;
    
    [Header("Debug")]
    public bool showDebug = false;
    public bool logMovement = false;

    [Header("Movement Smoothing")]
    public float directionSmoothTime = 0.3f;
    public float minDirectionChangeInterval = 0.2f;

    public PheromoneManager pheromoneManager;
    public FlowFieldManager flowFieldManager;
    public GridController gridController;
    public GridDataGenerator gridDataGenerator;
    public AIPriorityProfile priorityProfile;

    private Vector2Int currentCell;
    private Vector2 lastMoveDir = Vector2.zero;
    private Vector2 targetMoveDir = Vector2.zero; // Add this line to fix the error
    private Vector2 smoothedMoveDir = Vector2.zero;
    private Vector2 directionVelocity = Vector2.zero;
    private float nextDirectionChangeTime = 0f;
    private float debugTimer = 0f;

    private bool isInitialized = false;

    void Start()
    {
        // Find components if not assigned
        FindRequiredComponents();
    }

    private void FindRequiredComponents()
    {
        // Find core systems
        if (pheromoneManager == null) pheromoneManager = FindObjectOfType<PheromoneManager>();
        if (flowFieldManager == null) flowFieldManager = FindObjectOfType<FlowFieldManager>();
        if (gridController == null) gridController = FindObjectOfType<GridController>();
        if (gridDataGenerator == null) gridDataGenerator = FindObjectOfType<GridDataGenerator>();
        
        // Find proper priority profile based on enemy type
        if (priorityProfile == null)
        {
            // First try to find a specific one for this enemy type
            var profiles = Resources.FindObjectsOfTypeAll<AIPriorityProfile>();
            foreach (var profile in profiles)
            {
                if (profile.enemyType == this.enemyType)
                {
                    priorityProfile = profile;
                    break;
                }
            }
            
            // If still no profile, try to find default
            if (priorityProfile == null && profiles.Length > 0)
            {
                priorityProfile = profiles[0];
                Debug.LogWarning($"No specific priority profile found for {enemyType}, using first available.");
            }
        }
        
        // Log initialization status
        bool success = pheromoneManager != null && flowFieldManager != null && 
                      gridController != null && gridDataGenerator != null;
        
        if (success)
        {
            isInitialized = true;
            Debug.Log($"AI Agent initialized: Type={enemyType}, References found successfully");
        }
        else
        {
            Debug.LogError($"AI Agent failed to initialize: Missing components! " +
                          $"PM={pheromoneManager != null}, " +
                          $"FFM={flowFieldManager != null}, " +
                          $"GC={gridController != null}, " +
                          $"GDG={gridDataGenerator != null}");
        }
    }

    void Update()
    {
        // Try again if components weren't found on first attempt
        if (!isInitialized)
        {
            FindRequiredComponents();
            if (!isInitialized) return;
        }
        
        // Update debug timer
        debugTimer -= Time.deltaTime;
        bool shouldDebugThisFrame = showDebug && debugTimer <= 0f;
        if (shouldDebugThisFrame) debugTimer = 3f; // Debug every 3 seconds
            
        // Get current grid cell position
        Vector2Int cell = gridController.WorldToGridCoords(transform.position);
        currentCell = cell;
        
        // First priority: Select target using priority system
        Transform target = SelectTarget();
        
        Vector2 moveDir;
        if (target != null)
        {
            // Direct movement toward target
            Vector3 toTarget = (target.position - transform.position);
            moveDir = new Vector2(toTarget.x, toTarget.z).normalized;
            
            if (shouldDebugThisFrame)
                Debug.Log($"AI moving to target: {target.name}, distance: {toTarget.magnitude:F1}");
        }
        else
        {
            // Get pheromone gradient direction for this agent's type
            Vector2 pheromoneDir = GetPheromoneGradient(cell, (int)enemyType);
            float pheromoneStrength = GetMaxNeighborPheromone(cell, (int)enemyType);
            
            // Get flow field direction
            Vector2 flowDir = Vector2.zero;
            GridCell gc = gridDataGenerator.GetCell(cell.x, cell.y);
            if (gc != null)
                flowDir = gc.flowDirection;
                
            // Combine directions with weights based on pheromone presence
            float pheroWeight = pheromoneStrength > 0.1f ? pheromoneFollowWeight : 0.1f;
            moveDir = (pheromoneDir * pheroWeight) + (flowDir * flowFieldWeight);
            
            // If resulting direction is too weak, prioritize flow field
            if (moveDir.sqrMagnitude < 0.1f)
            {
                moveDir = flowDir;
                
                // If flow direction is also weak, add more randomness or use last direction
                if (moveDir.sqrMagnitude < 0.1f)
                {
                    moveDir = lastMoveDir != Vector2.zero ? lastMoveDir : Random.insideUnitCircle.normalized;
                }
            }
            
            // Add some randomness for natural movement
            moveDir += Random.insideUnitCircle * randomSteer;
            moveDir.Normalize();
            
            if (shouldDebugThisFrame)
            {
                Debug.Log($"AI using pheromone+flow: pheromone={pheromoneStrength:F2}, " +
                          $"pheroDir={pheromoneDir}, flowDir={flowDir}, final={moveDir}");
            }
        }
        
        // Store last direction for fallback
        if (moveDir.sqrMagnitude > 0.5f)
            lastMoveDir = moveDir;
        
        // Calculate raw moveDir as before
        
        // Apply smoothing to the movement direction
        if (Time.time > nextDirectionChangeTime)
        {
            // Only recalculate base direction periodically to avoid jitter
            nextDirectionChangeTime = Time.time + minDirectionChangeInterval;
            
            // Store this as our target direction, but don't apply immediately
            // Remove randomness from this cached direction so it's stable
            targetMoveDir = moveDir;
        }
        
        // Apply smoothing using SmoothDamp
        smoothedMoveDir = Vector2.SmoothDamp(
            smoothedMoveDir, 
            moveDir, 
            ref directionVelocity, 
            directionSmoothTime
        );
        
        // Move using the smoothed direction
        Vector3 smoothedMove = new Vector3(smoothedMoveDir.x, 0, smoothedMoveDir.y) * moveSpeed * Time.deltaTime;
        transform.position += smoothedMove;
        
        // Rotate to face movement direction - but only if we're actually moving
        if (smoothedMove.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(smoothedMove), 
                10f * Time.deltaTime
            );
        }
    }

    Vector2 GetPheromoneGradient(Vector2Int cell, int type)
    {
        if (pheromoneManager == null) return Vector2.zero;
        
        float cellValue = pheromoneManager.GetPheromone(cell, type);
        float best = cellValue;
        Vector2 bestDir = Vector2.zero;
        
        // Check all 8 neighbors for better pheromone values
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            Vector2Int neighbor = cell + new Vector2Int(dx, dy);
            if (!gridController.IsValidCell(neighbor.x, neighbor.y)) continue;
            
            float val = pheromoneManager.GetPheromone(neighbor, type);
            
            // Simply picks highest pheromone value with no smoothing
            if (val > best)
            {
                best = val;
                bestDir = new Vector2(dx, dy).normalized;
            }
        }
        
        return bestDir;
    }
    
    float GetMaxNeighborPheromone(Vector2Int cell, int type)
    {
        if (pheromoneManager == null) return 0f;
        
        float max = pheromoneManager.GetPheromone(cell, type);
        
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            
            Vector2Int n = cell + new Vector2Int(dx, dy);
            if (!gridController.IsValidCell(n.x, n.y)) continue;
            
            float val = pheromoneManager.GetPheromone(n, type);
            if (val > max) max = val;
        }
        
        return max;
    }

    private Transform SelectTarget()
    {
        if (priorityProfile == null) return null;

        // Sort priorities by ascending value (1 = highest)
        var sortedPriorities = priorityProfile.priorities.OrderBy(p => p.priority);

        foreach (var prio in sortedPriorities)
        {
            switch (prio.targetType)
            {
                case AITargetType.MainBuilding:
                    var main = StructureRegistry.GetMainBuilding();
                    if (main != null) return main.transform;
                    break;
                case AITargetType.DefenceStructure:
                    var defence = StructureRegistry.GetClosestStructureOfType(transform.position, AITargetType.DefenceStructure);
                    if (defence != null) return defence.transform;
                    break;
                case AITargetType.ResourceStructure:
                    var resource = StructureRegistry.GetClosestStructureOfType(transform.position, AITargetType.ResourceStructure);
                    if (resource != null) return resource.transform;
                    break;
                case AITargetType.PlayerUnit:
                    var playerUnit = UnitRegistry.GetClosestUnitOfType(transform.position, 9999f, UnitType.Civilian);
                    if (playerUnit != null) return playerUnit.transform;
                    break;
                // Explicitly handle flow field as fallback
                case AITargetType.FlowField:
                    // Return null to use flow field/pheromone fallback
                    return null;
            }
        }
        return null;
    }
    
    private void OnDrawGizmos()
    {
        if (!showDebug || !Application.isPlaying) return;
        
        // Draw path direction
        Gizmos.color = Color.yellow;
        Vector3 pos = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawLine(pos, pos + new Vector3(lastMoveDir.x, 0, lastMoveDir.y));
        
        // Show cell position
        Gizmos.color = Color.cyan;
        if (gridDataGenerator != null && currentCell.x >= 0)
        {
            GridCell gc = gridDataGenerator.GetCell(currentCell.x, currentCell.y);
            if (gc != null)
            {
                Gizmos.DrawWireCube(gc.worldPosition + Vector3.up * 0.05f, Vector3.one * 0.8f);
            }
        }
    }
}

