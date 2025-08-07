using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class ArmyUnit : BaseUnit
{
    [SerializeField] private ArmyData data;
    [SerializeField] private float recoilForce = 20f;
    private float roamRadius;
    private float roamInterval;
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

    private bool isReturningToFlag = false;
    [SerializeField] private bool Recoil = false;
    private bool isReturningAfterAttack = false;
    private Vector3 roamCenter;
    private bool isRoaming = false;
    private Coroutine roamingRoutine = null;


    protected override void Awake()
    {
        base.Awake();
        currHealth = data.Health;
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        roamRadius = data.RoamRadius;
        roamInterval = data.RoamInterval;

        // Apply movement values from data
        agent.speed = data.MovementSpeed;
        agent.acceleration = data.Acceleration;
        agent.angularSpeed = data.AngularSpeed;
        agent.stoppingDistance = data.StoppingDistance;


        PlayBackgroundSound(data.backgroundSound);
    }

    private void Update()
    {
        if (Recoil)
        {
            ApplyRecoil();
            Recoil = false;
        }
        // Debug.Log($"{agent.velocity.sqrMagnitude} -------------------------------------------------------------------------------------------------------------------------");
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            SetBool("isWalking", true);
        }
        else
        {
            SetBool("isWalking", false);
        }


        if (HasNotReachedDestination())
        {
            // Debug.Log("Army unit is still on its way to the barracks.____________++++++++++++++++++===============================");
        }

        if (isMoving)
        {
            MoveToTargetPosition();  // Call every frame while moving
        }

        // Start roaming if idle, not moving, it's night, and no target
        if (!isMoving && isNightTime && currentTarget == null && roamingRoutine == null)
        {
            roamCenter = guardPosition;
            roamingRoutine = StartCoroutine(RoamAroundFlag());
        }
    }

    protected override UnitData GetData() => data;

    public void Attack()
    {
        if (isReturningAfterAttack)
        {
            return;
        }

        //apply cooldown
        if (Time.time < lastAttackTime + data.AttackCooldown)
        {
            return;
        }

        if (currentTarget == null || currentTarget.IsDead())
        {
            var enemies = GetNearbyEnemies();
            if (enemies.Count > 0)
            {
                currentTarget = enemies[0]; // TODO: Add smarter target selection later------------------------------------------------------------------

                if (roamingRoutine != null)
                {
                    StopCoroutine(roamingRoutine);
                    roamingRoutine = null;
                    isRoaming = false;
                }
            }
            else
            {
                return; // No enemies to attack
            }
        }

        lastAttackTime = Time.time;
        PlaySound(data.AttackSound);

        ApplyRecoil();
        isReturningAfterAttack = true;

        currentTarget.TakeDamage(data.AttackDamage);
    }

    // private void ApplyRecoil()
    // {
    //     Rigidbody rb = GetComponent<Rigidbody>();

    //     if (rb != null)
    //     {
    //         Vector3 recoilDirection = -transform.forward;
    //         float recoilForce = 3f;

    //         rb.AddForce(recoilDirection * recoilForce, ForceMode.Impulse);

    //         if (!isReturningToFlag)
    //             StartCoroutine(ReturnToFlagAfterRecoil(1f)); 
    //     }
    // }


    private void ApplyRecoil()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            Vector3 recoilDirection = -transform.forward;

            agent.enabled = false;
            rb.isKinematic = false;  // enable physics
            transform.position += new Vector3(0, 1f, 0);
            rb.AddForce(recoilDirection * recoilForce, ForceMode.Impulse);

            StartCoroutine(ReenableAgentWhenStopped(rb));
            if (!isReturningToFlag)
                StartCoroutine(ReturnToFlagAfterRecoil(0.1f));
        }
    }

    private IEnumerator ReenableAgentWhenStopped(Rigidbody rb)
    {
        // Wait until velocity is low (almost stopped)
        yield return new WaitUntil(() => rb.linearVelocity.magnitude < 0.1f);

        rb.isKinematic = true;   // disable physics
        agent.enabled = true;
    }

    private IEnumerator ReturnToFlagAfterRecoil(float delay)
    {
        isReturningToFlag = true;
        yield return new WaitForSeconds(delay);
        MoveToFlag();
        isReturningToFlag = false;
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

        // roamCenter = position;
        // roamRadius = radius;

        // // Pick a random point within a circle (on XZ plane)
        // // Vector2 randomOffset = Random.insideUnitCircle * radius;
        // guardPosition = new Vector3(position.x + randomOffset.x, position.y, position.z + randomOffset.y);

    }

    public void SetTimeOfDay(bool isNight)
    {
        isNightTime = isNight;
    }

    public void MoveToFlag()
    {
        if (!isNightTime) return;

        StopRoaming();

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
            Debug.Log("16531278634568124598761263458762347895628371465 87231 59723459721349750 2309745 609273456 50972365 0978236 5097235490 762390745 5629307465 09723465 907");
            if (isReturningAfterAttack)
            {
                isReturningAfterAttack = false;
            }

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
            // SetBool("isWalking", true);
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

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(guardPosition, roamRadius);
#endif
    }

    public ArmyType GetArmyType()
    {
        return data.Type;
    }

    private IEnumerator RoamAroundFlag()
    {
        isRoaming = true;

        while (isRoaming && isNightTime && currentTarget == null)
        {
            yield return new WaitForSeconds(Random.Range(0.5f, 1.5f)); // Small wait before moving

            Vector2 randomCircle = Random.insideUnitCircle * roamRadius;
            Vector3 roamPoint = roamCenter + new Vector3(randomCircle.x, 0, randomCircle.y);
            agent.SetDestination(roamPoint);

            yield return new WaitForSeconds(roamInterval);
        }

        roamingRoutine = null;
        isRoaming = false;
    }

    private void StopRoaming()
    {
        if (roamingRoutine != null)
        {
            StopCoroutine(roamingRoutine);
            roamingRoutine = null;
            isRoaming = false;
            agent.ResetPath();
        }
    }
}