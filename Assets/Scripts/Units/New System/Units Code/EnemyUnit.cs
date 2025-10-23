
// using UnityEngine;
// using System.Collections.Generic;
// using System.Collections;
// using TMPro;
// using UnityEngine.UI;
// using System.Linq;

// public class EnemyUnit : BaseUnit
// {
//     [SerializeField] private EnemyData data;

//     private int currHealth;
//     private float lastAttackTime = 0f;
//     private MonoBehaviour currentTarget;
//     private GridDataGenerator _gridDataGenerator;
//     private float stoppingDistance; // Change based on attack range
//     private UnityEngine.AI.NavMeshAgent agent;
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



//     protected override void Awake()
//     {
//         base.Awake();
//         agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
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
//         // lastAttackTime = Time.time;
//     }


//     // public void Update()
//     // {
//     //     HandleTargetingAndMovement();
//     //     AttackIfInRange();
//     // }

//     private void Update()
//     {
//         if (agent.velocity.sqrMagnitude > 0.1f)
//         {
//             // Moving → set Speed to 1
//             SetFloat("speed", 1f);
//         }
//         else
//         {
//             // Idle → set Speed to 0
//             SetFloat("speed", 0f);
//         }
//         lastAttackTime += Time.deltaTime;
//         // Debug.Log("------------------------------------------------------------------ target: " + currentTarget);
//         if (retreating)
//         {
//             if (retreating && Vector3.Distance(transform.position, destination) < 5f)
//             {
//                 Destroy(gameObject);
//                 return;
//             }
//             return;
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

//     private void CacheAllTargets()
//     {
//         allTargets.Clear();
//         allTargets.AddRange(FindObjectsOfType<ArmyUnit>());
//         allTargets.AddRange(FindObjectsOfType<CropStructure>());
//         allTargets.AddRange(FindObjectsOfType<SiloStructure>());
//         allTargets.AddRange(FindObjectsOfType<DefenseStructure>());
//         allTargets.AddRange(FindObjectsOfType<BarracksStructure>());
//         allTargets.AddRange(FindObjectsOfType<AnimalStructure>());
//         allTargets.AddRange(FindObjectsOfType<Structure>()); // farm house etc
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
//                 return;
//             }
//         }

//         // Check if the main target is reachable
//         bool targetReachable = IsTargetEffectivelyReachable(mainTarget);

//         // If not reachable, find the blocking object
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

//         if (currentTarget == null) return;

//         Vector3 targetPos = currentTarget.transform.position;

//         // Only update destination if it moved far enough
//         if ((targetPos - lastDestination).sqrMagnitude > destinationUpdateThreshold * destinationUpdateThreshold)
//         {
//             agent.SetDestination(targetPos);
//             lastDestination = targetPos;
//         }
//     }





//     private MonoBehaviour GetBlockingObjectDirect()
//     {
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

//     //     Collider enemyCollider = GetComponent<Collider>();
//     //     Collider targetCollider = currentTarget.GetComponent<Collider>();

//     //     if (enemyCollider == null || targetCollider == null)
//     //         return;

//     //     // Edge-to-edge distance
//     //     // Vector3 closestPointEnemy = enemyCollider.ClosestPoint(targetCollider.transform.position);
//     //     // Vector3 closestPointTarget = targetCollider.ClosestPoint(closestPointEnemy);
//     //     // float distanceBetween = Vector3.Distance(closestPointEnemy, closestPointTarget);

//     //     // Vector3 closestPointEnemy = enemyCollider.ClosestPoint(targetCollider.transform.position);
//     //     // Vector3 closestPointTarget = targetCollider.ClosestPoint(closestPointEnemy);
//     //     // float distanceBetween = Vector3.Distance(closestPointEnemy, closestPointTarget);

//     //     // if (currentTarget.)


//     //     //works for chickens-----------------------------------------------------
//     //     // Vector3 wolfPoint = enemyCollider.ClosestPoint(currentTarget.transform.position);
//     //     // Vector3 targetPoint = targetCollider.ClosestPoint(wolfPoint);
//     //     // float distanceBetween = Vector3.Distance(transform.position, currentTarget.transform.position);
//     //     // float distanceBetween = Vector3.Distance(wolfPoint, targetPoint);

