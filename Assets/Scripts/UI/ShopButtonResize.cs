using UnityEngine;
using UnityEngine.UI;

public class ShopTabController : MonoBehaviour
{
    public Image[] imageButtons;       // Assign your Image+Button objects in Inspector
    public float normalWidth = 100f;
    public float selectedWidth = 150f;

    // void Start()
    // {
    //     foreach (Image img in imageButtons)
    //     {
    //         // Skip null images (for removed tabs)
    //         if (img == null) continue;
    //         RectTransform rt = img.GetComponent<RectTransform>();
    //         // rt.pivot = new Vector2(1f, 0.5f); // pivot right, grow left
    //         // rt.sizeDelta = new Vector2(normalWidth, rt.sizeDelta.y);

    //         Vector2 originalPivot = rt.pivot;
    //         Vector2 originalPosition = rt.anchoredPosition;

    //         // Change pivot to right
    //         rt.pivot = new Vector2(1f, 0.5f);

    //         // Offset position to cancel the visual shift caused by pivot change
    //         Vector2 pivotDelta = rt.pivot - originalPivot;
    //         Vector2 size = rt.rect.size;
    //         Vector2 positionOffset = new Vector2(pivotDelta.x * size.x, pivotDelta.y * size.y);
    //         rt.anchoredPosition = originalPosition + positionOffset;

    //         // Set the initial width
    //         rt.sizeDelta = new Vector2(normalWidth, rt.sizeDelta.y);

    //         // Capture this image for the listener
    //         Image captured = img;

    //         Button btn = img.GetComponent<Button>();
    //         if (btn != null)
    //         {
    //             btn.onClick.AddListener(() => OnTabClicked(captured));
    //         }
    //     }
    // }

    void Start()
{
    for (int i = 0; i < imageButtons.Length; i++)
    {
        Image img = imageButtons[i];
        if (img == null) continue;

        RectTransform rt = img.GetComponent<RectTransform>();
        Vector2 originalPivot = rt.pivot;
        Vector2 originalPosition = rt.anchoredPosition;

        rt.pivot = new Vector2(1f, 0.5f);

        Vector2 pivotDelta = rt.pivot - originalPivot;
        Vector2 size = rt.rect.size;
        Vector2 positionOffset = new Vector2(pivotDelta.x * size.x, pivotDelta.y * size.y);
        rt.anchoredPosition = originalPosition + positionOffset;

        // Set width: first tab expanded, others normal
        float width = (i == 0) ? selectedWidth : normalWidth;
        rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);

        Image captured = img;
        Button btn = img.GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(() => OnTabClicked(captured));
    }
}


    void OnTabClicked(Image clickedImage)
    {
        foreach (Image img in imageButtons)
        {
            // Add null check to handle removed tabs
            if (img == null) continue;
            
            RectTransform rt = img.GetComponent<RectTransform>();
            if (rt == null) continue;
            
            bool isSelected = img == clickedImage;
            float width = isSelected ? selectedWidth : normalWidth;
            rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
        }
    }
}
