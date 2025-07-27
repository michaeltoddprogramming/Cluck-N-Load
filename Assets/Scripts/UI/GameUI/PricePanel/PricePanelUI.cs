// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;

// public class PricePanelUI : MonoBehaviour
// {
//     [Header("Crops")]
//     [SerializeField] private TextMeshProUGUI sunflower;
//     [SerializeField] private TextMeshProUGUI wheat;
//     [SerializeField] private TextMeshProUGUI carrot;

//     [Header("Produce")]
//     [SerializeField] private TextMeshProUGUI eggs;
//     [SerializeField] private TextMeshProUGUI eggsBonus;
//     [SerializeField] private TextMeshProUGUI milk;
//     [SerializeField] private TextMeshProUGUI milkBonus;
//     [SerializeField] private TextMeshProUGUI bacon;
//     [SerializeField] private TextMeshProUGUI baconBonus;
//     [SerializeField] private TextMeshProUGUI cheese;
//     [SerializeField] private TextMeshProUGUI cheeseBonus;
//     [SerializeField] private TextMeshProUGUI wool;
//     [SerializeField] private TextMeshProUGUI woolBonus;

//     [Header("Buttons")]
//     // [SerializeField] private GameObject panelRoot; // Assign the root panel GameObject in the inspector
//     // [SerializeField] private Button openButton;    // Assign the open button in the inspector
//     [SerializeField] private Button closeButton;   // Assign the close button in the inspector

//     [Header("SFX")]
//     [SerializeField] public AudioSource audioSourceOpen;
//     [SerializeField] public AudioSource audioSourceClose;

//     [Header("Prefab Reference")]
//     [SerializeField] public GameObject pricePanelPrefab; // Assign your prefab in the Inspector

//     private GameObject pricePanelInstance;

//     [HideInInspector] public InventoryManager inventoryManager;
//     [HideInInspector] public AnimalStructure animalStructure;

//     //[0] -> Sunflower
//     //[1] -> Wheat
//     //[2] -> Carrot
//     private string[] crops = { "s", "w", "c" };
//     public int[] cropAmounts;
//     private string[] animals = { "Chicken", "Cow", "Sheep", "Pig", "Goat" };
//     private int[] produceAmounts;
//     private int[] boostedProducts;

//     private void Awake()
//     {
//         animalStructure = FindFirstObjectByType<AnimalStructure>();
//         inventoryManager = FindFirstObjectByType<InventoryManager>();
//         // AnimalStructure animalStructure = FindFirstObjectByType<AnimalStructure>();
//         // InventoryManager inventoryManager = FindFirstObjectByType<InventoryManager>();

//         // if (animalStructure != null)
//         // {
//         //     animalStructure.YourAnimalFunction(); // Replace with your actual function
//         // }

//         // if (inventoryManager != null)
//         // {
//         //     inventoryManager.YourInventoryFunction(); // Replace with your actual function
//         // }

//         // inventoryManager = GetComponent<InventoryManager>();
//         // animalStructure = GetComponent<AnimalStructure>();

//         cropAmounts = new int[crops.Length];
//         produceAmounts = new int[animals.Length];
//         boostedProducts = new int[animals.Length];

//         // if (openButton != null)
//         //     openButton.onClick.AddListener(OpenPanel);
//         if (closeButton != null)
//             closeButton.onClick.AddListener(ClosePanel);

//         // Optionally start with the panel hidden
//         // if (panelRoot != null)
//             // panelRoot.SetActive(false);

//         // populatePricePanel();
//     }

//     private void Start()
//     {
//         if (inventoryManager != null && animalStructure != null)
//         {
//             populatePricePanel();
//         }
//     }

//     public void Update()
//     {
//         populatePricePanel();
//     }

//     public void populatePricePanel()
//     {
//         cropAmounts = inventoryManager.getInventory(crops);
//         produceAmounts = AnimalStructure.getProductPrices(animals);
//         boostedProducts = AnimalStructure.whichProductsAreBoosted(animals);

//         populateCrops();
//         populateProduce();
//     }

//     private void populateProduce()
//     {
//         eggs.text = $"$ {produceAmounts[0]}";
//         milk.text = $"$ {produceAmounts[1]}";
//         bacon.text = $"$ {produceAmounts[2]}";
//         cheese.text = $"$ {produceAmounts[3]}";
//         wool.text = $"$ {produceAmounts[4]}";

//         eggsBonus.text = $"{boostedProducts[0]}%";
//         milkBonus.text = $"{boostedProducts[1]}%";
//         baconBonus.text = $"{boostedProducts[2]}%";
//         cheeseBonus.text = $"{boostedProducts[3]}%";
//         woolBonus.text = $"{boostedProducts[4]}%";
//     }