//     //     float distanceBetween;

//     //     Collider targetCol = currentTarget.GetComponent<Collider>();

//     //     if (targetCol != null)
//     //     {
//     //         // Get the closest point on the target's collider to the wolf's position
//     //         Vector3 closestPoint = targetCol.ClosestPoint(transform.position);
//     //         distanceBetween = Vector3.Distance(transform.position, closestPoint);
//     //     }
//     //     else
//     //     {
//     //         // Fallback if the target has no collider
//     //         distanceBetween = Vector3.Distance(transform.position, currentTarget.transform.position);
//     //     }

//     //     Debug.Log($"{name} → {currentTarget.name} | Dist = {distanceBetween:F2}, Stop = {data.StoppingDistance}");
//     //     if (distanceBetween <= data.StoppingDistance &&
//     //         lastAttackTime >= data.AttackCooldown &&
//     //         !IsDead())
//     //     {
//     //         lastAttackTime = 0f;
//     //         SetTrigger("Attack");
//     //         Attack(currentTarget);
//     //     }





//     //     // Debug logs for verification
//     //     // Debug.Log($"Time: {Time.time}, Last Attack: {lastAttackTime}, Next Allowed: {lastAttackTime + data.AttackCooldown}");
//     //     // Debug.Log($"Distance: {distanceBetween}, Stopping Distance: {data.StoppingDistance}");

//     //     // Only attack if within stopping distance and cooldown has passed
//     //     // if (distanceBetween <= data.StoppingDistance && Time.time >= (lastAttackTime + data.AttackCooldown))





//     //     // Debug.Log($"{name} → {currentTarget.name} | Dist = {distanceBetween:F2}, Stop = {data.StoppingDistance}");

//     //     // if (distanceBetween <= data.StoppingDistance && lastAttackTime >= data.AttackCooldown && !IsDead())
//     //     // {
//     //     //     Debug.Log("Attacking now: " + currentTarget.name + " whit this much: " + data.AttackDamage);
//     //     //     lastAttackTime = 0f;
//     //     //     SetTrigger("Attack");
//     //     //     Attack(currentTarget);
//     //     // }
//     // }

//     private void AttackIfInRange()
//     {
//         if (currentTarget == null || IsTargetDead(currentTarget)) return;

//         // Get target collider
//         Collider targetCollider = currentTarget.GetComponent<Collider>();
//         if (targetCollider == null)
//         {
//             // fallback: use center-to-center if no collider on target
//             float fallbackDist = Vector3.Distance(transform.position, currentTarget.transform.position);
//             Debug.LogWarning($"{name}: target {currentTarget.name} has no collider - fallback dist = {fallbackDist:F2}");
//             if (fallbackDist <= 2f && lastAttackTime >= data.AttackCooldown && !IsDead())
//             {
//                 lastAttackTime = 0f;
//                 // SetTrigger("Attack");
//                 Attack(currentTarget);
//             }
//             return;
//         }

//         // Closest point on the target to the wolf's position
//         Vector3 closestPointOnTarget = targetCollider.ClosestPoint(transform.position);
//         float distToTargetSurface = Vector3.Distance(transform.position, closestPointOnTarget);

//         float attackRange = 2f; // see helper below

//         // Debug info (remove or lower verbosity later)
//         // Debug.Log($"{name} -> {currentTarget.name} distToSurface={distToTargetSurface:F2} attackRange={attackRange:F2} agent.remaining={agent.remainingDistance:F2} agent.stop={agent.stoppingDistance:F2}");

//         // If the wolf has its own collider and you want precise edge-to-edge, you could optionally subtract
//         // the wolf's 'radius'. In most cases using distToTargetSurface <= attackRange is fine.
//         if (distToTargetSurface <= attackRange && lastAttackTime >= data.AttackCooldown && !IsDead())
//         {
//             lastAttackTime = 0f;
//             // SetTrigger("Attack");
//             Attack(currentTarget);
//         }
//     }




//     private bool IsAdjacentToTarget(MonoBehaviour target)
//     {
//         var enemyGridPos = GridController.Instance.WorldToGridCoords(transform.position);
//         var targetGridPos = GridController.Instance.WorldToGridCoords(target.transform.position);

