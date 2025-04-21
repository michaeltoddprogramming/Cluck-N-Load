using UnityEngine;
using System;

public class UnitCombat : MonoBehaviour
{
    [SerializeField] private int _attackPower = 10;
    [SerializeField] private float _attackRange = 1.5f;
    
    public event Action<Unit> AttackStarted;
    
    private void Start()
    {
        // Initialize from UnitData if available
        Unit unit = GetComponent<Unit>();
        if (unit != null && unit.Data != null)
        {
            _attackPower = unit.Data.AttackPower;
            _attackRange = unit.Data.AttackRange;
        }
    }
    
    // Basic attack stub - will be implemented later
    public void Attack(Unit target)
    {
        // Placeholder for future implementation
        AttackStarted?.Invoke(target);
    }
    
    // Basic range check - useful even in simplified version
    public bool IsInAttackRange(Transform target)
    {
        return Vector3.Distance(transform.position, target.position) <= _attackRange;
    }
}