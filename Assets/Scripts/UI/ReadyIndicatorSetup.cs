using UnityEngine;

public class ReadyIndicatorSetup : MonoBehaviour
{
    void Awake()
    {
        // Load and assign the harvest indicator prefab
        GameObject harvestPrefab = Resources.Load<GameObject>("Prefabs/Structures/HarvestIndicator");
        if (harvestPrefab != null)
        {
            ReadyIndicator.harvestIndicatorPrefab = harvestPrefab;
            Debug.Log("Harvest indicator prefab loaded successfully");
        }
        else
        {
            Debug.LogWarning("Could not load HarvestIndicator prefab from Resources/Prefabs/Structures/");
        }

        // Load and assign the collect indicator prefab (if you create one)
        GameObject collectPrefab = Resources.Load<GameObject>("Prefabs/Structures/CollectIndicator");
        if (collectPrefab != null)
        {
            ReadyIndicator.collectIndicatorPrefab = collectPrefab;
            Debug.Log("Collect indicator prefab loaded successfully");
        }
        else
        {
            Debug.Log("CollectIndicator prefab not found - using default green indicator");
        }
    }
}