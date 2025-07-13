using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class FeatureUnlockManager : MonoBehaviour
{
    public static FeatureUnlockManager Instance { get; private set; }

    [Header("Feature References")]
    [SerializeField] private Button shopButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject minimapPanel;
    [SerializeField] private List<GameObject> advancedUIElements = new List<GameObject>();

    [Header("Unlock Notifications")]
    [SerializeField] private GameObject unlockNotificationPrefab;
    [SerializeField] private Transform notificationParent;
    [SerializeField] private float notificationDuration = 3f;

    private HashSet<string> unlockedFeatures = new HashSet<string>();
    private Queue<string> pendingNotifications = new Queue<string>();
    private bool isShowingNotification = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeFeatures();
    }

    private void Start()
    {
        // Start with only basic features unlocked
        UnlockFeature("camera_movement");
        
        // Load saved progress
        LoadUnlockedFeatures();
        
        // Apply current unlock state
        RefreshFeatureVisibility();
    }

    private void InitializeFeatures()
    {
        // Initially hide all advanced features
        HideAllAdvancedFeatures();
    }

    private void HideAllAdvancedFeatures()
    {
        // Hide shop initially (unlocked by tutorial)
        if (shopButton != null)
            shopButton.gameObject.SetActive(false);

        // Hide delete button initially
        if (deleteButton != null)
            deleteButton.gameObject.SetActive(false);

        // Hide inventory panel
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        // Hide minimap
        if (minimapPanel != null)
            minimapPanel.SetActive(false);

        // Hide other advanced UI elements
        foreach (var element in advancedUIElements)
        {
            if (element != null)
                element.SetActive(false);
        }
    }

    public void UnlockFeature(string featureName)
    {
        if (unlockedFeatures.Contains(featureName))
            return;

        unlockedFeatures.Add(featureName);
        SaveUnlockedFeatures();

        // Apply the unlock
        ApplyFeatureUnlock(featureName);

        // Show notification
        ShowUnlockNotification(featureName);
    }

    private void ApplyFeatureUnlock(string featureName)
    {
        switch (featureName.ToLower())
        {
            case "shop_access":
                if (shopButton != null)
                {
                    shopButton.gameObject.SetActive(true);
                    AddPulseEffect(shopButton.gameObject);
                }
                break;

            case "delete_mode":
                if (deleteButton != null)
                {
                    deleteButton.gameObject.SetActive(true);
                    AddPulseEffect(deleteButton.gameObject);
                }
                break;

            case "inventory_system":
                if (inventoryPanel != null)
                {
                    inventoryPanel.SetActive(true);
                    AddPulseEffect(inventoryPanel);
                }
                break;

            case "minimap":
                if (minimapPanel != null)
                {
                    minimapPanel.SetActive(true);
                    AddPulseEffect(minimapPanel);
                }
                break;

            case "advanced_building":
                // Unlock more complex buildings in shop
                BroadcastUnlock("advanced_building");
                break;

            case "defense_system":
                // Enable barracks and military structures
                BroadcastUnlock("defense_system");
                break;

            case "resource_management":
                // Show detailed resource information
                BroadcastUnlock("resource_management");
                break;
        }

        }

    private void AddPulseEffect(GameObject target)
    {
        StartCoroutine(PulseEffect(target));
    }

    private IEnumerator PulseEffect(GameObject target)
    {
        if (target == null) yield break;

        Vector3 originalScale = target.transform.localScale;
        float duration = 1f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float scale = 1f + Mathf.Sin(elapsedTime * 8f) * 0.1f;
            target.transform.localScale = originalScale * scale;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        target.transform.localScale = originalScale;
    }

    private void ShowUnlockNotification(string featureName)
    {
        pendingNotifications.Enqueue(featureName);
        
        if (!isShowingNotification)
        {
            StartCoroutine(ProcessNotificationQueue());
        }
    }

    private IEnumerator ProcessNotificationQueue()
    {
        isShowingNotification = true;

        while (pendingNotifications.Count > 0)
        {
            string featureName = pendingNotifications.Dequeue();
            yield return StartCoroutine(ShowSingleNotification(featureName));
            yield return new WaitForSeconds(0.5f); // Brief pause between notifications
        }

        isShowingNotification = false;
    }

    private IEnumerator ShowSingleNotification(string featureName)
    {
        if (unlockNotificationPrefab == null || notificationParent == null)
            yield break;

        GameObject notification = Instantiate(unlockNotificationPrefab, notificationParent);
        
        // Set notification text
        TextMeshProUGUI notificationText = notification.GetComponentInChildren<TextMeshProUGUI>();
        if (notificationText != null)
        {
            notificationText.text = GetFeatureDisplayName(featureName) + " Unlocked!";
        }

        // Animate in
        RectTransform rectTransform = notification.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            Vector3 startPos = rectTransform.localPosition + Vector3.right * 300f;
            Vector3 endPos = rectTransform.localPosition;

            float animTime = 0.3f;
            float elapsedTime = 0f;

            while (elapsedTime < animTime)
            {
                rectTransform.localPosition = Vector3.Lerp(startPos, endPos, elapsedTime / animTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            rectTransform.localPosition = endPos;
        }

        // Wait
        yield return new WaitForSeconds(notificationDuration);

        // Animate out
        if (rectTransform != null)
        {
            Vector3 finalPos = rectTransform.localPosition + Vector3.right * 300f;
            float animTime = 0.3f;
            float elapsedTime = 0f;

            while (elapsedTime < animTime)
            {
                rectTransform.localPosition = Vector3.Lerp(rectTransform.localPosition, finalPos, elapsedTime / animTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        // Destroy notification
        Destroy(notification);
    }

    private string GetFeatureDisplayName(string featureName)
    {
        switch (featureName.ToLower())
        {
            case "shop_access": return "Shop";
            case "delete_mode": return "Delete Tool";
            case "inventory_system": return "Inventory";
            case "minimap": return "Minimap";
            case "advanced_building": return "Advanced Buildings";
            case "defense_system": return "Defense Structures";
            case "resource_management": return "Resource Details";
            default: return featureName.Replace("_", " ");
        }
    }

    private void BroadcastUnlock(string unlockType)
    {
        // Notify other systems about the unlock
        switch (unlockType)
        {
            case "advanced_building":
                // Enable more building options in shop
                ShopUIManager shopManager = FindFirstObjectByType<ShopUIManager>();
                if (shopManager != null)
                {
                    // shopManager.UnlockAdvancedBuildings();
                }
                break;

            case "defense_system":
                // Enable barracks and defensive structures
                break;

            case "resource_management":
                // Show detailed resource tooltips
                break;
        }
    }

    public bool IsFeatureUnlocked(string featureName)
    {
        return unlockedFeatures.Contains(featureName);
    }

    public void UnlockAllBasicFeatures()
    {
        UnlockFeature("shop_access");
        UnlockFeature("delete_mode");
        UnlockFeature("basic_building");
        UnlockFeature("resource_management");
    }

    public void RefreshFeatureVisibility()
    {
        foreach (string feature in unlockedFeatures)
        {
            ApplyFeatureUnlock(feature);
        }
    }

    private void SaveUnlockedFeatures()
    {
        string saveData = string.Join(",", unlockedFeatures);
        PlayerPrefs.SetString("UnlockedFeatures", saveData);
        PlayerPrefs.Save();
    }

    private void LoadUnlockedFeatures()
    {
        string saveData = PlayerPrefs.GetString("UnlockedFeatures", "");
        if (!string.IsNullOrEmpty(saveData))
        {
            string[] features = saveData.Split(',');
            foreach (string feature in features)
            {
                if (!string.IsNullOrEmpty(feature))
                {
                    unlockedFeatures.Add(feature);
                }
            }
        }
    }

    public void ResetProgress()
    {
        unlockedFeatures.Clear();
        PlayerPrefs.DeleteKey("UnlockedFeatures");
        PlayerPrefs.DeleteKey("TutorialCompleted");
        HideAllAdvancedFeatures();
        UnlockFeature("camera_movement");
    }
}
