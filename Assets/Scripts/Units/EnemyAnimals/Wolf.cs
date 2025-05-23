using UnityEngine;
using FarmDefender.Core.AI.FlowField;
using System.Collections.Generic;
using System.Linq;

public class Wolf : MonoBehaviour
{
    [Header("Wolf Stats")]
    [SerializeField] private float baseAttackRange = 2f;
    [SerializeField] private int damage = 50;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private float detectionRange = 20f;
    [SerializeField] private float globalSearchInterval = 0.5f;

    [Header("Target Priorities (Higher = More Priority)")]
    [SerializeField] private int chickenPriority = 10;
    [SerializeField] private int structurePriority = 5;
    [SerializeField] private float healthWeight = 0.5f;
    [SerializeField] private float animalCountWeight = 0.5f;

    [Header("Components")]
    [SerializeField] private FlowFieldAgent flowFieldAgent;
    [SerializeField] private Animator animator;
    [SerializeField] private string walkAnimParam = "IsWalking";
    [SerializeField] private string attackAnimParam = "Attack";

    private int currentHealth;
    private float lastAttackTime;
    private GameObject target;
    private Vector3 targetAttackPoint;
    private NightManager nightManager;
    private FlowFieldManager flowFieldManager;
    private float targetUpdateTimer = 0f;
    private float globalSearchTimer = 0f;
    private readonly List<GameObject> cachedTargets = new List<GameObject>();

    private void Awake()
    {
        if (flowFieldAgent == null)
            flowFieldAgent = GetComponent<FlowFieldAgent>() ?? gameObject.AddComponent<FlowFieldAgent>();
        if (animator == null)
            animator = GetComponent<Animator>();

        flowFieldManager = FindObjectOfType<FlowFieldManager>();
        nightManager = NightManager.Instance;

        if (flowFieldManager == null || nightManager == null)
        {
            Debug.LogError($"Wolf {name} missing required components: FlowFieldManager={flowFieldManager}, NightManager={nightManager}");
            Destroy(gameObject);
            return;
        }

        Debug.Log($"🐺 Wolf {name} initialized at {transform.position}");
    }

    private void Start()
    {
        currentHealth = maxHealth;
        nightManager.RegisterWolf(this);
        Structure.RegisterWolf(this);
        flowFieldAgent.SetMoving(true);

        if (animator != null)
            animator.SetBool(walkAnimParam, true);

        CacheTargets();
        FindTargetWithPriority();
    }

    private void OnDestroy()
    {
        if (nightManager != null)
            nightManager.UnregisterWolf(this);
        Structure.UnregisterWolf(this);
    }

    private void Update()
    {
        if (GameLoopManager.Instance.IsGameOver || nightManager.IsDay)
        {
            Die();
            return;
        }

        targetUpdateTimer -= Time.deltaTime;
        globalSearchTimer -= Time.deltaTime;

        if (targetUpdateTimer <= 0f)
        {
            FindNearbyTarget();
            targetUpdateTimer = 2f;
        }

        if (globalSearchTimer <= 0f)
        {
            CacheTargets();
            FindTargetWithPriority();
            globalSearchTimer = globalSearchInterval;
        }

        if (target != null && IsValidTarget(target))
        {
            float distance = Vector3.Distance(transform.position, targetAttackPoint);
            float effectiveAttackRange = GetEffectiveAttackRange(target);

            if (distance <= effectiveAttackRange)
            {
                flowFieldAgent.SetMoving(false);
                if (animator != null)
                    animator.SetBool(walkAnimParam, false);
                AttackTarget();
            }
            else
            {
                flowFieldAgent.SetMoving(true);
                if (animator != null)
                    animator.SetBool(walkAnimParam, true);
            }
        }
        else
        {
            flowFieldAgent.SetMoving(true);
            if (animator != null)
                animator.SetBool(walkAnimParam, true);
            target = null;
            cachedTargets.RemoveAll(go => go == null || !go);
            FindTargetWithPriority();
        }
    }

    private void CacheTargets()
    {
        cachedTargets.Clear();
        cachedTargets.AddRange(FindObjectsOfType<ArmyAnimal>()
            .Where(a => a != null && a.gameObject != null && a.gameObject.activeInHierarchy)
            .Select(a => a.gameObject));
        cachedTargets.AddRange(FindObjectsOfType<Structure>()
            .Where(s => s != null && s.gameObject != null && s.gameObject.activeInHierarchy && !s.isIndestructible)
            .Select(s => s.gameObject));
        Debug.Log($"Wolf {name} cached {cachedTargets.Count} potential targets");
    }

