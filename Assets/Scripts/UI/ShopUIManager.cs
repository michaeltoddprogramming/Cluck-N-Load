using UnityEngine;
using UnityEngine.UI;

public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager Instance { get; private set; }

    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button closeButton;

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
        else
        {
            Debug.LogWarning("Close button is not assigned in the inspector!");
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
        isVisible = true;
        shopPanel.SetActive(true);
        
        if (shopPanelUI != null)
        {
            shopPanelUI.OpenShop();
        }
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

    public void SetBuildTarget(StructureData data)
    {
        // Only allow building when shop is open
        if (!isVisible)
        {
            Debug.LogWarning("Cannot place structures while the shop is closed!");
            return;
        }

        Debug.Log($"🏗️ Spawning: {data.structureName}");

        if (data.prefab == null)
        {
            Debug.LogError($"❌ No prefab assigned to {data.structureName}!");
            return;
        }

        // Find ghost placer to show placement preview
        GhostPlacer ghostPlacer = FindObjectOfType<GhostPlacer>();
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
}