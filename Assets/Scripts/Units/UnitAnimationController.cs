// using UnityEngine;
// using System.Collections;

// [RequireComponent(typeof(Animator))]
// public class UnitAnimationController : MonoBehaviour
// {
//     // Core components
//     protected Animator animator;
//     protected AudioSource audioSource;
//     protected Unit unit;
    
//     // Animation parameter names - can be overridden by child classes
//     protected virtual string walkAnimParam => "isWalking";
//     protected virtual string attackAnimParam => "isAttacking";
//     protected virtual string deathAnimParam => "isDead";
    
//     protected virtual void Awake()
//     {
//         // Get required components
//         animator = GetComponent<Animator>();
//         audioSource = GetComponent<AudioSource>();
//         unit = GetComponent<Unit>();
        
//         if (audioSource == null)
//         {
//             audioSource = gameObject.AddComponent<AudioSource>();
//         }
        
//         // Register for unit events if available
//         if (unit != null)
//         {
//             if (unit.Movement != null)
//             {
//                 unit.Movement.MovementStarted += OnMovementStarted;
//                 unit.Movement.MovementStopped += OnMovementStopped;
//             }
            
//             if (unit.Combat != null)
//             {
//                 unit.Combat.AttackStarted += OnAttackStarted;
//             }
            
//             if (unit.Health != null)
//             {
//                 unit.Health.Died += OnDeath;
//             }
//         }
//     }
    
//     protected virtual void OnDestroy()
//     {
//         // Unregister from events
//         if (unit != null)
//         {
//             if (unit.Movement != null)
//             {
//                 unit.Movement.MovementStarted -= OnMovementStarted;
//                 unit.Movement.MovementStopped -= OnMovementStopped;
//             }
            
//             if (unit.Combat != null)
//             {
//                 unit.Combat.AttackStarted -= OnAttackStarted;
//             }
            
//             if (unit.Health != null)
//             {
//                 unit.Health.Died -= OnDeath;
//             }
//         }
//     }
    
//     // Event handlers with virtual methods for customization
//     protected virtual void OnMovementStarted()
//     {
//         if (animator != null)
//         {
//             animator.SetBool(walkAnimParam, true);
//         }
        
//         // Play move sound if available
//         PlaySound(unit.Data.MoveSound);
//     }
    
//     protected virtual void OnMovementStopped()
//     {
//         if (animator != null)
//         {
//             animator.SetBool(walkAnimParam, false);
//         }
//     }
    
//     protected virtual void OnAttackStarted(Unit target)
//     {
//         if (animator != null)
//         {
//             animator.SetBool(attackAnimParam, true);
            
//             // Auto-reset attack animation after a short time
//             StartCoroutine(ResetAttackAnimation(0.5f));
//         }
        
//         // Play attack sound
//         PlaySound(unit.Data.AttackSound);
//     }
    
//     protected virtual void OnDeath()
//     {
//         if (animator != null)
//         {
//             animator.SetBool(walkAnimParam, false);
//             animator.SetBool(attackAnimParam, false);
//             animator.SetBool(deathAnimParam, true);
//         }
        
//         // Play death sound
//         PlaySound(unit.Data.DeathSound);
//     }
    
//     protected IEnumerator ResetAttackAnimation(float delay)
//     {
//         yield return new WaitForSeconds(delay);
        
//         if (animator != null)
//         {
//             animator.SetBool(attackAnimParam, false);
//         }
//     }
    
//     protected void PlaySound(AudioClip clip)
//     {
//         if (audioSource != null && clip != null)
//         {
//             audioSource.clip = clip;
//             audioSource.Play();
//         }
//     }
// }