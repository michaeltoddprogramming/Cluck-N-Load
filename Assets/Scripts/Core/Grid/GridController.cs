using UnityEngine;


public class GridController : MonoBehaviour
{
    [Header("Terrain & Overlay Settings")]
    [SerializeField] private GameObject terrainSource; // The original floor mesh
    [SerializeField] private Material gridOverlayMaterial; // Material with your grid shader
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private GameObject itemPrefab;

    private GameObject ghostInstance;
    private Vector2Int currentHoveredCell;

    private GameObject gridOverlayInstance; // Duplicate of terrainSource to display grid overlay

    [Header("Grid System References")]
    [SerializeField] private GridDataGenerator gridDataGenerator; // Handles grid data creation
    // Note: TextureGenerator will be instantiated on gridOverlayInstance

    [Header("Grid Settings")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Color gridLineColor = new Color(0, 1, 0, 0.5f);
    [SerializeField] private Color highlightColor = new Color(1, 1, 0, 1); // Ensure alpha is 1
    [SerializeField] private float gridLineOpacity = 0.5f;
    [SerializeField] private float lineWidth = 0.02f;
    [SerializeField] private float highlightIntensity = 0.7f; // Highlight intensity for the shader

    private Material targetMaterial; // Material on the grid overlay
    private MeshRenderer targetRenderer; // MeshRenderer of the grid overlay

    private TextureGenerator textureGenerator; // This will be dynamically added to the overlay.

    [Header("Grid Colors")]
    [SerializeField] private GridColors gridColors; // Reference to GridCellColorResolver.Colors

    void Start()
    {
        // Assign the Inspector-modified GridColors to the static resolver
        GridCellColorResolver.Colors = gridColors;

        // Auto-find GridDataGenerator if not assigned
        if (gridDataGenerator == null)
            gridDataGenerator = FindObjectOfType<GridDataGenerator>();

        if (gridDataGenerator == null)
        {
            Debug.LogError("GridDataGenerator not found in scene.");
            return;
        }

        // Set up the grid overlay (duplicate the terrain mesh).
        SetUpGridOverlay();

        // Apply initial grid settings: update grid data, update shader properties, generate texture.
        ApplySettings();
    }

    void Update()
    {
        ProcessInput();
    }

    void SetUpGridOverlay()
    {
        // Ensure terrainSource is assigned.
        if (terrainSource == null)
        {
            Debug.LogError("terrainSource is not assigned!");
            return;
        }

        // Get MeshFilter and MeshRenderer from terrainSource.
        MeshFilter sourceMF = terrainSource.GetComponent<MeshFilter>();
        MeshRenderer sourceMR = terrainSource.GetComponent<MeshRenderer>();
        if (sourceMF == null || sourceMR == null)
        {
            Debug.LogError("terrainSource must have both MeshFilter and MeshRenderer.");
            return;
        }

        // Create the grid overlay GameObject.
        gridOverlayInstance = new GameObject("GridOverlayMesh");
        gridOverlayInstance.transform.SetParent(this.transform);
        // Position slightly above the original to avoid z-fighting.
        gridOverlayInstance.transform.position = terrainSource.transform.position + new Vector3(0, 0.01f, 0);
        gridOverlayInstance.transform.rotation = terrainSource.transform.rotation;
        gridOverlayInstance.transform.localScale = terrainSource.transform.localScale;

        // Add required components.
        MeshFilter overlayMF = gridOverlayInstance.AddComponent<MeshFilter>();
        MeshRenderer overlayMR = gridOverlayInstance.AddComponent<MeshRenderer>();

        // Duplicate the mesh from terrainSource.
        overlayMF.sharedMesh = Instantiate(sourceMF.sharedMesh);
        overlayMR.material = gridOverlayMaterial;

        // Save references to the overlay's renderer and material.
        targetRenderer = overlayMR;
        targetMaterial = overlayMR.material;

        // Assign the new overlay's MeshRenderer to GridDataGenerator so it uses the duplicate.
        gridDataGenerator.targetMeshRenderer = overlayMR;

        // Dynamically add the TextureGenerator component to the grid overlay.
        textureGenerator = gridOverlayInstance.AddComponent<TextureGenerator>();
        // Set gridData reference on TextureGenerator, if needed.
        textureGenerator.gridData = gridDataGenerator;
        // Pass the overlay's MeshRenderer to TextureGenerator.
        textureGenerator.SetMeshRenderer(overlayMR);
    }

    public void ApplySettings()
    {
        // Update cell size in GridDataGenerator and regenerate grid data.
        gridDataGenerator.cellSize = cellSize;
        gridDataGenerator.GenerateGridData();

        // Update shader properties on the grid overlay material.
        if (targetMaterial != null)
        {
            targetMaterial.SetColor("_Color", gridLineColor);
            targetMaterial.SetColor("_HighlightColor", highlightColor);
            targetMaterial.SetFloat("_GridLineOpacity", gridLineOpacity);
            targetMaterial.SetFloat("_LineWidth", lineWidth);
            targetMaterial.SetFloat("_HighlightIntensity", highlightIntensity);
        }

        // Generate the grid texture based on the current grid data.
        textureGenerator.GenerateGridTexture();
    }

    void ProcessInput()
    {
        // Cast a ray from the main camera.
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
        {
            Vector4 origin = gridDataGenerator.GetGridOrigin();      // (min.x, min.z)
            Vector4 worldSize = gridDataGenerator.GetGridWorldSize();    // (size.x, size.z)
            int gridW = gridDataGenerator.GetGridWidth();
            int gridH = gridDataGenerator.GetGridHeight();

            Vector3 hitPos = hit.point;
            // Convert world position into normalized coordinates in the grid.
            float u = Mathf.InverseLerp(origin.x, origin.x + worldSize.x, hitPos.x);
            float v = Mathf.InverseLerp(origin.y, origin.y + worldSize.y, hitPos.z);
            int cellX = Mathf.FloorToInt(u * gridW);
            int cellY = Mathf.FloorToInt(v * gridH);

            // Update shader's hover parameter.
            targetMaterial.SetVector("_HoverCell", new Vector4(cellX, cellY, 0, 0));

            // On left-click, toggle the cell's state.
            if (Input.GetMouseButtonDown(0))
            {
                ToggleCellState(cellX, cellY);
                textureGenerator.UpdateTexture();
            }
        }
    }

    void ToggleCellState(int x, int y)
    {
        if (x < 0 || x >= gridDataGenerator.GetGridWidth() || y < 0 || y >= gridDataGenerator.GetGridHeight())
            return;

        GridCell cell = gridDataGenerator.GetCell(x, y);

        // Toggle the flags for the cell.
        if (!cell.flags.isOwned)
        {
            cell.flags.Set(true, false, false); // Set to owned.
        }
        else if (!cell.flags.isOccupied)
        {
            cell.flags.Set(true, true, false); // Set to owned and occupied.
        }
        else
        {
            cell.flags.Set(false, false, false); // Reset to default.
        }

        gridDataGenerator.grid[x, y] = cell;
    }
}
