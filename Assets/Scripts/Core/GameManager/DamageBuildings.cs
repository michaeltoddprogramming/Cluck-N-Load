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

    private void DamageAllBuildings()
    {
        Structure[] allBuildings = FindObjectsOfType<Structure>();
        foreach (Structure structure in allBuildings)
        {
            if (structure != null)
            {
                structure.TakeDamage((int)(structure.GetMaxHealth() * 0.3)); // Fully damage for testing
            }
        }

        Debug.Log("All buildings damaged for testing!");
    }
}
