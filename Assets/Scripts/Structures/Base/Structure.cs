
using UnityEngine;
using System.Collections.Generic;
using FarmDefender.Core.AI.FlowField;
using System.Linq;
using System.Collections;

public class Structure : MonoBehaviour
{
    // Registration flag to prevent double registration
    private bool isRegisteredWithGameLoop = false;

    public StructureData StructureData => structureData; // Public property for external access

    [Header("Structure Properties")]
    [Tooltip("Reference to structure data asset")]
    public StructureData structureData;
    [Tooltip("Current health of the structure")]
    [SerializeField] private int currentHealth;
    [Tooltip("Whether this structure can be damaged")]
    public bool isIndestructible = false;

    private bool allowSelectionAndUI = true;

    [Header("Grid Integration")]
    [Tooltip("Whether this structure blocks pathfinding")]
    public bool blocksPathfinding = true;
    [Tooltip("Whether grid cells should be marked as occupied")]
    public bool occupiesGridCells = true;
    [Tooltip("Whether to automatically register with grid on start")]
    public bool autoRegisterWithGrid = true;

    [Header("Destruction Settings")]
    [Tooltip("Whether to destroy GameObject when health reaches zero")]
    public bool destroyOnZeroHealth = true;
    [Tooltip("Delay before destroying GameObject")]
    public float destroyDelay = 0.5f;
    [Tooltip("Optional effect to spawn when destroyed")]
    public GameObject destructionEffectPrefab;

    [Header("Debug")]
    [Tooltip("Show debug information")]
    public bool showDebugInfo = false;

    // Private references
    private GridController gridController;
    private FlowFieldManager flowFieldManager;
    private List<Vector2Int> occupiedCells = new List<Vector2Int>();
    private bool hasRegisteredWithGrid = false;

    // Selection properties
    private GameObject selectionIndicator;
    private bool isSelected = false;

    // Events
    public delegate void StructureEvent(Structure structure);
    public event StructureEvent OnDamaged;
    public event StructureEvent OnDestroyed;
    public delegate void StructureDestroyedEventHandler(Structure destroyedStructure);
    public event StructureDestroyedEventHandler OnStructureDestroyed;

    // Health changed event for UI updates
    public event System.Action OnHealthChanged;

    // Wolf notifications
    // private static readonly List<Wolf> registeredWolves = new List<Wolf>();

    // public static void RegisterWolf(Wolf wolf)
    // {
    //     if (wolf != null && !registeredWolves.Contains(wolf))
    //         registeredWolves.Add(wolf);
    // }

    // public static void UnregisterWolf(Wolf wolf)
    // {
    //     registeredWolves.Remove(wolf);
    // }

    #region Unity Lifecycle

