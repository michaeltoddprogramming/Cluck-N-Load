using UnityEngine;
using FarmDefender.Core.AI.FlowField;
using System.Collections.Generic;
using System.Linq;

public class Wolf : MonoBehaviour
{
    [Header("Performance Settings")]
    [SerializeField] private float targetCacheInterval = 5f; // Increased from 2f
    [SerializeField] private float nearbySearchInterval = 3f; // Reduced frequency
    [SerializeField] private int maxTargetsToConsider = 20; // Limit target search
    
    [Header("Wolf Stats")]
    [SerializeField] private float baseAttackRange = 3f;
    [SerializeField] private int damage = 20;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private float detectionRange = 20f;

    [Header("Target Priorities (Higher = More Priority)")]
    [SerializeField] private int chickenPriority = 10;
    [SerializeField] private int structurePriority = 5;
    [SerializeField] private float healthWeight = 0.5f;
    [SerializeField] private float animalCountWeight = 0.5f;

    [Header("Components")]
    [SerializeField] private FlowFieldAgent flowFieldAgent;

    [Header("Fallback Behavior")]
    [SerializeField] private float fallbackMoveRadius = 10f;
    [SerializeField] private float fallbackMoveInterval = 5f;

    [Header("Audio Behavior")]
    [SerializeField] private float minGrowlInterval = 5f; // Minimum time between growls
    [SerializeField] private float maxGrowlInterval = 20f; // Maximum time between growls

    private int currentHealth;
    private float lastAttackTime;
    private GameObject target;
    private Vector3 targetAttackPoint;
    private NightManager nightManager;
    private FlowFieldManager flowFieldManager;
    private float targetUpdateTimer = 0f;
    private float globalSearchTimer = 0f;
    private float fallbackMoveTimer = 0f;
    private float growlTimer = 0f;
    private GameObject fallbackTarget;
    private readonly List<GameObject> cachedTargets = new List<GameObject>();
    
    // Performance optimization caches
    private static readonly List<ArmyAnimal> _animalCache = new List<ArmyAnimal>();
    private static readonly List<Structure> _structureCache = new List<Structure>();
    private static float _lastGlobalCacheTime = -1f;
    private const float GLOBAL_CACHE_INTERVAL = 10f; // Share cache between all wolves
    private Vector3 _lastPosition;
    private float _targetSearchRangeSquared;

    // Events for animation and audio control
    public event System.Action OnStartMoving;
    public event System.Action OnStopMoving;
    public event System.Action OnAttack;
    public event System.Action OnGrowl;
    public event System.Action OnHurt;
    public event System.Action OnDeath;

    private void Awake()
    {
        if (flowFieldAgent == null)
            flowFieldAgent = GetComponent<FlowFieldAgent>() ?? gameObject.AddComponent<FlowFieldAgent>();

        flowFieldManager = FindFirstObjectByType<FlowFieldManager>();
        nightManager = NightManager.Instance;

        if (flowFieldManager == null || nightManager == null)
        {
            Debug.LogError($"Wolf {name} missing required components: FlowFieldManager={flowFieldManager}, NightManager={nightManager}");
            Destroy(gameObject);
            return;
        }

        fallbackTarget = new GameObject($"FallbackTarget_{name}");
        fallbackTarget.transform.position = transform.position;
        DontDestroyOnLoad(fallbackTarget);

        // Initialize growl timer
        growlTimer = Random.Range(minGrowlInterval, maxGrowlInterval);
        
        // Performance optimizations
        _targetSearchRangeSquared = detectionRange * detectionRange;
        _lastPosition = transform.position;
    }

    private void Start()
    {
        currentHealth = maxHealth;
        nightManager.RegisterWolf(this);
        Structure.RegisterWolf(this);
        flowFieldAgent.SetMoving(true);
        OnStartMoving?.Invoke();

        CacheTargets();
        FindTargetWithPriority();
        UpdateFallbackTarget();
    }

