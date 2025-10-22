using UnityEngine;
using UnityEngine.UI;
using TMPro;

// This script helps you create the notification prefab structure
// Attach this to an empty GameObject to auto-generate the prefab structure
public class NotificationPrefab : MonoBehaviour
{
    [ContextMenu("Generate Notification Prefab")]
    public void GeneratePrefab()
    {
        // Create main notification panel
        GameObject notification = new GameObject("NotificationPanel");
        notification.transform.SetParent(transform);
        
        // Add Image component for background
        Image bgImage = notification.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        // Setup RectTransform
        RectTransform mainRect = notification.GetComponent<RectTransform>();
        mainRect.sizeDelta = new Vector2(350, 80);
        mainRect.anchorMin = new Vector2(1, 1);
        mainRect.anchorMax = new Vector2(1, 1);
        mainRect.pivot = new Vector2(1, 1);
        mainRect.anchoredPosition = new Vector2(-20, -20);

        // Create border
        GameObject border = new GameObject("Border");
        border.transform.SetParent(notification.transform);
        Image borderImage = border.AddComponent<Image>();
        borderImage.color = Color.white;
        
        RectTransform borderRect = border.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-2, -2);
        borderRect.offsetMax = new Vector2(2, 2);
        borderRect.SetAsFirstSibling();

        // Create icon
        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(notification.transform);
        Image iconImage = icon.AddComponent<Image>();
        iconImage.color = Color.white;
        
        RectTransform iconRect = icon.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(40, 40);
        iconRect.anchorMin = new Vector2(0, 0.5f);
        iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = new Vector2(15, 0);

        // Create title text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(notification.transform);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Notification Title";
        titleText.fontSize = 16;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Left;
        
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.5f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = new Vector2(65, 0);
        titleRect.offsetMax = new Vector2(-15, -5);

        // Create message text
        GameObject messageObj = new GameObject("MessageText");
        messageObj.transform.SetParent(notification.transform);
        TextMeshProUGUI messageText = messageObj.AddComponent<TextMeshProUGUI>();
        messageText.text = "Notification message details...";
        messageText.fontSize = 12;
        messageText.color = new Color(0.9f, 0.9f, 0.9f);
        messageText.alignment = TextAlignmentOptions.Left;
        
        RectTransform messageRect = messageText.GetComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0, 0);
        messageRect.anchorMax = new Vector2(1, 0.5f);
        messageRect.offsetMin = new Vector2(65, 5);
        messageRect.offsetMax = new Vector2(-15, 0);

        Debug.Log("Notification prefab structure generated! Save it as a prefab.");
    }
}