//     private void populateCrops()
//     {
//         sunflower.text = $"Sunflower Seeds:\n{cropAmounts[0]}";
//         wheat.text = $"Wheat:\n{cropAmounts[2]}";
//         carrot.text = $"Carrots:\n{cropAmounts[2]}";
//     }

//     // public void OpenPanel()
//     // {
//     //     //     //     if (panelRoot != null)
//     //     {
//     //     //     //         panelRoot.SetActive(true);
//     //         audioSourceOpen.Play();
//     //     }
//     // }

//     public void OpenPanel()
//     {
//         // AnimalStructure animalStructure = FindFirstObjectByType<AnimalStructure>();
//         // InventoryManager inventoryManager = FindFirstObjectByType<InventoryManager>();

//         // if (pricePanelInstance == null)
//         // {
//         //     pricePanelInstance = Instantiate(pricePanelPrefab, transform.parent); // Or use a Canvas as parent
//         // }
//         // pricePanelInstance.SetActive(true);

//         if (pricePanelInstance == null)
//         {
//             pricePanelInstance = Instantiate(pricePanelPrefab, transform.parent);
//             var panelUI = pricePanelInstance.GetComponent<PricePanelUI>();
//             panelUI.inventoryManager = FindFirstObjectByType<InventoryManager>();
//             panelUI.animalStructure = FindFirstObjectByType<AnimalStructure>();
//             // panelUI.populatePricePanel(); // Call this after references are set
//         }
//         pricePanelInstance.SetActive(true);

//         // Optionally play sound here
//     }

//     // public void ClosePanel()
//     // {
//     //     if (panelRoot != null)
//     //     {
//     //         panelRoot.SetActive(false);
//     //         audioSourceClose.Play();
//     //     }
//     // }
    