    private void CacheTargets()
    {
        // Legacy method for compatibility - now uses the optimized UpdateTargetCache
        UpdateTargetCache();
    }

    private void OnDestroy()
    {
        if (nightManager != null)
            nightManager.UnregisterWolf(this);
        Structure.UnregisterWolf(this);
        if (fallbackTarget != null)
            Destroy(fallbackTarget);
            
        // Clear cached targets to prevent memory leaks
        cachedTargets.Clear();
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
        fallbackMoveTimer -= Time.deltaTime;
        growlTimer -= Time.deltaTime;

        // Random growling
        if (growlTimer <= 0f)
        {
            OnGrowl?.Invoke();
            growlTimer = Random.Range(minGrowlInterval, maxGrowlInterval);
        }

        if (targetUpdateTimer <= 0f)
        {
            FindNearbyTarget();
            targetUpdateTimer = nearbySearchInterval; // Use configurable interval
        }

        if (globalSearchTimer <= 0f)
        {
            UpdateTargetCache();
            FindTargetWithPriority();
            globalSearchTimer = targetCacheInterval; // Use configurable interval
        }

        if (target != null && IsValidTargetFast(target))
        {
            // Use squared distance for performance
            Vector3 diff = transform.position - targetAttackPoint;
            float sqrDistance = diff.sqrMagnitude;
            float effectiveAttackRange = GetEffectiveAttackRange(target);
            float sqrAttackRange = effectiveAttackRange * effectiveAttackRange;

            if (sqrDistance <= sqrAttackRange)
            {
                flowFieldAgent.SetMoving(false);
                OnStopMoving?.Invoke();
                AttackTarget();
            }
            else
            {
                flowFieldAgent.SetMoving(true);
                OnStartMoving?.Invoke();
                flowFieldManager.SetTargetTransformWithPoint(target.transform, targetAttackPoint);
            }
        }
        else
        {
            target = null;
            cachedTargets.RemoveAll(go => go == null || !go);
            FindTargetWithPriority();

            if (target == null)
            {
                // If no targets found, move to fallback position
                if (fallbackMoveTimer <= 0f)
                {
                    UpdateFallbackTarget();
                    fallbackMoveTimer = fallbackMoveInterval;
                }
                flowFieldAgent.SetMoving(true);
                OnStartMoving?.Invoke();
                flowFieldManager.SetTargetTransformWithPoint(fallbackTarget.transform, fallbackTarget.transform.position);
                
                // If we've been without targets for too long, do more expensive search
                if (cachedTargets.Count == 0 && globalSearchTimer <= -2f) // Less frequent expensive search
                {
                    // Force a global search but only occasionally
                    RefreshGlobalCache();
                    UpdateTargetCache();
                    if (cachedTargets.Count == 0)
                    {
                        // No targets found anywhere - move towards center of map
                        UpdateFallbackTargetTowardsCenter();
                        globalSearchTimer = targetCacheInterval; // Reset timer
                    }
                }
            }
        }
    }

    private void UpdateTargetCache()
    {
        // Use shared static cache to reduce FindObjectsByType calls across all wolves
        if (Time.time - _lastGlobalCacheTime > GLOBAL_CACHE_INTERVAL)
        {
            RefreshGlobalCache();
            _lastGlobalCacheTime = Time.time;
        }
        
        // Update local cache from global cache with distance filtering
        cachedTargets.Clear();
        Vector3 currentPos = transform.position;
        
        // Add animals from global cache (limit to reduce iteration)
        int animalCount = 0;
        for (int i = 0; i < _animalCache.Count && animalCount < maxTargetsToConsider; i++)
        {
            var animal = _animalCache[i];
            if (animal != null && animal.gameObject.activeInHierarchy)
            {
                // Quick squared distance check (faster than Vector3.Distance)
                Vector3 diff = animal.transform.position - currentPos;
                if (diff.sqrMagnitude <= _targetSearchRangeSquared * 4f) // 2x range for caching
                {
                    cachedTargets.Add(animal.gameObject);
                    animalCount++;
                }
            }
        }
        
        // Add structures from global cache
        int structureCount = 0;
        for (int i = 0; i < _structureCache.Count && structureCount < maxTargetsToConsider; i++)
        {
            var structure = _structureCache[i];
            if (structure != null && structure.gameObject.activeInHierarchy && !structure.isIndestructible)
            {
                Vector3 diff = structure.transform.position - currentPos;
                if (diff.sqrMagnitude <= _targetSearchRangeSquared * 4f)
                {
                    cachedTargets.Add(structure.gameObject);
                    structureCount++;
                }
            }
        }
    }
    
