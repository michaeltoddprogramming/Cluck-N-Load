using UnityEngine;
using System.Collections;

public class ArmyAnimal : MonoBehaviour
{
    [SerializeField] private AnimalStructure.AnimalType animalType;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private int damage = 5;
    [SerializeField] private float attackCooldown = 1.5f;
    
    // Animation parameters (if you're using animation)
    [SerializeField] private Animator animator;
    [SerializeField] private string walkAnimParam = "IsWalking";
    [SerializeField] private string attackAnimParam = "Attack";

    private Vector3 guardPosition;
    private float guardRadius;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private float lastAttackTime;
    private BarracksStructure barracks;
    private float idleTimer;
    private float idleInterval = 2f; // Time between idle position changes

    public AnimalStructure.AnimalType AnimalType => animalType;

    private void Start()
    {
        // Default guard position is spawn position
        guardPosition = transform.position;
        guardRadius = 5f;
        
        // Initial movement
        PickNewTargetPosition();
        
        // Initialize animator if present
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Update()
    {
        // Update idle timer
        idleTimer -= Time.deltaTime;
        
        // Check for enemies here (if you have an enemy system)
        // TODO: Implement enemy detection
        
        // If no enemies and not moving, pick a new position occasionally
        if (!isMoving && idleTimer <= 0)
        {
            PickNewTargetPosition();
            idleTimer = idleInterval;
        }
        
        // Move toward target position
        MoveToTarget();
    }
    
    public void SetBarracks(BarracksStructure barracksStructure)
    {
        barracks = barracksStructure;
    }

    public void SetGuardPosition(Vector3 position, float radius)
    {
        guardPosition = position;
        guardRadius = radius;
        
        // Immediately head toward the new guard position
        isMoving = false;
        PickNewTargetPosition();
        
        Debug.Log($"Army animal set to guard position {position} with radius {radius}");
    }

    private void PickNewTargetPosition()
    {
        // Either go to guard position or random point around it
        if (Vector3.Distance(transform.position, guardPosition) > guardRadius)
        {
            // Go directly to guard position if too far
            targetPosition = guardPosition;
        }
        else 
        {
            // Pick random point within guard radius
            Vector2 randomCircle = Random.insideUnitCircle * guardRadius;
            targetPosition = guardPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
        }
        
        // Start moving
        isMoving = true;
        
        // Update animation
        if (animator != null)
        {
            animator.SetBool(walkAnimParam, true);
        }
    }

    private void MoveToTarget()
    {
        if (!isMoving) return;

        Vector3 direction = targetPosition - transform.position;
        direction.y = 0;
        
        // Check if we've reached the target
        if (direction.magnitude < 0.3f)
        {
            isMoving = false;
            
            // Update animation
            if (animator != null)
            {
                animator.SetBool(walkAnimParam, false);
            }
            return;
        }

        // Rotate towards target
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        
        // Move forward
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }

    public void AttackEnemy(GameObject enemy)
    {
        if (Time.time < lastAttackTime + attackCooldown)
            return;
            
        // Play attack animation
        if (animator != null)
        {
            animator.SetTrigger(attackAnimParam);
        }
        
        Debug.Log($"Army {animalType} attacked enemy for {damage} damage!");
        lastAttackTime = Time.time;
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw guard radius in editor for debugging
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(guardPosition, guardRadius);
        
        // Draw line to target if moving
        if (isMoving)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }
}