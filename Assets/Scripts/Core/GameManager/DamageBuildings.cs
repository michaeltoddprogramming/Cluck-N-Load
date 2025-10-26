using UnityEngine;

public class DamageBuildings : MonoBehaviour
{
    [SerializeField] private KeyCode damageKey = KeyCode.LeftShift; 

    void Update()
    {
        if (Input.GetKeyDown(damageKey))
        {
            DamageAllBuildings();
        }
    }

    public void DamageAllBuildings()
    {
        Debug.Log("[DamageBuildings] DamageAllBuildings called!");
        Structure[] allBuildings = FindObjectsByType<Structure>(FindObjectsSortMode.None);
        Debug.Log($"[DamageBuildings] Found {allBuildings.Length} structures in scene");
        
        int damagedCount = 0;
        int skippedCount = 0;
        foreach (Structure structure in allBuildings)
        {
            if (structure != null)
            {
                // Skip the farmhouse - it can't be repaired
                string structureName = structure.GetStructureName().Trim();
                if (structureName.Equals("Farm House", System.StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"[DamageBuildings] Skipping {structure.name} ('{structureName}' - farmhouse cannot be repaired)");
                    skippedCount++;
                    continue;
                }
                
                int damageAmount = (int)(structure.GetMaxHealth() * 0.8); // 80% damage leaves them at 20% health
                Debug.Log($"[DamageBuildings] Damaging {structure.name}: {damageAmount} damage ({structure.GetMaxHealth()} max health)");
                structure.TakeDamage(damageAmount);
                damagedCount++;
            }
        }

        Debug.Log($"[DamageBuildings] Damaged {damagedCount} buildings, skipped {skippedCount} (farmhouse)");
    }
}