    private static void RefreshGlobalCache()
    {
        _animalCache.Clear();
        _structureCache.Clear();
        
        // Single FindObjectsByType call shared between all wolves
        var animals = FindObjectsByType<ArmyAnimal>(FindObjectsSortMode.None);
        _animalCache.AddRange(animals);
        
        var structures = FindObjectsByType<Structure>(FindObjectsSortMode.None);
        _structureCache.AddRange(structures);
    }

    private void FindNearbyTarget()
    {
        // Skip if we haven't moved much (optimization for stationary wolves)
        Vector3 currentPos = transform.position;
        if (Vector3.SqrMagnitude(currentPos - _lastPosition) < 1f && target != null)
        {
            return;
        }
        _lastPosition = currentPos;
        
        // Use cached targets instead of Physics.OverlapSphere for better performance
        GameObject bestTarget = null;
        Vector3 bestAttackPoint = Vector3.zero;
        float bestScore = float.MinValue;
        
        // Limit iterations for performance
        int checkedCount = 0;
        foreach (GameObject go in cachedTargets)
        {
            if (checkedCount++ > maxTargetsToConsider) break;
            
            if (!IsValidTargetFast(go)) continue;
            
            // Use squared distance for performance
            Vector3 diff = go.transform.position - currentPos;
            float sqrDistance = diff.sqrMagnitude;
            
            if (sqrDistance <= _targetSearchRangeSquared)
            {
                float score = CalculateTargetScoreFast(go, sqrDistance, out Vector3 attackPoint);
                if (score > bestScore)
                {
                    bestTarget = go;
                    bestAttackPoint = attackPoint;
                    bestScore = score;
                }
            }
        }

        if (bestTarget != null)
        {
            SetTarget(bestTarget, bestAttackPoint);
        }
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
        {
            SetTarget(bestTarget, bestAttackPoint);
        }
        else
        {
            target = null;
        }
    }

    private bool IsValidTargetFast(GameObject go)
    {
        // Fast null check without expensive operations
        return go != null && go.activeInHierarchy;
    }
    
    private float CalculateTargetScoreFast(GameObject go, float sqrDistance, out Vector3 attackPoint)
    {
        attackPoint = go.transform.position;
        int priority = 0;
        float healthFactor = 0f;
        float animalCountFactor = 0f;

        // Cache component lookups to avoid repeated GetComponent calls
        ArmyAnimal chicken = go.GetComponent<ArmyAnimal>();
        if (chicken != null)
        {
            priority = chickenPriority;
            attackPoint = GetNearestPointOnBoundsFast(go, transform.position);
        }
        else
        {
            Structure structure = go.GetComponent<Structure>();
            if (structure != null && !structure.isIndestructible)
            {
                priority = structurePriority;
                attackPoint = GetNearestPointOnBoundsFast(go, transform.position);
                
                // Only calculate health factors for structures (more expensive)
                int currentHealth = structure.GetCurrentHealth();
                int maxHealth = structure.GetMaxHealth();
                healthFactor = maxHealth > 0 ? (1f - (float)currentHealth / maxHealth) : 1f;
                
                AnimalStructure animalStructure = structure as AnimalStructure;
                if (animalStructure != null)
                    animalCountFactor = Mathf.Clamp01((float)animalStructure.AnimalCount / 10f);
            }
        }

        if (priority <= 0)
        {
            return float.MinValue;
        }

        // Use squared distance to avoid expensive sqrt calculation
        float distancePenalty = sqrDistance / (_targetSearchRangeSquared);
        float score = priority - distancePenalty + (healthWeight * healthFactor) + (animalCountWeight * animalCountFactor);
        return score;
    }
    
