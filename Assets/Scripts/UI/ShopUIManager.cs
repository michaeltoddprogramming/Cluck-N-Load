using UnityEngine;
using UnityEngine.UI;

public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager Instance { get; private set; }

    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button closeButton;
    
    //open shop button
    [SerializeField] private Button shopButton;
    private Color dayShop = Color.white;
    private Color nightShop = Color.grey * 0.9f;
    public Image shopIcon;

    private ShopPanelUI shopPanelUI;
    private bool isVisible = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple instances of ShopUIManager detected! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Find ShopPanelUI component
        shopPanelUI = shopPanel.GetComponent<ShopPanelUI>();
        if (shopPanelUI == null)
        {
            Debug.LogError("ShopPanelUI component not found on shopPanel!");
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseShop);
        }

        // Initially hide shop
        shopPanel.SetActive(false);
    }

    public void ToggleShop()
    {
        if (isVisible)
            CloseShop();
        else
            OpenShop();
    }

    public void OpenShop()
    {
        shopPanel.transform.SetAsLastSibling();

        isVisible = true;
        shopPanel.SetActive(true);

        if (shopPanelUI != null)
        {
            shopPanelUI.OpenShop();
        }
        
        TutorialManager.Instance?.Trigger(TutorialTrigger.ShopOpened);
    }

    public void CloseShop()
    {
        isVisible = false;
        
        if (shopPanelUI != null)
        {
            shopPanelUI.CloseShop();
        }
        else
        {
            shopPanel.SetActive(false);
        }
    }

    public bool IsShopOpen()
    {
        return isVisible;
    }

    public GameObject GetShopPanel()
    {
        return shopPanel;
    }

    public void SetBuildTarget(StructureData data)
    {
        // Only allow building when shop is open
        if (!isVisible)
        {
            return;
        }

        if (data.prefab == null)
        {
            Debug.LogError($"No prefab assigned to {data.structureName}!");
            return;
        }

        // Find ghost placer to show placement preview
        GhostPlacer ghostPlacer = FindFirstObjectByType<GhostPlacer>();
        if (ghostPlacer != null)
        {
            ghostPlacer.SetGhostPrefab(data.prefab);
        }
        else
        {
            // For testing, just spawn it at a random-ish position
            Vector3 spawnPosition = new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
            Instantiate(data.prefab, spawnPosition, Quaternion.identity);
        }
    }

    //disable the shop button
        public void disableShop()
    {
        shopButton.interactable = false;
        shopIcon.color = nightShop;
    }

    //enable the shop button
    public void enableShop()
    {
        shopButton.interactable = true;
        shopIcon.color = dayShop;
    }

    // Resets the shop state to fully closed and ready
    public void ResetShopState()
    {
        isVisible = false;
        if (shopPanelUI != null)
        {
            shopPanelUI.CloseShop();
        }
        shopPanel.SetActive(false);
    }
}