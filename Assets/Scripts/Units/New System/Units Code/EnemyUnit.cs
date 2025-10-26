using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class EnemyUnit : BaseUnit
{
    [SerializeField] private EnemyData data;

    private int currHealth;
    private float lastAttackTime = 0f;
    private MonoBehaviour currentTarget;
    private GridDataGenerator _gridDataGenerator;
    private float stoppingDistance;
    private UnityEngine.AI.NavMeshAgent agent;
    private Vector3 currentAttackPosition;

    [SerializeField] private GameObject healthBarPrefab;
    private GameObject healthBarInstance;
    private Slider healthBarSlider;
    private TextMeshProUGUI healthBarText;
    private CanvasGroup healthBarCanvasGroup;

    public int currentMaxSpawn;
    public int currentMinSpawn;

    public bool retreating = false;
    private Vector3 destination;

    // Jumping
    [SerializeField] private float jumpHeight = 20f;
    [SerializeField] private float jumpDuration = 0.5f;
    
    private MonoBehaviour mainTarget;
    private MonoBehaviour obstacleTarget;

    private float destinationUpdateThreshold = 0.3f;
    private Vector3 lastDestination;

    private List<MonoBehaviour> allTargets = new List<MonoBehaviour>();

    // OPTIMIZATION: Throttle target rechecking to reduce CPU usage
    private float targetRecheckInterval = 0.2f; // Check target validity every 0.2 seconds instead of every frame
    private float lastTargetRecheckTime = 0f;

    private Vector3 flag;
    private bool reachedFlag = false;

    protected override void Awake()
    {
        base.Awake();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        
        if (agent == null)
        {
            Debug.LogError($"{name}: NavMeshAgent component missing!");
            return;
        }

        currHealth = data.Health;
        _gridDataGenerator = FindFirstObjectByType<GridDataGenerator>();

        // Apply movement values from data
        agent.speed = data.MovementSpeed;
        agent.acceleration = data.Acceleration;
        agent.angularSpeed = data.AngularSpeed;
        agent.stoppingDistance = data.StoppingDistance;

        stoppingDistance = data.StoppingDistance;

        PlayBackgroundSound(data.backgroundSound);

        CacheAllTargets();

        // Health bar setup
        if (healthBarPrefab != null && healthBarInstance == null)
        {
            healthBarInstance = Instantiate(healthBarPrefab, transform);
            var rect = healthBarInstance.GetComponent<RectTransform>();
            if (rect != null)
                rect.localPosition = new Vector3(0, 2.5f, 0);
            healthBarSlider = healthBarInstance.GetComponentInChildren<Slider>();
            healthBarText = healthBarInstance.GetComponentInChildren<TextMeshProUGUI>();
            healthBarCanvasGroup = healthBarInstance.GetComponentInChildren<CanvasGroup>();
            healthBarInstance.SetActive(false);
        }
    }

    private void Start()
    {
        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning($"{name} is not on NavMesh at start!");
        }
        
        currentTarget = GetNearestAggroTargetOptimized();
    }

    private void Update()
    {
        if (agent == null || !agent.isOnNavMesh)
            return;

        // Animation based on velocity
        if (agent.velocity.sqrMagnitude > 0.1f)
            SetFloat("speed", 1f);
        else
            SetFloat("speed", 0f);

        lastAttackTime += Time.deltaTime;

        // Handle retreating
        if (retreating)
        {
            if (Vector3.Distance(transform.position, destination) < 5f)
            {
                Destroy(gameObject);
            }
            return;
        }

        // Update target if dead or null - but not every frame (optimization)
        if (Time.time - lastTargetRecheckTime > targetRecheckInterval)
        {
            if (currentTarget == null || IsTargetDead(currentTarget))
            {
                currentTarget = GetNearestAggroTargetOptimized();
            }
            lastTargetRecheckTime = Time.time;
        }

        // No target found
        if (currentTarget == null)
        {
            agent.ResetPath();
            return;
        }

        HandleTargetingAndMovement();
        AttackIfInRange();
    }

    private void CacheAllTargets()
    {
        // OPTIMIZATION: Use TargetManager instead of FindObjectsByType
        // This eliminates 7 expensive scene scans per enemy spawn
        // Old code was causing 350+ scene scans when 50 enemies spawned!
        
        if (TargetManager.Instance != null)
        {
            allTargets.Clear();
            allTargets.AddRange(TargetManager.Instance.GetAllTargets());
        }
        else
        {
            Debug.LogWarning("TargetManager not found - enemy targeting may not work correctly");
        }
    }

    protected override UnitData GetData() => data;

    private bool IsTargetEffectivelyReachable(MonoBehaviour target)
    {
        if (target == null) return false;

        Collider targetCollider = target.GetComponent<Collider>();
        Vector3 desiredPoint = target.transform.position;

        if (targetCollider != null)
        {
            desiredPoint = targetCollider.ClosestPoint(transform.position);
            if (desiredPoint == Vector3.zero)
                desiredPoint = target.transform.position;
        }

        UnityEngine.AI.NavMeshHit hit;
        float sampleRadius = 1.5f;
        bool foundSample = UnityEngine.AI.NavMesh.SamplePosition(desiredPoint, out hit, 0.5f, agent.areaMask);

        if (!foundSample)
        {
            Vector3[] offsets = {
                Vector3.zero,
                Vector3.forward * 0.5f,
                Vector3.back * 0.5f,
                Vector3.left * 0.5f,
                Vector3.right * 0.5f,
                Vector3.up * 0.2f
            };

            foreach (var off in offsets)
            {
                if (UnityEngine.AI.NavMesh.SamplePosition(desiredPoint + off, out hit, sampleRadius, agent.areaMask))
                {
                    foundSample = true;
                    break;
                }
            }
        }

        if (!foundSample)
            return false;

        UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
        agent.CalculatePath(hit.position, path);

        if (path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
            return true;

        if (path.status == UnityEngine.AI.NavMeshPathStatus.PathPartial && path.corners != null && path.corners.Length > 0)
        {
            Vector3 lastCorner = path.corners[path.corners.Length - 1];
            float distToTargetPoint = Vector3.Distance(lastCorner, hit.position);
            float buffer = 0.5f;
            if (distToTargetPoint <= agent.stoppingDistance + buffer)
                return true;
        }

        return false;
    }

    private void HandleTargetingAndMovement()
    {
        if (!agent.isOnNavMesh) return;

        // Ensure we have a main target
        if (mainTarget == null || IsTargetDead(mainTarget))
        {
            mainTarget = GetNearestAggroTargetOptimized();
            if(mainTarget != null)
            {
                if(mainTarget is ArmyUnit army)
                {
                    flag = army.GetGuardPosition();
                    reachedFlag = false;
                }
                else
                {
                    flag = Vector3.zero;
                    reachedFlag = true;
                    // flag = null;
                }
            }

            obstacleTarget = null;
            if (mainTarget == null)
            {
                agent.ResetPath();
                return;
            }
        }

        // Check if the main target is reachable
        bool targetReachable = IsTargetEffectivelyReachable(mainTarget);

        // If not reachable, find the blocking object
        if (!targetReachable)
        {
            obstacleTarget = GetBlockingObjectDirect();
        }
        else
        {
            obstacleTarget = null;
        }


        if (mainTarget is ArmyUnit && flag != null && !reachedFlag)
        {
            float flagDist = Vector3.Distance(transform.position, flag);

            // Go to the flag first
            if (flagDist > 5f)
            {
                agent.SetDestination(flag);
                return; // stop here — don’t switch to the unit yet
            }
            else
            {
                reachedFlag = true; // we’re close enough, switch to attacking the army unit now
            }
        }

        // Decide what to move toward
        currentTarget = obstacleTarget != null ? obstacleTarget : mainTarget;

        if (currentTarget == null) return;

        Vector3 targetPos = currentTarget.transform.position;

        // Only update destination if it moved far enough
        if ((targetPos - lastDestination).sqrMagnitude > destinationUpdateThreshold * destinationUpdateThreshold)
        {
            agent.SetDestination(targetPos);
            lastDestination = targetPos;
        }
    }

    private MonoBehaviour GetBlockingObjectDirect()
    {
        if (mainTarget == null) return null;

        Vector3 directionToTarget = (mainTarget.transform.position - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, mainTarget.transform.position);

        Vector3[] offsets = { Vector3.zero, Vector3.left * 0.5f, Vector3.right * 0.5f };

        foreach (var offset in offsets)
        {
            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f + offset;
            Debug.DrawRay(rayOrigin, directionToTarget * distanceToTarget, Color.red, 0.5f);

            if (Physics.Raycast(rayOrigin, directionToTarget, out RaycastHit hit, distanceToTarget))
            {
                // Check each structure type
                if (hit.collider.GetComponent<DefenseStructure>() != null)
                    return hit.collider.GetComponent<DefenseStructure>();
                else if (hit.collider.GetComponent<AnimalStructure>() != null)
                    return hit.collider.GetComponent<AnimalStructure>();
                else if (hit.collider.GetComponent<BarracksStructure>() != null)
                    return hit.collider.GetComponent<BarracksStructure>();
                else if (hit.collider.GetComponent<CropStructure>() != null)
                    return hit.collider.GetComponent<CropStructure>();
                else if (hit.collider.GetComponent<FarmHouseStructure>() != null)
                    return hit.collider.GetComponent<FarmHouseStructure>();
                else if (hit.collider.GetComponent<Structure>() != null)
                    return hit.collider.GetComponent<Structure>();
            }
        }

        return null;
    }

    private IEnumerator JumpOver(Collider wall)
    {
        agent.enabled = false;

        Vector3 startPos = transform.position;
        Vector3 endPos = wall.transform.position + wall.transform.forward * 1f;
        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            float t = elapsed / jumpDuration;
            transform.position = Vector3.Lerp(startPos, endPos, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * jumpHeight;
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
        agent.enabled = true;
    }

    private void AttackIfInRange()
    {
        if (currentTarget == null || IsTargetDead(currentTarget)) return;

        Collider enemyCollider = GetComponent<Collider>();
        Collider targetCollider = currentTarget.GetComponent<Collider>();
        
        float distToTarget;

        if (enemyCollider != null && targetCollider != null)
        {
            // Edge-to-edge distance using both colliders
            Vector3 closestPointOnEnemy = enemyCollider.ClosestPoint(targetCollider.transform.position);
            Vector3 closestPointOnTarget = targetCollider.ClosestPoint(closestPointOnEnemy);
            distToTarget = Vector3.Distance(closestPointOnEnemy, closestPointOnTarget);
        }
        else if (targetCollider != null)
        {
            // Only target has collider - use enemy center to target surface
            Vector3 closestPointOnTarget = targetCollider.ClosestPoint(transform.position);
            distToTarget = Vector3.Distance(transform.position, closestPointOnTarget);
        }
        else
        {
            // Fallback: center-to-center
            distToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
        }

        // Use the stopping distance from data as the attack range
        if (distToTarget <= data.StoppingDistance && lastAttackTime >= data.AttackCooldown && !IsDead())
        {
            lastAttackTime = 0f;
            Attack(currentTarget);
        }
    }

    private MonoBehaviour GetNearestAggroTargetOptimized()
    {
        if (TargetManager.Instance == null)
        {
            Debug.LogWarning("TargetManager.Instance is null!");
            return null;
        }
        
        // Pass instance ID for caching optimization
        return TargetManager.Instance.GetNearestAggroTargetOptimized(data, transform.position, GetInstanceID());
    }

    private void UpdateHealthBar()
    {
        if (healthBarSlider != null)
        {
            float pct = Mathf.Clamp01((float)currHealth / data.Health);
            healthBarSlider.value = pct;
            if (healthBarText != null)
                healthBarText.text = $"{currHealth} / {data.Health}";

            bool showBar = currHealth < data.Health && currHealth > 0;
            if (healthBarCanvasGroup != null)
            {
                healthBarCanvasGroup.alpha = showBar ? 1f : 0f;
                healthBarCanvasGroup.interactable = showBar;
                healthBarCanvasGroup.blocksRaycasts = showBar;
            }
            if (healthBarInstance != null)
                healthBarInstance.SetActive(showBar);
        }
    }

    public void TakeDamage(int damage)
    {
        if (currHealth <= 0)
            return;

        if (currHealth - damage <= 0)
        {
            PlaySound(data.DeathSound, 'd');
            currHealth = 0;
            UpdateHealthBar();
            Die();
        }
        else
        {
            PlaySound(data.HurtSound, 'h');
            currHealth -= damage;
            UpdateHealthBar();
        }
    }

    protected override void Die()
    {
        // Record enemy defeat for combat statistics
        if (CombatStatistics.Instance != null)
        {
            CombatStatistics.Instance.RecordEnemyDefeated(data.Type.ToString());
        }

        base.Die();
    }

    public bool IsDead()
    {
        return currHealth <= 0;
    }

    private bool IsTargetDead(MonoBehaviour target)
    {
        if (target == null) return true;

        return target switch
        {
            ArmyUnit u => u.IsDead(),
            CropStructure u => u.IsDead(),
            SiloStructure u => u.IsDead(),
            DefenseStructure u => u.IsDead(),
            BarracksStructure u => u.IsDead(),
            AnimalStructure u => u.IsDead(),
            Structure u => u.IsDead(),
            _ => true
        };
    }

    private void Attack(MonoBehaviour target)
    {
        if (target == null) return;

        SetTrigger("Attack");
        PlaySound(data.AttackSound, 'a');

        // Record damage dealt for combat statistics
        if (CombatStatistics.Instance != null)
        {
            CombatStatistics.Instance.RecordDamageDealt(data.AttackDamage);
        }

        // Apply damage based on target type
        switch (target)
        {
            case ArmyUnit u:
                u.TakeDamage(data.AttackDamage);
                break;
            case CropStructure u:
                u.TakeDamage(data.AttackDamage);
                TryPlayDamageEffect(u);
                break;
            case SiloStructure u:
                u.TakeDamage(data.AttackDamage);
                TryPlayDamageEffect(u);
                break;
            case DefenseStructure u:
                u.TakeDamage(data.AttackDamage);
                TryPlayDamageEffect(u);
                break;
            case BarracksStructure u:
                u.TakeDamage(data.AttackDamage);
                TryPlayDamageEffect(u);
                break;
            case AnimalStructure u:
                u.TakeDamage(data.AttackDamage);
                TryPlayDamageEffect(u);
                break;
            case Structure u:
                u.TakeDamage(data.AttackDamage);
                TryPlayDamageEffect(u);
                break;
        }
    }

    private void TryPlayDamageEffect(MonoBehaviour target)
    {
        DamageAnimation anim = target.GetComponent<DamageAnimation>();
        if (anim != null)
            anim.PlayDamageHitEffect();
    }

    private Vector3 GetRandomOutsidePosition()
    {
        if (_gridDataGenerator == null)
        {
            Debug.LogError("GridDataGenerator is null!");
            return transform.position;
        }

        int width = _gridDataGenerator.GetGridWidth();
        int height = _gridDataGenerator.GetGridHeight();

        if (width <= 0 || height <= 0)
        {
            Debug.LogError("Invalid grid dimensions!");
            return transform.position;
        }

        int maxRetries = 100;
        int retries = 0;

        while (retries < maxRetries)
        {
            retries++;

            int side = Random.Range(0, 4);
            int x = 0;
            int y = 0;

            switch (side)
            {
                case 0: // Top edge
                    x = Random.Range(0, width);
                    y = height - 1;
                    break;
                case 1: // Right edge
                    x = width - 1;
                    y = Random.Range(0, height);
                    break;
                case 2: // Bottom edge
                    x = Random.Range(0, width);
                    y = 0;
                    break;
                case 3: // Left edge
                    x = 0;
                    y = Random.Range(0, height);
                    break;
            }

            GridCell cell = _gridDataGenerator.GetCell(x, y);

            if (cell != null && !cell.flags.isObstacle && !cell.flags.isOccupied)
            {
                return cell.worldPosition;
            }
        }

        // Fallback: return corner position
        GridCell fallbackCell = _gridDataGenerator.GetCell(width - 1, height - 1);
        if (fallbackCell != null)
            return fallbackCell.worldPosition;

        return transform.position;
    }

    public void stopCombat()
    {
        retreating = true;
        currentTarget = null;
        mainTarget = null;
        obstacleTarget = null;
        
        destination = GetRandomOutsidePosition();
        

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning($"{name} not on NavMesh, cannot retreat properly!");
            Destroy(gameObject);
            return;
        }

        agent.SetDestination(destination);

        // Force despawn after delay
        StartCoroutine(DelayedDespawn(10f));
    }

    private IEnumerator DelayedDespawn(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (retreating && gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}

