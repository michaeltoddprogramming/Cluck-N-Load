// using UnityEngine;

// public class AddToAllStructures : MonoBehaviour
// {
//     // public void AddHitEffectToAllStructures()
//     // {
//     //     var allStructures = FindObjectsByType<Structure>(FindObjectsSortMode.None);

//     //     foreach (var structure in allStructures)
//     //     {
//     //         if (structure.GetComponent<DamageAnimation>() == null)
//     //         {
//     //             structure.gameObject.AddComponent<DamageAnimation>();
//     //         }
//     //     }

//     //     Debug.Log($"Added StructureHitEffect to {allStructures.Length} structures (if missing).");
//     // }

//     public void AddHitEffectToAllStructures()
//     {
//         var allWithSelectionAnim = FindObjectsOfType<StructureData>();
//         Debug.Log($"Found {allWithSelectionAnim.Length} structures with SelectionAnimation.");

//         foreach (var selectionAnim in allWithSelectionAnim)
//         {
//             if (selectionAnim.GetComponent<DamageAnimation>() == null)
//             {
//                 selectionAnim.gameObject.AddComponent<DamageAnimation>();
//                 Debug.Log($"Added HitEffect to {selectionAnim.gameObject.name}");
//             }
//         }
//     }
// }
