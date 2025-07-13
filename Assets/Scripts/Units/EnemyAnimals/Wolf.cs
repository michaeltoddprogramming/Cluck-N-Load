using UnityEngine;
using FarmDefender.Core.AI.FlowField;
using System.Collections.Generic;
using System.Linq;

public class Wolf : MonoBehaviour
{
    [Header("Performance Settings")]
    [SerializeField] private float targetCacheInterval = 3f; // Reduced from 5f for more responsive targeting
    [SerializeField] private float nearbySearchInterval = 1.5f; // Reduced from 3f for better responsiveness
    [SerializeField] private int maxTargetsToConsider = 30; // Increased from 20 to find more targets
    
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
    
    // Performance optimization caches - made non-static to prevent wolf interference
    private readonly List<ArmyAnimal> _localAnimalCache = new List<ArmyAnimal>();
    private readonly List<Structure> _localStructureCache = new List<Structure>();
    private float _lastLocalCacheTime = -1f;
    private const float LOCAL_CACHE_INTERVAL = 2f; // Each wolf has its own cache refresh
    
    // Shared static cache for initial discovery only
    private static readonly List<ArmyAnimal> _globalAnimalCache = new List<ArmyAnimal>();
    private static readonly List<Structure> _globalStructureCache = new List<Structure>();
    private static float _lastGlobalCacheTime = -1f;
    private const float GLOBAL_CACHE_INTERVAL = 8f; // Less frequent global updates
    
    private Vector3 _lastPosition;
    private float _targetSearchRangeSquared;
    private float _lastTargetDestroyTime = 0f; // Track when targets are destroyed
    private bool _isActivelyAttacking = false; // Track if this wolf is currently attacking

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
        
        // Ensure immediate movement and targeting
        _isActivelyAttacking = false;
        flowFieldAgent.SetMoving(true);
        OnStartMoving?.Invoke();

        // Force immediate cache refresh for this wolf
        RefreshLocalCache();
        CacheTargets();
        FindTargetWithPriority();
        