    private Vector3 GetNearestPointOnBoundsFast(GameObject go, Vector3 fromPosition)
    {
        Collider col = go.GetComponent<Collider>();
        if (col != null && col.enabled)
            return col.ClosestPoint(fromPosition);
        return go.transform.position;
    }

    private bool IsValidTarget(GameObject go)
    {
        bool valid = go != null && go && go.activeInHierarchy && !go.Equals(null);
        if (!valid && go != null)
        {
            cachedTargets.Remove(go);
        }
        return valid;
    }

    private float CalculateTargetScore(GameObject go, out Vector3 attackPoint)
    {
        attackPoint = go.transform.position;
        Vector3 diff = go.transform.position - transform.position;
        float sqrDistance = diff.sqrMagnitude; // Use squared distance for performance
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
        {
            return float.MinValue;
        }

        float distancePenalty = sqrDistance / _targetSearchRangeSquared; // Use squared values
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
        if (flowFieldManager != null)
            flowFieldManager.SetTargetTransformWithPoint(target.transform, attackPoint);
    }

    private void AttackTarget()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;

        if (target == null || !IsValidTarget(target))
        {
                        target = null;
            FindTargetWithPriority();
            return;
        }

        OnAttack?.Invoke();

        try
        {
            if (target != null)
                target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            else
                Debug.LogWarning($"Wolf {name} aborted attack: Target null in try block");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Wolf {name} failed to attack: {e.Message}");
            target = null;
            FindTargetWithPriority();
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);
        OnHurt?.Invoke();

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        flowFieldAgent.SetMoving(false);
        OnDeath?.Invoke();
        
        // Unregister from managers before destroying
        if (nightManager != null)
            nightManager.UnregisterWolf(this);
        Structure.UnregisterWolf(this);
        
        // Clean up fallback target
        if (fallbackTarget != null)
            Destroy(fallbackTarget);
            
        // Actually destroy the wolf
        Destroy(gameObject);
    }

    public void OnDayNightChanged(bool isNight)
    {
        if (!isNight)
        {
            // Wolves should die/despawn when day comes
            Die();
        }
    }

    public void OnTargetDestroyed(GameObject destroyedTarget)
    {
        if (destroyedTarget == target)
        {
            target = null;
            targetAttackPoint = Vector3.zero;
            FindTargetWithPriority();
        }
        cachedTargets.Remove(destroyedTarget);
    }

    private void UpdateFallbackTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * fallbackMoveRadius;
        Vector3 newPosition = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        fallbackTarget.transform.position = newPosition;
    }

    private void UpdateFallbackTargetTowardsCenter()
    {
        // Move towards the center of the map when no targets exist
        Vector3 centerPosition = Vector3.zero; // Assuming map center is at origin
        Vector3 directionToCenter = (centerPosition - transform.position).normalized;
        Vector3 newPosition = transform.position + directionToCenter * fallbackMoveRadius;
        fallbackTarget.transform.position = newPosition;
        
        if (Random.value < 0.1f) // 10% chance to log this behavior
        {
            Debug.Log($"Wolf {name} moving towards center - no targets found");
        }
    }

    private void OnDrawGizmos()
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
        if (target == null && fallbackTarget != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, fallbackTarget.transform.position);
            Gizmos.DrawWireSphere(fallbackTarget.transform.position, 0.5f);
        }
    }
}