// using UnityEngine;

// public class ShopTabs : MonoBehaviour
// {
//     [Header("Tab buttons")]
//     [SerializeField] private GameObject shopTab;
//     [SerializeField] private GameObject repairTab;

//     [Header("Tab panels")]
//     [SerializeField] private GameObject shopPanel;
//     [SerializeField] private GameObject repairPanel;
//     [SerializeField] private GameObject list;
    
//     [Header("Repair item prefab")]
//     [SerializeField] private GameObject repairItemPrefab;


//     void Start()
//     {
//         shopPanel.SetActive(true);
//         repairPanel.SetActive(false);   
//     }

//     // void Update()
//     // {
        
//     // }

//     public void openShopTab()
//     {
//         Debug.Log("shop tab...");
//         shopPanel.SetActive(true);
//         repairPanel.SetActive(false);
//     }

//     public void openRepairTab()
//     {
//         Debug.Log("Repair tab...");
//         repairPanel.SetActive(true);
//         shopPanel.SetActive(false);
//         PopulateList(list);
//     }

//     public void PopulateRepairList()
//     {
//         // Clear old items
//         foreach (Transform child in contentParent)
//             Destroy(child.gameObject);

//         // Example: just 5 dummy items
//         for (int i = 0; i < 5; i++)
//         {
//             GameObject item = Instantiate(itemPrefab, contentParent);
//             item.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = "Building " + (i + 1);
//             item.transform.Find("CostText").GetComponent<TextMeshProUGUI>().text = $"Repair Cost: {10*(i+1)}";

//             Button btn = item.transform.Find("RepairButton").GetComponent<Button>();
//             int index = i; // capture index
//             btn.onClick.AddListener(() => RepairBuilding(index));
//         }
//     }
// }
