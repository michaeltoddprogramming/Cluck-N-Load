using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ToggleButtonVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image buttonImage;
    public Sprite defaultSprite;
    public Sprite hoverSprite;
    public Sprite selectedSprite;

    private bool isSelected = false;
    private bool isHovered = false;

    private ToggleButtonGroup group;

    public void Initialize(ToggleButtonGroup buttonGroup)
    {
        group = buttonGroup;
    }

    public void OnClick()
    {
        group.OnButtonSelected(this);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        UpdateVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (isSelected)
            buttonImage.sprite = selectedSprite;
        else if (isHovered)
            buttonImage.sprite = hoverSprite;
        else
            buttonImage.sprite = defaultSprite;
    }
}

// using UnityEngine;
// using UnityEngine.UI;
// using UnityEngine.EventSystems;

// public class ToggleButtonVisual : MonoBehaviour, IPointerClickHandler
// {
//     public Image buttonImage;
//     public Sprite defaultSprite;
//     public Sprite selectedSprite;

//     private bool isSelected = false;

//     public void OnPointerClick(PointerEventData eventData)
//     {
//         ToggleButtonSelection();
//     }

//     private void ToggleButtonSelection()
//     {
//         isSelected = !isSelected;
//         UpdateButtonVisual();
//     }

//     private void UpdateButtonVisual()
//     {
//         buttonImage.sprite = isSelected ? selectedSprite : defaultSprite;
//     }

//     // Call this to reset the button to default state if needed (e.g., when switching from pause to play)
//     public void ResetButton()
//     {
//         isSelected = false;
//         UpdateButtonVisual();
//     }
// }
