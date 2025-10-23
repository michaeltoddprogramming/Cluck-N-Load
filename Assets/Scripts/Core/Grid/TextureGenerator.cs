using UnityEngine;
using System.Collections;

public class TextureGenerator : MonoBehaviour
{
    public GridDataGenerator gridData;
    public Color defaultColor = Color.white;
    public Color occupiedColor = Color.red; // Add this
    public Color ownedColor = Color.green;  // Add this
    [SerializeField] private Color highlightColor = new Color(1, 1, 0, 1); // Ensure alpha is 1

    private Texture2D gridTexture;
    private Material targetMaterial;
    private MeshRenderer meshRenderer;  // Reference to the grid's MeshRenderer.

    private void Start()
    {
        StartCoroutine(GenerateWhenReady());
    }

    private IEnumerator GenerateWhenReady()
    {
        // Wait for grid data to initialize before generating the texture.
        while (gridData == null || !gridData.IsInitialized)
        {
            yield return null;
        }

        // Use the mesh from the GridController to generate the texture.
        GenerateGridTexture();
    }

    public void SetMeshRenderer(MeshRenderer newMeshRenderer)
    {
        meshRenderer = newMeshRenderer;
    }

    // private void LateUpdate() 
    // {
    //     if (gridTexture != null && !updatedOnce) {
    //         UpdateTexture();
    //         updatedOnce = true;
    //     }
    // }

    public void GenerateGridTexture()
    {
        if (gridData.grid == null || meshRenderer == null)
        {
            Debug.LogError("Grid data or mesh renderer is not initialized.");
            return;
        }

        int w = gridData.GetGridWidth();
        int h = gridData.GetGridHeight();

        gridTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        gridTexture.filterMode = FilterMode.Point;
        gridTexture.wrapMode = TextureWrapMode.Clamp;

        // Populate texture pixels based on each grid cell's flags.
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                GridCell cell = gridData.grid[x, y];
                Color col = cell.flags.GetColor(); // Use the color resolver based on flags.
                gridTexture.SetPixel(x, y, col);
            }
        }

        gridTexture.Apply();

        // Apply the texture to the material.
        targetMaterial = meshRenderer.material;
        targetMaterial.SetTexture("_MainTex", gridTexture);

        // Pass grid parameters to the shader.
        targetMaterial.SetVector("_GridDivisions", new Vector4(w, h, 0, 0));
        targetMaterial.SetVector("_GridOrigin", gridData.GetGridOrigin());
        targetMaterial.SetVector("_GridWorldSize", gridData.GetGridWorldSize());

        }

    // In the UpdateTexture method, ensure invisible cells are transparent
    public void UpdateTexture()
    {
        if (gridTexture == null) return;

        int w = gridData.GetGridWidth();
        int h = gridData.GetGridHeight();

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                GridCell cell = gridData.grid[x, y];
                // Get color based on cell flags (including visibility)
                Color col = cell.flags.GetColor();
                gridTexture.SetPixel(x, y, col);
            }
        }

        gridTexture.Apply();
    }
}
