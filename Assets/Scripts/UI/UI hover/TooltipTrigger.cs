using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // [TextArea] public string message;
    public TooltipData data;
    private HoverToolTip tooltip;

    private void Start()
    {
        tooltip = FindFirstObjectByType<HoverToolTip>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        tooltip.Show(data.Title, data.Description, data.Type, GetComponent<RectTransform>());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltip.Hide();
    }
}
