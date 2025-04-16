using UnityEngine;

public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager Instance { get; private set; }

    [SerializeField] private GameObject shopPanel;

    private bool isVisible = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple instances of ShopUIManager detected! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional: Keeps the instance alive across scenes
    }

    public void ToggleShop()
    {
        isVisible = !isVisible;
        shopPanel.SetActive(isVisible);
    }

    public void SetBuildTarget(StructureData data)
    {
        Debug.Log($"🏗️ Spawning: {data.structureName}");

        if (data.prefab == null)
        {
            Debug.LogError($"❌ No prefab assigned to {data.structureName}!");
            return;
        }

        // For testing, just spawn it at a random-ish position
        Vector3 spawnPosition = new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
        Instantiate(data.prefab, spawnPosition, Quaternion.identity);
    }
}