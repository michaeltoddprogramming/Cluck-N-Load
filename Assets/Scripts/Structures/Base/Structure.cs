using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FarmDefender.Core.AI.FlowField;

public class Structure : MonoBehaviour
{
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

    #region Unity Lifecycle

    private void Awake()
    {
        // Initialize health from structure data if available
        if (structureData != null)
        {
            currentHealth = structureData.health;
        }
        else
        {
            currentHealth = 100; // Default health
            Debug.LogWarning($"No StructureData assigned to {gameObject.name}", this);
        }

        // Initialize selection indicator if it exists
        selectionIndicator = transform.Find("SelectionIndicator")?.gameObject;
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
        }
    }

    protected virtual void Start()
    {
        // Skip processing for ghost/preview structures
        if (gameObject.name == "BuildGhost")
        {
            return;
        }

        // Find required components
        gridController = FindObjectOfType<GridController>();
        if (flowFieldManager == null)
            flowFieldManager = FindObjectOfType<FlowFieldManager>();

        if (gridController == null)
        {
            Debug.LogWarning("Grid controller not found! Structure won't register with the grid.", this);
            return;
        }

        // Register this structure with the grid system
        if (autoRegisterWithGrid && gridController != null)
        {
            RegisterWithGrid();
        }

        // Ensure the structure has a collider for mouse interaction
        if (GetComponent<Collider>() == null)
        {
            Debug.Log($"Adding collider to {gameObject.name} for clickability");
            AddColliderToStructure();
        }
    }

    public void SetAllowSelectionAndUI(bool allow)
    {
        allowSelectionAndUI = allow;
    }

    protected virtual void OnDestroy() // Changed to protected virtual
    {
        // Ensure we unregister from grid if destroyed directly (without Die method)
        if (hasRegisteredWithGrid && gridController != null)
        {
            UnregisterFromGrid();
        }
    }

    #endregion

    #region Grid Integration

    public void RegisterWithGrid()
    {
        if (gridController == null || !occupiesGridCells) return;

        // Skip registration if this is a ghost/preview
        if (gameObject.name == "BuildGhost")
        {
            return;
        }

        // Calculate the cells this structure occupies WITHOUT marking them
        CalculateOccupiedCells();

        // We DON'T mark cells as occupied here anymore since BuildController already did that
        // We just mark them as obstacles for pathfinding if needed
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

        if (showDebugInfo)
        {
            Debug.Log($"Structure {gameObject.name} registered with grid for tracking {occupiedCells.Count} cells.");
        }
    }

    public void UnregisterFromGrid()
    {
        if (gridController == null || !hasRegisteredWithGrid || !occupiesGridCells) return;

        // Mark all cells as unoccupied
        foreach (Vector2Int cellPos in occupiedCells)
        {
            if (gridController.IsValidCell(cellPos.x, cellPos.y))
            {
                // Mark as unoccupied
                gridController.SetCellOccupied(cellPos.x, cellPos.y, false);

                // Also clear obstacle flag if needed
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

        // Update flow field to account for removed obstacles
        UpdateFlowField();

        if (showDebugInfo)
        {
            Debug.Log($"Structure {gameObject.name} unregistered from grid.");
        }
    }

    private void CalculateOccupiedCells()
    {
        occupiedCells.Clear();

        // Get the object's bounds in world space
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        // Calculate combined bounds of all renderers
        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        // Shrink bounds slightly to prevent edge cases
        combinedBounds.Expand(-0.1f);

        // Convert to grid coordinates
        Vector2Int bottomLeft = gridController.WorldToGridCoords(combinedBounds.min);
        Vector2Int topRight = gridController.WorldToGridCoords(combinedBounds.max);

        // Loop through all cells in bounds
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
        // Trigger flow field recalculation if flowFieldManager exists
        if (flowFieldManager != null)
        {
            // We should only update the flow field immediately if this structure is being 
            // destroyed (not during construction/placement)

            // Check if in build mode - if so, skip flow field update
            BuildController buildController = FindObjectOfType<BuildController>();
            if (buildController != null && buildController.IsBuildModeActive())
            {
                // Skip flow field update - will be handled when exiting build mode
                return;
            }

            // Check if gameObject is still active before starting coroutine
            if (gameObject.activeInHierarchy)
            {
                // Give a small delay for grid updates to complete
                StartCoroutine(TriggerFlowFieldUpdate());
            }
            else
            {
                // GameObject is being destroyed, so directly update flow field
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

        // Notify listeners
        OnDamaged?.Invoke(this);

        if (showDebugInfo)
        {
            Debug.Log($"Structure {gameObject.name} took {amount} damage. Health: {currentHealth}/{structureData?.health ?? 100}");
        }

        // Check if destroyed
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public virtual void Die()
    {
        // Notify listeners
        OnDestroyed?.Invoke(this);

        // Spawn destruction effect if available
        if (destructionEffectPrefab != null)
        {
            Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
        }

        // Unregister from grid
        UnregisterFromGrid();

        if (showDebugInfo)
        {
            Debug.Log($"Structure {gameObject.name} destroyed.");
        }

        // Destroy the game object after delay
        if (destroyOnZeroHealth)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    // Public getter for current health
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    // Public getter for max health
    public int GetMaxHealth()
    {
        return structureData != null ? structureData.health : 100;
    }

    #endregion

    #region Utility

    // Check if structure occupies a specific grid cell
    public bool OccupiesCell(int x, int y)
    {
        return occupiedCells.Contains(new Vector2Int(x, y));
    }

    public bool GetAllowSelectionAndUI()
    {
        return allowSelectionAndUI;
    }

    // Get the name of the structure
    public string GetStructureName()
    {
        return structureData != null ? structureData.structureName : gameObject.name;
    }

    // Get structure type
    public StructureType GetStructureType()
    {
        return structureData != null ? structureData.type : StructureType.Building;
    }

    // Get cost of the structure
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

        // Show occupied cells
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Orange

        foreach (Vector2Int cell in occupiedCells)
        {
            if (gridController.IsValidCell(cell.x, cell.y))
            {
                Vector3 cellCenter = gridController.GetCellCenterFromTexture(cell.x, cell.y);
                float cellSize = gridController.GetCellSize();

                Gizmos.DrawCube(cellCenter, new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f));
            }
        }

        // Show health bar above structure
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            Vector3 top = new Vector3(bounds.center.x, bounds.max.y + 0.5f, bounds.center.z);

            float healthPct = (float)currentHealth / GetMaxHealth();
            float width = bounds.size.x;

            // Background
            Gizmos.color = Color.gray;
            Gizmos.DrawCube(top, new Vector3(width, 0.1f, 0.1f));

            // Health fill
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
        isSelected = true;

        // Show selection indicator if available
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(true);
        }
        // Create selection indicator if it doesn't exist
        else
        {
            CreateSelectionIndicator();
        }

        Debug.Log($"Selected structure: {GetStructureName()}");
    }

    // Deselect this structure
    public virtual void Deselect()
    {
        isSelected = false;

        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
        }

        Debug.Log($"Deselected structure: {GetStructureName()}");
    }

    // Create a selection indicator
    private void CreateSelectionIndicator()
    {
        selectionIndicator = new GameObject("SelectionIndicator");
        selectionIndicator.transform.SetParent(transform);

        // Position above the structure
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            selectionIndicator.transform.localPosition = new Vector3(
                0,
                renderer.bounds.size.y + 0.1f,
                0
            );
        }
        else
        {
            selectionIndicator.transform.localPosition = new Vector3(0, 1f, 0);
        }

        // Add visual element (circle sprite)
        // You'll need to create a circle sprite and place it in Resources/UI
        // or modify this to use a different indicator

        // Make the indicator face up
        selectionIndicator.transform.rotation = Quaternion.Euler(90, 0, 0);

        // Create a quad for the selection
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.SetParent(selectionIndicator.transform);
        quad.transform.localPosition = Vector3.zero;
        quad.transform.localRotation = Quaternion.identity;
        quad.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

        // Remove the collider from the quad
        Destroy(quad.GetComponent<Collider>());

        // Set material to a simple highlight
        Renderer quadRenderer = quad.GetComponent<Renderer>();
        if (quadRenderer != null)
        {
            // Create a simple highlight material
            Material highlightMaterial = new Material(Shader.Find("Transparent/Diffuse"));
            highlightMaterial.color = new Color(1f, 1f, 0.5f, 0.3f); // Semi-transparent yellow
            quadRenderer.material = highlightMaterial;
        }

        selectionIndicator.SetActive(true);
    }

    // Add a collider to the structure if it doesn't have one
    private void AddColliderToStructure()
    {
        // Try to get bounds from renderers
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            // Calculate combined bounds
            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }

            // Add appropriate sized box collider
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.center = new Vector3(0, combinedBounds.size.y / 2, 0);
            boxCollider.size = combinedBounds.size;

            Debug.Log($"Added BoxCollider to {gameObject.name} based on renderer bounds");
        }
        else
        {
            // Fallback if no renderer
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.center = new Vector3(0, 0.5f, 0);
            boxCollider.size = new Vector3(1, 1, 1);
            Debug.Log($"Added default BoxCollider to {gameObject.name}");
        }
    }

    // Check if structure is currently selected
    public bool IsSelected()
    {
        return isSelected;
    }

    #endregion
    
    public void ApplyDamage(int damage)
    {
        TakeDamage(damage);
    }
}