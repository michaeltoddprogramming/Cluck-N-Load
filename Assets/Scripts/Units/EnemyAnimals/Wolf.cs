using UnityEngine;
using FarmDefender.Core.AI.FlowField;
using System.Collections;
using System.Linq;

public class Wolf : MonoBehaviour
{
    [Header("Wolf Stats")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private int damage = 50;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private float detectionRange = 20f; // Local targeting range
    [SerializeField] private float globalSearchInterval = 0.5f; // How often to search entire map

    [Header("Target Priorities (Higher = More Priority)")]
    [SerializeField] private int chickenPriority = 10;
    [SerializeField] private int structurePriority = 5;

    [Header("Components")]
    [SerializeField] private FlowFieldAgent flowFieldAgent;
    [SerializeField] private Animator animator;

    private int currentHealth;
    private float lastAttackTime;
    private GameObject target;
    private NightManager nightManager;
    private float targetUpdateTimer = 0f;
    private float globalSearchTimer = 0f;

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

        // Force agent to move
        if (flowFieldAgent != null)
        {
            flowFieldAgent.SetMoving(true);
        }

        // Immediately search for target
        FindTargetWithPriority();

        Debug.Log($"🐺 Wolf initialized at {transform.position}");
    }

    private void Update()
    {
        if (nightManager != null && nightManager.IsDay)
        {
            // Die when day comes
            if (gameObject.activeInHierarchy)
            {
                Die();
            }
            return;
        }

        // Update targeting timers
        targetUpdateTimer -= Time.deltaTime;
        globalSearchTimer -= Time.deltaTime;

        // Local search (frequent)
        if (targetUpdateTimer <= 0f)
        {
            FindNearbyTarget();
            targetUpdateTimer = 2f;
        }

        // Global search (less frequent)
        if (globalSearchTimer <= 0f)
        {
            FindTargetWithPriority();
            globalSearchTimer = globalSearchInterval;
        }

        // Attack if in range
        if (target != null)
        {
            float distance = Vector3.Distance(transform.position, target.transform.position);

            if (distance <= attackRange)
            {
                if (flowFieldAgent != null)
                    flowFieldAgent.SetMoving(false);

                AttackTarget();
            }
            else
            {
                if (flowFieldAgent != null)
                    flowFieldAgent.SetMoving(true);
            }
        }
        else
        {
            if (flowFieldAgent != null)
                flowFieldAgent.SetMoving(true);
        }
    }

    private void FindNearbyTarget()
    {
        // Only look for targets in our detection range
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange);

        float closestDistance = float.MaxValue;
        GameObject bestTarget = null;
        int highestPriority = -1;

        foreach (Collider col in colliders)
        {
            // Check for chickens (highest priority)
            ArmyAnimal chicken = col.GetComponent<ArmyAnimal>();
            if (chicken != null && chicken.gameObject.activeInHierarchy)
            {
                float distance = Vector3.Distance(transform.position, chicken.transform.position);
                if (chickenPriority > highestPriority ||
                   (chickenPriority == highestPriority && distance < closestDistance))
                {
                    bestTarget = chicken.gameObject;
                    closestDistance = distance;
                    highestPriority = chickenPriority;
                }
                continue;
            }

            // Check for structures (medium priority)
            Structure structure = col.GetComponent<Structure>();
            if (structure != null && structure.gameObject.activeInHierarchy && !structure.isIndestructible)
            {
                float distance = Vector3.Distance(transform.position, structure.transform.position);
                if (structurePriority > highestPriority ||
                   (structurePriority == highestPriority && distance < closestDistance))
                {
                    bestTarget = structure.gameObject;
                    closestDistance = distance;
                    highestPriority = structurePriority;
                }
            }
        }

        if (bestTarget != null)
        {
            SetTarget(bestTarget);
        }
    }

    private void FindTargetWithPriority()
    {
        // This is the global search (entire map)
        int highestPriority = -1;
        float closestDistance = float.MaxValue;
        GameObject bestTarget = null;

        // Get all chickens in scene (highest priority)
        foreach (ArmyAnimal chicken in FindObjectsOfType<ArmyAnimal>())
        {
            if (chicken != null && chicken.gameObject.activeInHierarchy)
            {
                float distance = Vector3.Distance(transform.position, chicken.transform.position);
                if (chickenPriority > highestPriority ||
                   (chickenPriority == highestPriority && distance < closestDistance))
                {
                    bestTarget = chicken.gameObject;
                    closestDistance = distance;
                    highestPriority = chickenPriority;
                }
            }
        }

        // If no chickens, check structures (medium priority)
        if (bestTarget == null)
        {
            foreach (Structure structure in FindObjectsOfType<Structure>())
            {
                if (structure != null && structure.gameObject.activeInHierarchy && !structure.isIndestructible)
                {
                    float distance = Vector3.Distance(transform.position, structure.transform.position);
                    if (structurePriority > highestPriority ||
                       (structurePriority == highestPriority && distance < closestDistance))
                    {
                        bestTarget = structure.gameObject;
                        closestDistance = distance;
                        highestPriority = structurePriority;
                    }
                }
            }
        }

        if (bestTarget != null)
        {
            SetTarget(bestTarget);
        }
    }

    private void SetTarget(GameObject newTarget)
    {
        if (newTarget != target)
        {
            target = newTarget;
            Debug.Log($"Wolf targeting: {target.name}");

            // Update flow field target
            FlowFieldManager manager = FindObjectOfType<FlowFieldManager>();
            if (manager != null && target != null)
            {
                manager.SetTargetTransform(target.transform);
            }
        }
    }

private void AttackTarget()
{
    // Don't attack if on cooldown
    if (Time.time < lastAttackTime + attackCooldown) 
        return;
    
    lastAttackTime = Time.time;
    
    // Play attack animation
    if (animator != null)
    {
        animator.SetTrigger("Attack");
    }
    
    // Deal damage to target
    if (target != null)
    {
        // Use SendMessage as a universal approach that will work with any damage system
        target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        Debug.Log($"Wolf attacked {target.name} for {damage} damage");
    }
}

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log($"Wolf took {amount} damage. Health: {currentHealth}/{maxHealth}");

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

        Debug.Log($"Wolf died at {transform.position}");
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        // Visual debugging aids
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
    public void OnDayNightChanged(bool isNight)
{
    if (!isNight)
    {
        // Die when day comes
        Debug.Log("Wolf dying as day has arrived");
        Die();
    }
}
}