    private void Awake()
    {
        if (structureData != null)
        {
            currentHealth = structureData.health;
        }
        else
        {
            currentHealth = 100;
            Debug.LogWarning($"No StructureData assigned to {gameObject.name}", this);
        }

        selectionIndicator = transform.Find("SelectionIndicator")?.gameObject;
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
        }
    }

    protected virtual void Start()
    {
        if (isRegisteredWithGameLoop)
        {
            Debug.LogWarning($"[Structure] Start() called but already registered: {gameObject.name}", this);
        }
        if (gameObject.name == "BuildGhost")
        {
            return;
        }

        gridController = FindFirstObjectByType<GridController>();
        if (flowFieldManager == null)
            flowFieldManager = FindFirstObjectByType<FlowFieldManager>();

        if (gridController == null)
        {
            Debug.LogWarning("Grid controller not found! Structure won't register with the grid.", this);
            return;
        }

        // Only register with GameLoopManager if not already registered (prevents double registration)
        if (!isRegisteredWithGameLoop && GameLoopManager.Instance != null && !GameLoopManager.Instance.IsStructureRegistered(this))
        {
            GameLoopManager.Instance.RegisterStructure(this);
            isRegisteredWithGameLoop = true;
        }

        if (autoRegisterWithGrid && gridController != null)
        {
            RegisterWithGrid();
        }

        if (GetComponent<Collider>() == null)
        {
            AddColliderToStructure();
        }
    }

    // Optional: If something is calling OnEnable, ensure it doesn't re-register
    private void OnEnable()
    {
        // Do not register here; registration is handled in Start only
    }

    public void SetAllowSelectionAndUI(bool allow)
    {
        allowSelectionAndUI = allow;
        if (!allow && isSelected)
        {
            Deselect();
        }
    }

    protected virtual void OnDestroy()
    {
        if (hasRegisteredWithGrid && gridController != null)
        {
            UnregisterFromGrid();
        }
        // Moved UnregisterStructure to Die to avoid delay
    }

    #endregion

    #region Grid Integration

    public void RegisterWithGrid()
    {
        if (gridController == null || !occupiesGridCells) return;
        if (gameObject.name == "BuildGhost") return;

        CalculateOccupiedCells();

        if (blocksPathfinding)
        {
            foreach (Vector2Int cellPos in occupiedCells)
            {
                if (gridController.IsValidCell(cellPos.x, cellPos.y))
                {
                    GridCell cell = gridController.GetCell(cellPos.x, cellPos.y);
                    if (cell != null)
                    {
                        cell.flags.isObstacle = true;
                    }
                }
            }
        }

        hasRegisteredWithGrid = true;
    }

    public void UnregisterFromGrid()
    {
        if (gridController == null || !hasRegisteredWithGrid || !occupiesGridCells) return;

        foreach (Vector2Int cellPos in occupiedCells)
        {
            if (gridController.IsValidCell(cellPos.x, cellPos.y))
            {
                gridController.SetCellOccupied(cellPos.x, cellPos.y, false);
                if (blocksPathfinding)
                {
                    GridCell cell = gridController.GetCell(cellPos.x, cellPos.y);
                    if (cell != null)
                    {
                        cell.flags.isObstacle = false;
                    }
                }
            }
        }

        hasRegisteredWithGrid = false;
        UpdateFlowField();
    }

    private void CalculateOccupiedCells()
    {
        occupiedCells.Clear();
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        combinedBounds.Expand(-0.1f);
        Vector2Int bottomLeft = gridController.WorldToGridCoords(combinedBounds.min);
        Vector2Int topRight = gridController.WorldToGridCoords(combinedBounds.max);

        for (int x = bottomLeft.x; x <= topRight.x; x++)
        {
            for (int y = bottomLeft.y; y <= topRight.y; y++)
            {
                if (gridController.IsValidCell(x, y))
                {
                    occupiedCells.Add(new Vector2Int(x, y));
                }
            }
        }
    }

    private void UpdateFlowField()
    {
        if (flowFieldManager != null)
        {
            BuildController buildController = FindFirstObjectByType<BuildController>();
            if (buildController != null && buildController.IsBuildModeActive())
            {
                return;
            }
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(TriggerFlowFieldUpdate());
            }
            else
            {
                Vector2Int targetCoord = flowFieldManager.GetTargetCoordinates();
                flowFieldManager.GenerateFlowField(targetCoord);
            }
        }
    }

    private IEnumerator TriggerFlowFieldUpdate()
    {
        yield return new WaitForSeconds(0.1f);
        Vector2Int targetCoord = flowFieldManager.GetTargetCoordinates();
        flowFieldManager.GenerateFlowField(targetCoord);
    }

    #endregion

    #region Health and Damage

    public void TakeDamage(int amount)
    {
        if (isIndestructible) return;

        currentHealth -= amount;
        OnDamaged?.Invoke(this);
        OnHealthChanged?.Invoke(); // Trigger health changed event for UI

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public virtual void Die()
    {
        OnDestroyed?.Invoke(this);
        OnStructureDestroyed?.Invoke(this);

        // foreach (Wolf wolf in registeredWolves.ToList())
        // {
        //     if (wolf != null && wolf)
        //         wolf.OnTargetDestroyed(gameObject);
        // }

        if (destructionEffectPrefab != null)
        {
            Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
        }

        // Unregister from GameLoopManager immediately
        if (isRegisteredWithGameLoop && GameLoopManager.Instance != null)
        {
            GameLoopManager.Instance.UnregisterStructure(this);
            isRegisteredWithGameLoop = false;
        }
        UnregisterFromGrid();

        if (destroyOnZeroHealth)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return structureData != null ? structureData.health : 100;
    }

    public bool IsDead()
    {
        return currentHealth <= 0;
    }

    #endregion

    #region Utility

    public bool OccupiesCell(int x, int y)
    {
        return occupiedCells.Contains(new Vector2Int(x, y));
    }

    public bool GetAllowSelectionAndUI()
    {
        return allowSelectionAndUI;
    }

    public string GetStructureName()
    {
        return structureData != null ? structureData.structureName : gameObject.name;
    }

    public StructureType GetStructureType()
    {
        return structureData != null ? structureData.type : StructureType.Building;
    }

    public int GetCost()
    {
        return structureData != null ? structureData.cost : 0;
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmosSelected()
    {
        if (!showDebugInfo || !Application.isPlaying || gridController == null)
            return;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        foreach (Vector2Int cell in occupiedCells)
        {
            if (gridController.IsValidCell(cell.x, cell.y))
            {
                Vector3 cellCenter = gridController.GetCellCenterFromTexture(cell.x, cell.y);
                float cellSize = gridController.GetCellSize();
                Gizmos.DrawCube(cellCenter, new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f));
            }
        }

        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            Vector3 top = new Vector3(bounds.center.x, bounds.max.y + 0.5f, bounds.center.z);
            float healthPct = (float)currentHealth / GetMaxHealth();
            float width = bounds.size.x;

            Gizmos.color = Color.gray;
            Gizmos.DrawCube(top, new Vector3(width, 0.1f, 0.1f));
            Gizmos.color = Color.green;
            Gizmos.DrawCube(
                top - new Vector3((width * (1 - healthPct)) / 2, 0, 0),
                new Vector3(width * healthPct, 0.1f, 0.1f)
            );
        }
    }

    #endregion

    #region Selection and Interaction

    public virtual void Select()
    {
        if (!allowSelectionAndUI) return;
        isSelected = true;
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(true);
        }
    }

    public virtual void Deselect()
    {
        isSelected = false;
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
        }
        Collider col = GetComponent<Collider>();
        if (col != null && !col.enabled)
        {
            col.enabled = true;
        }
    }

    private void AddColliderToStructure()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.center = new Vector3(0, combinedBounds.size.y / 2, 0);
            boxCollider.size = combinedBounds.size;
        }
        else
        {
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.center = new Vector3(0, 0.5f, 0);
            boxCollider.size = new Vector3(1, 1, 1);
        }
    }

    public bool IsSelected()
    {
        return isSelected;
    }

    public void ApplyDamage(int damage)
    {
        TakeDamage(damage);
    }
    
    public void SetStructureType(StructureType type)
    {
        if (structureData != null)
        {
            structureData.type = type;
        }
    }

    #endregion
}