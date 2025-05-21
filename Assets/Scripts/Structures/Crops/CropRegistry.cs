// using System.Collections.Generic;
// using UnityEngine;

// public class CropRegistry : MonoBehaviour
// {
//     public static CropRegistry Instance;

//     private List<CropStructure> crops = new List<CropStructure>();

//     private void Awake()
//     {
//         if (Instance == null) Instance = this;
//         else Destroy(gameObject);
//     }

//     public void RegisterCrop(CropStructure crop)
//     {
//         if (!crops.Contains(crop))
//         {
//             crops.Add(crop);
//         }
//     }

//     public List<CropStructure> GetCrops()
//     {
//         return crops;
//     }
// }
