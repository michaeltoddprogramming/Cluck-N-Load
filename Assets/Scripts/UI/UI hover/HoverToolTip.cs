using UnityEngine;
using TMPro;

public class HoverToolTip : MonoBehaviour
{
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI Title;
    [SerializeField] private TextMeshProUGUI Description;
    [SerializeField] private Vector2 offset = new Vector2(0, -200); // adjust vertical/horizontal offset


    void Awake()
    {
        tooltipPanel.SetActive(false);
    }
    public void Show(string title, string description, TooltipType type, RectTransform target)
    {
        tooltipPanel.SetActive(true);

        switch (type)
        {
            case TooltipType.crops:
                offset.x = 0;
                string[] cropNames = new string[] { "s", "w", "c" };
                int[] crops = InventoryManager.Instance.getInventory(cropNames);
                // string template1 = string.Format("{0,-12}{1}", "Crop", "Amount");

                description = $"Sunflowers\t\t{crops[0]}\nWheat\t\t\t{crops[1]}\nCarrots\t\t\t{crops[2]}";
                break;

            case TooltipType.enemies:
                offset.x = 0;
                string season = NightManager.Instance.GetSeason();
                string template = string.Format("{0,-15}{1}", "Animals", "AttackType");

                if (season == "Spring")
                {
                    description = string.Format("{0}\n{1,-15}{2}", template, "Wolves", "Animals");
                }
                else if (season == "Summer")
                {
                    description = string.Format("{0}\n{1,-15}{2}\n{3,-15}{4}", template, "Wolves", "Animals", "Racoons", "Resources");
                }
                else if (season == "Fall")
                {
                    description = string.Format("{0}\n{1,-15}{2}\n{3,-15}{4}\n{5,-15}{6}", template,
                        "Wolves", "Animals",
                        "Racoons", "Resources",
                        "Boars", "Walls");
                }
                else if (season == "Winter")
                {
                    description = string.Format("{0}\n{1,-15}{2}\n{3,-15}{4}\n{5,-15}{6}\n{7,-15}{8}", template,
                        "Wolves", "Animals",
                        "Racoons", "Resources",
                        "Boars", "Walls",
                        "Bears", "Attacks Buildings");
                }
                break;
            case TooltipType.season:
                offset.x = 0;
                description = string.Format(description, $"\t\t{NightManager.Instance?.GetSeason()}", $"\t\t{NightManager.Instance?.Days}");
                break;
            case TooltipType.coin:
                offset.x = 80;
                break;
            case TooltipType.pricePanel:
                offset.x = 80;
                break;
            default:
                offset.x = 0;
                break;

        }

        tooltipPanel.SetActive(true);
        Title.text = title;
        Description.text = description;

        // Position next to target UI element
        Vector3 worldPos = target.position + (Vector3)offset;
        tooltipPanel.transform.position = worldPos;
    }

    public void Hide()
    {
        tooltipPanel.SetActive(false);
    }
}
