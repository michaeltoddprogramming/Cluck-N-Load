using System.Collections.Generic;
using UnityEngine;

public class ArmyUnit : BaseUnit
{
    [SerializeField] private ArmyData data;
    private int currHealth;
    private float lastAttackTime = 0f;
    private EnemyUnit currentTarget;

    private BarracksStructure barracks;
    private Vector3 guardPosition;
    private bool isNightTime;

    private Vector3 targetAttackPoint;
    private Transform target;

    private Vector3 targetPosition;
    private bool isMoving = false;

    private UnityEngine.AI.NavMeshAgent agent;

    private bool hasReachedDestination = false;


    protected override void Awake()
    {
        base.Awake();
        currHealth = data.Health;
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        // Apply movement values from data
        agent.speed = data.MovementSpeed;
        agent.acceleration = data.Acceleration;
        agent.angularSpeed = data.AngularSpeed;
        agent.stoppingDistance = data.StoppingDistance;
    }

    private void Update()
    {
        if (HasNotReachedDestination())
        {
            Debug.Log("Army unit is still on its way to the barracks.____________++++++++++++++++++===============================");
        }
    }

    protected override UnitData GetData() => data;

    public void Attack()
    {
        //apply cooldown
        if (Time.time < lastAttackTime + data.AttackCooldown)
        {
            return;
        }

        if (currentTarget == null || currentTarget.IsDead())
        {
            var enemies = GetNearbyEnemies();
            if (enemies.Count > 0)
                currentTarget = enemies[0]; // TODO: Add smarter target selection later------------------------------------------------------------------
            else
                return; // No enemies to attack
        }

        lastAttackTime = Time.time;
        PlaySound(data.AttackSound);

        currentTarget.TakeDamage(data.AttackDamage);
    }

    public List<EnemyUnit> GetNearbyEnemies()
    {
        return GridController.Instance.GetEnemiesInRange(transform.position, data.AttackRange);
    }

    public void TakeDamage(int damage)
    {
        if (currHealth <= 0 || currHealth - damage <= 0)
        {
            PlaySound(data.DeathSound);
            Die();
        }
        else
        {
            PlaySound(data.HurtSound);
            currHealth -= damage;
        }
    }

    public bool IsDead()
    {
        return currHealth <= 0;
    }

    public void SetBarracks(BarracksStructure source)
    {
        barracks = source;
        guardPosition = barracks.GetFlagPosition;
    }

    public void SetGuardPosition(Vector3 position, float radius)
    {
        guardPosition = position;
    }

    public void SetTimeOfDay(bool isNight)
    {
        isNightTime = isNight;
    }

    public void MoveToFlag()
    {
        if (!isNightTime) return;

        targetPosition = guardPosition;
        isMoving = true;

        MoveToTargetPosition();
    }

    public void BackToBarracks()
    {
        if (isNightTime) return;

        if (barracks == null)
        {
            Debug.LogWarning("No barracks set for this unit.");
            return;
        }

        targetPosition = barracks.transform.position;
        isMoving = true;
        isNightTime = false;
        MoveToTargetPosition();
    }
    



    private void MoveToTargetPosition()
    {
        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("Army unit not on NavMesh.");
            return;
        }

        float distance = Vector3.Distance(transform.position, targetPosition);

        if (distance < agent.stoppingDistance + 0.1f)
        {
            agent.ResetPath();
            isMoving = false;
            if (!isNightTime)
            {
                barracks.AfterBackToBarracks();
                gameObject.SetActive(false);
            }
            return;
        }

        if (!agent.hasPath || Vector3.Distance(agent.destination, targetPosition) > 0.2f)
        {
            agent.SetDestination(targetPosition);
        }

        // Optional: Smooth manual rotation (if needed)
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
        }

        hasReachedDestination = !agent.pathPending
        && agent.remainingDistance <= agent.stoppingDistance
        && (!agent.hasPath || agent.velocity.sqrMagnitude == 0f);
    }

    private bool HasNotReachedDestination()
    {
        if (Vector3.Distance(transform.position, targetPosition) <= 3f && !isNightTime)
        {
            gameObject.SetActive(false);
            return true;
        }
        return false;
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (data == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.AttackRange);

        // Gizmos.color = Color.yellow;
        // Gizmos.DrawWireSphere(transform.position, data.DetectionRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(guardPosition, 1f); // You can expose guard radius if needed

        if (isMoving)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.2f);
        }

        if (currentTarget != null && currentTarget.gameObject.activeInHierarchy)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }
#endif
    }

    public ArmyType GetArmyType()
    {
        return data.Type;
    }
}