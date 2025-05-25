using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ArmyAnimal : MonoBehaviour
{
    [Header("Animal Configuration")]
    [SerializeField] private AnimalStructure.AnimalType animalType;
    [SerializeField] private AnimalDatabase animalDatabase;
    private AnimalData animalData;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkAnimParam = "IsWalking";
    [SerializeField] private string attackAnimParam = "Attack";

    [Header("Audio")]
    [SerializeField] private AudioSource attackAudioSource;
    [SerializeField] private AudioSource moveAudioSource;
    [SerializeField] private AudioSource deathAudioSource;
    [SerializeField] private AudioSource ambientAudioSource;

    [Header("Movement Settings")]
    [SerializeField] private float separationRadius = 1.5f; // Distance to keep from other animals
    [SerializeField] private float separationForce = 2f; // Strength of separation push
    [SerializeField] private float targetOffsetRadius = 1f; // Random offset for target positions
    [SerializeField] private int maxAnimalsPerEnemy = 2; // Max animals targeting one enemy

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
    private float lastMoveSoundTime;
    private int currentHealth;
    private float nextAmbientSoundTime;

    public AnimalStructure.AnimalType AnimalType => animalType;

    private void Awake()
    {
        if (animalDatabase == null)
        {
            Debug.LogError($"CRITICAL ERROR: {name} has no AnimalDatabase assigned in Inspector");
            Destroy(gameObject);
            return;
        }

        animalData = animalDatabase.GetAnimalByType(animalType);
        if (animalData == null)
        {
            Debug.LogError($"CRITICAL ERROR: {name} could not find AnimalData for type {animalType} in AnimalDatabase");
            Destroy(gameObject);
            return;
        }

        if (!animalData.targetAnimalType.Equals(animalType.ToString(), System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning($"Animal {name} type {animalType} does not match AnimalData targetAnimalType {animalData.targetAnimalType}");
        }

        AudioSource[] audioSources = GetComponents<AudioSource>();
        if (audioSources.Length < 4)
        {
            Debug.LogError($"CRITICAL ERROR: {name} requires at least 4 AudioSource components, found {audioSources.Length}");
            Destroy(gameObject);
            return;
        }

        attackAudioSource = audioSources[0];
        moveAudioSource = audioSources[1];
        deathAudioSource = audioSources[2];
        ambientAudioSource = audioSources[3];

        ConfigureAudioSource(attackAudioSource, "Attack");
        ConfigureAudioSource(moveAudioSource, "Move");
        ConfigureAudioSource(deathAudioSource, "Death");
        ConfigureAudioSource(ambientAudioSource, "Ambient");

        attackAudioSource.clip = animalData.attackClips != null && animalData.attackClips.Length > 0 ? animalData.attackClips[0] : null;
        moveAudioSource.clip = animalData.moveClips != null && animalData.moveClips.Length > 0 ? animalData.moveClips[0] : null;
        deathAudioSource.clip = animalData.deathClip;

        if (attackAudioSource.clip == null)
            Debug.LogWarning($"Attack AudioSource on {name} has no clip assigned. Check AnimalData for {animalType}.");
        if (moveAudioSource.clip == null)
            Debug.LogWarning($"Move AudioSource on {name} has no clip assigned. Check AnimalData for {animalType}.");
        if (deathAudioSource.clip == null)
            Debug.LogWarning($"Death AudioSource on {name} has no clip assigned. Check AnimalData for {animalType}.");
        if (animalData.ambientClips == null || animalData.ambientClips.Length == 0)
            Debug.LogWarning($"Ambient AudioSource on {name} has no clips assigned. Check AnimalData for {animalType}.");

        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
                Debug.LogWarning($"{name} No Animator assigned");
        }
    }

    private void ConfigureAudioSource(AudioSource source, string sourceName)
    {
        if (source == null) return;
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.volume = 1f;
        source.minDistance = 1f;
        source.maxDistance = 20f;
        Debug.Log($"{name} Configured {sourceName} AudioSource: Volume={source.volume}, SpatialBlend={source.spatialBlend}");
    }

    private void Start()
    {
        currentHealth = animalData.maxHealth;
        guardPosition = transform.position;
        guardRadius = 5f;
        MoveToFlag();
        SetTimeOfDay(true);
        nextAmbientSoundTime = Time.time + Random.Range(animalData.ambientSoundDelayMin, animalData.ambientSoundDelayMax);
        Debug.Log($"{name} Started: Type={animalData.animalType}, NightTime={isNightTime}");
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

        if (!isAttackingEnemy && Time.time >= nextAmbientSoundTime)
        {
            PlayAmbientSound();
            nextAmbientSoundTime = Time.time + Random.Range(animalData.ambientSoundDelayMin, animalData.ambientSoundDelayMax);
        }

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
            if (animalData.debugAttacks)
                Debug.Log($"{name} Distance to {enemy.name}: {distance:F2}, AttackRange={animalData.attackRange}, DetectionRange={animalData.detectionRange}");

            if (distance > animalData.attackRange)
            {
                isMoving = true;
                if (animator != null)
                    animator.SetBool(walkAnimParam, true);
                PlayMoveSound();

                // Add offset to enemy position to avoid clustering
                Vector3 offset = Random.insideUnitCircle * targetOffsetRadius;
                Vector3 targetEnemyPos = enemy.transform.position + new Vector3(offset.x, 0, offset.y);
                Vector3 direction = targetEnemyPos - transform.position;
                direction.y = 0;
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, animalData.rotationSpeed * Time.deltaTime);
                
                // Apply separation during movement
                Vector3 separation = CalculateSeparation();
                Vector3 moveDirection = (direction.normalized + separation).normalized;
                transform.position += moveDirection * animalData.moveSpeed * Time.deltaTime;
            }
            else
            {
                StopMovement();
                if (Time.time >= lastAttackTime + animalData.attackCooldown)
                {
                    AttackEnemy(enemy);
                    lastAttackTime = Time.time;
                }
            }

            if (enemy == null || !enemy.activeInHierarchy || distance > animalData.detectionRange * 1.5f)
            {
                if (animalData.debugAttacks)
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
        if (barracks != null && barracks.StructureData != null && barracks.StructureData.protectionRadius > 0)
        {
            guardRadius = barracks.StructureData.protectionRadius;
        }
        else
        {
            guardRadius = 5f;
            Debug.LogWarning($"BarracksStructure {barracks?.name} has no valid StructureData or protectionRadius, using default guardRadius={guardRadius}");
        }
        Debug.Log($"{name} SetBarracks: {barracks?.GetStructureName()}, GuardRadius={guardRadius}");
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
            nextAmbientSoundTime = Time.time + Random.Range(animalData.ambientSoundDelayMin, animalData.ambientSoundDelayMax);
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
            barracks.ReturnAnimal(this);
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, animalData.rotationSpeed * Time.deltaTime);
        transform.position += transform.forward * animalData.moveSpeed * Time.deltaTime;
        PlayMoveSound();
    }

    public void MoveToFlag()
    {
        // Add random offset to flag position
        Vector2 offset = Random.insideUnitCircle * targetOffsetRadius;
        targetPosition = guardPosition + new Vector3(offset.x, 0, offset.y);
        isMoving = true;
        if (animator != null)
            animator.SetBool(walkAnimParam, true);
        PlayMoveSound();
        Debug.Log($"{name} Moving to flag at {targetPosition} with offset ({offset.x:F2}, {offset.y:F2})");
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
        // Add random offset to avoid clustering
        Vector2 offset = Random.insideUnitCircle * targetOffsetRadius;
        targetPosition += new Vector3(offset.x, 0, offset.y);
        isMoving = true;
        if (animator != null)
            animator.SetBool(walkAnimParam, true);
        PlayMoveSound();
        Debug.Log($"{name} Picked new target at {targetPosition} with offset ({offset.x:F2}, {offset.y:F2})");
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

        // Apply separation force
        Vector3 separation = CalculateSeparation();
        Vector3 moveDirection = (direction.normalized + separation).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, animalData.rotationSpeed * Time.deltaTime);
        transform.position += moveDirection * animalData.moveSpeed * Time.deltaTime;
        PlayMoveSound();
    }

    private Vector3 CalculateSeparation()
    {
        Vector3 separation = Vector3.zero;
        int nearbyCount = 0;

        Collider[] colliders = Physics.OverlapSphere(transform.position, separationRadius);
        foreach (Collider col in colliders)
        {
            if (col.gameObject == gameObject) continue;
            ArmyAnimal otherAnimal = col.GetComponent<ArmyAnimal>();
            if (otherAnimal != null)
            {
                Vector3 direction = transform.position - col.transform.position;
                float distance = direction.magnitude;
                if (distance < separationRadius && distance > 0)
                {
                    separation += direction.normalized / distance; // Stronger push when closer
                    nearbyCount++;
                }
            }
        }

        if (nearbyCount > 0)
        {
            separation /= nearbyCount;
            separation *= separationForce;
            Debug.Log($"{name} Applying separation force: {separation.magnitude:F2} from {nearbyCount} neighbors");
        }

        return separation;
    }

    private void PlayMoveSound()
    {
        if (moveAudioSource == null)
        {
            Debug.LogWarning($"{name} Move AudioSource is null");
            return;
        }
        if (animalData.moveClips == null || animalData.moveClips.Length == 0)
        {
            Debug.LogWarning($"{name} No move clips assigned in AnimalData for {animalType}");
            return;
        }
        if (Time.time < lastMoveSoundTime + animalData.moveSoundDelay)
            return;
        if (Random.value > animalData.moveSoundChance)
            return;

        AudioClip clip = animalData.moveClips[Random.Range(0, animalData.moveClips.Length)];
        if (clip == null)
        {
            Debug.LogWarning($"{name} Selected move clip is null for {animalType}");
            return;
        }

        moveAudioSource.clip = clip;
        moveAudioSource.pitch = Random.Range(animalData.minPitch, animalData.maxPitch);
        moveAudioSource.Play();
        lastMoveSoundTime = Time.time;
        Debug.Log($"{name} Playing move sound: {clip.name}, Pitch={moveAudioSource.pitch}");
    }

    private void PlayAmbientSound()
    {
        if (ambientAudioSource == null)
        {
            Debug.LogWarning($"{name} Ambient AudioSource is null");
            return;
        }
        if (animalData.ambientClips == null || animalData.ambientClips.Length == 0)
        {
            Debug.LogWarning($"{name} No ambient clips assigned in AnimalData for {animalType}");
            return;
        }
        if (Random.value > animalData.ambientSoundChance)
            return;

        AudioClip clip = animalData.ambientClips[Random.Range(0, animalData.ambientClips.Length)];
        if (clip == null)
        {
            Debug.LogWarning($"{name} Selected ambient clip is null for {animalType}");
            return;
        }

        ambientAudioSource.clip = clip;
        ambientAudioSource.pitch = Random.Range(animalData.minPitch, animalData.maxPitch);
        ambientAudioSource.Play();
        Debug.Log($"{name} Playing ambient sound: {clip.name}, Pitch={ambientAudioSource.pitch}");
    }

    private GameObject FindNearestEnemy()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, animalData.detectionRange);
        GameObject closestEnemy = null;
        float closestDistance = animalData.detectionRange;

        if (animalData.debugAttacks)
            Debug.Log($"{name} Checking enemies in {animalData.detectionRange} range. Found {colliders.Length} colliders");

        foreach (Collider col in colliders)
        {
            if (col == null) continue;

            Wolf wolf = col.GetComponent<Wolf>();
            if (wolf != null && wolf.gameObject.activeInHierarchy)
            {
                float distance = Vector3.Distance(transform.position, wolf.transform.position);
                int assignedCount = CountAnimalsTargeting(wolf.gameObject);
                if (animalData.debugAttacks)
                    Debug.Log($"{name} Found wolf {wolf.name} at distance {distance:F2}, Assigned={assignedCount}, Layer={LayerMask.LayerToName(col.gameObject.layer)}");
                if (distance < closestDistance && assignedCount < maxAnimalsPerEnemy)
                {
                    closestDistance = distance;
                    closestEnemy = wolf.gameObject;
                }
            }
        }

        if (closestEnemy != null)
        {
            if (animalData.debugAttacks)
                Debug.Log($"{name} Selected closest wolf: {closestEnemy.name} at {closestDistance:F2}");
        }
        else if (animalData.debugAttacks)
        {
            Debug.Log($"{name} No wolves found in range or all targets assigned");
        }

        return closestEnemy;
    }

    private int CountAnimalsTargeting(GameObject enemy)
    {
        int count = 0;
        ArmyAnimal[] animals = FindObjectsOfType<ArmyAnimal>();
        foreach (ArmyAnimal animal in animals)
        {
            if (animal != this && animal.currentEnemy == enemy)
            {
                count++;
            }
        }
        return count;
    }

    public void AttackEnemy(GameObject enemy)
    {
        if (enemy == null || !enemy.activeInHierarchy) return;

        Debug.Log($"{name} Attacking {enemy.name}");
        if (animator != null)
            animator.SetTrigger(attackAnimParam);

        if (Random.value <= animalData.attackSoundChance && attackAudioSource != null && animalData.attackClips != null && animalData.attackClips.Length > 0)
        {
            AudioClip clip = animalData.attackClips[Random.Range(0, animalData.attackClips.Length)];
            if (clip != null)
            {
                attackAudioSource.clip = clip;
                attackAudioSource.pitch = Random.Range(animalData.minPitch, animalData.maxPitch);
                attackAudioSource.Play();
                Debug.Log($"{name} Playing attack sound: {clip.name}, Pitch={attackAudioSource.pitch}");
            }
            else
            {
                Debug.LogWarning($"{name} Selected attack clip is null for {animalType}");
            }
        }

        Wolf wolf = enemy.GetComponent<Wolf>();
        if (wolf != null)
        {
            wolf.TakeDamage(animalData.damage);
            if (animalData.debugAttacks)
                Debug.Log($"⚔️ {name} Hit {wolf.name} for {animalData.damage} damage (direct)");
        }
        else
        {
            enemy.SendMessage("TakeDamage", animalData.damage, SendMessageOptions.DontRequireReceiver);
            if (animalData.debugAttacks)
                Debug.Log($"⚔️ {name} Hit {enemy.name} for {animalData.damage} damage (SendMessage)");
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log($"{name} Took {amount} damage. Health: {currentHealth}/{animalData.maxHealth}");

        if (currentHealth > 0 && attackAudioSource != null && animalData.attackClips != null && animalData.attackClips.Length > 0)
        {
            if (Random.value <= animalData.attackSoundChance)
            {
                AudioClip clip = animalData.attackClips[Random.Range(0, animalData.attackClips.Length)];
                if (clip != null)
                {
                    attackAudioSource.clip = clip;
                    attackAudioSource.pitch = Random.Range(animalData.minPitch, animalData.maxPitch);
                    attackAudioSource.Play();
                    Debug.Log($"{name} Playing pain sound: {clip.name}, Pitch={attackAudioSource.pitch}");
                }
                else
                {
                    Debug.LogWarning($"{name} Selected pain clip is null for {animalType}");
                }
            }
        }

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);

        if (deathAudioSource != null && animalData.deathClip != null)
        {
            deathAudioSource.clip = animalData.deathClip;
            deathAudioSource.pitch = Random.Range(animalData.minPitch, animalData.maxPitch);
            deathAudioSource.Play();
            Debug.Log($"{name} Playing death sound: {animalData.deathClip.name}, Pitch={deathAudioSource.pitch}");
        }
        else
        {
            Debug.LogWarning($"{name} Cannot play death sound: deathAudioSource={deathAudioSource}, deathClip={(animalData.deathClip != null ? animalData.deathClip.name : "null")}");
        }

        Debug.Log($"{name} Died");
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        if (animalData == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, animalData.attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, animalData.detectionRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(guardPosition, guardRadius);
        if (isMoving)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.2f); // Show target point
        }
        if (currentEnemy != null && currentEnemy.activeInHierarchy)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, currentEnemy.transform.position);
        }
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, separationRadius); // Show separation radius
    }
}