using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class ArmyUnit : BaseUnit
{
    [SerializeField] private ArmyData data;
    private float roamRadius;
    private float roamInterval;
    protected int currHealth;

    // Add protected getter for current health
    protected int CurrentHealth => currHealth;

    private float lastAttackTime = 0f;
    private EnemyUnit currentTarget;

    protected BarracksStructure barracks;
    private Vector3 guardPosition;
    private bool isNightTime;

    private Vector3 targetAttackPoint;
    private Transform target;

    private Vector3 targetPosition;
    private bool isMoving = false;

    private UnityEngine.AI.NavMeshAgent agent;

    private bool hasReachedDestination = false;

    private Vector3 roamCenter;
    private bool isRoaming = false;
    private Coroutine roamingRoutine = null;

    [SerializeField] private GameObject healthBarPrefab;
    private GameObject healthBarInstance;
    private Slider healthBarSlider;
    private TextMeshProUGUI healthBarText;
    private CanvasGroup healthBarCanvasGroup;

    // Health bar optimization - only update when health changes
    private int lastDisplayedHealth = -1;

    public bool shoot = false;
    [SerializeField] private float recoilDistance = 2f;  // how far back it moves
    [SerializeField] private float recoilTime = 0.2f;    // how fast it moves back
    private bool isRecoiling = false;
    private bool canShoot = true; // Only true when at the flag
    public bool attackNow = false;


    protected override void Awake()
    {
        base.Awake();
        TargetManager.Instance.RegisterTarget(this);
        CombatManager.Instance?.RegisterUnit(this); // Register with CombatManager for optimized combat checks
        currHealth = data.Health;
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        roamRadius = data.RoamRadius;
        roamInterval = data.RoamInterval;

        // Apply movement values from data
        agent.speed = data.MovementSpeed;
        agent.acceleration = data.Acceleration;
        agent.angularSpeed = data.AngularSpeed;
        agent.stoppingDistance = data.StoppingDistance;


        // Instantiate health bar but keep hidden
        if (healthBarPrefab != null && healthBarInstance == null)
        {
            healthBarInstance = Instantiate(healthBarPrefab, transform);
            var rect = healthBarInstance.GetComponent<RectTransform>();
            if (rect != null)
                rect.localPosition = new Vector3(0, 2.5f, 0); // Adjust Y as needed
            healthBarSlider = healthBarInstance.GetComponentInChildren<Slider>();
            healthBarText = healthBarInstance.GetComponentInChildren<TextMeshProUGUI>();
            healthBarCanvasGroup = healthBarInstance.GetComponentInChildren<CanvasGroup>();
            healthBarInstance.SetActive(false); // Start hidden
        }
    }

    protected void UpdateHealthBar()
    {
        // Only update if health actually changed - prevents unnecessary UI updates
        if (currHealth == lastDisplayedHealth)
            return;
            
        lastDisplayedHealth = currHealth;

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

    public void Update()
    {
        lastAttackTime += Time.deltaTime;
        
        // Attack flag system (triggered by CombatManager)
        if (attackNow)
        {
            Attack();
            attackNow = false;
        }

        // Face target if one exists
        if (currentTarget != null)
        {
            FaceTarget();
        }

        // Animation speed - cache velocity to avoid multiple property accesses
        if (!isRecoiling)
        {
            float velocitySqr = agent.velocity.sqrMagnitude;
            SetFloat("speed", velocitySqr > 0.1f ? 1f : 0f);
        }

        if (HasNotReachedDestination())
        {
            // Army unit is still on its way to the barracks
        }

        if (isMoving)
        {
            MoveToTargetPosition();  // Call every frame while moving
        }

        // Start roaming if idle, not moving, it's night, and no target
        if (!isMoving && currentTarget == null && roamingRoutine == null && isNightTime)
        {
            roamCenter = guardPosition;
            roamingRoutine = StartCoroutine(RoamAroundFlag());
        }
    }

    protected override UnitData GetData() => data;

    public virtual void Attack()
    {
        if (!canShoot || isRecoiling)
            return;
        // Debug.Log("it attacked 98475923459234985723498475982347598723498057293875982347598237459807234985723908475");
        // if (isReturningAfterAttack)
        // {
        //     return;
        // }

        //apply cooldown
        // if (Time.time < lastAttackTime + data.AttackCooldown)
        if (lastAttackTime < data.AttackCooldown && !IsDead())
        {
            return;
        }

        var enemies = GetNearbyEnemies();
        if (currentTarget == null || !enemies.Contains(currentTarget) || currentTarget.IsDead())
        {
            if (enemies.Count > 0)
            {
                currentTarget = enemies[0];

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

        lastAttackTime = 0f;

        canShoot = false;


        if (data.Type == ArmyType.Chicken)
        {
            if (!isRecoiling)
            {
                agent.enabled = false;
                // SetTrigger("Attack");
                StartCoroutine(RecoilAndReturnToFlag());
            }
        }
        else if (data.Type == ArmyType.Goat)
        {
            SetTrigger("Attack");
            // Delay impact for goats so it syncs with animation
            StartCoroutine(DelayedImpactAndReturn(0.8f));
        }
        else if (data.Type == ArmyType.Cow)
        {
            // SetTrigger("Attack");
            // StartCoroutine(DelayedImpactAndReturn(0.33f));
            StartCoroutine(CowAttack());
            // SetTrigger("Attack");
            // PerformAttackImpact();
            // For cows, sheep, pigs - perform attack and return to flag
            // StartCoroutine(AttackAndReturnToFlag());
        }
        else
        {
            PerformAttackImpact();
        }

        // PlaySound(data.AttackSound, 'a');

        // playVFX();

        // ApplyRecoil();
        // isReturningAfterAttack = true;

        // currentTarget.TakeDamage(data.AttackDamage);
    }

    // private IEnumerator CowAttackRoutine()
    // {
    //     if (isRecoiling) yield break;

    //     canShoot = false;
    //     agent.ResetPath();
    //     agent.isStopped = true; // stop movement
    //     agent.velocity = Vector3.zero;
    //     agent.enabled = false;

    //     if (data.AttackSound != null)
    //     {
    //         PlaySound(data.AttackSound, 'a');
    //     }

    //     // Trigger attack animation
    //     SetTrigger("Attack");

    //     // Remember last known target position
    //     Vector3 targetPos = currentTarget != null ? currentTarget.transform.position : transform.position + transform.forward * 3f;

    //     // Shoot VFX
    //     CowShootingVFX cowVFX = GetComponent<CowShootingVFX>();
    //     cowVFX.ShootCow(targetPos);

    //     currentTarget.TakeDamage(data.AttackDamage);

    //     // Stay still for 2.5 seconds
    //     yield return new WaitForSeconds(2.5f);

    //     // Unlock cow and move back to flag
    //     agent.enabled = true;
    //     agent.isStopped = false;
    //     canShoot = true;

    //     // Optional: set destination to flag
    //     if (guardPosition != Vector3.zero)
    //     {
    //         isMoving = true;
    //         agent.SetDestination(guardPosition);
    //     }
    // }

    // public IEnumerator CowAttack()
    // {
    //     if (isRecoiling) yield break;

    //     canShoot = false;

    //     // --- STOP MOVEMENT COMPLETELY ---
    //     agent.ResetPath();
    //     agent.isStopped = true;
    //     agent.velocity = Vector3.zero;
    //     agent.enabled = false;

    //     float attackDuration = 2.5f;
    //     float elapsed = 0f;
    //     float fireRate = 0.1f; // shoot every 0.1s

    //     // Trigger attack animation
    //     SetTrigger("Attack");

    //     // Loop fire for 2.5s
    //     while (elapsed < attackDuration && currentTarget != null && !currentTarget.IsDead())
    //     {
    //         // Play sound (limited by SoundLimiter)
    //         PlaySound(data.AttackSound, 'a');

    //         // Fire VFX toward target’s current position
    //         GetComponent<CowShootingVFX>().ShootCow(currentTarget.transform.position);
    //         currentTarget.TakeDamage(data.AttackDamage);

    //         yield return new WaitForSeconds(fireRate);
    //         elapsed += fireRate;
    //     }

    //     // --- UNLOCK MOVEMENT ---
    //     agent.enabled = true;
    //     agent.isStopped = false;
    //     canShoot = true;


    // }

    public IEnumerator CowAttack()
{
    if (isRecoiling) yield break;

    canShoot = false;
    agent.ResetPath();
    agent.isStopped = true;
    agent.velocity = Vector3.zero;
    agent.enabled = false;

    SetTrigger("Attack");

    float attackDuration = 2.5f;
    float fireRate = 0.1f;
    float elapsed = 0f;

    while (elapsed < attackDuration && currentTarget != null && !currentTarget.IsDead())
    {
        PlaySound(data.AttackSound, 'a');
        GetComponent<CowShootingVFX>().ShootCow(currentTarget.transform.position);
        currentTarget.TakeDamage(data.AttackDamage);

        elapsed += fireRate;
        yield return new WaitForSeconds(fireRate);
    }

    agent.enabled = true;
    agent.isStopped = false;
    canShoot = true;
}







    private void PerformAttackImpact()
    {
        // Check if target is still valid before VFX and damage
        if (currentTarget == null || !currentTarget.gameObject || !currentTarget.gameObject.activeInHierarchy)
        {
            // Debug.LogWarning($"Target invalid during attack impact for {gameObject.name}");
            return;
        }

        // Sound (restored to original timing)
        if (data.AttackSound != null)
        {
            PlaySound(data.AttackSound, 'a');
        }

        // VFX
        playVFX();

        // damage
        // Debug.Log("Chicken attacking " + currentTarget.name);
        currentTarget.TakeDamage(data.AttackDamage);

        // Record damage dealt for combat statistics
        if (CombatStatistics.Instance != null)
        {
            CombatStatistics.Instance.RecordDamageDealt(data.AttackDamage);
        }

        canShoot = true;
    }

    private IEnumerator DelayedImpact(float delay)
    {
        yield return new WaitForSeconds(delay);
        PerformAttackImpact();
    }

    //-------------------------------------------------------------------------------- best so far

    private IEnumerator RecoilAndReturnToFlag()
    {
        if (isRecoiling) yield break;  // prevent double start
        isRecoiling = true;
        canShoot = false;  // Prevent attacking until back at flag

        // Disable collider temporarily
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // --- Step 1: Deal damage & play VFX ---
        if (!IsDead() && currentTarget != null && !currentTarget.IsDead())
        {
            PerformAttackImpact();
            canShoot = false;
        }

        // --- Step 2: Trigger attack animation ---
        SetTrigger("Attack");

        // --- Step 3: Recoil backwards manually ---
        Vector3 startPos = transform.position;
        Vector3 endPos = transform.position - transform.forward * recoilDistance;

        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(endPos, out hit, 1f, UnityEngine.AI.NavMesh.AllAreas))
            endPos = hit.position;

        float elapsed = 0f;
        while (elapsed < recoilTime)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / recoilTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = endPos;

        agent.enabled = true;
        targetPosition = guardPosition;
        isMoving = true;
        agent.SetDestination(targetPosition); // make sure agent has the target

        // Keep moving until unit fully reaches flag
        while (Vector3.Distance(transform.position, guardPosition) > agent.stoppingDistance + 0.1f)
        {
            // Only reset destination if needed
            // Only reset destination if agent is valid
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                if (!agent.hasPath || Vector3.Distance(agent.destination, targetPosition) > 0.2f)
                {
                    agent.SetDestination(targetPosition);
                }
            }
            else
            {
                Debug.LogWarning($"{gameObject.name} agent not on NavMesh during return to flag");
                yield break; // stop coroutine if agent can't move
            }

            // if (!agent.hasPath || Vector3.Distance(agent.destination, targetPosition) > 0.2f)
            //     agent.SetDestination(targetPosition);

            // Smooth rotation toward movement
            if (agent.velocity.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(agent.velocity.normalized),
                    Time.deltaTime * 5f
                );
            }

            yield return null; // let agent move naturally
        }

        // --- Step 5: Reset state after reaching flag ---
        agent.ResetPath();
        isMoving = false;
        isRecoiling = false;
        canShoot = true; // NOW unit can attack again
        if (col != null) col.enabled = true;
    }


    private IEnumerator DelayedImpactAndReturn(float delay)
    {
        yield return new WaitForSeconds(delay);
        PerformAttackImpact();

        // After impact, return to flag
        // yield return StartCoroutine(ReturnToFlagAfterAttack());
    }

    private IEnumerator AttackAndReturnToFlag()
    {
        // Immediate impact for cows, sheep, pigs
        PerformAttackImpact();

        // Then return to flag
        yield return StartCoroutine(ReturnToFlagAfterAttack());
    }

    private IEnumerator ReturnToFlagAfterAttack()
    {
        // Wait a brief moment after attack
        yield return new WaitForSeconds(0.5f);

        // Start moving back to flag using NavMesh
        targetPosition = guardPosition;
        isMoving = true;

        // Ensure agent is enabled and on NavMesh
        if (!agent.isOnNavMesh)
        {
            agent.enabled = true;
            yield return new WaitUntil(() => agent.isOnNavMesh);
        }
        else
        {
            agent.enabled = true;
        }

        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        // while (Vector3.Distance(transform.position, guardPosition) > agent.stoppingDistance + 0.1f)
        // {
        MoveToFlag();
        // if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        // {
        //     if (!agent.hasPath || Vector3.Distance(agent.destination, targetPosition) > 0.2f)
        //         agent.SetDestination(targetPosition);
        // }

        // // Smooth rotation toward movement direction
        // if (agent.velocity.sqrMagnitude > 0.01f)
        // {
        //     Quaternion targetRot = Quaternion.LookRotation(agent.velocity.normalized);
        //     transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
        // }

        // yield return null;
        // }

        // Reached flag
        // if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        // {
        // agent.ResetPath();
        // }
        isMoving = false;
        canShoot = true;
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
        yield return new WaitForSeconds(delay);
        MoveToFlag();
    }

    private float lastNearbyCheckTime = 0f;
    private List<EnemyUnit> cachedNearbyEnemies = new List<EnemyUnit>();
    private float nearbyCheckInterval;

    private void Start()
    {
        PlayBackgroundSound(data.backgroundSound);
        nearbyCheckInterval = Mathf.Max(0.05f, 0.2f / (data.AttackRange / 10f)); // Shorter interval for larger ranges
    }

    public List<EnemyUnit> GetNearbyEnemies()
    {
        if (Time.time - lastNearbyCheckTime > nearbyCheckInterval)
        {
            cachedNearbyEnemies = GridController.Instance.GetEnemiesInRange(transform.position, data.AttackRange);
            lastNearbyCheckTime = Time.time;
        }
        return cachedNearbyEnemies;
    }

    public virtual void TakeDamage(int damage)
    {
        // Debug.Log("Army unit took damage: " + damage + "siedruhfgiowuehfiuwehfiuwehfiuwehiufhweiurfhiuehrfg");
        if (currHealth <= 0 || currHealth - damage <= 0)
        {
            // Record damage taken before death
            if (CombatStatistics.Instance != null)
            {
                CombatStatistics.Instance.RecordDamageTaken(damage);
            }

            PlaySound(data.DeathSound, 'd');
            currHealth = 0;
            UpdateHealthBar();
            barracks?.OnAnimalDied(this);
            handleDie();
        }
        else
        {
            // Record damage taken
            if (CombatStatistics.Instance != null)
            {
                CombatStatistics.Instance.RecordDamageTaken(damage);
            }

            PlaySound(data.HurtSound, 'h');
            currHealth -= damage;
            UpdateHealthBar();
        }
    }

    protected void handleDie()
    {
        // Record army unit loss for combat statistics
        if (CombatStatistics.Instance != null)
        {
            CombatStatistics.Instance.RecordArmyUnitLoss(data.Type.ToString());
        }

        TargetManager.Instance.UnregisterTarget(this);
        CombatManager.Instance?.UnregisterUnit(this); // Unregister from combat manager

        Die();
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

        if (!isNightTime)
        {
            // Daytime: stop fighting and run back
            currentTarget = null;
            StopRoaming();

            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.ResetPath();
            }

            BackToBarracks();   // force return to barracks
        }
        else
        {
            // Nighttime: go to the flag / defend
            MoveToFlag();
        }
    }

    public bool GetTimeOfDay()
    {
        return isNightTime;
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
        // if (isNightTime) return;
        if (barracks == null)
        {
            Debug.LogWarning("No barracks set for this unit.");
            return;
        }

        targetPosition = barracks.transform.position;
        isMoving = true;
        // isNightTime = false;
        MoveToTargetPosition();
    }




    private void MoveToTargetPosition()
    {
        // ADDED: Check if unit is valid before proceeding
        if (agent == null || !agent.isActiveAndEnabled || IsDead())
        {
            isMoving = false;
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("Army unit not on NavMesh.");
            isMoving = false;
            return;
        }

        float distance = Vector3.Distance(transform.position, targetPosition);

        if (distance < agent.stoppingDistance + 1.5f)
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
            isMoving = false;
            SetFloat("speed", 0f);
            if (!isNightTime)
            {
                barracks.AfterBackToBarracks();
                gameObject.SetActive(false);
            }
            return;
        }

        if (!agent.hasPath || Vector3.Distance(agent.destination, targetPosition) > 0.2f)
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.SetDestination(targetPosition);
            }
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

    protected virtual void OnDrawGizmos()
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

        // if (agent == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(guardPosition, roamRadius);

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(agent.transform.position, agent.stoppingDistance + 1f);
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

            // Check if unit is still valid
            if (!gameObject.activeInHierarchy || IsDead())
            {
                isRoaming = false;
                roamingRoutine = null;
                yield break;
            }

            // Check if agent is valid before setting destination
            if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
            {
                Debug.LogWarning($"The agent is null: {agent == null} and the is activeis: {agent.isActiveAndEnabled}");
                Debug.LogWarning($"NavMeshAgent invalid in RoamAroundFlag for {gameObject.name}");
                isRoaming = false;
                roamingRoutine = null;
                yield break;
            }

            Vector2 randomCircle = Random.insideUnitCircle * roamRadius;
            Vector3 roamPoint = roamCenter + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Set destination safely
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

            // ADDED: Safety check before ResetPath
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
        }
    }

    private void FaceTarget()
    {
        if (currentTarget == null) return;

        // Direction to the target (ignore Y if you don't want it tilting up/down)
        Vector3 direction = currentTarget.transform.position - transform.position;
        direction.y = 0f; // Keep only horizontal rotation

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f); // Smooth rotation
        }
    }

    public Vector3 GetTargetCenter(EnemyUnit enemy)
    {
        // Check if enemy is null or destroyed
        if (enemy == null || !enemy.gameObject || !enemy.isActiveAndEnabled)
        {
            // Return a fallback position if target is invalid
            return transform.position + transform.forward * 5f; // Some position in front of the unit
        }

        // Try to get the collider
        Collider col = enemy.GetComponent<Collider>();
        if (col != null)
        {
            return col.bounds.center; // Center of the bounding box (height + width + depth)
        }

        // Fallback: just aim 1 unit above base
        return enemy.transform.position + Vector3.up * 1f;
    }

    private void playVFX()
    {
        // Check if target is valid before playing VFX
        if (currentTarget == null || !currentTarget.gameObject || !currentTarget.gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"Skipping VFX for {gameObject.name} - target is invalid");
            return;
        }

        if (data.Type == ArmyType.Chicken)
        {
            ShootingVFX shootingVFX = GetComponent<ShootingVFX>();

            Vector3 targetPosition = GetTargetCenter(currentTarget);

            shootingVFX.Shoot(targetPosition);
            // return;
        }
        else if (data.Type == ArmyType.Cow)
        {
            CowShootingVFX cowShootingVFX = GetComponent<CowShootingVFX>();

            Vector3 targetPosition = GetTargetCenter(currentTarget);

            cowShootingVFX.ShootCow(targetPosition);
            // return;
        }
        else if (data.Type == ArmyType.Goat)
        {
            GoatShootingVFX goatShootingVFX = GetComponent<GoatShootingVFX>();

            Vector3 targetPosition = GetTargetCenter(currentTarget);

            goatShootingVFX.ShootSniper(targetPosition);
            // return;
        }
        else if (data.Type == ArmyType.Pig)
        {
            PigFlameVFX pigFlameVFX = GetComponent<PigFlameVFX>();

            Vector3 targetPosition = GetTargetCenter(currentTarget);

            pigFlameVFX.Shoot(targetPosition);
            // return;
        }
        else
        {

        }
    }
}



