using UnityEngine;
using FarmDefender.Core.AI.FlowField;
using System.Collections;

public class Wolf : MonoBehaviour
{
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private float detectionRange = 1000f;
    
    [SerializeField] private FlowFieldAgent flowFieldAgent;
    [SerializeField] private Animator animator;
    
    private int currentHealth;
    private float lastAttackTime;
    private GameObject target;
    private NightManager nightManager;
    private float targetUpdateTimer = 2f;
    private bool hasDebuggedPathfinding = false;

    private void Start()
    {
        currentHealth = maxHealth;
        
        // Get references
        nightManager = NightManager.Instance;
        if (nightManager != null)
        {
            nightManager.RegisterWolf(this);
        }
        
        // Get components if not assigned in inspector
        if (flowFieldAgent == null)
            flowFieldAgent = GetComponent<FlowFieldAgent>();
        if (animator == null)
            animator = GetComponent<Animator>();
            
        // CRITICAL: Force wolf to move
        if (flowFieldAgent != null)
        {
            flowFieldAgent.SetMoving(true);
        }
        else
        {
            Debug.LogError($"{name} has no FlowFieldAgent component!");
        }
        
        // Start looking for targets immediately
        StartCoroutine(FindTargetWithDelay(0.5f));
        
        Debug.Log($"Wolf spawned at {transform.position}. Agent active: {flowFieldAgent != null && flowFieldAgent.enabled}");
    }

    private void Update()
    {
        // Update target periodically
        targetUpdateTimer -= Time.deltaTime;
        if (targetUpdateTimer <= 0f)
        {
            FindTarget();
            targetUpdateTimer = 2f;
        }
        
        // Attack target if in range
        if (target != null && Vector3.Distance(transform.position, target.transform.position) <= attackRange)
        {
            AttackTarget();
        }
    }
    
    private IEnumerator FindTargetWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        FindTarget();
    }
    
    private void FindTarget()
    {
        // First look for chickens (ArmyAnimal)
        ArmyAnimal[] chickens = FindObjectsOfType<ArmyAnimal>();
        
        float closestDistance = float.MaxValue;
        GameObject closestTarget = null;
        
        foreach (ArmyAnimal chicken in chickens)
        {
            if (chicken == null || !chicken.gameObject.activeInHierarchy) continue;
            
            float distance = Vector3.Distance(transform.position, chicken.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = chicken.gameObject;
            }
        }
        
        // If no chickens found, look for structures
        if (closestTarget == null)
        {
            Structure[] structures = FindObjectsOfType<Structure>();
            
            foreach (Structure structure in structures)
            {
                if (structure == null || !structure.gameObject.activeInHierarchy || structure.isIndestructible) continue;
                
                float distance = Vector3.Distance(transform.position, structure.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = structure.gameObject;
                }
            }
        }
        
        // Set target and debug info
        target = closestTarget;
        
        // Update FlowFieldManager target
        if (target != null)
        {
            FlowFieldManager manager = FindObjectOfType<FlowFieldManager>();
            if (manager != null)
            {
                manager.SetTargetTransform(target.transform);
                
                // Force agent to move
                if (flowFieldAgent != null)
                {
                    flowFieldAgent.SetMoving(true);
                }
                
                Debug.Log($"Wolf targeting {target.name} at {target.transform.position}");
            }
            else
            {
                Debug.LogError("No FlowFieldManager found in scene!");
            }
        }
        else if (!hasDebuggedPathfinding)
        {
            // Log available targets once
            LogAvailableTargets();
            hasDebuggedPathfinding = true;
        }
    }
    
    private void LogAvailableTargets()
    {
        ArmyAnimal[] chickens = FindObjectsOfType<ArmyAnimal>();
        Structure[] structures = FindObjectsOfType<Structure>();
        
        Debug.Log($"Wolf target debug - Found {chickens.Length} chickens and {structures.Length} structures");
        foreach (ArmyAnimal chicken in chickens)
        {
            Debug.Log($" - Chicken: {chicken.name} at {chicken.transform.position}, Active: {chicken.gameObject.activeInHierarchy}");
        }
        foreach (Structure structure in structures)
        {
            Debug.Log($" - Structure: {structure.name} at {structure.transform.position}, Active: {structure.gameObject.activeInHierarchy}, Indestructible: {structure.isIndestructible}");
        }
    }
    
        private void AttackTarget()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;
        
        lastAttackTime = Time.time;
        
        // Play attack animation if available
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        
        // Apply damage (simplified approach)
        if (target != null)
        {
            target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }
        
        Debug.Log($"Wolf attacked {target.name} for {damage} damage");
    }
    
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log($"Wolf took {amount} damage, health: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        if (nightManager != null)
        {
            nightManager.UnregisterWolf(this);
        }
        
        Destroy(gameObject);
    }
    
    public void OnDayNightChanged(bool isNight)
    {
        if (!isNight)
        {
            // Die when day comes
            Die();
        }
    }
}