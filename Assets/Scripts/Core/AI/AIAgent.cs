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

    public PheromoneManager pheromoneManager;
    public FlowFieldManager flowFieldManager;
    public GridController gridController;
    public GridDataGenerator gridDataGenerator;
    public AIPriorityProfile priorityProfile;

    void Start()
    {
        if (pheromoneManager == null) pheromoneManager = FindObjectOfType<PheromoneManager>();
        if (flowFieldManager == null) flowFieldManager = FindObjectOfType<FlowFieldManager>();
        if (gridController == null) gridController = FindObjectOfType<GridController>();
        if (gridDataGenerator == null) gridDataGenerator = FindObjectOfType<GridDataGenerator>();
    }

    void Update()
    {
        Transform target = SelectTarget();
        Vector2Int cell = gridController.WorldToGridCoords(transform.position);

        Vector2 moveDir;
        if (target != null)
        {
            Vector3 toTarget = (target.position - transform.position);
            moveDir = new Vector2(toTarget.x, toTarget.z).normalized;
        }
        else
        {
            // Fallback to pheromone/flow field logic
            Vector2 pheromoneDir = GetPheromoneGradient(cell, (int)enemyType);
            Vector2 flowDir = gridDataGenerator.GetCell(cell.x, cell.y).flowDirection;
            moveDir = pheromoneDir * pheromoneFollowWeight + flowDir * flowFieldWeight;
            if (moveDir.sqrMagnitude < 0.01f)
                moveDir = flowDir;
            moveDir += Random.insideUnitCircle * randomSteer;
            moveDir.Normalize();
        }

        Vector3 move = new Vector3(moveDir.x, 0, moveDir.y) * moveSpeed * Time.deltaTime;
        transform.position += move;
    }

    Vector2 GetPheromoneGradient(Vector2Int cell, int type)
    {
        float best = pheromoneManager.GetPheromone(cell, type);
        Vector2 bestDir = Vector2.zero;
        foreach (var offset in new Vector2Int[] {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
        {
            Vector2Int n = cell + offset;
            if (gridController.IsValidCell(n.x, n.y))
            {
                float val = pheromoneManager.GetPheromone(n, type);
                if (val > best)
                {
                    best = val;
                    bestDir = new Vector2(offset.x, offset.y);
                }
            }
        }
        return bestDir.normalized;
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
                    var playerUnit = UnitRegistry.GetClosestUnitOfType(transform.position, 9999f, UnitType.Civilian); // or UnitType.Military
                    if (playerUnit != null) return playerUnit.transform;
                    break;
                case AITargetType.FlowField:
                    // Fallback: use flow field, no explicit target
                    return null;
            }
        }
        return null;
    }
}

