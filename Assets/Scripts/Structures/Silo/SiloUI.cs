// using UnityEngine;

// public class SiloUI : BaseStructureUI
// {

//     [Header("UI Elements")]
//     [SerializeField] private TextMeshProUGUI sunflowerAmount;
//     [SerializeField] private TextMeshProUGUI wheatAmount;
//     [SerializeField] private TextMeshProUGUI carrotAmount;
//     [SerializeField] private TextMeshProUGUI storageAmount;


//     private InventoryManager inventoryManager; 
//     private bool isSiloStructure = true;
//     public override void Initialize(Structure structure)
//     {
//         base.Initialize(structure);

//         isSiloStructure = structure is SiloStructure;

//         if (isSiloStructure)
//         {
//             siloStructure = (SiloStructure)structure;
//         }

//         if (!isSiloStructure)
//         {
//             Debug.LogWarning($"AnimalStructureUI used with non-animal structure: {structure.GetType().Name}");
//             HideAnimalSpecificUI();
//             return;
//         }

//         UpdateUI();

//         if (feedButton != null)
//         {
//             feedButton.onClick.RemoveAllListeners();
//             feedButton.onClick.AddListener(() =>
//             {
//                 animalStructure.Feed();
//                 UpdateUI();
//             });
//         }
//         else
//         {
//             Debug.LogWarning("Feed button is not assigned in the AnimalStructureUI prefab!");
//             if (feedButton != null) feedButton.gameObject.SetActive(false);
//         }

//         if (collectButton != null)
//         {
//             collectButton.onClick.RemoveAllListeners();
//             collectButton.onClick.AddListener(() =>
//             {
//                 animalStructure.Collect();
//                 UpdateUI();
//             });
//         }
//         else
//         {
//             Debug.LogWarning("Collect button is not assigned in the AnimalStructureUI prefab!");
//             if (collectButton != null) collectButton.gameObject.SetActive(false);
//         }
//     }
    
//     void Start()
//     {
        
//     }

//     // Update is called once per frame
//     void Update()
//     {
        
//     }
// }



using UnityEngine;
using TMPro;

public class SiloUI : BaseStructureUI
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI sunflowerAmount;
    [SerializeField] private TextMeshProUGUI wheatAmount;
    [SerializeField] private TextMeshProUGUI carrotAmount;
    [SerializeField] private TextMeshProUGUI storageAmount;
    [SerializeField] private TextMeshProUGUI currAmount;

    private SiloStructure siloStructure;
    private bool isSiloStructure = false;
    private InventoryManager inventoryManager;

    public override void Initialize(Structure structure)
    {
        base.Initialize(structure);

        isSiloStructure = structure is SiloStructure;
        if (isSiloStructure)
        {
            siloStructure = (SiloStructure)structure;
        }

        inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
        {
            Debug.LogError("InventoryManager not found in the scene! SiloUI requires InventoryManager to function.");
        }

        if (!isSiloStructure)
        {
            Debug.LogWarning($"SiloUI used with non-silo structure: {structure.GetType().Name}");
            HideSiloSpecificUI();
            return;
        }

        UpdateUI();
    }

    protected override void Update()
    {
        base.Update();
        if (isSiloStructure)
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (!isSiloStructure || inventoryManager == null)
            return;

        if (sunflowerAmount != null)
            sunflowerAmount.text = inventoryManager.GetItemCount("Sunflower").ToString();

        if (wheatAmount != null)
            wheatAmount.text = inventoryManager.GetItemCount("Wheat").ToString();

        if (carrotAmount != null)
            carrotAmount.text = inventoryManager.GetItemCount("Carrots").ToString();

        if (storageAmount != null)
        {
            // You need to implement GetTotalSiloCapacity() in your InventoryManager or SiloManager
            storageAmount.text = "Max Capacity:\n" + inventoryManager.GetTotalSiloCapacity().ToString();
        }

        if (currAmount != null)
        {
            // You need to implement GetTotalSiloCapacity() in your InventoryManager or SiloManager
            currAmount.text = "Current Capacity:\n" + inventoryManager.GetCurrentSiloCapacity().ToString();
        }
    }

    private void HideSiloSpecificUI()
    {
        if (sunflowerAmount != null) sunflowerAmount.gameObject.SetActive(false);
        if (wheatAmount != null) wheatAmount.gameObject.SetActive(false);
        if (carrotAmount != null) carrotAmount.gameObject.SetActive(false);
        if (storageAmount != null) storageAmount.gameObject.SetActive(false);
    }
}