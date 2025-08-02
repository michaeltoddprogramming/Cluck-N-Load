// using UnityEngine;
// using System;

// public class UnitMovement : MonoBehaviour
// {
//     [SerializeField] private float _moveSpeed = 3f;
    
//     public event Action MovementStarted;
//     public event Action MovementStopped;
    
//     private void Start()
//     {
//         // Initialize from UnitData if available
//         Unit unit = GetComponent<Unit>();
//         if (unit != null && unit.Data != null)
//         {
//             _moveSpeed = unit.Data.MovementSpeed;
//         }
//     }
    
//     // Basic movement stub - will be implemented later
//     public void MoveTo(Vector3 position)
//     {
//         // Placeholder for future implementation
//         MovementStarted?.Invoke();
//     }
    
//     public void StopMoving()
//     {
//         // Placeholder for future implementation
//         MovementStopped?.Invoke();
//     }
// }