using UnityEngine;

public class FarmHouseStructure : Structure
{
    [Header("Farm House Settings")]
    [SerializeField] private bool isMainBuilding = true;
    [SerializeField] private float farmRadius = 20f;
    [SerializeField] private int maxWorkers = 5;
    [SerializeField] private int currentWorkers = 0;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip backgroundAmbient;
    [SerializeField] private AudioClip constructionComplete;

    public bool IsMainBuilding => isMainBuilding;
    public float FarmRadius => farmRadius;
    public int MaxWorkers => maxWorkers;
    public int CurrentWorkers => currentWorkers;

    protected override void Start()
    {
        base.Start();

        // Don't run initialization for BuildGhost (preview when placing)
        if (gameObject.name == "BuildGhost")
        {
            return;
        }

        // Validate structure type
        if (structureData != null && structureData.type != StructureType.Building)
        {
            Debug.LogWarning($"FarmHouseStructure {name} has incorrect StructureType: {structureData.type}. Should be Building.");
        }

        // Play construction complete sound
        if (constructionComplete != null && audioSource != null)
        {
            audioSource.PlayOneShot(constructionComplete);
        }

        // Start ambient sounds
        PlayAmbientSounds();

        // NOTE: Tutorial notification is handled by TutorialConditionTracker
        // No need to notify here to avoid duplicate triggers

        Debug.Log($"FarmHouse '{name}' constructed successfully!");
    }

    private void PlayAmbientSounds()
    {
        if (audioSource != null && backgroundAmbient != null)
        {
            audioSource.clip = backgroundAmbient;
            audioSource.loop = true;
            audioSource.volume = 0.3f;
            audioSource.Play();
        }
    }

    protected override void OnDestroy()
    {
        // Don't log destruction to reduce console spam
        base.OnDestroy();
    }

    public void AddWorker()
    {
        if (currentWorkers < maxWorkers)
        {
            currentWorkers++;
            Debug.Log($"Worker added to {name}. Workers: {currentWorkers}/{maxWorkers}");
        }
    }

    public void RemoveWorker()
    {
        if (currentWorkers > 0)
        {
            currentWorkers--;
            Debug.Log($"Worker removed from {name}. Workers: {currentWorkers}/{maxWorkers}");
        }
    }

    public bool CanAddWorker()
    {
        return currentWorkers < maxWorkers;
    }

    // Method to get farm efficiency based on workers
    public float GetFarmEfficiency()
    {
        if (maxWorkers == 0) return 1f;
        return 1f + (currentWorkers / (float)maxWorkers) * 0.5f; // Up to 50% bonus
    }
}
