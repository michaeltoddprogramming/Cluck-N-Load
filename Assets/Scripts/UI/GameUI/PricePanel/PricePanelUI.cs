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

        inventoryManager = FindFirstObjectByType<InventoryManager>();
        animalStructure = FindFirstObjectByType<AnimalStructure>();
        productionBoosts = FindFirstObjectByType<ProductionBoosts>();


        // audioSourceOpen.clip = audioClipOpen;
        // audioSourceOpen.Play();

        gameObject.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    private void Start()
    {
        // inventoryManager = FindFirstObjectByType<InventoryManager>();
        // animalStructure = FindFirstObjectByType<AnimalStructure>();



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
        // if (activePricePanelInstance != null && activePricePanelInstance.activeInHierarchy)
        // {
        // populatePricePanel();
        // }
    }

    public void populatePricePanel()
    {
        // Ensure all required managers are initialized
        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
            if (inventoryManager == null)
            {
                Debug.LogError("InventoryManager not found in scene!");
                return;
            }
        }

        if (productionBoosts == null)
        {
            productionBoosts = FindFirstObjectByType<ProductionBoosts>();
            if (productionBoosts == null)
            {
                Debug.LogError("ProductionBoosts not found in scene!");
                return;
            }
        }

        cropAmounts = inventoryManager.getInventory(crops);
        // produceAmounts = AnimalStructure.getProductPrices(animals);
        produceAmounts = productionBoosts.GetProductPrices();
        // boostedProducts = AnimalStructure.whichProductsAreBoosted(animals);
        // boostedProducts = AnimalStructure.whichProductsAreBoosted(animals);
        boostedProducts = productionBoosts.GetBoostedProducts();
        
        // Safe debug logging with null and bounds checking
        if (boostedProducts != null)
        {
            Debug.Log($"These are the products boosted: {boostedProducts.Length}");
            if (boostedProducts.Length >= 5)
            {
                Debug.Log($"These are the products boosted: {boostedProducts[0]}, {boostedProducts[1]}, {boostedProducts[2]}, {boostedProducts[3]}, {boostedProducts[4]}");
            }
        }
        
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

        if (boostedProducts[0] == 0 && boostedProducts[1] == 0 && boostedProducts[2] == 0 && boostedProducts[3] == 0 && boostedProducts[4] == 0)
        {
            eggs.text = $"$ {(int)(produceAmounts[0])}";
            milk.text = $"$ {(int)(produceAmounts[1])}";
            wool.text = $"$ {(int)(produceAmounts[2])}";
            bacon.text = $"$ {(int)(produceAmounts[4])}";
            cheese.text = $"$ {(int)(produceAmounts[3])}";
        }
        else
        {
            eggs.text = $"$ {(int)(produceAmounts[0] * boostedProducts[0])}";
            milk.text = $"$ {(int)(produceAmounts[1] * boostedProducts[1])}";
            wool.text = $"$ {(int)(produceAmounts[2] * boostedProducts[2])}";
            bacon.text = $"$ {(int)(produceAmounts[4] * boostedProducts[4])}";
            cheese.text = $"$ {(int)(produceAmounts[3] * boostedProducts[3])}";
        }

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
        milkBonus.text = $"{boostedProducts[1] * 100 / amount}%";
        woolBonus.text = $"{boostedProducts[2] * 100 / amount}%";
        baconBonus.text = $"{boostedProducts[4] * 100 / amount}%";
        cheeseBonus.text = $"{boostedProducts[3] * 100 / amount}%";

    }

    private void populateCrops()
    {
        sunflower.text = $"Sunflower Seeds:\n{cropAmounts[0]}";
        wheat.text = $"Wheat:\n{cropAmounts[1]}";
        carrot.text = $"Carrots:\n{cropAmounts[2]}";
    }

    public void OpenPanel()
    {
        gameObject.SetActive(true);
        populatePricePanel();
        if (audioClipOpen != null && audioSourceOpen != null)
            audioSourceOpen.PlayOneShot(audioClipOpen);
        
        // Trigger tutorial step for price panel opened
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.Trigger(TutorialTrigger.PricePanelOpened);
        // if (activePricePanelInstance == null)
        // {
        // Canvas canvas = FindFirstObjectByType<Canvas>();
        // if (canvas == null)
        // {
        // Debug.LogError("No Canvas found in scene for PricePanelUI!");
        // return;
        // }
        // activePricePanelInstance = Instantiate(pricePanelPrefab, canvas.transform);
        // // var panelUI = activePricePanelInstance.GetComponent<PricePanelUI>();
        // panelUI.inventoryManager = FindFirstObjectByType<InventoryManager>();
        // panelUI.animalStructure = FindFirstObjectByType<AnimalStructure>();
        // panelUI.populatePricePanel();
        // }

        // activePricePanelInstance.SetActive(true);
        // audioSourceOpen.clip = audioClipOpen;
        // audioSourceOpen.Play();
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
        if (audioClipClose != null && audioSourceClose != null)
            audioSourceClose.PlayOneShot(audioClipClose);
        
        // Trigger tutorial step for price panel closed
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.Trigger(TutorialTrigger.PricePanelClosed);
        // if (activePricePanelInstance != null)
        // {
        // AudioSource.PlayClipAtPoint(audioClipClose, Camera.main.transform.position);

        //     Destroy(activePricePanelInstance);
        //     activePricePanelInstance = null;

        // }
        // else
        // {
        //     Debug.LogWarning("ClosePanel called but no active panel instance.");
        // }
    }
}