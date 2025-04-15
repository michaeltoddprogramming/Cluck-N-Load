// using UnityEngine;

// public class GridHighlighter : MonoBehaviour
// {
//     private GridDataGenerator gridDataGenerator;
//     private MeshRenderer targetRenderer;
//     private Material targetMaterial;

//     void Start()
//     {
//         gridDataGenerator = FindObjectOfType<GridDataGenerator>();
//         if (gridDataGenerator == null)
//         {
//             Debug.LogError("GridDataGenerator not found!");
//             return;
//         }

//         targetRenderer = gridDataGenerator.GetTargetMeshRenderer();
//         if (targetRenderer == null)
//         {
//             Debug.LogError("No target mesh renderer assigned in GridDataGenerator!");
//             return;
//         }

//         targetMaterial = targetRenderer.material;
//     }

//     void Update()
//     {
//         if (gridDataGenerator == null || targetRenderer == null || targetMaterial == null)
//             return;

//         if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
//         {
//             Vector4 origin = gridDataGenerator.GetGridOrigin();      // (min.x, min.z)
//             Vector4 worldSize = gridDataGenerator.GetGridWorldSize();    // (size.x, size.z)
//             int gridW = gridDataGenerator.GetGridWidth();
//             int gridH = gridDataGenerator.GetGridHeight();

//             Vector3 hitPos = hit.point;

//             // Calculate normalized UV coordinates relative to world grid
//             float u = Mathf.InverseLerp(origin.x, origin.x + worldSize.x, hitPos.x);
//             float v = Mathf.InverseLerp(origin.y, origin.y + worldSize.y, hitPos.z);

//             // Convert UVs to grid cell indices
//             int cellX = Mathf.FloorToInt(u * gridW);
//             int cellY = Mathf.FloorToInt(v * gridH);

//             // Pass to shader
//             targetMaterial.SetVector("_HoverCell", new Vector4(cellX, cellY, 0, 0));

//             if (Input.GetMouseButtonDown(0))
//             {
//                 ToggleCellState(cellX, cellY);

//                 // 🔎 Log the grid cell number + its current state
//                 GridCell cell = gridDataGenerator.GetCell(cellX, cellY);
//                 Debug.Log ($"[CLICK] Cell ({cellX}, {cellY}) | isOwned: {cell.isOwned}");

//                 // Refresh texture visual
//                 GetComponent<TextureGenerator>()?.UpdateTexture();
//             }
//         }
//     }

//     void ToggleCellState(int x, int y)
//     {
//         if (x < 0 || x >= gridDataGenerator.GetGridWidth() ||
//             y < 0 || y >= gridDataGenerator.GetGridHeight())
//             return;

//         GridCell cell = gridDataGenerator.GetCell(x, y);

//         // 👇 Cycle through states: Empty → Owned → Occupied → Empty
//         if (!cell.isOwned && !cell.isOccupied)
//         {
//             cell.isOwned = true;
//             cell.isOccupied = false;
//         }
//         else if (cell.isOwned && !cell.isOccupied)
//         {
//             cell.isOwned = false;
//             cell.isOccupied = true;
//         }
//         else
//         {
//             cell.isOwned = false;
//             cell.isOccupied = false;
//         }

//         gridDataGenerator.grid[x, y] = cell;
//     }
// }