//         int dx = Mathf.Abs(enemyGridPos.x - targetGridPos.x);
//         int dy = Mathf.Abs(enemyGridPos.y - targetGridPos.y);

//         return (dx + dy == 1); // True if exactly one cell apart (up, down, left, or right)
//     }





//     private MonoBehaviour GetNearestAggroTargetOptimized()
//     {
//         return TargetManager.Instance.GetNearestAggroTargetOptimized(data, transform.position);

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
//         // Debug.Log("attacking target " + target.name + " with " + data.AttackDamage);
//         switch (target)
//         {
//             case ArmyUnit u:
//                 SetTrigger("Attack");
//                 u.TakeDamage(data.AttackDamage);
//                 PlaySound(data.AttackSound, 'a');
//                 // DamageAnimation anim = u.GetComponent<DamageAnimation>();
//                 // if (anim != null)
//                 //     anim.PlayDamageHitEffect();
//                 // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
//                 break;
//             case CropStructure u:
//                 SetTrigger("Attack");
//                 PlaySound(data.AttackSound, 'a');
//                 u.TakeDamage(data.AttackDamage);
//                 DamageAnimation anim = u.GetComponent<DamageAnimation>();
//                 if (anim != null)
//                     anim.PlayDamageHitEffect();
//                 // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
//                 break;
//             case SiloStructure u:
//                 SetTrigger("Attack");
//                 PlaySound(data.AttackSound, 'a');
//                 u.TakeDamage(data.AttackDamage);
//                 anim = u.GetComponent<DamageAnimation>();
//                 if (anim != null)
//                     anim.PlayDamageHitEffect();
//                 // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
//                 break;
//             case DefenseStructure u:
//                 SetTrigger("Attack");
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
//                 SetTrigger("Attack");
//                 PlaySound(data.AttackSound, 'a');
//                 u.TakeDamage(data.AttackDamage);
//                 anim = u.GetComponent<DamageAnimation>();
//                 if (anim != null)
//                     anim.PlayDamageHitEffect();
//                 // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
//                 break;
//             case AnimalStructure u:
//                 SetTrigger("Attack");
//                 PlaySound(data.AttackSound, 'a');
//                 u.TakeDamage(data.AttackDamage);
//                 anim = u.GetComponent<DamageAnimation>();
//                 if (anim != null)
//                     anim.PlayDamageHitEffect();
//                 // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
//                 break;
//             case Structure u: // This will catch your farmhouse
//                 SetTrigger("Attack");
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
    private bool hasAttackPosition = false;
    private float attackPositionUpdateThreshold = 0.3f;
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

    // Jumping
    [SerializeField] private float jumpCheckDistance = 1.5f;
    [SerializeField] private float jumpHeight = 20f;
    [SerializeField] private float jumpDuration = 0.5f;
    
    private MonoBehaviour mainTarget;
    private MonoBehaviour obstacleTarget;

    private float destinationUpdateThreshold = 0.3f;
    private Vector3 lastDestination;

    private List<MonoBehaviour> allTargets = new List<MonoBehaviour>();

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
        _gridDataGenerator = FindObjectOfType<GridDataGenerator>();

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

        // Update target if dead or null
        if (currentTarget == null || IsTargetDead(currentTarget))
        {
            currentTarget = GetNearestAggroTargetOptimized();
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
        allTargets.Clear();
        allTargets.AddRange(FindObjectsOfType<ArmyUnit>());
        allTargets.AddRange(FindObjectsOfType<CropStructure>());
        allTargets.AddRange(FindObjectsOfType<SiloStructure>());
        allTargets.AddRange(FindObjectsOfType<DefenseStructure>());
        allTargets.AddRange(FindObjectsOfType<BarracksStructure>());
        allTargets.AddRange(FindObjectsOfType<AnimalStructure>());
        allTargets.AddRange(FindObjectsOfType<Structure>());
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
        
        return TargetManager.Instance.GetNearestAggroTargetOptimized(data, transform.position);
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
        Debug.Log($"{name} retreating to: {destination}");

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

