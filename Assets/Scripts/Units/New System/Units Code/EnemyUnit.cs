using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
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

    [SerializeField] private GameObject healthBarPrefab;
    private GameObject healthBarInstance;
    private Slider healthBarSlider;
    private TextMeshProUGUI healthBarText;
    private CanvasGroup healthBarCanvasGroup;

    public int currentMaxSpawn;

    public int currentMinSpawn;

    public bool retreating = false;
    private Vector3 destination;

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

        HandleTargetingAndMovement();
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

        if (currentTarget == null || IsTargetDead(currentTarget))
        {
            // currentTarget = GetNearestAggroTarget();
            currentTarget = GetNearestAggroTargetOptimized();
            if (currentTarget == null)
            {
                agent.ResetPath();
                return;
            }
            // Reset stored position when target changes
            currentAttackPosition = Vector3.zero;
        }

        Collider targetCollider = currentTarget.GetComponent<Collider>();

        Vector3 newAttackPosition;

        if (targetCollider != null)
        {
            newAttackPosition = targetCollider.ClosestPoint(transform.position);

            // Add a fixed random offset only once when we pick the target (to avoid jitter)
            if (currentAttackPosition == Vector3.zero ||
                Vector3.Distance(newAttackPosition, currentAttackPosition) > attackPositionUpdateThreshold)
            {
                currentAttackPosition = newAttackPosition + new Vector3(Random.Range(-0.3f, 0.3f), 0, Random.Range(-0.3f, 0.3f));
            }
        }
        else
        {
            newAttackPosition = currentTarget.transform.position;

            if (currentAttackPosition == Vector3.zero ||
                Vector3.Distance(newAttackPosition, currentAttackPosition) > attackPositionUpdateThreshold)
            {
                currentAttackPosition = newAttackPosition;
            }
        }

        float distance = Vector3.Distance(transform.position, currentAttackPosition);

        if (distance > agent.stoppingDistance)
        {
            agent.SetDestination(currentAttackPosition);
        }
        else
        {
            agent.ResetPath();
        }

        // Smooth rotation toward movement
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }

        // Small idle sway when stopped
        if (!agent.hasPath)
        {
            float swayAmount = 0.05f;
            float swaySpeed = 2f;
            Vector3 swayOffset = new Vector3(Mathf.Sin(Time.time * swaySpeed), 0, Mathf.Cos(Time.time * swaySpeed)) * swayAmount;
            transform.position += swayOffset * Time.deltaTime;
        }
    }


    private void SetAttackPosition()
    {
        if (currentTarget == null) return;

        Collider targetCollider = currentTarget.GetComponent<Collider>();
        if (targetCollider != null)
        {
            // Get closest point on the target collider to this enemy's current position
            currentAttackPosition = targetCollider.ClosestPoint(transform.position);
        }
        else
        {
            currentAttackPosition = currentTarget.transform.position;
        }

        hasAttackPosition = true;
    }

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
        float attackRange = 0.1f;

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



    public List<object> GetAggroThingsInRange()
    {
        List<object> targets = new();
        GridController grid = GridController.Instance;

        // Get all cells within attack range (radius in grid cells)
        List<GridCell> cellsInRange = grid.GetCellsInRange(transform.position, data.AttackRange);

        foreach (GridCell cell in cellsInRange)
        {
            Vector3 cellWorldPos = cell.worldPosition;
            Collider[] hits = Physics.OverlapSphere(cellWorldPos, grid.GetCellSize() * 0.4f);

            foreach (Collider col in hits)
            {
                switch (data.AttType)
                {
                    case AttType.Animals:
                        ArmyUnit army = col.GetComponent<ArmyUnit>();
                        if (army != null && !army.IsDead() && !targets.Contains(army)) targets.Add(army);
                        break;

                    case AttType.Resources:
                        if (col.GetComponent<CropStructure>() is var crop && crop != null) targets.Add(crop);
                        if (col.GetComponent<SiloStructure>() is var silo && silo != null) targets.Add(silo);
                        break;

                    case AttType.Defense:
                        // if (col.GetComponent<DefenseStructure>() is var def && def != null) targets.Add(def);
                        break;

                    case AttType.Buildings:
                        AddIfFound<FarmHouseStructure>(col, targets);
                        AddIfFound<CropStructure>(col, targets);
                        AddIfFound<SiloStructure>(col, targets);
                        AddIfFound<BarracksStructure>(col, targets);
                        AddIfFound<AnimalStructure>(col, targets);
                        break;
                }
            }
        }

        return targets;

        void AddIfFound<T>(Collider col, List<object> list) where T : MonoBehaviour
        {
            T comp = col.GetComponent<T>();
            if (comp != null && !list.Contains(comp)) list.Add(comp);
        }
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
            PlaySound(data.DeathSound);
            currHealth = 0;
            UpdateHealthBar();
            Die();
        }
        else
        {
            PlaySound(data.HurtSound);
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
            // DefenseStructure u => u.IsDead(),
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
                // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
                break;
            case SiloStructure u:
                PlaySound(data.AttackSound);
                u.TakeDamage(data.AttackDamage);
                // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
                break;
            // case DefenseStructure u: u.TakeDamage(data.AttackDamage); break;
            case FarmHouseStructure u:
                PlaySound(data.AttackSound);
                u.TakeDamage(data.AttackDamage);
                // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
                break;
            case BarracksStructure u:
                PlaySound(data.AttackSound);
                u.TakeDamage(data.AttackDamage);
                // Debug.Log($"Attacking {target.name} with {data.AttackDamage} damage.");
                break;
            case AnimalStructure u:
                PlaySound(data.AttackSound);
                u.TakeDamage(data.AttackDamage);
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

        agent.SetDestination(destination);
    }
}