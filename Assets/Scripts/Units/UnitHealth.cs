using UnityEngine;
using System;

public class UnitHealth : MonoBehaviour 
{
    [SerializeField] private int _maxHealth = 100;
    private int _currentHealth;
    
    public int MaxHealth => _maxHealth;
    public int CurrentHealth => _currentHealth;
    
    public event Action<int> HealthChanged;
    public event Action Died;
    
    private void Awake() 
    {
        _currentHealth = _maxHealth;
    }
    
    private void Start() 
    {
        // Initialize from UnitData if available
        Unit unit = GetComponent<Unit>();
        if (unit != null && unit.Data != null) 
        {
            _maxHealth = unit.Data.Health;
            _currentHealth = _maxHealth;
        }
    }
    
    public void TakeDamage(int amount) 
    {
        if (_currentHealth <= 0) return;
        
        _currentHealth = Mathf.Max(0, _currentHealth - amount);
        HealthChanged?.Invoke(_currentHealth);
        
        if (_currentHealth <= 0) 
        {
            Die();
        }
    }
    
    public void Die() 
    {
        Died?.Invoke();
        Destroy(gameObject, 0.2f);
    }
}