using UnityEngine;

public class ArmyAnimal : MonoBehaviour
{
    [SerializeField] private AnimalStructure.AnimalType animalType = AnimalStructure.AnimalType.Chicken;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private int damage = 5;
    [SerializeField] private float attackCooldown = 1.5f;

    [SerializeField] private Animator animator;
    [SerializeField] private string walkAnimParam = "IsWalking";
    [SerializeField] private string attackAnimParam = "Attack";

    private Vector3 guardPosition;
    private float guardRadius;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private float lastAttackTime;
    private BarracksStructure barracks;
    private float idleTimer;
    private float idleInterval = 2f;
    private bool isNightTime = false;
    private bool isReturningToBarracks = false;

    public AnimalStructure.AnimalType AnimalType => animalType;

    private void Start()
    {
        guardPosition = transform.position;
        guardRadius = 5f;
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        MoveToFlag();
        Debug.Log($"{name} Started at Time={Time.time:F2}, Active={gameObject.activeSelf}");
    }

    private void Update()
    {
        if (isReturningToBarracks)
        {
            ReturnToBarracks();
            return;
        }

        if (!isNightTime)
        {
            return;
        }

        if (barracks != null)
        {
            guardPosition = barracks.GetFlagPosition;
        }

        idleTimer -= Time.deltaTime;

        GameObject enemy = FindNearestEnemy();
        if (enemy != null && Vector3.Distance(transform.position, enemy.transform.position) <= attackRange)
        {
            AttackEnemy(enemy);
            return;
        }

        if (!isMoving && idleTimer <= 0)
        {
            PickNewTargetPosition();
            idleTimer = idleInterval;
        }

        MoveToTarget();
    }

    public void SetBarracks(BarracksStructure barracksStructure)
    {
        barracks = barracksStructure;
        guardPosition = barracks != null ? barracks.GetFlagPosition : transform.position;
        Debug.Log($"{name} SetBarracks: {barracks?.GetStructureName()}, GuardPosition={guardPosition} at Time={Time.time:F2}");
    }

    public void SetGuardPosition(Vector3 position, float radius)
    {
        guardPosition = position;
        guardRadius = radius;
        if (isNightTime)
        {
            MoveToFlag();
        }
        Debug.Log($"{name} Set to guard flag at {position} with radius {radius} at Time={Time.time:F2}");
    }

    public void SetTimeOfDay(bool isNight)
    {
        isNightTime = isNight;
        if (isNight)
        {
            gameObject.SetActive(true); // Ensure active at night
            MoveToFlag();
            isReturningToBarracks = false; // Stop any return
        }
        else
        {
            gameObject.SetActive(false); // Deactivate during day
            isReturningToBarracks = false; // Prevent return movement
        }
        Debug.Log($"{name} SetTimeOfDay: isNight={isNight}, Active={gameObject.activeSelf}, isReturningToBarracks={isReturningToBarracks} at Time={Time.time:F2}");
    }

    public void StartReturnToBarracks()
    {
        if (barracks == null)
        {
            Debug.LogWarning($"{name} No barracks to return to, destroying at Time={Time.time:F2}");
            Destroy(gameObject);
            return;
        }

        if (isNightTime)
        {
            Debug.LogWarning($"{name} StartReturnToBarracks blocked during night at Time={Time.time:F2}");
            return; // Prevent return during night
        }

        isReturningToBarracks = true;
        targetPosition = barracks.transform.position;
        isMoving = true;

        if (animator != null)
        {
            animator.SetBool(walkAnimParam, true);
        }

        Debug.Log($"{name} Returning to barracks at {targetPosition} at Time={Time.time:F2}");
    }

    private void ReturnToBarracks()
    {
        if (!isReturningToBarracks || barracks == null)
        {
            Debug.LogWarning($"{name} ReturnToBarracks aborted: isReturning={isReturningToBarracks}, barracks={(barracks != null)} at Time={Time.time:F2}");
            return;
        }

        if (isNightTime)
        {
            Debug.LogWarning($"{name} ReturnToBarracks stopped due to night at Time={Time.time:F2}");
            isReturningToBarracks = false;
            MoveToFlag();
            return;
        }

        Vector3 direction = targetPosition - transform.position;
        direction.y = 0;

        if (direction.magnitude < 1.5f)
        {
            Debug.Log($"{name} Reached barracks, returning to duty at Time={Time.time:F2}");
            barracks.ReturnChicken(this);
            Destroy(gameObject);
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }

    public void MoveToFlag()
    {
        targetPosition = guardPosition;
        isMoving = true;
        if (animator != null)
        {
            animator.SetBool(walkAnimParam, true);
        }
        Debug.Log($"{name} Moving to flag at {guardPosition} at Time={Time.time:F2}");
    }

    private void PickNewTargetPosition()
    {
        if (Vector3.Distance(transform.position, guardPosition) > guardRadius)
        {
            targetPosition = guardPosition;
        }
        else
        {
            Vector2 randomCircle = Random.insideUnitCircle * guardRadius;
            targetPosition = guardPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
        }
        isMoving = true;
        if (animator != null)
        {
            animator.SetBool(walkAnimParam, true);
        }
    }

    private void MoveToTarget()
    {
        if (!isMoving) return;

        Vector3 direction = targetPosition - transform.position;
        direction.y = 0;

        if (direction.magnitude < 0.3f)
        {
            isMoving = false;
            if (animator != null)
            {
                animator.SetBool(walkAnimParam, false);
            }
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }

    private GameObject FindNearestEnemy()
    {
        return null; // Placeholder
    }

    public void AttackEnemy(GameObject enemy)
    {
        if (Time.time < lastAttackTime + attackCooldown) return;
        if (animator != null)
        {
            animator.SetTrigger(attackAnimParam);
        }
        enemy?.SendMessage("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);
        Debug.Log($"{name} Attacked {enemy?.name} for {damage} damage at Time={Time.time:F2}");
        lastAttackTime = Time.time;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(guardPosition, guardRadius);
        if (isMoving)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }
}