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
                // string template = string.Format("{0,-15}{1}", "Animals", "AttackType");
                string template = "Animals\t\tAttackType\n";

                if (season == "Spring")
                {
                    // description = string.Format("{0}\n{1,-15}{2}", template, "Wolves", "Animals");
                    description = template + "Wolves\t\tAnimals\n";
                }
                else if (season == "Summer")
                {
                    // description = string.Format("{0}\n{1,-15}{2}\n{3,-15}{4}", template, "Wolves", "Animals", "Racoons", "Resources");
                    description = template + "Wolves\t\tAnimals\n" + "Racoons\t\tResources\n";
                }
                else if (season == "Fall")
                {
                    // description = string.Format("{0}\n{1,-15}{2}\n{3,-15}{4}\n{5,-15}{6}", template,
                    //     "Wolves", "Animals",
                    //     "Racoons", "Resources",
                    //     "Boars", "Walls");
                    description = template + "Wolves\t\tAnimals\n" + "Racoons\t\tResources\n" + "Boars\t\tWalls\n";
                }
                else if (season == "Winter")
                {
                    // description = string.Format("{0}\n{1,-15}{2}\n{3,-15}{4}\n{5,-15}{6}\n{7,-15}{8}", template,
                    //     "Wolves", "Animals",
                    //     "Racoons", "Resources",
                    //     "Boars", "Walls",
                    //     "Bears", "Attacks Buildings");
                    description = template + "Wolves\t\tAnimals\n" + "Racoons\t\tResources\n" + "Boars\t\tWalls\n" + "Bears\t\tBuildings\n";
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
            case TooltipType.animals:
                offset.x = 0;
                ProductionBoosts productionBoosts = FindFirstObjectByType<ProductionBoosts>();
                float[] products = productionBoosts.GetBoostedProducts();
                string[] animalNames = {"Chicken", "Cow", "Sheep", "Goat", "Pig"};

                // template = string.Format("{0,-15}{1}", "Animal", "Boost");
                // string template1 = string.Format("{0,-25}{1, 2}", "Animal", "Boost");
                string template1 = "Animal\t\tBoost";

                string tableBody = "";


                // description = string.Format("{0}\n{1,-15}{2}", template, "Wolves", "Animals");



                // Find longest animal name
                // int maxLength = 0;
                // for (int i = 0; i < animalNames.Length; i++)
                //     if (animalNames[i].Length > maxLength) maxLength = animalNames[i].Length;

                for(int k = 0; k < products.Length; k ++)
                {
                    if(products[k] == 2f)
                    {
                        switch(k)
                        {
                            case 0:
                                tableBody += $"{animalNames[k]}\t\t100%\n";
                                break;
                            case 1:
                                tableBody += $"{animalNames[k]}\t\t\t100%\n";
                                break;
                            case 2:
                                tableBody += $"{animalNames[k]}\t\t100%\n";
                                break;
                            case 3:
                                tableBody += $"{animalNames[k]}\t\t100%\n";
                                break;
                            case 4:
                                tableBody += $"{animalNames[k]}\t\t\t100%\n";
                                break;

                        }
                        // tableBody += string.Format("{0,-25}{1, 2}\n", animalNames[k], "100%");
                        // tableBody = string.Format("{0,-15}{1}", animalNames[k], "100%");
                        // description = tableHeader + tableBody;
                        description = template1 + "\t" + tableBody;
                        break;
                    }
                }

                for(int k = 0; k < products.Length; k ++)
                {
                    if(products[k] == 1.5f)
                    {
                        switch(k)
                        {
                            case 0:
                                tableBody += $"{animalNames[k]}\t\t50%\n";
                                break;
                            case 1:
                                tableBody += $"{animalNames[k]}\t\t\t50%\n";
                                break;
                            case 2:
                                tableBody += $"{animalNames[k]}\t\t50%\n";
                                break;
                            case 3:
                                tableBody += $"{animalNames[k]}\t\t50%\n";
                                break;
                            case 4:
                                tableBody += $"{animalNames[k]}\t\t\t50%\n";
                                break;
                            // case 0:
                            //     tableBody += $"{animalNames[k]}\t\t50%\n";
                            //     break;
                            // case 1:
                            //     tableBody += $"{animalNames[k]}\t\t50%\n";
                            //     break;
                            // case 2:
                            //     tableBody += $"{animalNames[k]}\t50%\n";
                            //     break;
                            // case 3:
                            //     tableBody += $"{animalNames[k]}\t\t50%\n";
                            //     break;
                            // case 4:
                            //     tableBody += $"{animalNames[k]}\t\t50%\n";
                            //     break;

                        }
                        // tableBody += string.Format("{0,-25}{1, 2}\n", animalNames[k], "50%");
                        // tableBody += string.Format("{0,-15}{1}\n", animalNames[k], "50%");
                        // tableBody += string.Format("{0}\n{1,-15}{2}", animalNames[k], "50%");
                    }
                }

                // tableBody += $"{animalNames[0]}\t\t50%\n";
                // tableBody += $"{animalNames[1]}\t\t\t50%\n";
                // tableBody += $"{animalNames[2]}\t\t50%\n";
                // tableBody += $"{animalNames[3]}\t\t50%\n";
                // tableBody += $"{animalNames[4]}\t\t\t50%\n";

                description = template1 + "\n" + tableBody;
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
