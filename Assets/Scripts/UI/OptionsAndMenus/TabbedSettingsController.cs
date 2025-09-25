using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class TabbedSettingsController : MonoBehaviour
{
    [System.Serializable]
    public class SettingsTab
    {
        public string tabName;
        public Button tabButton;
        public GameObject tabContent;
        public Color activeColor = Color.white;
        public Color inactiveColor = Color.gray;
    }

    [Header("Tab Configuration")]
    public List<SettingsTab> tabs = new List<SettingsTab>();
    public int defaultTabIndex = 0;

    [Header("Tab Button Styling")]
    public Color selectedTabColor = new Color(1f, 0.8f, 0.4f, 1f); // Golden/orange
    public Color unselectedTabColor = new Color(0.6f, 0.4f, 0.2f, 1f); // Darker brown
    public Color selectedTextColor = Color.black;
    public Color unselectedTextColor = Color.white;

    [Header("Animation Settings")]
    public float tabTransitionDuration = 0.3f;
    public LeanTweenType easeType = LeanTweenType.easeOutQuad;

    private int currentTabIndex = -1;
    private RectTransform contentContainer;

    private void Start()
    {
        InitializeTabs();
        
        // Force hide all content panels first
        for (int i = 0; i < tabs.Count; i++)
        {
            if (tabs[i].tabContent != null)
            {
                tabs[i].tabContent.SetActive(false);
            }
        }
        
        // Then show the default tab
        ShowTab(defaultTabIndex);
    }

    private void InitializeTabs()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            int tabIndex = i; // Capture for closure
            if (tabs[i].tabButton != null)
            {
                tabs[i].tabButton.onClick.RemoveAllListeners();
                tabs[i].tabButton.onClick.AddListener(() => ShowTab(tabIndex));
                
                // Add UIButtonSound component if it doesn't exist
                if (tabs[i].tabButton.GetComponent<UIButtonSound>() == null)
                {
                    tabs[i].tabButton.gameObject.AddComponent<UIButtonSound>();
                }
            }

            if (tabs[i].tabContent != null)
            {
                tabs[i].tabContent.SetActive(false);
            }
        }
    }

    public void ShowTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= tabs.Count) return;
        
        Debug.Log($"ShowTab called with index: {tabIndex}, tab name: {tabs[tabIndex].tabName}");
        
        // Hide ALL content panels first
        for (int i = 0; i < tabs.Count; i++)
        {
            if (tabs[i].tabContent != null)
            {
                tabs[i].tabContent.SetActive(false);
            }
        }
        
        // Show the selected tab content
        if (tabs[tabIndex].tabContent != null)
        {
            tabs[tabIndex].tabContent.SetActive(true);
            Debug.Log($"Activated content panel: {tabs[tabIndex].tabContent.name}");
        }
        else
        {
            Debug.LogWarning($"Tab {tabIndex} ({tabs[tabIndex].tabName}) has no content panel assigned!");
        }

        UpdateTabButtonStates(tabIndex);
        currentTabIndex = tabIndex;
    }

    private void ShowNewTab(int tabIndex)
    {
        if (tabs[tabIndex].tabContent != null)
        {
            tabs[tabIndex].tabContent.SetActive(true);
            AnimateTabIn(tabs[tabIndex].tabContent);
        }
    }

    private void AnimateTabOut(GameObject tab, System.Action onComplete = null)
    {
        RectTransform rectTransform = tab.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            LeanTween.scale(rectTransform, Vector3.zero, tabTransitionDuration)
                .setEase(easeType)
                .setOnComplete(onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    private void AnimateTabIn(GameObject tab)
    {
        RectTransform rectTransform = tab.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.zero;
            LeanTween.scale(rectTransform, Vector3.one, tabTransitionDuration)
                .setEase(easeType);
        }
    }

    private void UpdateTabButtonStates(int activeTabIndex)
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            if (tabs[i].tabButton != null)
            {
                bool isActive = i == activeTabIndex;
                
                // Update button image color
                Image buttonImage = tabs[i].tabButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = isActive ? selectedTabColor : unselectedTabColor;
                }

                // Update button text color
                TextMeshProUGUI buttonText = tabs[i].tabButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.color = isActive ? selectedTextColor : unselectedTextColor;
                }

                // Scale effect for active tab
                RectTransform buttonRect = tabs[i].tabButton.GetComponent<RectTransform>();
                if (buttonRect != null)
                {
                    float targetScale = isActive ? 1.1f : 1.0f;
                    LeanTween.scale(buttonRect, Vector3.one * targetScale, 0.2f)
                        .setEase(LeanTweenType.easeOutBack);
                }
            }
        }
    }

    public void NextTab()
    {
        int nextIndex = (currentTabIndex + 1) % tabs.Count;
        ShowTab(nextIndex);
    }

    public void PreviousTab()
    {
        int prevIndex = currentTabIndex - 1;
        if (prevIndex < 0) prevIndex = tabs.Count - 1;
        ShowTab(prevIndex);
    }

    public int GetCurrentTabIndex()
    {
        return currentTabIndex;
    }

    public string GetCurrentTabName()
    {
        if (currentTabIndex >= 0 && currentTabIndex < tabs.Count)
            return tabs[currentTabIndex].tabName;
        return "";
    }

    // Keyboard navigation support
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (Input.GetKey(KeyCode.LeftShift))
                PreviousTab();
            else
                NextTab();
        }
    }
}