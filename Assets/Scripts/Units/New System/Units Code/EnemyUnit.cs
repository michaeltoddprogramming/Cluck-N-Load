
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
    private float stoppingDistance; // Change based on attack range
    private UnityEngine.AI.NavMeshAgent agent;
    private Vector3 currentAttackPosition;
    private bool hasAttackPosition = false;
    private float attackPositionUpdateThreshold = 0.3f; // minimum movement distance to update
    private float targetSearchCooldown = 0.5f;
    private float lastTargetSearchTime = 0f;
    private bool hasNoTarget = false;

    [SerializeField] private GameObject healthBarPrefab;
    private GameObject healthBarInstance;
    private Slider healthBarSlider;
    private TextMeshProUGUI healthBarText;
    private CanvasGroup healthBarCanvasGroup;

    public int currentMaxSpawn;

    public int currentMinSpawn;

    public bool retreating = false;
    private Vector3 destination;


    //jumping
    [SerializeField] private float jumpCheckDistance = 1.5f;
    [SerializeField] private float jumpHeight = 20f;
    [SerializeField] private float jumpDuration = 0.5f;
    // [SerializeField] private bool dead = false;
    private MonoBehaviour mainTarget; // the building
    // private MonoBehaviour obstacleTarget; // wall temporarily
    private MonoBehaviour obstacleTarget; // wall temporarily

    private float destinationUpdateThreshold = 0.3f; // minimum distance to update destination
    private Vector3 lastDestination;

    private List<MonoBehaviour> allTargets = new List<MonoBehaviour>();

    // protected override void Awake()
    // {
    //     base.Awake();
    //     agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    //     currHealth = data.Health;
    //     _gridDataGenerator = FindObjectOfType<GridDataGenerator>();
    //     // navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    //     // HandleTargetingAndMovement();
    //     // AttackIfInRange();
    // }

    // protected override void Awake()
    // {
    //     base.Awake();
    //     agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    //     currHealth = data.Health;
    //     _gridDataGenerator = FindObjectOfType<GridDataGenerator>();

    //     // Apply movement values from data
    //     agent.speed = data.MovementSpeed;
    //     agent.acceleration = data.Acceleration;
    //     agent.angularSpeed = data.AngularSpeed;
    //     agent.stoppingDistance = data.StoppingDistance;

    //     PlayBackgroundSound(data.backgroundSound);

    //     // Instantiate health bar but keep hidden
    //     if (healthBarPrefab != null && healthBarInstance == null)
    //     {
    //         healthBarInstance = Instantiate(healthBarPrefab, transform);
    //         var rect = healthBarInstance.GetComponent<RectTransform>();
    //         if (rect != null)
    //             rect.localPosition = new Vector3(0, 2.5f, 0); // Adjust Y as needed
    //         healthBarSlider = healthBarInstance.GetComponentInChildren<Slider>();
    //         healthBarText = healthBarInstance.GetComponentInChildren<TextMeshProUGUI>();
    //         healthBarCanvasGroup = healthBarInstance.GetComponentInChildren<CanvasGroup>();
    //         healthBarInstance.SetActive(false); // Start hidden
    //     }
    // }

    protected override void Awake()
    {
        base.Awake();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        currHealth = data.Health;
        _gridDataGenerator = FindObjectOfType<GridDataGenerator>();

        // Apply movement values from data
        agent.speed = data.MovementSpeed;
        agent.acceleration = data.Acceleration;
        agent.angularSpeed = data.AngularSpeed;
        agent.stoppingDistance = data.StoppingDistance;

        stoppingDistance = data.StoppingDistance;

        PlayBackgroundSound(data.backgroundSound);

        CacheAllTargets();

        // Health bar setup (unchanged)
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
        currentTarget = GetNearestAggroTargetOptimized();
    }


    // public void Update()
    // {
    //     HandleTargetingAndMovement();
    //     AttackIfInRange();
    // }

    private void Update()
    {
        // Debug.Log("------------------------------------------------------------------ target: " + currentTarget);
        if (retreating)
        {
            if (retreating && Vector3.Distance(transform.position, destination) < 5f)
            {
                Destroy(gameObject);
                return;
            }
            return;
        }

        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            // Moving → set Speed to 1
            SetFloat("speed", 1f);
        }
        else
        {
            // Idle → set Speed to 0
            SetFloat("speed", 0f);
        }

        if (currentTarget == null || IsTargetDead(currentTarget))
        {
            currentTarget = GetNearestAggroTargetOptimized();
        }

        HandleTargetingAndMovement();
        AttackIfInRange();

        // currentMaxSpawn = data.maxSpawnAmount;
        // currentMinSpawn = data.minSpawnAmount;



        // HandleTargetingAndMovement();


        // if (Time.time - lastTargetSearchTime > targetSearchCooldown || currentTarget == null || IsTargetDead(currentTarget))
        // {
        //     currentTarget = GetNearestAggroTargetOptimized();
        //     lastTargetSearchTime = Time.time;
        // }

        if (currentTarget == null)
        {
            agent.ResetPath();
            return;
        }

        // HandleTargetingAndMovement();
        // AttackIfInRange();


    }

    private void CacheAllTargets()
    {
        allTargets.Clear();
        allTargets.AddRange(FindObjectsOfType<ArmyUnit>());
        allTargets.AddRange(FindObjectsOfType<CropStructure>());
        allTargets.AddRange(FindObjectsOfType<SiloStructure>());
        allTargets.AddRange(FindObjectsOfType<DefenseStructure>());
        allTargets.AddRange(FindObjectsOfType<BarracksStructure>());
        allTargets.AddRange(FindObjectsOfType<AnimalStructure>());
        allTargets.AddRange(FindObjectsOfType<Structure>()); // farm house etc
    }


    protected override UnitData GetData() => data;



    private bool IsTargetEffectivelyReachable(MonoBehaviour target)
    {
        if (target == null) return false;

        Collider targetCollider = target.GetComponent<Collider>();
        Vector3 desiredPoint = target != null ? target.transform.position : transform.position;

        // Prefer closest point on collider (so we try to path to the actual closest surface)
        if (targetCollider != null)
        {
            desiredPoint = targetCollider.ClosestPoint(transform.position);
            // If ClosestPoint is the same as transform (no collider), fallback to transform.position
            if (desiredPoint == Vector3.zero)
                desiredPoint = target.transform.position;
        }

        // Try to sample the navmesh at/around the desired point
        UnityEngine.AI.NavMeshHit hit;
        float sampleRadius = 1.5f; // try small radius first, increase if needed
        bool foundSample = UnityEngine.AI.NavMesh.SamplePosition(desiredPoint, out hit, 0.5f, agent.areaMask);

        if (!foundSample)
        {
            // Try a few offsets around the point in case the target sits slightly off the mesh
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
        {
            // No valid navmesh near the target at all
            return false;
        }

        // Calculate path to the sampled navmesh point
        UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
        agent.CalculatePath(hit.position, path);

        // Consider the path valid if Complete
        if (path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
            return true;

        // If partial, check last corner distance to the sampled target point:
        if (path.status == UnityEngine.AI.NavMeshPathStatus.PathPartial && path.corners != null && path.corners.Length > 0)
        {
            Vector3 lastCorner = path.corners[path.corners.Length - 1];
            float distToTargetPoint = Vector3.Distance(lastCorner, hit.position);

            // If the last corner is close enough to the target (within stopping distance + buffer),
            // we can treat it as reachable (agent can get near enough to attack).
            float buffer = 0.5f;
            if (distToTargetPoint <= agent.stoppingDistance + buffer)
                return true;
        }

        // Otherwise treat as not reachable
        return false;
    }

    //much better best so far

    // private void HandleTargetingAndMovement()
    // {
    //     if (!agent.isOnNavMesh) return;

    //     // Ensure we have a main target
    //     if (mainTarget == null || IsTargetDead(mainTarget))
    //     {
    //         mainTarget = GetNearestAggroTargetOptimized();
    //         obstacleTarget = null;
    //         if (mainTarget == null)
    //         {
    //             agent.ResetPath();
    //             return;
    //         }
    //     }

    //     // Decide what target to move toward
    //     bool targetReachable = IsTargetEffectivelyReachable(mainTarget);
    //     currentTarget = targetReachable ? mainTarget : (obstacleTarget != null ? obstacleTarget : mainTarget);

    //     if (currentTarget == null) return;

    //     Vector3 targetPos = currentTarget.transform.position;

    //     // Only update destination if it moved far enough
    //     if ((targetPos - lastDestination).sqrMagnitude > destinationUpdateThreshold * destinationUpdateThreshold)
    //     {
    //         agent.SetDestination(targetPos);
    //         lastDestination = targetPos;
    //     }

    //     // Smooth rotation toward movement
    //     // if (agent.velocity.sqrMagnitude > 0.1f)
    //     // {
    //     //     Quaternion targetRotation = Quaternion.LookRotation(agent.velocity.normalized);
    //     //     transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
    //     // }
    // }


    private void HandleTargetingAndMovement()
    {
        if (!agent.isOnNavMesh) return;

        // Ensure we have a main target
        if (mainTarget == null || IsTargetDead(mainTarget))
        {
            mainTarget = GetNearestAggroTargetOptimized();
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

        // Offsets for multiple rays to cover wider obstacles
        Vector3[] offsets = { Vector3.zero, Vector3.left * 0.5f, Vector3.right * 0.5f };

        foreach (var offset in offsets)
        {
            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f + offset;
            Debug.DrawRay(rayOrigin, directionToTarget * distanceToTarget, Color.red, 0.5f);

            if (Physics.Raycast(rayOrigin, directionToTarget, out RaycastHit hit, distanceToTarget))
            {
                // if (hit.collider.CompareTag("Jumpable"))
                // {
                //     Debug.Log("Blocking object detected: " + hit.collider.name);
                //     return hit.collider.GetComponent<DefenseStructure>();
                // }
                if (hit.collider.GetComponent<DefenseStructure>() != null)
                {
                    // Debug.Log("Blocking object detected: " + hit.collider.name);
                    return hit.collider.GetComponent<DefenseStructure>();
                }
                else if (hit.collider.GetComponent<AnimalStructure>() != null)
                {
                    // Debug.Log("Blocking object detected: " + hit.collider.name);
                    return hit.collider.GetComponent<AnimalStructure>();
                }
                else if (hit.collider.GetComponent<BarracksStructure>() != null)
                {
                    // Debug.Log("Blocking object detected: " + hit.collider.name);
                    return hit.collider.GetComponent<BarracksStructure>();
                }
                else if (hit.collider.GetComponent<Structure>() != null)
                {
                    // Debug.Log("Blocking object detected: " + hit.collider.name);
                    return hit.collider.GetComponent<Structure>();
                }
                else if (hit.collider.GetComponent<CropStructure>() != null)
                {
                    // Debug.Log("Blocking object detected: " + hit.collider.name);
                    return hit.collider.GetComponent<CropStructure>();
                }
                else if (hit.collider.GetComponent<FarmHouseStructure>() != null)
                {
                    // Debug.Log("Blocking object detected: " + hit.collider.name);
                    return hit.collider.GetComponent<FarmHouseStructure>();
                }

            }
        }

        return null;
    }



    private IEnumerator JumpOver(Collider wall)
    {
        agent.enabled = false;

        Vector3 startPos = transform.position;
        Vector3 endPos = wall.transform.position + wall.transform.forward * 1f; // adjust distance past wall
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


    // private void AttackIfInRange()
    // {
    //     if (currentTarget == null || IsTargetDead(currentTarget))
    //         return;

    //     // Collider enemyCollider = GetComponent<Collider>();
    //     Collider targetCollider = currentTarget.GetComponent<Collider>();

    //     if (targetCollider == null)
    //         return;

    //     // Distance between closest points of colliders
    //     // Vector3 closestPointEnemy = enemyCollider.ClosestPoint(targetCollider.transform.position);
    //     // Vector3 closestPointTarget = targetCollider.ClosestPoint(closestPointEnemy);
    //     // float distanceBetween = Vector3.Distance(closestPointEnemy, closestPointTarget);
    //     float distanceBetween = Vector3.Distance(transform.position, targetCollider.transform.position);

    //     // Attack range buffer (tweak this)
    //     // float attackRange = data.StoppingDistance;
    //     Debug.Log("time: " + Time.time + " last time: " + lastAttackTime + " cool down ples last: " + (lastAttackTime + data.AttackCooldown));
    //     Debug.Log("disanceL: " + distanceBetween + " stopping: " + data.StoppingDistance);

    //     if (distanceBetween <= data.StoppingDistance && Time.time >= lastAttackTime + data.AttackCooldown)
    //     {
    //         // if (Time.time >= lastAttackTime + data.AttackCooldown)
    //         // {
    //         lastAttackTime = Time.time;
    //         // PlaySound(data.AttackSound);
    //         SetTrigger("Attack");
    //         Attack(currentTarget);
    //         // }
    //     }
    // }

    private void AttackIfInRange()
    {
        if (currentTarget == null || IsTargetDead(currentTarget))
            return;

        Collider enemyCollider = GetComponent<Collider>();
        Collider targetCollider = currentTarget.GetComponent<Collider>();

        if (enemyCollider == null || targetCollider == null)
            return;

        // Edge-to-edge distance
        // Vector3 closestPointEnemy = enemyCollider.ClosestPoint(targetCollider.transform.position);
        // Vector3 closestPointTarget = targetCollider.ClosestPoint(closestPointEnemy);
        // float distanceBetween = Vector3.Distance(closestPointEnemy, closestPointTarget);

        Vector3 closestPointEnemy = enemyCollider.ClosestPoint(targetCollider.transform.position);
        Vector3 closestPointTarget = targetCollider.ClosestPoint(closestPointEnemy);
        float distanceBetween = Vector3.Distance(closestPointEnemy, closestPointTarget);


        // Debug logs for verification
        Debug.Log($"Time: {Time.time}, Last Attack: {lastAttackTime}, Next Allowed: {lastAttackTime + data.AttackCooldown}");
        Debug.Log($"Distance: {distanceBetween}, Stopping Distance: {data.StoppingDistance}");

        // Only attack if within stopping distance and cooldown has passed
        if (distanceBetween <= data.StoppingDistance && Time.time >= (lastAttackTime + data.AttackCooldown))
        {
            Debug.Log("Attacking now: " + currentTarget.name + " whit this much: " + data.AttackDamage);
            lastAttackTime = Time.time;
            SetTrigger("Attack");
            Attack(currentTarget);
        }
    }



    private bool IsAdjacentToTarget(MonoBehaviour target)
    {
        var enemyGridPos = GridController.Instance.WorldToGridCoords(transform.position);
        var targetGridPos = GridController.Instance.WorldToGridCoords(target.transform.position);

        int dx = Mathf.Abs(enemyGridPos.x - targetGridPos.x);
        int dy = Mathf.Abs(enemyGridPos.y - targetGridPos.y);

        return (dx + dy == 1); // True if exactly one cell apart (up, down, left, or right)
    }


    // private MonoBehaviour GetNearestAggroTargetOptimized()
    // {
    //     Collider[] hits = Physics.OverlapSphere(transform.position, data.AttackRange);
    //     MonoBehaviour nearest = null;
    //     float closestDist = float.MaxValue;

    //     foreach (var col in hits)
    //     {
    //         MonoBehaviour candidate = null;

    //         switch (data.AttType)
    //         {
    //             case AttType.Animals:
    //                 candidate = col.GetComponent<ArmyUnit>();
    //                 // Debug.Log("animal attack: " + candidate);
    //                 break;

    //             case AttType.Resources:
    //                 CropStructure crop = col.GetComponent<CropStructure>();
    //                 if (crop != null)
    //                     candidate = crop;
    //                 else
    //                 {
    //                     SiloStructure silo = col.GetComponent<SiloStructure>();
    //                     if (silo != null)
    //                         candidate = silo;
    //                 }
    //                 break;

    //             case AttType.Defense:
    //                 candidate = col.GetComponent<DefenseStructure>();
    //                 // Debug.Log("Defence attack: " + candidate);
    //                 break;

    //             case AttType.Buildings:
    //                 {
    //                     // FarmHouseStructure farmHouse = col.GetComponent<FarmHouseStructure>();
    //                     // if (farmHouse != null)
    //                     //     candidate = farmHouse;
    //                     // else
    //                     // {
    //                     CropStructure crop2 = col.GetComponent<CropStructure>();
    //                     if (crop2 != null)
    //                         candidate = crop2;
    //                     else
    //                     {
    //                         SiloStructure silo2 = col.GetComponent<SiloStructure>();
    //                         if (silo2 != null)
    //                             candidate = silo2;
    //                         else
    //                         {
    //                             BarracksStructure barracks = col.GetComponent<BarracksStructure>();
    //                             if (barracks != null)
    //                                 candidate = barracks;
    //                             else
    //                             {
    //                                 AnimalStructure animal = col.GetComponent<AnimalStructure>();
    //                                 if (animal != null)
    //                                     candidate = animal;
    //                             }
    //                         }
    //                     }
    //                     // }
    //                 }
    //                 // Debug.Log("building attack: " + candidate);
    //                 break;
    //         }

    //         if (candidate != null && !IsTargetDead(candidate))
    //         {
    //             float dist = Vector3.Distance(transform.position, candidate.transform.position);
    //             if (dist < closestDist)
    //             {
    //                 closestDist = dist;
    //                 nearest = candidate;
    //             }
    //         }
    //     }

    //     // If nothing found, attack the nearest target of any type
    //     if (nearest == null)
    //     {
    //         // Debug.Log("There was nothing so we go for anything!");
    //         // Fallback: find the nearest *any* target if no preferred AttType target was found
    //         Collider[] fallbackHits = Physics.OverlapSphere(transform.position, data.AttackRange);

    //         foreach (var col in fallbackHits)
    //         {
    //             MonoBehaviour candidate = null;

    //             if (col.TryGetComponent<ArmyUnit>(out var army)) candidate = army;
    //             else if (col.TryGetComponent<CropStructure>(out var crop)) candidate = crop;
    //             else if (col.TryGetComponent<SiloStructure>(out var silo)) candidate = silo;
    //             // else if (col.TryGetComponent<FarmHouseStructure>(out var farm)) candidate = farm;
    //             else if (col.TryGetComponent<BarracksStructure>(out var barracks)) candidate = barracks;
    //             else if (col.TryGetComponent<AnimalStructure>(out var animal)) candidate = animal;
    //             // else if (col.TryGetComponent<DefenseStructure>(out var defense))
    //             // {
    //             //     candidate = defense;
    //             //     // Debug.Log("WE found a defense");
    //             // }

    //             if (candidate != null && !IsTargetDead(candidate))
    //             {
    //                 float dist = Vector3.Distance(transform.position, candidate.transform.position);
    //                 if (dist < closestDist)
    //                 {
    //                     closestDist = dist;
    //                     nearest = candidate;
    //                 }
    //             }
    //         }
    //     }
    //     // Debug.Log("nothing-----------------------");


    //     //still nothing found, go for the farm house
    //     if (nearest == null)
    //     {
    //         // Debug.Log("nothing++++++++++++++++++++");
    //         // if (nearest == null)
    //         // {
    //         // Debug.Log("No other targets, checking for farm house...");

    //         Structure[] allStructures = FindObjectsOfType<Structure>();
    //         foreach (var s in allStructures)
    //         {
    //             // The farmhouse is the ONLY Structure that is not a child type
    //             // if (s.GetType() == typeof(Structure) && !IsTargetDead(s))

    //             if (s.GetType() != typeof(Structure))
    //             {
    //                 continue;
    //             }

    //             if (!IsTargetDead(s))
    //             {
    //                 nearest = s;
    //                 // Debug.Log("Farm house targeted: " + s.name);
    //                 break; // stop after the first (should be the only one)
    //             }
    //         }

    //     }

    //     return nearest;
    // }


    private MonoBehaviour GetNearestAggroTargetOptimized()
    {
        return TargetManager.Instance.GetNearestAggroTargetOptimized(data, transform.position);
        //     MonoBehaviour nearest = null;
        //     float closestDist = float.MaxValue;

        //     foreach (var target in allTargets)
        //     {
        //         if (IsTargetDead(target)) continue;

        //         bool validType = data.AttType switch
        //         {
        //             AttType.Animals => target is ArmyUnit,
        //             AttType.Resources => target is CropStructure || target is SiloStructure,
        //             AttType.Defense => target is DefenseStructure,
        //             AttType.Buildings => target is Structure || target is BarracksStructure || target is AnimalStructure || target is CropStructure || target is SiloStructure,
        //             _ => false
        //         };

        //         if (!validType) continue;

        //         float dist = Vector3.Distance(transform.position, target.transform.position);
        //         if (dist < closestDist)
        //         {
        //             closestDist = dist;
        //             nearest = target;
        //         }
        //     }

        //     // fallback to farm house if nothing found
        //     if (nearest == null)
        //     {
        //         foreach (var s in allTargets.OfType<Structure>())
        //         {
        //             if (s.GetType() == typeof(Structure) && !IsTargetDead(s))
        //             {
        //                 nearest = s;
        //                 break;
        //             }
        //         }
        //     }

        //     return nearest;
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

    // Update TakeDamage to show health bar only when damaged:
    public void TakeDamage(int damage)
    {
        if (currHealth <= 0 || currHealth - damage <= 0)
        {
            // Debug.Log("I have sadly died |~|~|~||~|~|~||~||~|||~|~||~|~|~|~|||~|~|~|~||~|~|~|~|~|siuhohuifowrihufrehiuoerihuhewriuiweisufrghsireufhgiuershtgiuhrestuig");
            PlaySound(data.DeathSound, 'd');
            currHealth = 0;
            UpdateHealthBar();
            Die();
        }
        else
        {
            // Debug.Log("Taking damage: " + damage + "----------------------------------------------------------------------------------");
            PlaySound(data.HurtSound, 'h');
            currHealth -= damage;
            UpdateHealthBar();
        }
    }

    public bool IsDead()
    {
        return currHealth <= 0;
    }

    private bool IsTargetDead(MonoBehaviour target)
    {
        return target switch
        {
            ArmyUnit u => u.IsDead(),
            CropStructure u => u.IsDead(),
            SiloStructure u => u.IsDead(),
            DefenseStructure u => u.IsDead(),
            // Structure u => u.IsDead(),
            BarracksStructure u => u.IsDead(),
            AnimalStructure u => u.IsDead(),
            Structure u when u.GetType() == typeof(Structure) => u.IsDead(),
            _ => true // Assume dead if type unknown
        };
    }

    private void Attack(MonoBehaviour target)
    {
        Debug.Log("attacking target " + target.name + " with " + data.AttackDamage);
        switch (target)
        {
            case ArmyUnit u:
                u.TakeDamage(data.AttackDamage);
                PlaySound(data.AttackSound, 'a');
                // DamageAnimation anim = u.GetComponent<DamageAnimation>();
                // if (anim != null)
                //     anim.PlayDamageHitEffect();
                // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
                break;
            case CropStructure u:
                PlaySound(data.AttackSound, 'a');
                u.TakeDamage(data.AttackDamage);
                DamageAnimation anim = u.GetComponent<DamageAnimation>();
                if (anim != null)
                    anim.PlayDamageHitEffect();
                // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
                break;
            case SiloStructure u:
                PlaySound(data.AttackSound, 'a');
                u.TakeDamage(data.AttackDamage);
                anim = u.GetComponent<DamageAnimation>();
                if (anim != null)
                    anim.PlayDamageHitEffect();
                // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
                break;
            case DefenseStructure u:
                u.TakeDamage(data.AttackDamage);
                PlaySound(data.AttackSound, 'a');
                anim = u.GetComponent<DamageAnimation>();
                if (anim != null)
                    anim.PlayDamageHitEffect();
                break;
            // case FarmHouseStructure u:
            // case FarmHouseStructure u:
            //     PlaySound(data.AttackSound, 'a');
            //     u.TakeDamage(data.AttackDamage);
            //     anim = u.GetComponent<DamageAnimation>();
            //     if (anim != null)
            //         anim.PlayDamageHitEffect();
            //     // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
            //     break;
            case BarracksStructure u:
                PlaySound(data.AttackSound, 'a');
                u.TakeDamage(data.AttackDamage);
                anim = u.GetComponent<DamageAnimation>();
                if (anim != null)
                    anim.PlayDamageHitEffect();
                // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
                break;
            case AnimalStructure u:
                PlaySound(data.AttackSound, 'a');
                u.TakeDamage(data.AttackDamage);
                anim = u.GetComponent<DamageAnimation>();
                if (anim != null)
                    anim.PlayDamageHitEffect();
                // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
                break;
            case Structure u: // This will catch your farmhouse
                u.TakeDamage(data.AttackDamage);
                PlaySound(data.AttackSound, 'a');
                anim = u.GetComponent<DamageAnimation>();
                if (anim != null)
                    anim.PlayDamageHitEffect();
                break;
        }
    }

    private Vector3 GetRandomOutsidePosition()
    {
        if (_gridDataGenerator == null) return Vector3.zero;

        int width = _gridDataGenerator.GetGridWidth();
        int height = _gridDataGenerator.GetGridHeight();
        // Debug.Log($"Grid Size: {width}x{height}+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++------------------------------------------------------");
        int maxRetries = 100;
        int retries = 0;
        while (true) // Keep trying until valid
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
                // Spawn exactly at the grid cell position
                return cell.worldPosition;
            }
            else
            {
                GridCell cell2 = _gridDataGenerator.GetCell(width, height - 1);
                return cell2.worldPosition;
            }
            // else retry with another random edge cell
        }
    }

    public void stopCombat()
    {
        retreating = true;
        currentTarget = null;
        destination = GetRandomOutsidePosition();
        Debug.Log($"destination: {destination}");

        if (!agent.isOnNavMesh)
        {
            return;
        }

        agent.SetDestination(destination);

        //force despawn if not already
        StartCoroutine(DelayedDespawn(10f)); // Despawn after 5 seconds
    }

    private IEnumerator DelayedDespawn(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Check if the enemy is still active and hasn't been despawned
        if (retreating)
        {
            // Debug.Log("Enemy despawned automatically after delay.");
            Destroy(gameObject); // Despawn the enemy
        }
    }

    private void OnDestroy()
    {
        // Stop all coroutines to prevent delayed despawn from running after destruction
        StopAllCoroutines();
    }
}

// using UnityEngine;
// using System.Collections.Generic;
// using System.Collections;
// using TMPro;
// using UnityEngine.UI;
// using System.Linq;

// public class EnemyUnit : BaseUnit
// {
//     [SerializeField] private EnemyData data;

//     // Throttle expensive raycasting operations
//     private static float lastRaycastTime = 0f;
//     private static readonly float raycastThrottleInterval = 0.2f; // 5 raycasts per second max

//     private int currHealth;
//     private float lastAttackTime = 0f;
//     private MonoBehaviour currentTarget;
//     private GridDataGenerator _gridDataGenerator;
//     private float stoppingDistance; // Change based on attack range
//     private UnityEngine.AI.NavMeshAgent agent;
//     private UnityEngine.AI.NavMeshAgent cachedNavMeshAgent;
//     private Collider cachedCollider;
//     private Vector3 currentAttackPosition;
//     private bool hasAttackPosition = false;
//     private float attackPositionUpdateThreshold = 0.3f; // minimum movement distance to update
//     private float targetSearchCooldown = 0.5f;
//     private float lastTargetSearchTime = 0f;
//     private bool hasNoTarget = false;

//     [SerializeField] private GameObject healthBarPrefab;
//     private GameObject healthBarInstance;
//     private Slider healthBarSlider;
//     private TextMeshProUGUI healthBarText;
//     private CanvasGroup healthBarCanvasGroup;

//     public int currentMaxSpawn;

//     public int currentMinSpawn;

//     public bool retreating = false;
//     private Vector3 destination;


//     //jumping
//     [SerializeField] private float jumpCheckDistance = 1.5f;
//     [SerializeField] private float jumpHeight = 20f;
//     [SerializeField] private float jumpDuration = 0.5f;
//     // [SerializeField] private bool dead = false;
//     private MonoBehaviour mainTarget; // the building
//     // private MonoBehaviour obstacleTarget; // wall temporarily
//     private MonoBehaviour obstacleTarget; // wall temporarily

//     private float destinationUpdateThreshold = 0.3f; // minimum distance to update destination
//     private Vector3 lastDestination;

//     private List<MonoBehaviour> allTargets = new List<MonoBehaviour>();

//     // protected override void Awake()
//     // {
//     //     base.Awake();
//     //     agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
//     //     currHealth = data.Health;
//     //     _gridDataGenerator = FindObjectOfType<GridDataGenerator>();
//     //     // navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
//     //     // HandleTargetingAndMovement();
//     //     // AttackIfInRange();
//     // }

//     // protected override void Awake()
//     // {
//     //     base.Awake();
//     //     agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
//     //     currHealth = data.Health;
//     //     _gridDataGenerator = FindObjectOfType<GridDataGenerator>();

//     //     // Apply movement values from data
//     //     agent.speed = data.MovementSpeed;
//     //     agent.acceleration = data.Acceleration;
//     //     agent.angularSpeed = data.AngularSpeed;
//     //     agent.stoppingDistance = data.StoppingDistance;

//     //     PlayBackgroundSound(data.backgroundSound);

//     //     // Instantiate health bar but keep hidden
//     //     if (healthBarPrefab != null && healthBarInstance == null)
//     //     {
//     //         healthBarInstance = Instantiate(healthBarPrefab, transform);
//     //         var rect = healthBarInstance.GetComponent<RectTransform>();
//     //         if (rect != null)
//     //             rect.localPosition = new Vector3(0, 2.5f, 0); // Adjust Y as needed
//     //         healthBarSlider = healthBarInstance.GetComponentInChildren<Slider>();
//     //         healthBarText = healthBarInstance.GetComponentInChildren<TextMeshProUGUI>();
//     //         healthBarCanvasGroup = healthBarInstance.GetComponentInChildren<CanvasGroup>();
//     //         healthBarInstance.SetActive(false); // Start hidden
//     //     }
//     // }

//     protected override void Awake()
//     {
//         base.Awake();
//         agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
//         cachedNavMeshAgent = agent; // Cache for performance
//         cachedCollider = GetComponent<Collider>(); // Cache collider to avoid repeated GetComponent calls
//         currHealth = data.Health;
//         _gridDataGenerator = FindObjectOfType<GridDataGenerator>();

//         // Apply movement values from data
//         agent.speed = data.MovementSpeed;
//         agent.acceleration = data.Acceleration;
//         agent.angularSpeed = data.AngularSpeed;
//         agent.stoppingDistance = data.StoppingDistance;

//         stoppingDistance = data.StoppingDistance;

//         PlayBackgroundSound(data.backgroundSound);

//         CacheAllTargets();

//         // Health bar setup (unchanged)
//         if (healthBarPrefab != null && healthBarInstance == null)
//         {
//             healthBarInstance = Instantiate(healthBarPrefab, transform);
//             var rect = healthBarInstance.GetComponent<RectTransform>();
//             if (rect != null)
//                 rect.localPosition = new Vector3(0, 2.5f, 0);
//             healthBarSlider = healthBarInstance.GetComponentInChildren<Slider>();
//             healthBarText = healthBarInstance.GetComponentInChildren<TextMeshProUGUI>();
//             healthBarCanvasGroup = healthBarInstance.GetComponentInChildren<CanvasGroup>();
//             healthBarInstance.SetActive(false);
//         }
//     }

//     private void Start()
//     {
//         currentTarget = GetNearestAggroTargetOptimized();
//     }


//     // public void Update()
//     // {
//     //     HandleTargetingAndMovement();
//     //     AttackIfInRange();
//     // }

//     // Throttle expensive operations to improve performance
//     private float lastUpdateTime = 0f;
//     private const float updateInterval = 0.1f; // Update 10 times per second instead of 60

//     // Static caching system to avoid multiple FindObjectsOfType calls across all enemies
//     private static List<MonoBehaviour> cachedAllTargets = new List<MonoBehaviour>();
//     private static float lastTargetCacheTime = 0f;
//     private static readonly float targetCacheInterval = 1f; // Cache targets every 1 second

//     private void Update()
//     {
//         // If god mode is active, destroy this enemy
//         if (CheatManager.Instance != null && CheatManager.Instance.IsGodModeActive())
//         {
//             Destroy(gameObject);
//             return;
//         }

//         // Throttle expensive operations to reduce CPU load
//         if (Time.time - lastUpdateTime < updateInterval)
//             return;
//         lastUpdateTime = Time.time;

//         if (retreating)
//         {
//             if (retreating && Vector3.Distance(transform.position, destination) < 5f)
//             {
//                 Destroy(gameObject);
//                 return;
//             }
//             return;
//         }

//         if (cachedNavMeshAgent.velocity.sqrMagnitude > 0.1f)
//         {
//             // Moving → set Speed to 1
//             SetFloat("speed", 1f);
//         }
//         else
//         {
//             // Idle → set Speed to 0
//             SetFloat("speed", 0f);
//         }

//         if (currentTarget == null || IsTargetDead(currentTarget))
//         {
//             currentTarget = GetNearestAggroTargetOptimized();
//         }

//         HandleTargetingAndMovement();
//         AttackIfInRange();

//         // currentMaxSpawn = data.maxSpawnAmount;
//         // currentMinSpawn = data.minSpawnAmount;



//         // HandleTargetingAndMovement();


//         // if (Time.time - lastTargetSearchTime > targetSearchCooldown || currentTarget == null || IsTargetDead(currentTarget))
//         // {
//         //     currentTarget = GetNearestAggroTargetOptimized();
//         //     lastTargetSearchTime = Time.time;
//         // }

//         if (currentTarget == null)
//         {
//             agent.ResetPath();
//             return;
//         }

//         // HandleTargetingAndMovement();
//         // AttackIfInRange();


//     }

//     // Static method to update cached targets (called by first enemy only)
//     private static void UpdateCachedTargets()
//     {
//         if (Time.time - lastTargetCacheTime < targetCacheInterval)
//             return;

//         cachedAllTargets.Clear();
//         cachedAllTargets.AddRange(FindObjectsOfType<ArmyUnit>());
//         cachedAllTargets.AddRange(FindObjectsOfType<CropStructure>());
//         cachedAllTargets.AddRange(FindObjectsOfType<SiloStructure>());
//         cachedAllTargets.AddRange(FindObjectsOfType<DefenseStructure>());
//         cachedAllTargets.AddRange(FindObjectsOfType<BarracksStructure>());
//         cachedAllTargets.AddRange(FindObjectsOfType<AnimalStructure>());
//         cachedAllTargets.AddRange(FindObjectsOfType<Structure>()); // farm house etc

//         lastTargetCacheTime = Time.time;
//     }

//     private void CacheAllTargets()
//     {
//         // Use static cache instead of individual FindObjectsOfType calls
//         UpdateCachedTargets();
//         allTargets.Clear();
//         allTargets.AddRange(cachedAllTargets);
//     }


//     protected override UnitData GetData() => data;



//     private bool IsTargetEffectivelyReachable(MonoBehaviour target)
//     {
//         if (target == null) return false;

//         Collider targetCollider = target.GetComponent<Collider>();
//         Vector3 desiredPoint = target != null ? target.transform.position : transform.position;

//         // Prefer closest point on collider (so we try to path to the actual closest surface)
//         if (targetCollider != null)
//         {
//             desiredPoint = targetCollider.ClosestPoint(transform.position);
//             // If ClosestPoint is the same as transform (no collider), fallback to transform.position
//             if (desiredPoint == Vector3.zero)
//                 desiredPoint = target.transform.position;
//         }

//         // Try to sample the navmesh at/around the desired point
//         UnityEngine.AI.NavMeshHit hit;
//         float sampleRadius = 1.5f; // try small radius first, increase if needed
//         bool foundSample = UnityEngine.AI.NavMesh.SamplePosition(desiredPoint, out hit, 0.5f, agent.areaMask);

//         if (!foundSample)
//         {
//             // Try a few offsets around the point in case the target sits slightly off the mesh
//             Vector3[] offsets = {
//             Vector3.zero,
//             Vector3.forward * 0.5f,
//             Vector3.back * 0.5f,
//             Vector3.left * 0.5f,
//             Vector3.right * 0.5f,
//             Vector3.up * 0.2f
//         };

//             foreach (var off in offsets)
//             {
//                 if (UnityEngine.AI.NavMesh.SamplePosition(desiredPoint + off, out hit, sampleRadius, agent.areaMask))
//                 {
//                     foundSample = true;
//                     break;
//                 }
//             }
//         }

//         if (!foundSample)
//         {
//             // No valid navmesh near the target at all
//             return false;
//         }

//         // Calculate path to the sampled navmesh point
//         UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
//         agent.CalculatePath(hit.position, path);

//         // Consider the path valid if Complete
//         if (path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
//             return true;

//         // If partial, check last corner distance to the sampled target point:
//         if (path.status == UnityEngine.AI.NavMeshPathStatus.PathPartial && path.corners != null && path.corners.Length > 0)
//         {
//             Vector3 lastCorner = path.corners[path.corners.Length - 1];
//             float distToTargetPoint = Vector3.Distance(lastCorner, hit.position);

//             // If the last corner is close enough to the target (within stopping distance + buffer),
//             // we can treat it as reachable (agent can get near enough to attack).
//             float buffer = 0.5f;
//             if (distToTargetPoint <= agent.stoppingDistance + buffer)
//                 return true;
//         }

//         // Otherwise treat as not reachable
//         return false;
//     }

//     //much better best so far

//     // private void HandleTargetingAndMovement()
//     // {
//     //     if (!agent.isOnNavMesh) return;

//     //     // Ensure we have a main target
//     //     if (mainTarget == null || IsTargetDead(mainTarget))
//     //     {
//     //         mainTarget = GetNearestAggroTargetOptimized();
//     //         obstacleTarget = null;
//     //         if (mainTarget == null)
//     //         {
//     //             agent.ResetPath();
//     //             return;
//     //         }
//     //     }

//     //     // Decide what target to move toward
//     //     bool targetReachable = IsTargetEffectivelyReachable(mainTarget);
//     //     currentTarget = targetReachable ? mainTarget : (obstacleTarget != null ? obstacleTarget : mainTarget);

//     //     if (currentTarget == null) return;

//     //     Vector3 targetPos = currentTarget.transform.position;

//     //     // Only update destination if it moved far enough
//     //     if ((targetPos - lastDestination).sqrMagnitude > destinationUpdateThreshold * destinationUpdateThreshold)
//     //     {
//     //         agent.SetDestination(targetPos);
//     //         lastDestination = targetPos;
//     //     }

//     //     // Smooth rotation toward movement
//     //     // if (agent.velocity.sqrMagnitude > 0.1f)
//     //     // {
//     //     //     Quaternion targetRotation = Quaternion.LookRotation(agent.velocity.normalized);
//     //     //     transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
//     //     // }
//     // }

//     // private Vector3 GetAttackPoint(MonoBehaviour target)
//     // {
//     //     Collider targetCollider = target.GetComponent<Collider>();
//     //     if (targetCollider == null)
//     //         return target.transform.position;

//     //     // Closest point on the collider to the enemy
//     //     Vector3 closestPoint = targetCollider.ClosestPoint(transform.position);

//     //     // Optional: add a tiny offset backwards to avoid clipping
//     //     Vector3 directionToEnemy = (transform.position - closestPoint).normalized;
//     //     float offset = 0.1f; // distance to stay away from wall
//     //     return closestPoint + directionToEnemy * offset;
//     // }

//     // private Vector3 GetAttackPoint(MonoBehaviour target)
//     // {
//     //     Collider targetCollider = target.GetComponent<Collider>();
//     //     if (!targetCollider) return target.transform.position;

//     //     Vector3 closest = targetCollider.ClosestPoint(transform.position);
//     //     Vector3 direction = (closest - transform.position).normalized;

//     //     // Use agent.stoppingDistance + small buffer
//     //     float safeOffset = agent.stoppingDistance + 0.2f;
//     //     return closest - direction * safeOffset;
//     // }






//     // private void HandleTargetingAndMovement()
//     // {
//     //     if (!agent.isOnNavMesh) return;

//     //     // Ensure we have a main target
//     //     if (mainTarget == null || IsTargetDead(mainTarget))
//     //     {
//     //         mainTarget = GetNearestAggroTargetOptimized();
//     //         obstacleTarget = null;
//     //         if (mainTarget == null)
//     //         {
//     //             agent.ResetPath();
//     //             return;
//     //         }
//     //     }

//     //     // Check if the main target is reachable
//     //     bool targetReachable = IsTargetEffectivelyReachable(mainTarget);

//     //     // If not reachable, find the blocking object
//     //     if (!targetReachable)
//     //     {
//     //         obstacleTarget = GetBlockingObjectDirect();
//     //     }
//     //     else
//     //     {
//     //         obstacleTarget = null;
//     //     }

//     //     // Decide what to move toward
//     //     currentTarget = obstacleTarget != null ? obstacleTarget : mainTarget;

//     //     if (currentTarget == null) return;

//     //     Vector3 targetPos = currentTarget.transform.position;

//     //     // Only update destination if it moved far enough
//     //     if ((targetPos - lastDestination).sqrMagnitude > destinationUpdateThreshold * destinationUpdateThreshold)
//     //     {
//     //         agent.SetDestination(targetPos);
//     //         lastDestination = targetPos;
//     //     }
//     // }




//     // private void HandleTargetingAndMovement()
//     // {
//     //     if (!agent.isOnNavMesh) return;

//     //     // Ensure we have a main target
//     //     if (mainTarget == null || IsTargetDead(mainTarget))
//     //     {
//     //         mainTarget = GetNearestAggroTargetOptimized();
//     //         obstacleTarget = null;
//     //         if (mainTarget == null)
//     //         {
//     //             agent.ResetPath();
//     //             return;
//     //         }
//     //     }

//     //     // Decide what to move toward
//     //     currentTarget = obstacleTarget != null ? obstacleTarget : mainTarget;
//     //     if (currentTarget == null) return;

//     //     // Calculate distance to target
//     //     float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);

//     //     // If we're close enough to attack, stop moving
//     //     if (distanceToTarget <= stoppingDistance)
//     //     {
//     //         agent.ResetPath();
//     //         agent.velocity = Vector3.zero;
//     //         return;
//     //     }

//     //     // Move directly toward the target
//     //     Vector3 targetPos = currentTarget.transform.position;

//     //     // Only update destination if it moved far enough or we don't have a path
//     //     bool needsNewPath = (targetPos - lastDestination).sqrMagnitude > destinationUpdateThreshold * destinationUpdateThreshold;
//     //     needsNewPath = needsNewPath || !agent.hasPath || agent.pathStatus != UnityEngine.AI.NavMeshPathStatus.PathComplete;

//     //     if (needsNewPath)
//     //     {
//     //         agent.stoppingDistance = stoppingDistance; // Let NavMesh handle stopping
//     //         agent.SetDestination(targetPos);
//     //         lastDestination = targetPos;
//     //     }
//     // }

//     // // private void AttackIfInRange()
//     // // {
//     // //     if (currentTarget == null || IsTargetDead(currentTarget))
//     // //         return;

//     // //     // Simple distance check
//     // //     float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);

//     // //     if (distanceToTarget <= transform.position + 2f)
//     // //     {
//     // //         // Face the target when attacking
//     // //         Vector3 directionToTarget = (currentTarget.transform.position - transform.position).normalized;
//     // //         if (directionToTarget.magnitude > 0.1f)
//     // //         {
//     // //             Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
//     // //             transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
//     // //         }

//     // //         // Attack if cooldown is ready
//     // //         if (Time.time >= lastAttackTime + data.AttackCooldown)
//     // //         {
//     // //             lastAttackTime = Time.time;
//     // //             SetTrigger("Attack");
//     // //             Attack(currentTarget);
//     // //             Debug.Log($"[DEBUG] Enemy {name} attacks {currentTarget.name} for {data.AttackDamage} damage.");
//     // //         }
//     // //     }
//     // // }

//     // private void AttackIfInRange()
//     // {
//     //     if (currentTarget == null || IsTargetDead(currentTarget))
//     //         return;

//     //     // Get direction and distance
//     //     Vector3 directionToTarget = currentTarget.transform.position - transform.position;
//     //     float distanceToTarget = directionToTarget.magnitude;

//     //     // Face the target
//     //     if (directionToTarget.magnitude > 0.1f)
//     //     {
//     //         Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
//     //         transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
//     //     }

//     //     if (distanceToTarget > 2f)
//     //     {
//     //         // Move toward the target if out of attack range
//     //         transform.position += directionToTarget.normalized * data.MoveSpeed * Time.deltaTime;
//     //     }
//     //     else
//     //     {
//     //         // Attack if cooldown is ready
//     //         if (Time.time >= lastAttackTime + data.AttackCooldown)
//     //         {
//     //             lastAttackTime = Time.time;
//     //             SetTrigger("Attack");
//     //             Attack(currentTarget);
//     //             Debug.Log($"[DEBUG] Enemy {name} attacks {currentTarget.name} for {data.AttackDamage} damage.");
//     //         }
//     //     }
//     // }

//     private void HandleTargetingAndMovement()
//     {
//         if (!agent.isOnNavMesh) return;

//         // Ensure we have a main target
//         if (mainTarget == null || IsTargetDead(mainTarget))
//         {
//             mainTarget = GetNearestAggroTargetOptimized();
//             obstacleTarget = null;
//             if (mainTarget == null)
//             {
//                 agent.ResetPath();
//                 SetFloat("speed", 0f);
//                 return;
//             }
//         }

//         // Check for blocking obstacles
//         bool targetReachable = IsTargetEffectivelyReachable(mainTarget);
//         if (!targetReachable)
//         {
//             obstacleTarget = GetBlockingObjectDirect();
//         }
//         else
//         {
//             obstacleTarget = null;
//         }

//         // Decide what to move toward
//         currentTarget = obstacleTarget != null ? obstacleTarget : mainTarget;
//         if (currentTarget == null)
//         {
//             agent.ResetPath();
//             SetFloat("speed", 0f);
//             return;
//         }

//         // Get the attack position (stopping distance away from target)
//         Vector3 targetPos = GetAttackPoint(currentTarget);

//         // Check for jumpable obstacles
//         if (obstacleTarget != null && obstacleTarget.GetComponent<Collider>().CompareTag("Jumpable"))
//         {
//             float distanceToObstacle = Vector3.Distance(transform.position, obstacleTarget.transform.position);
//             if (distanceToObstacle <= jumpCheckDistance)
//             {
//                 StartCoroutine(JumpOver(obstacleTarget.GetComponent<Collider>()));
//                 return;
//             }
//         }

//         // Only update destination if it moved far enough or path is invalid
//         bool needsNewPath = (targetPos - lastDestination).sqrMagnitude > destinationUpdateThreshold * destinationUpdateThreshold;
//         needsNewPath = needsNewPath || !agent.hasPath || agent.pathStatus != UnityEngine.AI.NavMeshPathStatus.PathComplete;

//         if (needsNewPath)
//         {
//             agent.stoppingDistance = stoppingDistance;
//             agent.SetDestination(targetPos);
//             lastDestination = targetPos;
//         }

//         // Update animation speed
//         SetFloat("speed", agent.velocity.sqrMagnitude > 0.1f ? 1f : 0f);
//     }

//     private Vector3 GetAttackPoint(MonoBehaviour target)
//     {
//         Collider targetCollider = target.GetComponent<Collider>();
//         if (!targetCollider) return target.transform.position;

//         // Get closest point on target's collider
//         Vector3 closestPoint = targetCollider.ClosestPoint(transform.position);
//         Vector3 directionToEnemy = (transform.position - closestPoint).normalized;

//         // If direction is zero (on top of target), use forward
//         if (directionToEnemy.magnitude < 0.1f)
//             directionToEnemy = transform.forward;

//         // Calculate attack position at stopping distance
//         Vector3 attackPosition = closestPoint + directionToEnemy * stoppingDistance;

//         // Ensure position is on NavMesh
//         UnityEngine.AI.NavMeshHit hit;
//         if (UnityEngine.AI.NavMesh.SamplePosition(attackPosition, out hit, 1f, agent.areaMask))
//         {
//             return hit.position;
//         }

//         return attackPosition; // Fallback to calculated position
//     }

//     private void AttackIfInRange()
//     {
//         if (currentTarget == null || IsTargetDead(currentTarget))
//             return;

//         // Use cached collider for enemy
//         Collider enemyCollider = cachedCollider;
//         Collider targetCollider = currentTarget.GetComponent<Collider>();

//         if (enemyCollider == null || targetCollider == null)
//             return;

//         // Calculate distance between closest points of colliders
//         Vector3 closestPointEnemy = enemyCollider.ClosestPoint(targetCollider.transform.position);
//         Vector3 closestPointTarget = targetCollider.ClosestPoint(closestPointEnemy);
//         float distanceBetween = Vector3.Distance(closestPointEnemy, closestPointTarget);

//         // Face the target
//         Vector3 directionToTarget = (currentTarget.transform.position - transform.position).normalized;
//         if (directionToTarget.magnitude > 0.1f)
//         {
//             Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
//             transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
//         }

//         // Attack if within range and cooldown is ready
//         if (distanceBetween <= stoppingDistance && Time.time >= lastAttackTime + data.AttackCooldown)
//         {
//             lastAttackTime = Time.time;
//             SetTrigger("Attack");
//             Attack(currentTarget);
//             Debug.Log($"[DEBUG] Enemy {name} attacks {currentTarget.name} for {data.AttackDamage} damage.");
//         }
//     }



//     // private void HandleTargetingAndMovement()
//     // {
//     //     if (!agent.isOnNavMesh) return;

//     //     // Ensure we have a main target
//     //     if (mainTarget == null || IsTargetDead(mainTarget))
//     //     {
//     //         mainTarget = GetNearestAggroTargetOptimized();
//     //         obstacleTarget = null;
//     //         if (mainTarget == null)
//     //         {
//     //             agent.ResetPath();
//     //             return;
//     //         }
//     //     }

//     //     // Decide what to move toward
//     //     currentTarget = obstacleTarget != null ? obstacleTarget : mainTarget;
//     //     if (currentTarget == null) return;

//     //     // Calculate distance to target
//     //     float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);

//     //     // If we're close enough to attack, stop moving
//     //     if (distanceToTarget <= stoppingDistance)
//     //     {
//     //         agent.ResetPath(); // Stop the agent
//     //         agent.velocity = Vector3.zero; // Ensure it stops immediately
//     //         return;
//     //     }

//     //     // Get the attack position (stopping distance away from target)
//     //     Vector3 targetPos = GetAttackPoint(currentTarget);

//     //     // Only update destination if it moved far enough or we don't have a path
//     //     bool needsNewPath = (targetPos - lastDestination).sqrMagnitude > destinationUpdateThreshold * destinationUpdateThreshold;
//     //     needsNewPath = needsNewPath || !agent.hasPath || agent.pathStatus != UnityEngine.AI.NavMeshPathStatus.PathComplete;

//     //     if (needsNewPath)
//     //     {
//     //         agent.stoppingDistance = 0.1f; // Small stopping distance to prevent NavMesh from stopping too early
//     //         agent.SetDestination(targetPos);
//     //         lastDestination = targetPos;
//     //     }

//     //     // Manual stopping check - if we're very close to destination, force stop
//     //     if (agent.hasPath && Vector3.Distance(transform.position, targetPos) <= stoppingDistance + 0.2f)
//     //     {
//     //         agent.ResetPath();
//     //         agent.velocity = Vector3.zero;
//     //     }
//     // }

//     // private Vector3 GetAttackPoint(MonoBehaviour target)
//     // {
//     //     Collider targetCollider = target.GetComponent<Collider>();
//     //     if (!targetCollider) return target.transform.position;

//     //     // Get the closest point on the target's collider to our position
//     //     Vector3 closestPointOnTarget = targetCollider.ClosestPoint(transform.position);

//     //     // Calculate direction from target to enemy
//     //     Vector3 directionToEnemy = (transform.position - closestPointOnTarget).normalized;

//     //     // If we're directly on top of the target, use forward direction
//     //     if (directionToEnemy.magnitude < 0.1f)
//     //     {
//     //         directionToEnemy = transform.forward;
//     //     }

//     //     // Position ourselves at stopping distance away from the target
//     //     Vector3 attackPosition = closestPointOnTarget + directionToEnemy * stoppingDistance;

//     //     // Ensure the attack position is on the NavMesh
//     //     UnityEngine.AI.NavMeshHit hit;
//     //     if (UnityEngine.AI.NavMesh.SamplePosition(attackPosition, out hit, 1f, agent.areaMask))
//     //     {
//     //         return hit.position;
//     //     }

//     //     // Fallback to original position if NavMesh sampling fails
//     //     return attackPosition;
//     // }

//     // private void AttackIfInRange()
//     // {
//     //     if (currentTarget == null || IsTargetDead(currentTarget))
//     //         return;

//     //     // Calculate distance to target
//     //     float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);

//     //     // Use stopping distance as attack range
//     //     if (distanceToTarget <= stoppingDistance)
//     //     {
//     //         // Face the target when attacking
//     //         Vector3 directionToTarget = (currentTarget.transform.position - transform.position).normalized;
//     //         if (directionToTarget.magnitude > 0.1f)
//     //         {
//     //             Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
//     //             transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
//     //         }

//     //         // Attack if cooldown is ready
//     //         if (Time.time >= lastAttackTime + data.AttackCooldown)
//     //         {
//     //             lastAttackTime = Time.time;
//     //             SetTrigger("Attack");

//     //             int damage = data.AttackDamage;
//     //             Attack(currentTarget);
//     //             Debug.Log($"[DEBUG] Enemy {name} attacks {currentTarget.name} for {damage} damage.");
//     //         }
//     //     }
//     // }







//     private MonoBehaviour GetBlockingObjectDirect()
//     {
//         // Throttle expensive raycasting to prevent performance issues
//         if (Time.time - lastRaycastTime < raycastThrottleInterval)
//         {
//             return null; // Return null if we raycasted too recently
//         }
//         lastRaycastTime = Time.time;

//         if (mainTarget == null) return null;

//         Vector3 directionToTarget = (mainTarget.transform.position - transform.position).normalized;
//         float distanceToTarget = Vector3.Distance(transform.position, mainTarget.transform.position);

//         // Offsets for multiple rays to cover wider obstacles
//         Vector3[] offsets = { Vector3.zero, Vector3.left * 0.5f, Vector3.right * 0.5f };

//         foreach (var offset in offsets)
//         {
//             Vector3 rayOrigin = transform.position + Vector3.up * 0.5f + offset;
//             Debug.DrawRay(rayOrigin, directionToTarget * distanceToTarget, Color.red, 0.5f);

//             if (Physics.Raycast(rayOrigin, directionToTarget, out RaycastHit hit, distanceToTarget))
//             {
//                 // if (hit.collider.CompareTag("Jumpable"))
//                 // {
//                 //     Debug.Log("Blocking object detected: " + hit.collider.name);
//                 //     return hit.collider.GetComponent<DefenseStructure>();
//                 // }
//                 if (hit.collider.GetComponent<DefenseStructure>() != null)
//                 {
//                     // Debug.Log("Blocking object detected: " + hit.collider.name);
//                     return hit.collider.GetComponent<DefenseStructure>();
//                 }
//                 else if (hit.collider.GetComponent<AnimalStructure>() != null)
//                 {
//                     // Debug.Log("Blocking object detected: " + hit.collider.name);
//                     return hit.collider.GetComponent<AnimalStructure>();
//                 }
//                 else if (hit.collider.GetComponent<BarracksStructure>() != null)
//                 {
//                     // Debug.Log("Blocking object detected: " + hit.collider.name);
//                     return hit.collider.GetComponent<BarracksStructure>();
//                 }
//                 else if (hit.collider.GetComponent<Structure>() != null)
//                 {
//                     // Debug.Log("Blocking object detected: " + hit.collider.name);
//                     return hit.collider.GetComponent<Structure>();
//                 }
//                 else if (hit.collider.GetComponent<CropStructure>() != null)
//                 {
//                     // Debug.Log("Blocking object detected: " + hit.collider.name);
//                     return hit.collider.GetComponent<CropStructure>();
//                 }
//                 else if (hit.collider.GetComponent<FarmHouseStructure>() != null)
//                 {
//                     // Debug.Log("Blocking object detected: " + hit.collider.name);
//                     return hit.collider.GetComponent<FarmHouseStructure>();
//                 }

//             }
//         }

//         return null;
//     }



//     private IEnumerator JumpOver(Collider wall)
//     {
//         agent.enabled = false;

//         Vector3 startPos = transform.position;
//         Vector3 endPos = wall.transform.position + wall.transform.forward * 1f; // adjust distance past wall
//         float elapsed = 0f;

//         while (elapsed < jumpDuration)
//         {
//             float t = elapsed / jumpDuration;
//             transform.position = Vector3.Lerp(startPos, endPos, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * jumpHeight;
//             elapsed += Time.deltaTime;
//             yield return null;
//         }

//         transform.position = endPos;
//         agent.enabled = true;
//     }


//     // private void AttackIfInRange()
//     // {
//     //     if (currentTarget == null || IsTargetDead(currentTarget))
//     //         return;

//     //     // Use cached colliders instead of expensive GetComponent calls
//     //     Collider enemyCollider = cachedCollider;
//     //     Collider targetCollider = currentTarget.GetComponent<Collider>();

//     //     if (enemyCollider == null || targetCollider == null)
//     //         return;

//     //     // Distance between closest points of colliders
//     //     Vector3 closestPointEnemy = enemyCollider.ClosestPoint(targetCollider.transform.position);
//     //     Vector3 closestPointTarget = targetCollider.ClosestPoint(closestPointEnemy);
//     //     float distanceBetween = Vector3.Distance(closestPointEnemy, closestPointTarget);

//     //     // Attack range buffer (tweak this)
//     //     float attackRange = data.StoppingDistance;

//     //     if (distanceBetween <= attackRange)
//     //     {
//     //         // if (Time.time >= lastAttackTime + data.AttackCooldown)
//     //         // {
//     //         //     lastAttackTime = Time.time;
//     //         //     // PlaySound(data.AttackSound);
//     //         //     SetTrigger("Attack");
//     //         //     Attack(currentTarget);
//     //         // }

//     //         float cooldown = Mathf.Max(0.1f, data.AttackCooldown);
//     //         if (Time.time >= lastAttackTime + cooldown)
//     //         {
//     //             lastAttackTime = Time.time;
//     //             SetTrigger("Attack");

//     //             // DEBUG: log attack
//     //             Debug.Log($"[DEBUG] Enemy {name} attacks {currentTarget.name} for {data.AttackDamage} damage.");

//     //             Attack(currentTarget);
//     //         }
//     //     }
//     // }

//     // private void AttackIfInRange()
//     // {
//     //     if (currentTarget == null || IsTargetDead(currentTarget))
//     //         return;

//     //     // Use cached colliders
//     //     Collider enemyCollider = cachedCollider;
//     //     Collider targetCollider = currentTarget.GetComponent<Collider>();

//     //     if (enemyCollider == null || targetCollider == null)
//     //         return;

//     //     // Distance calculations
//     //     Vector3 closestPointEnemy = enemyCollider.ClosestPoint(targetCollider.transform.position);
//     //     Vector3 closestPointTarget = targetCollider.ClosestPoint(closestPointEnemy);
//     //     float distanceBetween = Vector3.Distance(closestPointEnemy, closestPointTarget);
//     //     float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);

//     //     // Debug log for when enemy is in range
//     //     if (distanceBetween <= data.StoppingDistance)
//     //     {
//     //         Debug.Log($"[DEBUG] Enemy {name} has reached target {currentTarget.name} " +
//     //                   $"(distanceBetween: {distanceBetween:F2}, distanceToTarget: {distanceToTarget:F2}) " +
//     //                   $"and will start attacking.");
//     //     }

//     //     // Attack cooldown check
//     //     if (distanceBetween <= data.StoppingDistance && Time.time >= lastAttackTime + data.AttackCooldown)

//     //     {
//     //         lastAttackTime = Time.time;
//     //         SetTrigger("Attack");

//     //         // Perform attack and log
//     //         int damage = data.AttackDamage;
//     //         Attack(currentTarget);
//     //         Debug.Log($"[DEBUG] Enemy {name} attacks {currentTarget.name} for {damage} damage.");
//     //     }
//     // }

//     // private void AttackIfInRange()
//     // {
//     //     if (currentTarget == null || IsTargetDead(currentTarget))
//     //         return;

//     //     // Distance to target
//     //     float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);

//     //     // Debug log when in attack range
//     //     if (distanceToTarget <= data.StoppingDistance)
//     //     {
//     //         Debug.Log($"[DEBUG] Enemy {name} has reached target {currentTarget.name} " +
//     //                   $"(distanceToTarget: {distanceToTarget:F2}) and will start attacking.");
//     //     }

//     //     // Attack cooldown check
//     //     if (distanceToTarget <= data.StoppingDistance && Time.time >= lastAttackTime + data.AttackCooldown)
//     //     {
//     //         lastAttackTime = Time.time;
//     //         SetTrigger("Attack");

//     //         // Perform attack and log
//     //         int damage = data.AttackDamage;
//     //         Attack(currentTarget);
//     //         Debug.Log($"[DEBUG] Enemy {name} attacks {currentTarget.name} for {damage} damage.");
//     //     }
//     // }

//     //------------------------------------------------------------------------------------------------------------------------------------------------
//     // private void AttackIfInRange()
//     // {
//     //     if (currentTarget == null || IsTargetDead(currentTarget))
//     //         return;

//     //     // Use cached collider for enemy
//     //     Collider enemyCollider = cachedCollider;
//     //     Collider targetCollider = currentTarget.GetComponent<Collider>();

//     //     if (enemyCollider == null || targetCollider == null)
//     //         return;

//     //     // Distance between closest points of colliders
//     //     Vector3 closestPointEnemy = enemyCollider.ClosestPoint(targetCollider.transform.position);
//     //     Vector3 closestPointTarget = targetCollider.ClosestPoint(closestPointEnemy);
//     //     float distanceBetween = Vector3.Distance(closestPointEnemy, closestPointTarget);

//     //     // Debug log
//     //     if (distanceBetween <= data.StoppingDistance)
//     //     {
//     //         Debug.Log($"[DEBUG] Enemy {name} has reached target {currentTarget.name} " +
//     //                   $"(closestPointDistance: {distanceBetween:F2}) and will start attacking.");
//     //     }

//     //     // Attack cooldown check
//     //     if (distanceBetween <= data.StoppingDistance && Time.time >= lastAttackTime + data.AttackCooldown)
//     //     {
//     //         lastAttackTime = Time.time;
//     //         SetTrigger("Attack");

//     //         // Perform attack
//     //         int damage = data.AttackDamage;
//     //         Attack(currentTarget);
//     //         Debug.Log($"[DEBUG] Enemy {name} attacks {currentTarget.name} for {damage} damage.");
//     //     }
//     // }





//     private bool IsAdjacentToTarget(MonoBehaviour target)
//     {
//         var enemyGridPos = GridController.Instance.WorldToGridCoords(transform.position);
//         var targetGridPos = GridController.Instance.WorldToGridCoords(target.transform.position);

//         int dx = Mathf.Abs(enemyGridPos.x - targetGridPos.x);
//         int dy = Mathf.Abs(enemyGridPos.y - targetGridPos.y);

//         return (dx + dy == 1); // True if exactly one cell apart (up, down, left, or right)
//     }


//     // private MonoBehaviour GetNearestAggroTargetOptimized()
//     // {
//     //     Collider[] hits = Physics.OverlapSphere(transform.position, data.AttackRange);
//     //     MonoBehaviour nearest = null;
//     //     float closestDist = float.MaxValue;

//     //     foreach (var col in hits)
//     //     {
//     //         MonoBehaviour candidate = null;

//     //         switch (data.AttType)
//     //         {
//     //             case AttType.Animals:
//     //                 candidate = col.GetComponent<ArmyUnit>();
//     //                 // Debug.Log("animal attack: " + candidate);
//     //                 break;

//     //             case AttType.Resources:
//     //                 CropStructure crop = col.GetComponent<CropStructure>();
//     //                 if (crop != null)
//     //                     candidate = crop;
//     //                 else
//     //                 {
//     //                     SiloStructure silo = col.GetComponent<SiloStructure>();
//     //                     if (silo != null)
//     //                         candidate = silo;
//     //                 }
//     //                 break;

//     //             case AttType.Defense:
//     //                 candidate = col.GetComponent<DefenseStructure>();
//     //                 // Debug.Log("Defence attack: " + candidate);
//     //                 break;

//     //             case AttType.Buildings:
//     //                 {
//     //                     // FarmHouseStructure farmHouse = col.GetComponent<FarmHouseStructure>();
//     //                     // if (farmHouse != null)
//     //                     //     candidate = farmHouse;
//     //                     // else
//     //                     // {
//     //                     CropStructure crop2 = col.GetComponent<CropStructure>();
//     //                     if (crop2 != null)
//     //                         candidate = crop2;
//     //                     else
//     //                     {
//     //                         SiloStructure silo2 = col.GetComponent<SiloStructure>();
//     //                         if (silo2 != null)
//     //                             candidate = silo2;
//     //                         else
//     //                         {
//     //                             BarracksStructure barracks = col.GetComponent<BarracksStructure>();
//     //                             if (barracks != null)
//     //                                 candidate = barracks;
//     //                             else
//     //                             {
//     //                                 AnimalStructure animal = col.GetComponent<AnimalStructure>();
//     //                                 if (animal != null)
//     //                                     candidate = animal;
//     //                             }
//     //                         }
//     //                     }
//     //                     // }
//     //                 }
//     //                 // Debug.Log("building attack: " + candidate);
//     //                 break;
//     //         }

//     //         if (candidate != null && !IsTargetDead(candidate))
//     //         {
//     //             float dist = Vector3.Distance(transform.position, candidate.transform.position);
//     //             if (dist < closestDist)
//     //             {
//     //                 closestDist = dist;
//     //                 nearest = candidate;
//     //             }
//     //         }
//     //     }

//     //     // If nothing found, attack the nearest target of any type
//     //     if (nearest == null)
//     //     {
//     //         // Debug.Log("There was nothing so we go for anything!");
//     //         // Fallback: find the nearest *any* target if no preferred AttType target was found
//     //         Collider[] fallbackHits = Physics.OverlapSphere(transform.position, data.AttackRange);

//     //         foreach (var col in fallbackHits)
//     //         {
//     //             MonoBehaviour candidate = null;

//     //             if (col.TryGetComponent<ArmyUnit>(out var army)) candidate = army;
//     //             else if (col.TryGetComponent<CropStructure>(out var crop)) candidate = crop;
//     //             else if (col.TryGetComponent<SiloStructure>(out var silo)) candidate = silo;
//     //             // else if (col.TryGetComponent<FarmHouseStructure>(out var farm)) candidate = farm;
//     //             else if (col.TryGetComponent<BarracksStructure>(out var barracks)) candidate = barracks;
//     //             else if (col.TryGetComponent<AnimalStructure>(out var animal)) candidate = animal;
//     //             // else if (col.TryGetComponent<DefenseStructure>(out var defense))
//     //             // {
//     //             //     candidate = defense;
//     //             //     // Debug.Log("WE found a defense");
//     //             // }

//     //             if (candidate != null && !IsTargetDead(candidate))
//     //             {
//     //                 float dist = Vector3.Distance(transform.position, candidate.transform.position);
//     //                 if (dist < closestDist)
//     //                 {
//     //                     closestDist = dist;
//     //                     nearest = candidate;
//     //                 }
//     //             }
//     //         }
//     //     }
//     //     // Debug.Log("nothing-----------------------");


//     //     //still nothing found, go for the farm house
//     //     if (nearest == null)
//     //     {
//     //         // Debug.Log("nothing++++++++++++++++++++");
//     //         // if (nearest == null)
//     //         // {
//     //         // Debug.Log("No other targets, checking for farm house...");

//     //         Structure[] allStructures = FindObjectsOfType<Structure>();
//     //         foreach (var s in allStructures)
//     //         {
//     //             // The farmhouse is the ONLY Structure that is not a child type
//     //             // if (s.GetType() == typeof(Structure) && !IsTargetDead(s))

//     //             if (s.GetType() != typeof(Structure))
//     //             {
//     //                 continue;
//     //             }

//     //             if (!IsTargetDead(s))
//     //             {
//     //                 nearest = s;
//     //                 // Debug.Log("Farm house targeted: " + s.name);
//     //                 break; // stop after the first (should be the only one)
//     //             }
//     //         }

//     //     }

//     //     return nearest;
//     // }


//     private MonoBehaviour GetNearestAggroTargetOptimized()
//     {
//         return TargetManager.Instance.GetNearestAggroTargetOptimized(data, transform.position);
//         //     MonoBehaviour nearest = null;
//         //     float closestDist = float.MaxValue;

//         //     foreach (var target in allTargets)
//         //     {
//         //         if (IsTargetDead(target)) continue;

//         //         bool validType = data.AttType switch
//         //         {
//         //             AttType.Animals => target is ArmyUnit,
//         //             AttType.Resources => target is CropStructure || target is SiloStructure,
//         //             AttType.Defense => target is DefenseStructure,
//         //             AttType.Buildings => target is Structure || target is BarracksStructure || target is AnimalStructure || target is CropStructure || target is SiloStructure,
//         //             _ => false
//         //         };

//         //         if (!validType) continue;

//         //         float dist = Vector3.Distance(transform.position, target.transform.position);
//         //         if (dist < closestDist)
//         //         {
//         //             closestDist = dist;
//         //             nearest = target;
//         //         }
//         //     }

//         //     // fallback to farm house if nothing found
//         //     if (nearest == null)
//         //     {
//         //         foreach (var s in allTargets.OfType<Structure>())
//         //         {
//         //             if (s.GetType() == typeof(Structure) && !IsTargetDead(s))
//         //             {
//         //                 nearest = s;
//         //                 break;
//         //             }
//         //         }
//         //     }

//         //     return nearest;
//     }



//     private void UpdateHealthBar()
//     {
//         if (healthBarSlider != null)
//         {
//             float pct = Mathf.Clamp01((float)currHealth / data.Health);
//             healthBarSlider.value = pct;
//             if (healthBarText != null)
//                 healthBarText.text = $"{currHealth} / {data.Health}";

//             bool showBar = currHealth < data.Health && currHealth > 0;
//             if (healthBarCanvasGroup != null)
//             {
//                 healthBarCanvasGroup.alpha = showBar ? 1f : 0f;
//                 healthBarCanvasGroup.interactable = showBar;
//                 healthBarCanvasGroup.blocksRaycasts = showBar;
//             }
//             if (healthBarInstance != null)
//                 healthBarInstance.SetActive(showBar);
//         }
//     }

//     // Update TakeDamage to show health bar only when damaged:
//     public void TakeDamage(int damage)
//     {
//         if (currHealth <= 0 || currHealth - damage <= 0)
//         {
//             // Debug.Log("I have sadly died |~|~|~||~|~|~||~||~|||~|~||~|~|~|~|||~|~|~|~||~|~|~|~|~|siuhohuifowrihufrehiuoerihuhewriuiweisufrghsireufhgiuershtgiuhrestuig");
//             PlaySound(data.DeathSound, 'd');
//             currHealth = 0;
//             UpdateHealthBar();
//             Die();
//         }
//         else
//         {
//             // Debug.Log("Taking damage: " + damage + "----------------------------------------------------------------------------------");
//             PlaySound(data.HurtSound, 'h');
//             currHealth -= damage;
//             UpdateHealthBar();
//         }
//     }

//     public bool IsDead()
//     {
//         return currHealth <= 0;
//     }

//     private bool IsTargetDead(MonoBehaviour target)
//     {
//         return target switch
//         {
//             ArmyUnit u => u.IsDead(),
//             CropStructure u => u.IsDead(),
//             SiloStructure u => u.IsDead(),
//             DefenseStructure u => u.IsDead(),
//             // Structure u => u.IsDead(),
//             BarracksStructure u => u.IsDead(),
//             AnimalStructure u => u.IsDead(),
//             Structure u when u.GetType() == typeof(Structure) => u.IsDead(),
//             _ => true // Assume dead if type unknown
//         };
//     }

//     private void Attack(MonoBehaviour target)
//     {
//         // God mode prevents enemy attacks
//         if (CheatManager.Instance != null && CheatManager.Instance.IsGodModeActive())
//         {
//             return;
//         }

//         switch (target)
//         {
//             case ArmyUnit u:
//                 u.TakeDamage(data.AttackDamage);
//                 PlaySound(data.AttackSound, 'a');
//                 // DamageAnimation anim = u.GetComponent<DamageAnimation>();
//                 // if (anim != null)
//                 //     anim.PlayDamageHitEffect();
//                 // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
//                 break;
//             case CropStructure u:
//                 PlaySound(data.AttackSound, 'a');
//                 u.TakeDamage(data.AttackDamage);
//                 DamageAnimation anim = u.GetComponent<DamageAnimation>();
//                 if (anim != null)
//                     anim.PlayDamageHitEffect();
//                 // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
//                 break;
//             case SiloStructure u:
//                 PlaySound(data.AttackSound, 'a');
//                 u.TakeDamage(data.AttackDamage);
//                 anim = u.GetComponent<DamageAnimation>();
//                 if (anim != null)
//                     anim.PlayDamageHitEffect();
//                 // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
//                 break;
//             case DefenseStructure u:
//                 u.TakeDamage(data.AttackDamage);
//                 PlaySound(data.AttackSound, 'a');
//                 anim = u.GetComponent<DamageAnimation>();
//                 if (anim != null)
//                     anim.PlayDamageHitEffect();
//                 break;
//             // case FarmHouseStructure u:
//             // case FarmHouseStructure u:
//             //     PlaySound(data.AttackSound, 'a');
//             //     u.TakeDamage(data.AttackDamage);
//             //     anim = u.GetComponent<DamageAnimation>();
//             //     if (anim != null)
//             //         anim.PlayDamageHitEffect();
//             //     // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
//             //     break;
//             case BarracksStructure u:
//                 PlaySound(data.AttackSound, 'a');
//                 u.TakeDamage(data.AttackDamage);
//                 anim = u.GetComponent<DamageAnimation>();
//                 if (anim != null)
//                     anim.PlayDamageHitEffect();
//                 // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
//                 break;
//             case AnimalStructure u:
//                 PlaySound(data.AttackSound, 'a');
//                 u.TakeDamage(data.AttackDamage);
//                 anim = u.GetComponent<DamageAnimation>();
//                 if (anim != null)
//                     anim.PlayDamageHitEffect();
//                 // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
//                 break;
//             case Structure u: // This will catch your farmhouse
//                 u.TakeDamage(data.AttackDamage);
//                 PlaySound(data.AttackSound, 'a');
//                 anim = u.GetComponent<DamageAnimation>();
//                 if (anim != null)
//                     anim.PlayDamageHitEffect();
//                 break;
//         }
//     }

//     private Vector3 GetRandomOutsidePosition()
//     {
//         if (_gridDataGenerator == null) return Vector3.zero;

//         int width = _gridDataGenerator.GetGridWidth();
//         int height = _gridDataGenerator.GetGridHeight();
//         // Debug.Log($"Grid Size: {width}x{height}+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++------------------------------------------------------");
//         int maxRetries = 100;
//         int retries = 0;
//         while (true) // Keep trying until valid
//         {
//             retries++;

//             int side = Random.Range(0, 4);
//             int x = 0;
//             int y = 0;

//             switch (side)
//             {
//                 case 0: // Top edge
//                     x = Random.Range(0, width);
//                     y = height - 1;
//                     break;
//                 case 1: // Right edge
//                     x = width - 1;
//                     y = Random.Range(0, height);
//                     break;
//                 case 2: // Bottom edge
//                     x = Random.Range(0, width);
//                     y = 0;
//                     break;
//                 case 3: // Left edge
//                     x = 0;
//                     y = Random.Range(0, height);
//                     break;
//             }

//             GridCell cell = _gridDataGenerator.GetCell(x, y);

//             if (cell != null && !cell.flags.isObstacle && !cell.flags.isOccupied)
//             {
//                 // Spawn exactly at the grid cell position
//                 return cell.worldPosition;
//             }
//             else
//             {
//                 GridCell cell2 = _gridDataGenerator.GetCell(width, height - 1);
//                 return cell2.worldPosition;
//             }
//             // else retry with another random edge cell
//         }
//     }

//     public void stopCombat()
//     {
//         retreating = true;
//         currentTarget = null;
//         destination = GetRandomOutsidePosition();
//         Debug.Log($"destination: {destination}");

//         if (!agent.isOnNavMesh)
//         {
//             return;
//         }

//         agent.SetDestination(destination);

//         //force despawn if not already
//         StartCoroutine(DelayedDespawn(10f)); // Despawn after 5 seconds
//     }

//     private IEnumerator DelayedDespawn(float delay)
//     {
//         yield return new WaitForSeconds(delay);

//         // Check if the enemy is still active and hasn't been despawned
//         if (retreating)
//         {
//             // Debug.Log("Enemy despawned automatically after delay.");
//             Destroy(gameObject); // Despawn the enemy
//         }
//     }

//     private void OnDestroy()
//     {
//         // Stop all coroutines to prevent delayed despawn from running after destruction
//         StopAllCoroutines();
//     }
// }