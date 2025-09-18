using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Level of Detail manager for structures to improve performance
/// </summary>
public class StructureLODManager : MonoBehaviour
{
    public static StructureLODManager Instance { get; private set; }

    [Header("LOD Settings")]
    [SerializeField] private float closeDistance = 15f;
    [SerializeField] private float mediumDistance = 30f;
    [SerializeField] private float farDistance = 50f;
    [SerializeField] private float updateInterval = 0.2f;

    [Header("Performance Settings")]
    [SerializeField] private int maxUpdatesPerFrame = 10;
    [SerializeField] private bool enableLOD = true;

    private List<StructureLOD> managedStructures = new List<StructureLOD>();
    private int currentUpdateIndex = 0;
    private float nextUpdateTime = 0f;
    private Camera playerCamera;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("StructureLODManager: No main camera found!");
        }
    }

    private void Update()
    {
        if (!enableLOD || playerCamera == null || Time.time < nextUpdateTime)
            return;

        UpdateStructureLODs();
        nextUpdateTime = Time.time + updateInterval;
    }

    private void UpdateStructureLODs()
    {
        int updatesThisFrame = 0;
        Vector3 cameraPos = playerCamera.transform.position;

        for (int i = 0; i < managedStructures.Count && updatesThisFrame < maxUpdatesPerFrame; i++)
        {
            int index = (currentUpdateIndex + i) % managedStructures.Count;
            StructureLOD structureLOD = managedStructures[index];

            if (structureLOD == null || structureLOD.gameObject == null)
            {
                managedStructures.RemoveAt(index);
                continue;
            }

            // Use sqrMagnitude for better performance (no square root calculation)
            float sqrDistance = (cameraPos - structureLOD.transform.position).sqrMagnitude;
            float distance = Mathf.Sqrt(sqrDistance); // Only calculate sqrt when needed for LOD calculation
            LODLevel newLOD = CalculateLODLevel(distance);

            if (structureLOD.CurrentLOD != newLOD)
            {
                structureLOD.SetLODLevel(newLOD);
                updatesThisFrame++;
            }
        }

        currentUpdateIndex = (currentUpdateIndex + maxUpdatesPerFrame) % Mathf.Max(1, managedStructures.Count);
    }

    private LODLevel CalculateLODLevel(float distance)
    {
        if (distance <= closeDistance) return LODLevel.High;
        if (distance <= mediumDistance) return LODLevel.Medium;
        if (distance <= farDistance) return LODLevel.Low;
        return LODLevel.Culled;
    }

    public void RegisterStructure(StructureLOD structureLOD)
    {
        if (!managedStructures.Contains(structureLOD))
        {
            managedStructures.Add(structureLOD);
        }
    }

    public void UnregisterStructure(StructureLOD structureLOD)
    {
        managedStructures.Remove(structureLOD);
    }

    public void SetLODDistances(float close, float medium, float far)
    {
        closeDistance = close;
        mediumDistance = medium;
        farDistance = far;
    }

    public void SetLODEnabled(bool enabled)
    {
        enableLOD = enabled;
        
        if (!enabled)
        {
            // Set all structures to high LOD when disabled
            foreach (var structure in managedStructures)
            {
                if (structure != null)
                {
                    structure.SetLODLevel(LODLevel.High);
                }
            }
        }
    }
}

/// <summary>
/// LOD component for individual structures
/// </summary>
public class StructureLOD : MonoBehaviour
{
    [Header("LOD Objects")]
    [SerializeField] private GameObject highLOD;
    [SerializeField] private GameObject mediumLOD;
    [SerializeField] private GameObject lowLOD;

    [Header("Component Management")]
    [SerializeField] private Collider structureCollider;
    [SerializeField] private MonoBehaviour[] disableableComponents;

    public LODLevel CurrentLOD { get; private set; } = LODLevel.High;

    private void Start()
    {
        // Register with LOD manager
        if (StructureLODManager.Instance != null)
        {
            StructureLODManager.Instance.RegisterStructure(this);
        }

        // Initialize with high LOD
        SetLODLevel(LODLevel.High);
    }

    public void SetLODLevel(LODLevel newLOD)
    {
        if (CurrentLOD == newLOD) return;

        CurrentLOD = newLOD;
        ApplyLODLevel(newLOD);
    }

    private void ApplyLODLevel(LODLevel level)
    {
        // Deactivate all LOD objects first
        if (highLOD != null) highLOD.SetActive(false);
        if (mediumLOD != null) mediumLOD.SetActive(false);
        if (lowLOD != null) lowLOD.SetActive(false);

        switch (level)
        {
            case LODLevel.High:
                if (highLOD != null) highLOD.SetActive(true);
                EnableComponents(true);
                EnableCollider(true);
                break;

            case LODLevel.Medium:
                if (mediumLOD != null) mediumLOD.SetActive(true);
                else if (highLOD != null) highLOD.SetActive(true);
                EnableComponents(true);
                EnableCollider(true);
                break;

            case LODLevel.Low:
                if (lowLOD != null) lowLOD.SetActive(true);
                else if (mediumLOD != null) mediumLOD.SetActive(true);
                else if (highLOD != null) highLOD.SetActive(true);
                EnableComponents(false);
                EnableCollider(true);
                break;

            case LODLevel.Culled:
                EnableComponents(false);
                EnableCollider(false);
                break;
        }
    }

    private void EnableComponents(bool enable)
    {
        foreach (var component in disableableComponents)
        {
            if (component != null)
            {
                component.enabled = enable;
            }
        }
    }

    private void EnableCollider(bool enable)
    {
        if (structureCollider != null)
        {
            structureCollider.enabled = enable;
        }
    }

    private void OnDestroy()
    {
        // Unregister from LOD manager
        if (StructureLODManager.Instance != null)
        {
            StructureLODManager.Instance.UnregisterStructure(this);
        }
    }
}

public enum LODLevel
{
    High,    // Full detail, all components active
    Medium,  // Reduced detail, most components active
    Low,     // Low detail, minimal components
    Culled   // Hidden/disabled
}