//     public void ClosePanel()
//     {
//         if (pricePanelInstance != null)
//         {
//             pricePanelInstance.SetActive(false);
//             // Optionally play sound here
//         }
//     }
// }

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PricePanelUI : MonoBehaviour
{
    [Header("Crops")]
    [SerializeField] private TextMeshProUGUI sunflower;
    [SerializeField] private TextMeshProUGUI sunflowerBonus;
    [SerializeField] private TextMeshProUGUI wheat;
    [SerializeField] private TextMeshProUGUI wheatBonus;
    [SerializeField] private TextMeshProUGUI carrot;
    [SerializeField] private TextMeshProUGUI carrotBonus;

    [Header("Produce")]
    [SerializeField] private TextMeshProUGUI eggs;
    [SerializeField] private TextMeshProUGUI eggsBonus;
    [SerializeField] private TextMeshProUGUI milk;
    [SerializeField] private TextMeshProUGUI milkBonus;
    [SerializeField] private TextMeshProUGUI bacon;
    [SerializeField] private TextMeshProUGUI baconBonus;
    [SerializeField] private TextMeshProUGUI cheese;
    [SerializeField] private TextMeshProUGUI cheeseBonus;
    [SerializeField] private TextMeshProUGUI wool;
    [SerializeField] private TextMeshProUGUI woolBonus;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;

    [Header("SFX")]
    [SerializeField] public AudioSource audioSourceOpen;
    [SerializeField] public AudioSource audioSourceClose;
    [SerializeField] public AudioClip audioClipClose;
    [SerializeField] public AudioClip audioClipOpen;

    [Header("Prefab Reference")]
    [SerializeField] public GameObject pricePanelPrefab;
    [SerializeField] public GameObject gameManger;

    private static GameObject activePricePanelInstance;

    [HideInInspector] public InventoryManager inventoryManager;
    [HideInInspector] public AnimalStructure animalStructure;
    [HideInInspector] public ProductionBoosts productionBoosts;

    private string[] crops = { "s", "w", "c" };
    public int[] cropAmounts;
    private string[] animals = { "Chicken", "Cow", "Sheep", "Pig", "Goat" };
    private int[] produceAmounts;
    private float[] boostedProducts;
    private int[] boostedCrops;

    // private GameObject pricePanelInstance;

    public void SetAnimalStructure(AnimalStructure structure)
    {
        if (structure == null)
        {
            Debug.LogError("PricePanelUI: SetAnimalStructure called with null!");
            return;
        }
        animalStructure = structure;
    }

    private void Awake()
    {
        cropAmounts = new int[crops.Length];
        produceAmounts = new int[animals.Length];
        boostedProducts = new float[5];
        productionBoosts = FindObjectOfType<ProductionBoosts>();

        // if(productionBoosts == null)
        // {
        //     Debug.LogError("ProductionBoosts not found in the scene!");
        // }

        audioSourceOpen.clip = audioClipOpen;
            audioSourceOpen.Play();

        // if (closeButton != null)
        // closeButton.onClick.AddListener(ClosePanel);
    }

    private void Start()
    {
        inventoryManager = FindFirstObjectByType<InventoryManager>();
        animalStructure = FindFirstObjectByType<AnimalStructure>();

        

        if (inventoryManager != null && animalStructure != null)
        {
            populatePricePanel();
        }
    }

    // private void Update()
    // {
    //     if (inventoryManager != null && animalStructure != null)
    //     {
    //         populatePricePanel();
    //     }
    // }

    private void Update()
    {
        if (activePricePanelInstance != null && activePricePanelInstance.activeInHierarchy)
        {
            // populatePricePanel();
        }
    }

    public void populatePricePanel()
    {
        if (inventoryManager == null)
            Debug.LogError("inventoryManager is null!");

        cropAmounts = inventoryManager.getInventory(crops);
        // produceAmounts = AnimalStructure.getProductPrices(animals);
        produceAmounts = productionBoosts.GetProductPrices();
        // boostedProducts = AnimalStructure.whichProductsAreBoosted(animals);
        // boostedProducts = AnimalStructure.whichProductsAreBoosted(animals);
        boostedProducts = productionBoosts.GetBoostedProducts();
        Debug.Log($"These are the products boosted: {boostedProducts.Length}");
        Debug.Log($"These are the products boosted: {boostedProducts[0]}, {boostedProducts[1]}, {boostedProducts[2]}, {boostedProducts[3]}, {boostedProducts[4]}");
        // boostedCrops = inventoryManager.whichProductsAreBoosted(animals);
        populateCrops();
        populateProduce();
        getCropBoosts();
    }

    private void getCropBoosts()
    {
        // Assuming your crop types are "Sunflower", "Wheat", "Carrots"
        string[] cropTypes = { "Sunflower", "Wheat", "Carrots" };
        float[] cropMultipliers = CropStructure.GetAllCropHarvestMultipliers(cropTypes);

        // Example: Debug log to see the multipliers
        // for (int i = 0; i < cropTypes.Length; i++)
        // {
        //     // }

        // int sun = cropAmounts[0];
        // int wheat = cropAmounts[1];
        // int  = cropAmounts[2];

        sunflowerBonus.text = $"{(cropMultipliers[0] * 100) / 3}%";
        wheatBonus.text = $"{(cropMultipliers[1] * 100) / 3}%";
        carrotBonus.text = $"{(cropMultipliers[2] * 100) / 3}%";

    }

    private void populateProduce()
    {
        float amount = 3f;


        eggs.text = $"$ {(int)(produceAmounts[0] * boostedProducts[0])}";
        milk.text = $"$ {(int)(produceAmounts[1] * boostedProducts[1])}";
        wool.text = $"$ {(int)(produceAmounts[2] * boostedProducts[2])}";
        bacon.text = $"$ {(int)(produceAmounts[4] * boostedProducts[4])}";
        cheese.text = $"$ {(int)(produceAmounts[3] * boostedProducts[3])}";

        for (int k = 0; k < boostedProducts.Length; k++)
        {
            if (boostedProducts[k] == 2)
            {
                amount = 2f;
            }

            if (boostedProducts[k] == 1)
            {
                boostedProducts[k] = 0f;
            }
        }

        eggsBonus.text = $"{boostedProducts[0] * 100 / amount}%";
        milkBonus.text = $"{boostedProducts[1]  * 100 / amount}%";
        woolBonus.text = $"{boostedProducts[2]  * 100 / amount}%";
        baconBonus.text = $"{boostedProducts[4]  * 100 / amount}%";
        cheeseBonus.text = $"{boostedProducts[3]  * 100 / amount}%";
    }

    private void populateCrops()
    {
        sunflower.text = $"Sunflower Seeds:\n{cropAmounts[0]}";
        wheat.text = $"Wheat:\n{cropAmounts[1]}";
        carrot.text = $"Carrots:\n{cropAmounts[2]}";
    }

    // public void OpenPanel()
    // {
    //     if (pricePanelInstance == null)
    //     {
    //         pricePanelInstance = Instantiate(pricePanelPrefab, transform.parent);
    //         var panelUI = pricePanelInstance.GetComponent<PricePanelUI>();

    //         panelUI.inventoryManager = FindFirstObjectByType<InventoryManager>();
    //         panelUI.animalStructure = FindFirstObjectByType<AnimalStructure>();
    //     }

    //     pricePanelInstance.SetActive(true);
    //     audioSourceOpen?.Play();
    // }

    //     public void OpenPanel()
    // {
    //     if (pricePanelInstance == null)
    //     {
    //         Canvas canvas = FindFirstObjectByType<Canvas>();
    //         if (canvas == null)
    //         {
        //             return;
    //         }

    //         pricePanelInstance = Instantiate(pricePanelPrefab, canvas.transform);
    //         var panelUI = pricePanelInstance.GetComponent<PricePanelUI>();
    //         panelUI.inventoryManager = FindFirstObjectByType<InventoryManager>();
    //         panelUI.animalStructure = FindFirstObjectByType<AnimalStructure>();

    //         panelUI.populatePricePanel(); // ensure it's populated immediately
    //     }

    //     pricePanelInstance.SetActive(true);
    //     audioSourceOpen?.Play();
    // }

    // public void OpenPanel()
    // {
    //     if (pricePanelInstance == null)
    //     {
    //         Canvas canvas = FindFirstObjectByType<Canvas>();
    //         if (canvas == null)
    //         {
        //             return;
    //         }

    //         pricePanelInstance = Instantiate(pricePanelPrefab, canvas.transform);
    //         var panelUI = pricePanelInstance.GetComponent<PricePanelUI>();

    //         panelUI.inventoryManager = FindFirstObjectByType<InventoryManager>();
    //         panelUI.animalStructure = FindFirstObjectByType<AnimalStructure>();

    //         if (panelUI.animalStructure == null)
    //         {
        //         }

    //         panelUI.populatePricePanel();
    //     }

    //     pricePanelInstance.SetActive(true);
    //     audioSourceOpen?.Play();
    // }

    // public void ClosePanel()
    // {
    //     if (pricePanelInstance != null)
    //     {
    //         pricePanelInstance.SetActive(false);
    //         audioSourceClose?.Play();
    //     }
    // }

    //     public void ClosePanel()
    // {
        //     if (pricePanelInstance != null)
    //         {
        //             audioSourceClose?.Play();
    //             Destroy(pricePanelInstance);
    //             pricePanelInstance = null;
    //         }
    // }

    // public void ClosePanel()
    // {
    //     //     if (pricePanelInstance != null)
    //     {
    //         //         audioSourceClose?.Play();
    //         Destroy(pricePanelInstance);
    //         pricePanelInstance = null;
    //     }
    //     else
    //     {
        //     }
    // }

    public void OpenPanel()
        {
            if (activePricePanelInstance == null)
            {
                Canvas canvas = FindFirstObjectByType<Canvas>();
                activePricePanelInstance = Instantiate(pricePanelPrefab, canvas.transform);
                var panelUI = activePricePanelInstance.GetComponent<PricePanelUI>();
                panelUI.inventoryManager = FindFirstObjectByType<InventoryManager>();
                panelUI.animalStructure = FindFirstObjectByType<AnimalStructure>();
                panelUI.populatePricePanel();
                // audioSourceOpen.Play();
            }

            activePricePanelInstance.SetActive(true);
            audioSourceOpen.clip = audioClipOpen;
            audioSourceOpen.Play();
        }

        public void ClosePanel()
        {
            if (activePricePanelInstance != null)
            {
                if (audioClipClose != null)
                AudioSource.PlayClipAtPoint(audioClipClose, Camera.main.transform.position);

                Destroy(activePricePanelInstance);
                activePricePanelInstance = null;

                // audioSourceClose.Play();
            }
            else
            {
                Debug.LogWarning("ClosePanel called but no active panel instance.");
            }
        }

// public void OpenPanel()
// {
//     if (activePricePanelInstance == null)
//     {
//         Canvas canvas = FindFirstObjectByType<Canvas>();
//         activePricePanelInstance = Instantiate(pricePanelPrefab, canvas.transform);
//         var panelUI = activePricePanelInstance.GetComponent<PricePanelUI>();
//         panelUI.inventoryManager = FindFirstObjectByType<InventoryManager>();
//         panelUI.animalStructure = FindFirstObjectByType<AnimalStructure>();
//         panelUI.populatePricePanel();
//     }

//     activePricePanelInstance.SetActive(true);
//     if (audioClipOpen != null)
//         AudioSource.PlayClipAtPoint(audioClipOpen, Vector3.zero);
// }

// public void ClosePanel()
// {
//     if (activePricePanelInstance != null)
//     {
//             if (audioClipClose == null)
//             audioSourceClose.PlayOneShot(audioClipClose);

//         Destroy(activePricePanelInstance);
//         activePricePanelInstance = null;
//     }
//     else
//     {
//     }
// }
}
