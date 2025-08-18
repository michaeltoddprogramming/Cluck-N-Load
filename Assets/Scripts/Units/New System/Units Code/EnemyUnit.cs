using UnityEngine;
using System.Collections.Generic;
using System.Collections;
public class EnemyUnit : BaseUnit
{
    [SerializeField] private EnemyData data;

    private int currHealth;
    private float lastAttackTime = 0f;
    private MonoBehaviour currentTarget;
    private GridDataGenerator _gridDataGenerator;
    private float stoppingDistance = 1.5f; // Change based on attack range
    private UnityEngine.AI.NavMeshAgent agent;
    private Vector3 currentAttackPosition;
    private bool hasAttackPosition = false;
    private float attackPositionUpdateThreshold = 0.3f; // minimum movement distance to update
    private float targetSearchCooldown = 0.5f;
    private float lastTargetSearchTime = 0f;
    private bool hasNoTarget = false;

    public int currentMaxSpawn;

    public int currentMinSpawn;

    public bool retreating = false;
    private Vector3 destination;

    //jumping
    [SerializeField] private float jumpCheckDistance = 1.5f;
    [SerializeField] private float jumpHeight = 20f;
    [SerializeField] private float jumpDuration = 0.5f;
    private MonoBehaviour mainTarget; // the building
    // private MonoBehaviour obstacleTarget; // wall temporarily
    private MonoBehaviour obstacleTarget; // wall temporarily

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

