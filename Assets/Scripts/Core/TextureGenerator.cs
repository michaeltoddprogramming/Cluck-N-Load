using UnityEngine;
using System.Collections;

public class TextureGenerator : MonoBehaviour
{
    public GridDataGenerator gridData;
    public Color defaultColor = Color.white;
    public Color occupiedColor = Color.red;
    public Color ownedColor = Color.green;

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

    // Now accepts the MeshRenderer directly, so we can apply to the grid mesh.
    public void SetMeshRenderer(MeshRenderer newMeshRenderer)
    {
        meshRenderer = newMeshRenderer;
    }

    // Generates a texture based on the grid cell states and the passed mesh's UVs.
    public void GenerateGridTexture()
    {
        if (gridData.grid == null || meshRenderer == null)
        {
            Debug.LogError("Grid data or mesh renderer is not initialized.");
            return;
        }

        // Get the dimensions of the grid.
        int w = gridData.GetGridWidth();
        int h = gridData.GetGridHeight();

        // Create a new texture.
        gridTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        gridTexture.filterMode = FilterMode.Point;
        gridTexture.wrapMode = TextureWrapMode.Clamp;

        // Get the UVs from the mesh (you will need to ensure your mesh has grid-like UVs).
        Vector2[] uvs = meshRenderer.GetComponent<MeshFilter>().mesh.uv;

        // Populate texture pixels based on each grid cell's state.
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                GridCell cell = gridData.grid[x, y];
                Color col = defaultColor;

                // Check the cell state and set color accordingly.
                if (cell.state == CellState.Occupied)
                    col = occupiedColor;
                else if (cell.state == CellState.Owned)
                    col = ownedColor;

                // Set pixel color for the cell. Use UVs for texture coordinates.
                // Assuming UVs are set up correctly for a grid.
                gridTexture.SetPixel(x, y, col);
            }
        }

        gridTexture.Apply();

        // Apply the texture to the material.
        targetMaterial = meshRenderer.material;
        targetMaterial.SetTexture("_MainTex", gridTexture);

        // Pass grid parameters to the shader (as before).
        targetMaterial.SetVector("_GridDivisions", new Vector4(w, h, 0, 0));
        targetMaterial.SetVector("_GridOrigin", gridData.GetGridOrigin());
        targetMaterial.SetVector("_GridWorldSize", gridData.GetGridWorldSize());

        Debug.Log("Texture Generated: " + gridTexture.width + "x" + gridTexture.height);
    }

    // Call this to update the texture after any grid state change.
    public void UpdateTexture()
    {
        if (gridTexture == null) return;
        
        int w = gridData.GetGridWidth();
        int h = gridData.GetGridHeight();

        // Update the texture for each grid cell.
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                GridCell cell = gridData.grid[x, y];
                Color col = defaultColor;
                if (cell.state == CellState.Occupied)
                    col = occupiedColor;
                else if (cell.state == CellState.Owned)
                    col = ownedColor;

                gridTexture.SetPixel(x, y, col);
            }
        }

        gridTexture.Apply();
    }

    
}


// using UnityEngine;
// using System.Collections;

// public class TextureGenerator : MonoBehaviour
// {
//     public GridDataGenerator gridData;
//     public Color defaultColor = Color.white;
//     public Color occupiedColor = Color.red;
//     public Color ownedColor = Color.green;

//     private Texture2D gridTexture;
//     private Material targetMaterial;

//     private void Start()
//     {
//         StartCoroutine(GenerateWhenReady());
//     }

//     private IEnumerator GenerateWhenReady()
//     {
//         while (gridData == null || !gridData.IsInitialized)
//         {
//             yield return null;
//         }
//         GenerateGridTexture();
//     }

//     // Generates a texture based on the grid cell states.
//     // The texture dimensions match the grid data dimensions.
//     public void GenerateGridTexture()
//     {
//         if (gridData.grid == null)
//         {
//             Debug.LogError("Grid data is not initialized.");
//             return;
//         }

//         int w = gridData.GetGridWidth();
//         int h = gridData.GetGridHeight();

//         gridTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);
//         gridTexture.filterMode = FilterMode.Point;
//         gridTexture.wrapMode = TextureWrapMode.Clamp;

//         // Populate texture pixels based on each grid cell's state.
//         for (int x = 0; x < w; x++)
//         {
//             for (int y = 0; y < h; y++)
//             {
//                 GridCell cell = gridData.grid[x, y];
//                 Color col = defaultColor;
//                 if (cell.state == CellState.Occupied)
//                     col = occupiedColor;
//                 else if (cell.state == CellState.Owned)
//                     col = ownedColor;

//                 // Set pixel color for the cell.
//                 gridTexture.SetPixel(x, y, col);
//             }
//         }

//         gridTexture.Apply();

//         // Apply the texture to this object's material.
//         var renderer = GetComponent<Renderer>();
//         if (renderer != null)
//         {
//             targetMaterial = renderer.material;
//             targetMaterial.SetTexture("_MainTex", gridTexture);

//             // Pass the grid parameters to the shader.
//             targetMaterial.SetVector("_GridDivisions", new Vector4(w, h, 0, 0));
//             targetMaterial.SetVector("_GridOrigin", gridData.GetGridOrigin());
//             targetMaterial.SetVector("_GridWorldSize", gridData.GetGridWorldSize());
//         }

//         Debug.Log("Texture Generated: " + gridTexture.width + "x" + gridTexture.height);
//     }

//     // Call this to update the texture after any grid state change.
//     public void UpdateTexture()
//     {
//         if (gridTexture == null) return;
//         int w = gridData.GetGridWidth();
//         int h = gridData.GetGridHeight();

//         for (int x = 0; x < w; x++)
//         {
//             for (int y = 0; y < h; y++)
//             {
//                 GridCell cell = gridData.grid[x, y];
//                 Color col = defaultColor;
//                 if (cell.state == CellState.Occupied)
//                     col = occupiedColor;
//                 else if (cell.state == CellState.Owned)
//                     col = ownedColor;
//                 else
//                     col = defaultColor;

//                 gridTexture.SetPixel(x, y, col);

//             }
//         }
//         gridTexture.Apply();
//     }
// }
