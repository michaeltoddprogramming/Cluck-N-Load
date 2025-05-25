using UnityEngine;
using System.Collections;

public class ArmyAnimal : MonoBehaviour
{
    [Header("Animal Settings")]
    [SerializeField] private AnimalStructure.AnimalType animalType = AnimalStructure.AnimalType.Chicken;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 3f; // Increased for reliability
    [SerializeField] private float detectionRange = 20f; // Increased for detection
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private bool debugAttacks = true; // Enabled for debugging

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkAnimParam = "IsWalking";
    [SerializeField] private string attackAnimParam = "Attack";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] attackSounds;
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.2f;
    [SerializeField] private float attackSoundChance = 0.8f;

    // Internal variables
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
    private GameObject currentEnemy;
    private bool isAttackingEnemy = false;
    private Coroutine attackCoroutine;

    [Header("Health")]
    [SerializeField] private int maxHealth = 50; // Increased for balance
    private int currentHealth;

    public AnimalStructure.AnimalType AnimalType => animalType;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            Debug.Log($"{name} Added AudioSource");
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
                Debug.LogWarning($"{name} No Animator assigned");
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
        guardPosition = transform.position;
        guardRadius = 5f;
        MoveToFlag();
        SetTimeOfDay(true); // Force night for testing
        Debug.Log($"{name} Started: Type={animalType}, NightTime={isNightTime}");
    }

    private void Update()
    {
        Debug.Log($"{name} Update: NightTime={isNightTime}, Attacking={isAttackingEnemy}, Enemy={(currentEnemy != null ? currentEnemy.name : "None")}");
        if (isReturningToBarracks)
        {
            ReturnToBarracks();
            return;
        }

        if (!isNightTime)
        {
            Debug.Log($"{name} Skipping update: Not night time");
            return;
        }

        if (barracks != null)
        {
            guardPosition = barracks.GetFlagPosition;
        }

        idleTimer -= Time.deltaTime;

        if (!isAttackingEnemy)
        {
            GameObject enemy = FindNearestEnemy();
            if (enemy != null && enemy.activeInHierarchy)
            {
                currentEnemy = enemy;
                isAttackingEnemy = true;
                StopMovement();
                if (attackCoroutine != null)
                    StopCoroutine(attackCoroutine);
                attackCoroutine = StartCoroutine(AttackEnemyRoutine(enemy));
                return;
            }
        }

        if (!isAttackingEnemy)
        {
            if (!isMoving && idleTimer <= 0)
            {
                PickNewTargetPosition();
                idleTimer = idleInterval;
            }
            MoveToTarget();
        }
    }

    private IEnumerator AttackEnemyRoutine(GameObject enemy)
    {
        Debug.Log($"{name} Started attacking {enemy.name}");
        while (enemy != null && enemy.activeInHierarchy)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (debugAttacks)
                Debug.Log($"{name} Distance to {enemy.name}: {distance:F2}, AttackRange={attackRange}, DetectionRange={detectionRange}");

            if (distance > attackRange)
            {
                isMoving = true;
                if (animator != null)
                    animator.SetBool(walkAnimParam, true);

                Vector3 direction = enemy.transform.position - transform.position;
                direction.y = 0;
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                transform.position += transform.forward * moveSpeed * Time.deltaTime;
            }
            else
            {
                StopMovement();
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    AttackEnemy(enemy);
                    lastAttackTime = Time.time;
                }
            }

            if (enemy == null || !enemy.activeInHierarchy || distance > detectionRange * 1.5f)
            {
                if (debugAttacks)
                    Debug.Log($"{name} Lost enemy: Null={enemy == null}, Active={enemy?.activeInHierarchy ?? false}, Distance={distance:F2}");
                break;
            }

            yield return null;
        }

        isAttackingEnemy = false;
        currentEnemy = null;
        MoveToFlag();
        attackCoroutine = null;
        Debug.Log($"{name} Stopped attacking");
    }

    private void StopMovement()
    {
        isMoving = false;
        if (animator != null)
            animator.SetBool(walkAnimParam, false);
    }

    public void SetBarracks(BarracksStructure barracksStructure)
    {
        barracks = barracksStructure;
        guardPosition = barracks != null ? barracks.GetFlagPosition : transform.position;
        Debug.Log($"{name} SetBarracks: {barracks?.GetStructureName()}");
    }

    public void SetGuardPosition(Vector3 position, float radius)
    {
        guardPosition = position;
        guardRadius = radius;
        if (isNightTime)
            MoveToFlag();
    }

    public void SetTimeOfDay(bool isNight)
    {
        isNightTime = isNight;
        if (isNight)
        {
            gameObject.SetActive(true);
            MoveToFlag();
            isReturningToBarracks = false;
            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
                attackCoroutine = null;
                isAttackingEnemy = false;
                currentEnemy = null;
            }
            Debug.Log($"{name} Set to night");
        }
        else
        {
            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
                attackCoroutine = null;
            }
            isAttackingEnemy = false;
            currentEnemy = null;
            gameObject.SetActive(false);
            isReturningToBarracks = false;
            Debug.Log($"{name} Set to day");
        }
    }

    public void StartReturnToBarracks()
    {
        if (barracks == null)
        {
            Destroy(gameObject);
            return;
        }

        if (isNightTime)
            return;

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        isAttackingEnemy = false;
        currentEnemy = null;

        isReturningToBarracks = true;
        targetPosition = barracks.transform.position;
        isMoving = true;

        if (animator != null)
            animator.SetBool(walkAnimParam, true);
    }

    private void ReturnToBarracks()
    {
        if (!isReturningToBarracks || barracks == null)
            return;

        if (isNightTime)
        {
            isReturningToBarracks = false;
            MoveToFlag();
            return;
        }

        Vector3 direction = targetPosition - transform.position;
        direction.y = 0;

        if (direction.magnitude < 1.5f)
        {
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
            animator.SetBool(walkAnimParam, true);
    }

    private void PickNewTargetPosition()
    {
        if (Vector3.Distance(transform.position, guardPosition) > guardRadius)
        {
            targetPosition = guardPosition;
        }
        else
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * guardRadius;
            targetPosition = guardPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
        }
        isMoving = true;
        if (animator != null)
            animator.SetBool(walkAnimParam, true);
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
                animator.SetBool(walkAnimParam, false);
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }

    private GameObject FindNearestEnemy()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange);
        GameObject closestEnemy = null;
        float closestDistance = detectionRange;

        if (debugAttacks)
            Debug.Log($"{name} Checking enemies in {detectionRange} range. Found {colliders.Length} colliders");

        foreach (Collider col in colliders)
        {
            if (col == null) continue;

            Wolf wolf = col.GetComponent<Wolf>();
            if (wolf != null && wolf.gameObject.activeInHierarchy)
            {
                float distance = Vector3.Distance(transform.position, wolf.transform.position);
                if (debugAttacks)
                    Debug.Log($"{name} Found wolf {wolf.name} at distance {distance:F2}, Layer={LayerMask.LayerToName(col.gameObject.layer)}");
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = wolf.gameObject;
                }
            }
        }

        if (closestEnemy != null)
        {
            if (debugAttacks)
                Debug.Log($"{name} Selected closest wolf: {closestEnemy.name} at {closestDistance:F2}");
        }
        else if (debugAttacks)
        {
            Debug.Log($"{name} No wolves found in range");
        }

        return closestEnemy;
    }

    public void AttackEnemy(GameObject enemy)
    {
        if (enemy == null || !enemy.activeInHierarchy) return;

        Debug.Log($"{name} Attacking {enemy.name}");
        if (animator != null)
            animator.SetTrigger(attackAnimParam);

        if (UnityEngine.Random.value <= attackSoundChance && attackSounds != null && attackSounds.Length > 0)
        {
            AudioClip clip = attackSounds[UnityEngine.Random.Range(0, attackSounds.Length)];
            if (clip != null && audioSource != null)
            {
                audioSource.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
                audioSource.PlayOneShot(clip);
            }
        }

        Wolf wolf = enemy.GetComponent<Wolf>();
        if (wolf != null)
        {
            wolf.TakeDamage(damage);
            if (debugAttacks)
                Debug.Log($"⚔️ {name} Hit {wolf.name} for {damage} damage (direct)");
        }
        else
        {
            enemy.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            if (debugAttacks)
                Debug.Log($"⚔️ {name} Hit {enemy.name} for {damage} damage (SendMessage)");
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log($"{name} Took {amount} damage. Health: {currentHealth}/{maxHealth}");

        if (attackSounds != null && attackSounds.Length > 0 && audioSource != null)
        {
            AudioClip clip = attackSounds[UnityEngine.Random.Range(0, attackSounds.Length)];
            if (clip != null)
            {
                audioSource.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
                audioSource.PlayOneShot(clip);
            }
        }

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);
        Debug.Log($"{name} Died");
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(guardPosition, guardRadius);
        if (isMoving)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetPosition);
        }
        if (currentEnemy != null && currentEnemy.activeInHierarchy)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, currentEnemy.transform.position);
        }
    }
}