        PlayBackgroundSound(data.backgroundSound);
    }


    // public void Update()
    // {
    //     HandleTargetingAndMovement();
    //     AttackIfInRange();
    // }

    private void Update()
    {

        // currentMaxSpawn = data.maxSpawnAmount;
        // currentMinSpawn = data.minSpawnAmount;
        if (retreating)
        {
            if (retreating && Vector3.Distance(transform.position, destination) < 5f)
            {
                Destroy(gameObject);
                return;
            }
            return;
        }



        HandleTargetingAndMovement();
        if (hasNoTarget && !retreating)
        {
            if (Time.time - lastTargetSearchTime >= 10f)
            {
                currentTarget = GetNearestAggroTargetOptimized();
                lastTargetSearchTime = Time.time;

                if (currentTarget == null)
                {
                    // Go to map center if still nothing
                    if (agent.destination != Vector3.zero)
                        agent.SetDestination(Vector3.zero);
                    return;
                }
            }
            else
            {
                return;
            }
        }

        if (Time.time - lastTargetSearchTime > targetSearchCooldown || currentTarget == null || IsTargetDead(currentTarget))
        {
            currentTarget = GetNearestAggroTargetOptimized();
            lastTargetSearchTime = Time.time;
        }

        if (currentTarget == null)
        {
            agent.ResetPath();
            return;
        }

        // HandleTargetingAndMovement();
        AttackIfInRange();


    }


    protected override UnitData GetData() => data;

    //much better best so far

    private void HandleTargetingAndMovement()
    {
        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("Agent not on NavMesh, skipping movement");
            return;
        }

        // if (currentTarget == null || IsTargetDead(currentTarget))
        // {
        //     // currentTarget = GetNearestAggroTarget();
        //     currentTarget = GetNearestAggroTargetOptimized();
        //     if (currentTarget == null)
        //     {
        //         agent.ResetPath();
        //         return;
        //     }
        //     // Reset stored position when target changes
        //     // currentAttackPosition = Vector3.zero;
        // }

        if (mainTarget == null || IsTargetDead(mainTarget))
        {
            Debug.Log("ieruehfweiurhgiouwrehjtgoiujhwrtoiugjorwijtgiorjtgoirjtgoijertgoijretgoreitjgersotighj");
            mainTarget = GetNearestAggroTargetOptimized();
            Debug.Log("mainTarget: " + mainTarget + "9999999999999999999999999999999999999999999999999999999999999999999999999999999999999");
            if (mainTarget == null)
            {
                Debug.Log("`````````````````````````````````````````````````````````````````````````````````` NULL");
                agent.ResetPath();
                // Debug.LogError("sdofligvbrhuygbhvifjusrdhjbikflderas;hijuo;fhouj;gfsdhoujn;garefdoihujb;tgedfoihuj;bngfedohujn;agdsfouhj;asfdrgg");
                return;
            }

            // Vector3 directionToTarget = (mainTarget.transform.position - transform.position).normalized;
            // directionToTarget.y = 0; // keep rotation only on horizontal plane
            // if (directionToTarget != Vector3.zero)
            //     transform.rotation = Quaternion.LookRotation(directionToTarget);

        }

        // currentTarget = mainTarget;

        // Debug.Log("main target is set " + mainTarget);

        // UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
        // bool pathFound = agent.CalculatePath(currentTarget.transform.position, path);

        // UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
        // bool pathFound = agent.CalculatePath(mainTarget.transform.position, path);

        // if (pathFound || path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
        // {
        //     Debug.Log("sidrfugh guiherguih         1 1 1  1 1 1 1 1  1 1 1  1 1  1 1  1 1 11  1  1  1 1 1 1 1  1 1 1 1 1 1 1 1 1 1 1  1");
        //     return;
        // }
        // if (pathFound == true)
        // {
        //     Debug.Log("path found.////////////////////////////////////////////////////////////////////////////");
        // }
        // else
        // {
        //     Debug.Log("No path exists! Attack wall.6546546546564554456645645645654654645465564564645654456");
        //     // Path exists, move normally
        //     // agent.SetDestination(mainTarget.transform.position);
        // }

        if (!agent.enabled) Debug.LogError("Agent disabled");
        Debug.Log($"isOnNavMesh={agent.isOnNavMesh} areaMask={agent.areaMask}");

        UnityEngine.AI.NavMeshHit startHit, endHit;
        bool startOk = UnityEngine.AI.NavMesh.SamplePosition(agent.transform.position, out startHit, 2f, agent.areaMask);
        bool endOk = UnityEngine.AI.NavMesh.SamplePosition(mainTarget.transform.position, out endHit, 2f, agent.areaMask);
        Debug.Log($"startOk={startOk} endOk={endOk} start={startHit.position} end={endHit.position}");


        // Debug.Log($"calc ok={ok} status={path.status} corners={(path.corners != null ? path.corners.Length : 0)}");
        if (endOk)
        {
            Debug.Log("please))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))");
            UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
            bool ok2 = agent.CalculatePath(endHit.position, path);
            bool ok = agent.CalculatePath(mainTarget.transform.position, path);
            // bool pathExists = agent.CalculatePath(mainTarget.transform.position, path)
            //       && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete;
            if (ok)
            {
                currentTarget = mainTarget;
                Debug.Log(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> yayayayayayya");

            }
            else if (ok2)
            {
                currentTarget = mainTarget;
                Debug.Log(",,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,yayayayayayya");
            }
            else
            {
                Debug.Log("..............................................................................................");
            }
        }
        else
        {
            Debug.Log("??????????????????????????????????????????????????????????????????????nah");
            if (obstacleTarget == null)
            {
                obstacleTarget = GetBlockingObjectDirect();
                if (obstacleTarget != null)
                {
                    currentTarget = obstacleTarget;
                }
                else
                {
                    Debug.Log("||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||");
                    return;
                }
                Debug.Log("WOWOWOWOWOOWOWOWOWOWOWOWOWOWOWOOWOWOWOWOWOWOWOOWOWOWOWOWWOWOOWOWOWOWOWOWOWOOWOWOWWOWOWOWOWOWOOWOWOWOWOWOWOWOWOWOOWOOWOWOWOW ");

            }
            else
            {

                currentTarget = obstacleTarget;
                Debug.Log("neeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee");
                // currentTarget = mainTarget;

            }
        }

        // bool pathComplete = agent.CalculatePath(mainTarget.transform.position, path);
        // bool pathComplete = agent.CalculatePath(mainTarget.transform.position, path) ||
        //                     path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete;

        // if (!pathFound || path.status != UnityEngine.AI.NavMeshPathStatus.PathComplete)
        // if (agent.CalculatePath(mainTarget.transform.position, path) && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
        // {
        //     Debug.Log("it has done this>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
        //     // A valid path exists, so stop targeting the obstacle
        //     obstacleTarget = null;
        //     currentTarget = mainTarget;
        // }

        //--------------------------------------------------------------
        // if (path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
        // {
        //     Debug.Log("<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");
        //     // There is a valid path on the NavMesh
        //     obstacleTarget = null;
        //     currentTarget = mainTarget;
        //     // agent.SetDestination(mainTarget.transform.position); // follow the path normally
        // }
        // if (!pathComplete)
        // {
        //     // Vector3 directionToTarget = (mainTarget.transform.position - transform.position).normalized;
        //     // directionToTarget.y = 0; // keep rotation only on horizontal plane
        //     // if (directionToTarget != Vector3.zero)
        //     //     transform.rotation = Quaternion.LookRotation(directionToTarget);
        //     // MonoBehaviour block = GetBlockingObject(path);
        //     // obstacleTarget = GetBlockingObject(path);
        //     obstacleTarget = GetBlockingObjectDirect();
        //     // Debug.Log("obsticle Target assigned: " + obstacleTarget + "++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
        //     if (obstacleTarget != null)
        //     {
        //         Debug.Log("obsticle Target not null: " + obstacleTarget);
        //         // obstacleTarget = block;
        //         currentTarget = obstacleTarget;
        //         // obstacleTarget = block;
        //         // mainTarget = block;
        //         // obstacleTarget = GetBlockingObject();
        //     }
        //     else
        //     {
        //         currentTarget = mainTarget; // fallback
        //         // Debug.Log("Fallabck-------------------------------------");
        //     }

        //     // Debug.Log("currentarget is new: " + obstacleTarget + "++++++++++++++++++++++++++++++++++++");
        //     // if (obstacleTarget != null)
        //     // {
        //     //     currentTarget = obstacleTarget;
        //     //     Debug.Log("currentarget is new: " + currentTarget + "--------------------------------------------------------------");
        //     // }
        //     // No valid path, attack obstacles in the way
        //     // MonoBehaviour blockingObject = GetBlockingObject();
        //     // if (blockingObject != null)
        //     // {
        //     //     currentTarget = blockingObject; // temporarily target the wall
        //     //     AttackIfInRange(); // start attacking immediately if in range
        //     //     return; // skip normal movement this frame
        //     // }
        // }
        // else
        // {
        //     Debug.Log("WEeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee");
        //     obstacleTarget = null;
        //     currentTarget = mainTarget;
        // }
        // //--------------------------------------------------------
        // float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        // if (distance <= data.StoppingDistance)
        // {
        //     agent.ResetPath();
        // }
        // else
        // {
        //     Debug.Log("current target is: " + currentTarget + "\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\");
        //     agent.SetDestination(currentTarget.transform.position);

        // }
        agent.SetDestination(currentTarget.transform.position);

        // agent.SetDestination(currentTarget.transform.position);

        // Collider targetCollider = currentTarget.GetComponent<Collider>();
        // // Vector3 newAttackPosition = targetCollider != null ? targetCollider.ClosestPoint(transform.position) : currentTarget.transform.position;
        // Vector3 newAttackPosition = targetCollider.ClosestPoint(transform.position);
        // newAttackPosition = Vector3.MoveTowards(newAttackPosition, transform.position, 1.5f); // 0.5 units closer

        // if (currentAttackPosition == Vector3.zero ||
        // Vector3.Distance(newAttackPosition, currentAttackPosition) > attackPositionUpdateThreshold)
        // {
        //     currentAttackPosition = newAttackPosition;
        // }

        // float distance = Vector3.Distance(transform.position, currentAttackPosition);

        // if (distance > agent.stoppingDistance)
        //     agent.SetDestination(currentAttackPosition);
        // else
        //     agent.ResetPath();


        // if (currentTarget != null &&
        // Vector3.Distance(agent.destination, currentTarget.transform.position) > 0.1f)
        // {
        //     agent.SetDestination(currentTarget.transform.position);
        // }

        // Debug.Log("this si the currtarget: " + currentTarget + "---------------------------------------");
        // if (currentTarget != null && Vector3.Distance(agent.destination, currentTarget.transform.position) > 0.1f)
        // {
        //     agent.SetDestination(currentTarget.transform.position);
        // }

        // Set destination once
        // if (currentTarget != null)
        // {
        //     if (Vector3.Distance(agent.destination, currentTarget.transform.position) > 0.1f)
        //     {
        //         agent.SetDestination(currentTarget.transform.position);
        //     }
        // }

        // if (obstacleTarget != null)
        // {
        //     UnityEngine.AI.NavMeshPath path1 = new UnityEngine.AI.NavMeshPath();
        //     bool pathFound1 = agent.CalculatePath(mainTarget.transform.position, path1);
        //     if (pathFound1 && path1.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
        //     {
        //         currentTarget = mainTarget;
        //         obstacleTarget = null;
        //     }
        // }

        // if (currentTarget != null)
        // {
        //     // Only update the destination if it's far from the previous destination
        //     if (Vector3.Distance(agent.destination, currentTarget.transform.position) > 0.1f)
        //     {
        //         agent.SetDestination(currentTarget.transform.position);
        //     }
        //     //  agent.SetDestination(currentTarget.transform.position);
        // }

        // SetAttackPosition();

        // Check for walls in front
        // if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out RaycastHit hit, jumpCheckDistance))
        // {
        //     if (hit.collider.CompareTag("Jumpable"))
        //     {
        //         StartCoroutine(JumpOver(hit.collider));
        //         return; // Skip normal movement this frame
        //     }
        // }

        // Collider targetCollider = currentTarget.GetComponent<Collider>();

        // Vector3 newAttackPosition;

        // if (Vector3.Distance(transform.position, currentAttackPosition) > agent.stoppingDistance)
        // {
        //     agent.SetDestination(currentAttackPosition);
        // }
        // else
        // {
        //     agent.ResetPath();
        // }

        //had this------------------------
        // if (targetCollider != null)
        // {
        //     newAttackPosition = targetCollider.ClosestPoint(transform.position);

        //     // Add a fixed random offset only once when we pick the target (to avoid jitter)
        //     if (currentAttackPosition == Vector3.zero ||
        //         Vector3.Distance(newAttackPosition, currentAttackPosition) > attackPositionUpdateThreshold)
        //     {
        //         currentAttackPosition = newAttackPosition + new Vector3(Random.Range(-0.3f, 0.3f), 0, Random.Range(-0.3f, 0.3f));
        //     }
        // }
        // else
        // {
        //     newAttackPosition = currentTarget.transform.position;

        //     if (currentAttackPosition == Vector3.zero ||
        //         Vector3.Distance(newAttackPosition, currentAttackPosition) > attackPositionUpdateThreshold)
        //     {
        //         currentAttackPosition = newAttackPosition;
        //     }
        // }

        // float distance = Vector3.Distance(transform.position, currentAttackPosition);

        // MonoBehaviour targetToCheck = obstacleTarget != null ? obstacleTarget : currentTarget;


        //had this--------------------
        // if (distance > agent.stoppingDistance)
        // {
        //     if (obstacleTarget == null)
        //     {
        //         agent.SetDestination(currentAttackPosition);
        //     }
        //     else
        //     {
        //         agent.SetDestination(obstacleTarget.transform.position);
        //     }
        // }
        // else
        // {
        //     agent.ResetPath();
        // }

        // Smooth rotation toward movement
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }

        // Small idle sway when stopped ------------------------ had this
        // if (!agent.hasPath)
        // {
        //     float swayAmount = 0.05f;
        //     float swaySpeed = 2f;
        //     Vector3 swayOffset = new Vector3(Mathf.Sin(Time.time * swaySpeed), 0, Mathf.Cos(Time.time * swaySpeed)) * swayAmount;
        //     transform.position += swayOffset * Time.deltaTime;
        // }
    }

    // private MonoBehaviour GetBlockingObject(UnityEngine.AI.NavMeshPath path)
    // {
    //     for (int i = 0; i < path.corners.Length - 1; i++)
    //     {
    //         Vector3 start = path.corners[i];
    //         Vector3 end = path.corners[i + 1];
    //         Vector3 dir = (end - start).normalized;
    //         float dist = Vector3.Distance(start, end);

    //         if (Physics.Raycast(start, dir, out RaycastHit hit, dist))
    //         {
    //             // Only treat walls/structures as blocking objects
    //             Debug.DrawRay(start, dir * dist, Color.red, 1f);
    //             if (hit.collider.CompareTag("Jumpable"))
    //             {
    //                 return hit.collider.GetComponent<DefenseStructure>();
    //             }
    //         }
    //     }
    //     return null;
    // }

    // private MonoBehaviour GetBlockingObjectDirect()
    // {
    //     if (mainTarget == null) return null;

    //     Vector3 directionToTarget = (mainTarget.transform.position - transform.position).normalized;
    //     float distanceToTarget = Vector3.Distance(transform.position, mainTarget.transform.position);

    //     if (Physics.Raycast(transform.position + Vector3.up * 0.5f, directionToTarget, out RaycastHit hit, distanceToTarget))
    //     {
    //         if (hit.collider.CompareTag("Jumpable"))
    //             return hit.collider.GetComponent<DefenseStructure>();
    //     }

    //     return null;
    // }
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
                    Debug.Log("Blocking object detected: " + hit.collider.name);
                    return hit.collider.GetComponent<DefenseStructure>();
                }
                else if (hit.collider.GetComponent<AnimalStructure>() != null)
                {
                    Debug.Log("Blocking object detected: " + hit.collider.name);
                    return hit.collider.GetComponent<AnimalStructure>();
                }
                else if (hit.collider.GetComponent<BarracksStructure>() != null)
                {
                    Debug.Log("Blocking object detected: " + hit.collider.name);
                    return hit.collider.GetComponent<BarracksStructure>();
                }
                else if (hit.collider.GetComponent<Structure>() != null)
                {
                    Debug.Log("Blocking object detected: " + hit.collider.name);
                    return hit.collider.GetComponent<Structure>();
                }
                else if (hit.collider.GetComponent<CropStructure>() != null)
                {
                    Debug.Log("Blocking object detected: " + hit.collider.name);
                    return hit.collider.GetComponent<CropStructure>();
                }
                else if (hit.collider.GetComponent<FarmHouseStructure>() != null)
                {
                    Debug.Log("Blocking object detected: " + hit.collider.name);
                    return hit.collider.GetComponent<FarmHouseStructure>();
                }
                // else if (hit.collider.GetComponent<CropStructure>() != null)
                // {
                //     Debug.Log("Blocking object detected: " + hit.collider.name);
                //     return hit.collider.GetComponent<CropStructure>();
                // }
                // else if (hit.collider.GetComponent<CropStructure>() != null)
                // {
                //     Debug.Log("Blocking object detected: " + hit.collider.name);
                //     return hit.collider.GetComponent<CropStructure>();
                // }
            }
        }

        return null;
    }


    // private MonoBehaviour GetBlockingObject(UnityEngine.AI.NavMeshPath path)
    // {
    //     // UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
    //     // if (!agent.CalculatePath(mainTarget.transform.position, path))
    //     // return null;

    //     // Debug.Log("2342342334244444444444444444444444444444z44444444444444444444444444444444444444");
    //     Debug.Log("Path corners count: " + path.corners.Length);
    //     for (int i = 0; i < path.corners.Length - 1; i++)
    //     {
    //         Debug.Log("7777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777");
    //         Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.red, 2f);
    //         Vector3 start = path.corners[i];
    //         Vector3 end = path.corners[i + 1];
    //         Vector3 dir = (end - start).normalized;
    //         float dist = Vector3.Distance(start, end) + 60f;

    //         if (Physics.Raycast(start, dir, out RaycastHit hit, dist))
    //         {
    //             Debug.Log("Hit: " + hit.collider.name + " | Tag: " + hit.collider.tag + "**************************************************************************************************************");
    //             if (hit.collider.CompareTag("Jumpable"))
    //                 return hit.collider.GetComponent<DefenseStructure>();
    //         }
    //         else
    //         {
    //             Debug.Log("Raycast missed from " + start + " to " + end);
    //         }
    //     }
    //     return null;
    // }
    // private MonoBehaviour GetBlockingObject()
    // {
    //     Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * 1f, 1f);
    //     foreach (var hit in hits)
    //     {
    //         if (hit.CompareTag("Jumpable")) // or Layer check
    //         {
    //             return hit.GetComponent<DefenseStructure>();
    //         }
    //     }
    //     return null;
    // }

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


    // private void SetAttackPosition()
    // {
    //     if (currentTarget == null) return;

    //     Collider targetCollider = currentTarget.GetComponent<Collider>();
    //     if (targetCollider != null)
    //     {
    //         // Get closest point on the target collider to this enemy's current position
    //         currentAttackPosition = targetCollider.ClosestPoint(transform.position);
    //     }
    //     else
    //     {
    //         currentAttackPosition = currentTarget.transform.position;
    //     }

    //     hasAttackPosition = true;
    // }

    private void AttackIfInRange()
    {
        if (currentTarget == null || IsTargetDead(currentTarget))
            return;

        Collider enemyCollider = GetComponent<Collider>();
        Collider targetCollider = currentTarget.GetComponent<Collider>();

        if (enemyCollider == null || targetCollider == null)
            return;

        // Distance between closest points of colliders
        Vector3 closestPointEnemy = enemyCollider.ClosestPoint(targetCollider.transform.position);
        Vector3 closestPointTarget = targetCollider.ClosestPoint(closestPointEnemy);
        float distanceBetween = Vector3.Distance(closestPointEnemy, closestPointTarget);

        // Attack range buffer (tweak this)
        float attackRange = 2f;

        if (distanceBetween <= attackRange)
        {
            if (Time.time >= lastAttackTime + data.AttackCooldown)
            {
                lastAttackTime = Time.time;
                PlaySound(data.AttackSound);
                Attack(currentTarget);
            }
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
    // private MonoBehaviour GetNearestAggroTarget()
    // {
    //     var aggroThings = GetAggroThingsInRange();
    //     // Debug.Log($"Aggro Things Count: {aggroThings.Count}+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
    //     MonoBehaviour nearest = null;
    //     float closestDist = float.MaxValue;

    //     foreach (var obj in aggroThings)
    //     {
    //         if (obj is MonoBehaviour mb)
    //         {
    //             float dist = Vector3.Distance(transform.position, mb.transform.position);
    //             // Debug.Log($"Found target: {mb.name} ({mb.GetType().Name}) at distance {dist}****************************************************************************");
    //             if (dist < closestDist)
    //             {
    //                 closestDist = dist;
    //                 nearest = mb;
    //             }
    //         }
    //     }

    //     return nearest;
    // }

    private MonoBehaviour GetNearestAggroTargetOptimized()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, data.AttackRange);
        MonoBehaviour nearest = null;
        float closestDist = float.MaxValue;

        foreach (var col in hits)
        {
            MonoBehaviour candidate = null;

            switch (data.AttType)
            {
                case AttType.Animals:
                    candidate = col.GetComponent<ArmyUnit>();
                    // Debug.Log("animal attack: " + candidate);
                    break;

                case AttType.Resources:
                    CropStructure crop = col.GetComponent<CropStructure>();
                    if (crop != null)
                        candidate = crop;
                    else
                    {
                        SiloStructure silo = col.GetComponent<SiloStructure>();
                        if (silo != null)
                            candidate = silo;
                    }
                    break;

                case AttType.Defense:
                    candidate = col.GetComponent<DefenseStructure>();
                    // Debug.Log("Defence attack: " + candidate);
                    break;

                case AttType.Buildings:
                    {
                        FarmHouseStructure farmHouse = col.GetComponent<FarmHouseStructure>();
                        if (farmHouse != null)
                            candidate = farmHouse;
                        else
                        {
                            CropStructure crop2 = col.GetComponent<CropStructure>();
                            if (crop2 != null)
                                candidate = crop2;
                            else
                            {
                                SiloStructure silo2 = col.GetComponent<SiloStructure>();
                                if (silo2 != null)
                                    candidate = silo2;
                                else
                                {
                                    BarracksStructure barracks = col.GetComponent<BarracksStructure>();
                                    if (barracks != null)
                                        candidate = barracks;
                                    else
                                    {
                                        AnimalStructure animal = col.GetComponent<AnimalStructure>();
                                        if (animal != null)
                                            candidate = animal;
                                    }
                                }
                            }
                        }
                    }
                    // Debug.Log("building attack: " + candidate);
                    break;
            }

            if (candidate != null && !IsTargetDead(candidate))
            {
                float dist = Vector3.Distance(transform.position, candidate.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    nearest = candidate;
                }
            }
        }

        // If nothing found, attack the nearest target of any type
        if (nearest == null)
        {
            Debug.Log("There was nothing so we go for anything!");
            // Fallback: find the nearest *any* target if no preferred AttType target was found
            Collider[] fallbackHits = Physics.OverlapSphere(transform.position, data.AttackRange);

            foreach (var col in fallbackHits)
            {
                MonoBehaviour candidate = null;

                if (col.TryGetComponent<ArmyUnit>(out var army)) candidate = army;
                else if (col.TryGetComponent<CropStructure>(out var crop)) candidate = crop;
                else if (col.TryGetComponent<SiloStructure>(out var silo)) candidate = silo;
                else if (col.TryGetComponent<FarmHouseStructure>(out var farm)) candidate = farm;
                else if (col.TryGetComponent<BarracksStructure>(out var barracks)) candidate = barracks;
                else if (col.TryGetComponent<AnimalStructure>(out var animal)) candidate = animal;
                else if (col.TryGetComponent<DefenseStructure>(out var defense))
                {
                    candidate = defense;
                    Debug.Log("WE found a defense");
                }

                if (candidate != null && !IsTargetDead(candidate))
                {
                    float dist = Vector3.Distance(transform.position, candidate.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        nearest = candidate;
                    }
                }
            }
        }


        //still nothing found, go to map center
        if (nearest == null)
        {
            hasNoTarget = true;
            agent.SetDestination(Vector3.zero); // You can change this to any other fallback location
        }

        return nearest;
    }



    // public List<object> GetAggroThingsInRange()
    // {
    //     List<object> targets = new();
    //     GridController grid = GridController.Instance;

    //     // Get all cells within attack range (radius in grid cells)
    //     List<GridCell> cellsInRange = grid.GetCellsInRange(transform.position, data.AttackRange);

    //     foreach (GridCell cell in cellsInRange)
    //     {
    //         Vector3 cellWorldPos = cell.worldPosition;
    //         Collider[] hits = Physics.OverlapSphere(cellWorldPos, grid.GetCellSize() * 0.4f);

    //         foreach (Collider col in hits)
    //         {
    //             switch (data.AttType)
    //             {
    //                 case AttType.Animals:
    //                     ArmyUnit army = col.GetComponent<ArmyUnit>();
    //                     if (army != null && !army.IsDead() && !targets.Contains(army)) targets.Add(army);
    //                     break;

    //                 case AttType.Resources:
    //                     if (col.GetComponent<CropStructure>() is var crop && crop != null) targets.Add(crop);
    //                     if (col.GetComponent<SiloStructure>() is var silo && silo != null) targets.Add(silo);
    //                     break;

    //                 case AttType.Defense:
    //                     if (col.GetComponent<DefenseStructure>() is var def && def != null) targets.Add(def);
    //                     break;

    //                 case AttType.Buildings:
    //                     AddIfFound<FarmHouseStructure>(col, targets);
    //                     AddIfFound<CropStructure>(col, targets);
    //                     AddIfFound<SiloStructure>(col, targets);
    //                     AddIfFound<BarracksStructure>(col, targets);
    //                     AddIfFound<AnimalStructure>(col, targets);
    //                     break;
    //             }
    //         }
    //     }

    //     return targets;

    //     void AddIfFound<T>(Collider col, List<object> list) where T : MonoBehaviour
    //     {
    //         T comp = col.GetComponent<T>();
    //         if (comp != null && !list.Contains(comp)) list.Add(comp);
    //     }
    // }

    public void TakeDamage(int damage)
    {
        if (currHealth <= 0 || currHealth - damage <= 0)
        {
            // Debug.Log("Died: ----------------------------------------------------------------------------------");
            PlaySound(data.DeathSound);
            Die();
        }
        else
        {
            Debug.Log("Taking damage: " + damage + "----------------------------------------------------------------------------------");
            PlaySound(data.HurtSound);
            currHealth -= damage;
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
            FarmHouseStructure u => u.IsDead(),
            BarracksStructure u => u.IsDead(),
            AnimalStructure u => u.IsDead(),
            _ => true // Assume dead if type unknown
        };
    }

    private void Attack(MonoBehaviour target)
    {
        switch (target)
        {
            case ArmyUnit u:
                u.TakeDamage(data.AttackDamage);
                PlaySound(data.AttackSound);
                // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
                break;
            case CropStructure u:
                PlaySound(data.AttackSound);
                u.TakeDamage(data.AttackDamage);
                DamageAnimation anim = u.GetComponent<DamageAnimation>();
                if (anim != null)
                    anim.PlayDamageHitEffect();
                // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
                break;
            case SiloStructure u:
                PlaySound(data.AttackSound);
                u.TakeDamage(data.AttackDamage);
                anim = u.GetComponent<DamageAnimation>();
                if (anim != null)
                    anim.PlayDamageHitEffect();
                // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
                break;
            case DefenseStructure u:
                u.TakeDamage(data.AttackDamage);
                PlaySound(data.AttackSound);
                anim = u.GetComponent<DamageAnimation>();
                if (anim != null)
                    anim.PlayDamageHitEffect();
                break;
            case FarmHouseStructure u:
                PlaySound(data.AttackSound);
                u.TakeDamage(data.AttackDamage);
                anim = u.GetComponent<DamageAnimation>();
                if (anim != null)
                    anim.PlayDamageHitEffect();
                // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
                break;
            case BarracksStructure u:
                PlaySound(data.AttackSound);
                u.TakeDamage(data.AttackDamage);
                anim = u.GetComponent<DamageAnimation>();
                if (anim != null)
                    anim.PlayDamageHitEffect();
                // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
                break;
            case AnimalStructure u:
                PlaySound(data.AttackSound);
                u.TakeDamage(data.AttackDamage);
                anim = u.GetComponent<DamageAnimation>();
                if (anim != null)
                    anim.PlayDamageHitEffect();
                // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
                break;
        }
    }


    // public void increaseAfterNight()
    // {
    //     // data.maxSpawnAmount += data.nightlySpawnMultiplier;
    //     // data.minSpawnAmount += data.nightlySpawnMultiplier;
    //     // Debug.Log($"max: {data.maxSpawnAmount} min: {data.minSpawnAmount}************************************************");

    // }
    // public void increaseAfterSeason()
    // {
    //     // data.maxSpawnAmount = (int)(data.maxSpawnAmount * data.seasonSpawnMultiplier);
    //     // data.minSpawnAmount = (int)(data.minSpawnAmount * data.seasonSpawnMultiplier);
    //     // Debug.Log($"max: {data.maxSpawnAmount} min: {data.minSpawnAmount}===============================================");

    //     // data.nightlySpawnMultiplier = (int)(data.nightlySpawnMultiplier * data.seasonSpawnMultiplier);
    //     // Debug.Log($"nightly things: {data.nightlySpawnMultiplier}=====================================================");
    // }

    private Vector3 GetRandomOutsidePosition()
    {
        if (_gridDataGenerator == null) return Vector3.zero;

        int width = _gridDataGenerator.GetGridWidth();
        int height = _gridDataGenerator.GetGridHeight();
        // Debug.Log($"Grid Size: {width}x{height}+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++------------------------------------------------------");

        while (true) // Keep trying until valid
        {
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
    }

    // private IEnumerator TryActionCoroutine()
    // {
    //     while (!actionSucceeded)
    //     {
    //         // Try your action
    //         actionSucceeded = TryAction();

    //         if (!actionSucceeded)
    //         {
    //             // Wait 2 seconds before trying again
    //             yield return new WaitForSeconds(2f);
    //         }
    //     }

    //     // Action succeeded, continue
    //     Debug.Log("Action succeeded!");
    // }

    // private bool moveAnimal(Vector3 end)
    // {
    //     agent.SetDestination(end);
    // }
}