        // If no target found immediately, start moving towards potential targets
        if (target == null)
        {
            UpdateFallbackTarget();
            flowFieldManager.SetTargetTransformWithPoint(fallbackTarget.transform, fallbackTarget.transform.position);
        }
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
                _isActivelyAttacking = true;
                flowFieldAgent.SetMoving(false);
                OnStopMoving?.Invoke();
                AttackTarget();
            }
            else
            {
                _isActivelyAttacking = false;
                flowFieldAgent.SetMoving(true);
                OnStartMoving?.Invoke();
                flowFieldManager.SetTargetTransformWithPoint(target.transform, targetAttackPoint);
            }
        }
        else
        {
            _isActivelyAttacking = false;
            target = null;
            
            // More aggressive cleanup of invalid targets
            cachedTargets.RemoveAll(go => go == null || !go || !IsValidTargetFast(go));
            
            // Force a fresh search when we lose our target (but only for this wolf)
            bool shouldForceRefresh = Time.time - _lastTargetDestroyTime > 0.5f; // Reduced from 1f for faster response
            if (shouldForceRefresh)
            {
                RefreshLocalCache(); // Use local cache instead of global
                UpdateTargetCache();
                _lastTargetDestroyTime = Time.time;
            }
            
            FindTargetWithPriority();

            if (target == null)
            {
                // If no targets found, move to fallback position more aggressively
                if (fallbackMoveTimer <= 0f)
                {
                    UpdateFallbackTarget();
                    fallbackMoveTimer = fallbackMoveInterval * 0.5f; // Faster fallback updates
                }
                flowFieldAgent.SetMoving(true);
                OnStartMoving?.Invoke();
                flowFieldManager.SetTargetTransformWithPoint(fallbackTarget.transform, fallbackTarget.transform.position);
                
                // If we've been without targets for too long, do more aggressive search
                if (cachedTargets.Count == 0 && globalSearchTimer <= -0.5f) // Even more frequent expensive search
                {
                    // Force a local search more aggressively
                    RefreshLocalCache();
                    UpdateTargetCache();
                    if (cachedTargets.Count == 0)
                    {
                        // No targets found anywhere - move towards center of map
                        UpdateFallbackTargetTowardsCenter();
                        globalSearchTimer = targetCacheInterval * 0.25f; // Reset timer very aggressively
                    }
                    else
                    {
                        // Found targets, reset timer normally
                        globalSearchTimer = targetCacheInterval;
                    }
                }
            }
        }
    }

    private void UpdateTargetCache()
    {
        // Each wolf maintains its own cache to prevent interference
        bool forceRefresh = Time.time - _lastLocalCacheTime > LOCAL_CACHE_INTERVAL;
        bool targetWasDestroyed = Time.time - _lastTargetDestroyTime < 2f;
        
        if (forceRefresh || targetWasDestroyed)
        {
            RefreshLocalCache();
            _lastLocalCacheTime = Time.time;
        }
        
        // Update local cached targets from this wolf's cache
        cachedTargets.Clear();
        Vector3 currentPos = transform.position;
        
        // Add animals from local cache
        int animalCount = 0;
        for (int i = 0; i < _localAnimalCache.Count && animalCount < maxTargetsToConsider; i++)
        {
            var animal = _localAnimalCache[i];
            if (animal != null && animal.gameObject != null && animal.gameObject.activeInHierarchy)
            {
                // Quick squared distance check (faster than Vector3.Distance)
                Vector3 diff = animal.transform.position - currentPos;
                if (diff.sqrMagnitude <= _targetSearchRangeSquared * 9f) // Increased range for better detection
                {
                    cachedTargets.Add(animal.gameObject);
                    animalCount++;
                }
            }
        }
        
        // Add structures from local cache
        int structureCount = 0;
        for (int i = 0; i < _localStructureCache.Count && structureCount < maxTargetsToConsider; i++)
        {
            var structure = _localStructureCache[i];
            if (structure != null && structure.gameObject != null && structure.gameObject.activeInHierarchy && !structure.isIndestructible)
            {
                Vector3 diff = structure.transform.position - currentPos;
                if (diff.sqrMagnitude <= _targetSearchRangeSquared * 9f) // Increased range
                {
                    cachedTargets.Add(structure.gameObject);
                    structureCount++;
                }
            }
        }
        
        // Clean up any null references that slipped through
        cachedTargets.RemoveAll(go => go == null || !go.activeInHierarchy);
    }
    
    private void RefreshLocalCache()
    {
        _localAnimalCache.Clear();
        _localStructureCache.Clear();
        
        try
        {
            // Each wolf does its own FindObjectsByType call to avoid interference
            var animals = FindObjectsByType<ArmyAnimal>(FindObjectsSortMode.None);
            if (animals != null)
            {
                foreach (var animal in animals)
                {
                    if (animal != null && animal.gameObject != null && animal.gameObject.activeInHierarchy)
                    {
                        _localAnimalCache.Add(animal);
                    }
                }
            }
            
            var structures = FindObjectsByType<Structure>(FindObjectsSortMode.None);
            if (structures != null)
            {
                foreach (var structure in structures)
                {
                    if (structure != null && structure.gameObject != null && 
                        structure.gameObject.activeInHierarchy && !structure.isIndestructible &&
                        structure.GetCurrentHealth() > 0)
                    {
                        _localStructureCache.Add(structure);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Wolf {name} failed to refresh cache: {e.Message}");
            // Continue with whatever cache we have
        }
    }

    private void FindNearbyTarget()
    {
        // Skip movement check if we just lost a target - always search for new ones
        Vector3 currentPos = transform.position;
        bool justLostTarget = Time.time - _lastTargetDestroyTime < 3f;
        
        if (!justLostTarget && Vector3.SqrMagnitude(currentPos - _lastPosition) < 1f && target != null)
        {
            return;
        }
        _lastPosition = currentPos;
        
        // Use cached targets instead of Physics.OverlapSphere for better performance
        GameObject bestTarget = null;
        Vector3 bestAttackPoint = Vector3.zero;
        float bestScore = float.MinValue;
        
        // Increase search range if we just lost a target
        float searchRangeMultiplier = justLostTarget ? 2f : 1f;
        float currentSearchRange = _targetSearchRangeSquared * searchRangeMultiplier;
        
        // Limit iterations for performance
        int checkedCount = 0;
        foreach (GameObject go in cachedTargets)
        {
            if (checkedCount++ > maxTargetsToConsider) break;
            
            if (!IsValidTargetFast(go)) continue;
            
            // Use squared distance for performance
            Vector3 diff = go.transform.position - currentPos;
            float sqrDistance = diff.sqrMagnitude;
            
            if (sqrDistance <= currentSearchRange)
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
        // Fast null check without expensive operations, but still thorough
        if (go == null) return false;
        
        try
        {
            // Quick check if object exists and is active
            return !go.Equals(null) && go.activeInHierarchy;
        }
        catch (System.Exception)
        {
            // Object was destroyed between checks
            return false;
        }
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
        if (go == null) return false;
        
        try
        {
            // Check if object still exists and is active
            if (go.Equals(null) || !go.activeInHierarchy) 
            {
                if (go != null) cachedTargets.Remove(go);
                return false;
            }
            
            // Check if it's a valid target type
            ArmyAnimal animal = go.GetComponent<ArmyAnimal>();
            if (animal != null)
            {
                return true; // Animals are always valid targets if alive
            }
            
            Structure structure = go.GetComponent<Structure>();
            if (structure != null)
            {
                // Check if structure is still valid and not indestructible
                if (structure.isIndestructible) return false;
                
                // Check if structure is still alive
                if (structure.GetCurrentHealth() <= 0)
                {
                    cachedTargets.Remove(go);
                    return false;
                }
                
                return true;
            }
            
            // If we get here, it's not a recognized target type
            cachedTargets.Remove(go);
            return false;
        }
        catch (System.Exception)
        {
            // If any exception occurs (destroyed object, etc.), it's not valid
            if (go != null) cachedTargets.Remove(go);
            return false;
        }
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
        _isActivelyAttacking = false; // Reset attacking state when getting new target
        
        if (flowFieldManager != null)
            flowFieldManager.SetTargetTransformWithPoint(target.transform, attackPoint);
            
        // Debug output to track targeting
        if (Random.value < 0.1f) // 10% chance to log
        {
            string targetType = target.GetComponent<ArmyAnimal>() != null ? "Animal" : "Structure";
            Debug.Log($"Wolf {name} acquired new target: {target.name} ({targetType})");
        }
    }

    private void AttackTarget()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;

        if (target == null || !IsValidTarget(target))
        {
            target = null;
            _isActivelyAttacking = false;
            _lastTargetDestroyTime = Time.time; // Mark target as lost
            
            // Force immediate cache refresh and new target search
            RefreshLocalCache();
            UpdateTargetCache();
            FindTargetWithPriority();
            
            // If no new target, start moving immediately
            if (target == null)
            {
                flowFieldAgent.SetMoving(true);
                OnStartMoving?.Invoke();
                UpdateFallbackTarget();
                flowFieldManager.SetTargetTransformWithPoint(fallbackTarget.transform, fallbackTarget.transform.position);
            }
            return;
        }

        OnAttack?.Invoke();

        try
        {
            if (target != null)
            {
                target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                
                // Check if target was destroyed by our attack
                if (!IsValidTarget(target))
                {
                    target = null;
                    _isActivelyAttacking = false;
                    _lastTargetDestroyTime = Time.time;
                    
                    // Immediate search for new target
                    RefreshLocalCache();
                    UpdateTargetCache();
                    FindTargetWithPriority();
                    
                    // If no new target found, start moving
                    if (target == null)
                    {
                        flowFieldAgent.SetMoving(true);
                        OnStartMoving?.Invoke();
                        UpdateFallbackTarget();
                        flowFieldManager.SetTargetTransformWithPoint(fallbackTarget.transform, fallbackTarget.transform.position);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Wolf {name} aborted attack: Target null in try block");
                target = null;
                _isActivelyAttacking = false;
                FindTargetWithPriority();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Wolf {name} failed to attack: {e.Message}");
            target = null;
            _isActivelyAttacking = false;
            _lastTargetDestroyTime = Time.time;
            
            // Force fresh search after attack failure
            RefreshLocalCache();
            UpdateTargetCache();
            FindTargetWithPriority();
            
            if (target == null)
            {
                flowFieldAgent.SetMoving(true);
                OnStartMoving?.Invoke();
                UpdateFallbackTarget();
                flowFieldManager.SetTargetTransformWithPoint(fallbackTarget.transform, fallbackTarget.transform.position);
            }
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
            _lastTargetDestroyTime = Time.time; // Mark when target was destroyed
            _isActivelyAttacking = false;
            
            // Force immediate local cache refresh and target search
            RefreshLocalCache();
            UpdateTargetCache();
            FindTargetWithPriority();
            
            // If still no target, start moving immediately
            if (target == null)
            {
                UpdateFallbackTarget();
                flowFieldAgent.SetMoving(true);
                OnStartMoving?.Invoke();
                flowFieldManager.SetTargetTransformWithPoint(fallbackTarget.transform, fallbackTarget.transform.position);
            }
        }
        
        // Remove from cache regardless
        cachedTargets.Remove(destroyedTarget);
    }

    private void UpdateFallbackTarget()
    {
        // More aggressive search pattern - move towards areas that might have targets
        Vector3 bestDirection = Vector3.zero;
        float bestScore = 0f;
        
        // Try multiple directions to find the best one
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 testPosition = transform.position + direction * fallbackMoveRadius;
            
            // Score this direction based on potential target density
            float score = ScoreFallbackDirection(testPosition);
            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = direction;
            }
        }
        
        // If no good direction found, use random
        if (bestDirection == Vector3.zero)
        {
            Vector2 randomCircle = Random.insideUnitCircle * fallbackMoveRadius;
            bestDirection = new Vector3(randomCircle.x, 0, randomCircle.y);
        }
        
        Vector3 newPosition = transform.position + bestDirection * fallbackMoveRadius;
        fallbackTarget.transform.position = newPosition;
    }
    
    private float ScoreFallbackDirection(Vector3 position)
    {
        float score = 0f;
        
        // Check for nearby targets in this direction using local cache
        foreach (var structure in _localStructureCache)
        {
            if (structure != null && structure.gameObject != null && structure.gameObject.activeInHierarchy && !structure.isIndestructible)
            {
                Vector3 diff = structure.transform.position - position;
                float sqrDistance = diff.sqrMagnitude;
                
                // Closer targets give higher score
                if (sqrDistance < _targetSearchRangeSquared * 4f)
                {
                    score += 1f / (1f + sqrDistance * 0.01f);
                }
            }
        }
        
        foreach (var animal in _localAnimalCache)
        {
            if (animal != null && animal.gameObject != null && animal.gameObject.activeInHierarchy)
            {
                Vector3 diff = animal.transform.position - position;
                float sqrDistance = diff.sqrMagnitude;
                
                // Animals are higher priority
                if (sqrDistance < _targetSearchRangeSquared * 4f)
                {
                    score += 2f / (1f + sqrDistance * 0.01f);
                }
            }
        }
        
        return score;
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