    private void FindNearbyTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange);
        GameObject bestTarget = null;
        Vector3 bestAttackPoint = Vector3.zero;
        float bestScore = float.MinValue;

        foreach (Collider col in colliders)
        {
            if (col == null || col.gameObject == null) continue;
            GameObject go = col.gameObject;
            if (!IsValidTarget(go)) continue;

            float score = CalculateTargetScore(go, out Vector3 attackPoint);
            if (score > bestScore)
            {
                bestTarget = go;
                bestAttackPoint = attackPoint;
                bestScore = score;
            }
        }

        if (bestTarget != null)
            SetTarget(bestTarget, bestAttackPoint);
    }

    private void FindTargetWithPriority()
    {
        GameObject bestTarget = null;
        Vector3 bestAttackPoint = Vector3.zero;
        float bestScore = float.MinValue;

        cachedTargets.RemoveAll(go => go == null || !go);

        foreach (GameObject go in cachedTargets)
        {
            if (!IsValidTarget(go)) continue;

            float score = CalculateTargetScore(go, out Vector3 attackPoint);
            if (score > bestScore)
            {
                bestTarget = go;
                bestAttackPoint = attackPoint;
                bestScore = score;
            }
        }

        if (bestTarget != null)
            SetTarget(bestTarget, bestAttackPoint);
        else
            Debug.Log($"Wolf {name} found no valid targets");
    }

    private bool IsValidTarget(GameObject go)
    {
        return go != null && go && go.activeInHierarchy;
    }

    private float CalculateTargetScore(GameObject go, out Vector3 attackPoint)
    {
        attackPoint = go.transform.position;
        float distance = Vector3.Distance(transform.position, go.transform.position);
        int priority = 0;
        float healthFactor = 0f;
        float animalCountFactor = 0f;

        ArmyAnimal chicken = go.GetComponent<ArmyAnimal>();
        if (chicken != null)
        {
            priority = chickenPriority;
            attackPoint = GetNearestPointOnBounds(go, transform.position);
        }
        else
        {
            Structure structure = go.GetComponent<Structure>();
            if (structure != null && !structure.isIndestructible)
            {
                priority = structurePriority;
                attackPoint = GetNearestPointOnBounds(go, transform.position);
                int currentHealth = structure.GetCurrentHealth();
                int maxHealth = structure.GetMaxHealth();
                healthFactor = maxHealth > 0 ? (1f - (float)currentHealth / maxHealth) : 1f;
                AnimalStructure animalStructure = structure as AnimalStructure;
                if (animalStructure != null)
                    animalCountFactor = Mathf.Clamp01((float)animalStructure.AnimalCount / 10f);
            }
        }

        if (priority <= 0)
            return float.MinValue;

        float distancePenalty = distance / detectionRange;
        float score = priority - distancePenalty + (healthWeight * healthFactor) + (animalCountWeight * animalCountFactor);
        return score;
    }

    private Vector3 GetNearestPointOnBounds(GameObject go, Vector3 fromPosition)
    {
        Collider col = go.GetComponent<Collider>();
        if (col != null && col)
            return col.ClosestPoint(fromPosition);
        return go.transform.position;
    }

    private float GetEffectiveAttackRange(GameObject go)
    {
        Collider col = go.GetComponent<Collider>();
        if (col != null && col)
        {
            Vector3 size = col.bounds.size;
            float maxDimension = Mathf.Max(size.x, size.y, size.z);
            return baseAttackRange + maxDimension * 0.5f;
        }
        return baseAttackRange;
    }

    private void SetTarget(GameObject newTarget, Vector3 attackPoint)
    {
        if (newTarget == null || newTarget == target || !IsValidTarget(newTarget)) return;

        target = newTarget;
        targetAttackPoint = attackPoint;
        Debug.Log($"Wolf {name} targeting {target.name} at attack point {targetAttackPoint}");

        if (flowFieldManager != null)
            flowFieldManager.SetTargetTransformWithPoint(target.transform, attackPoint);
    }

    private void AttackTarget()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;

        if (animator != null)
            animator.SetTrigger(attackAnimParam);

        // Double-check target validity
        if (target == null || !IsValidTarget(target))
        {
            target = null;
            FindTargetWithPriority();
            return;
        }

        target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        Debug.Log($"Wolf {name} attacked {target.name} for {damage} damage at {transform.position}");
    }

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);
        Debug.Log($"Wolf {name} took {amount} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        flowFieldAgent.SetMoving(false);
        if (animator != null)
            animator.SetBool(walkAnimParam, false);

        Debug.Log($"Wolf {name} died at {transform.position}");
        Destroy(gameObject);
    }

    public void OnDayNightChanged(bool isNight)
    {
        if (!isNight)
        {
            Debug.Log($"Wolf {name} dying due to day transition");
            Die();
        }
    }

    public void OnTargetDestroyed(GameObject destroyedTarget)
    {
        if (destroyedTarget == target)
        {
            target = null;
            targetAttackPoint = Vector3.zero;
            FindTargetWithPriority(); // Immediately find a new target
        }
        cachedTargets.Remove(destroyedTarget);
        Debug.Log($"Wolf {name} removed destroyed target {destroyedTarget?.name ?? "null"} from cache");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, baseAttackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (target != null && target)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetAttackPoint);
            Gizmos.DrawWireSphere(targetAttackPoint, 0.5f);
        }
    }
}