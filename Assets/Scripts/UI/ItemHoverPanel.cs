using UnityEngine;
using TMPro;

public class ItemHoverPanel : MonoBehaviour
{
    public static ItemHoverPanel Instance { get; private set; }

    [SerializeField] private RectTransform panelRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI tipsText;


    private void Awake()
    {
        Instance = this;
        HideImmediate();
    }

    public void Show(StructureData data)
    {
        LeanTween.cancel(gameObject);

        nameText.text = data.structureName;
        descriptionText.text = data.description;

        string stats = $"Cost: {data.cost}\n\nHealth: {data.health}\n";

        if (data.type == StructureType.Silo)
        {
            int perSilo = 0;
            int current = 0;
            int total = 0;
            if (InventoryManager.Instance != null)
            {
                perSilo = InventoryManager.Instance.totalPerSilo;
                current = InventoryManager.Instance.GetCurrentSiloCapacity();
                total = InventoryManager.Instance.GetTotalSiloCapacity();
            }
            stats += $"\nSingle Silo Capacity: {perSilo}\n";
            stats += $"\nYour Used Capacity: {current} / {total}";
        }

        if (data.type == StructureType.CropPlot)
        {
            stats += "\nGrows: Sunflower, Wheat, Carrots";
        }

        if (data.type == StructureType.Animal)
        {
            if (data.prefab == null)
            {
                stats += "\n<color=red>Prefab not assigned in StructureData!</color>";
            }
            else
            {
                var animalStructure = data.prefab.GetComponent<AnimalStructure>();
                if (animalStructure == null)
                {
                    stats += "\n<color=red>AnimalStructure script missing on prefab!</color>";
                }
                else
                {
                    stats += $"\nMax Capacity: {animalStructure.MaxAnimalCount}\n";
                    stats += $"\nProduction Return: {animalStructure.baseMoneyPerProduct}";
                }
            }
        }
        else if (data.type == StructureType.Barracks)
        {
            ArmyType armyType;
            if (System.Enum.TryParse(data.targetAnimalType, out armyType))
            {
                ArmyData armyData = Resources.Load<ArmyData>($"Prefabs/Units/All new stuff/Army/{armyType}");
                if (armyData != null)
                {
                    stats +=
                        $"\nArmy Health: {armyData.Health}\n" +
                        $"\nDamage: {armyData.AttackDamage}\n" +
                        $"\nSpeed: {armyData.MovementSpeed}\n" +
                        $"\nAttack Range: {armyData.AttackRange}";
                }
                else
                {
                    stats += $"\n<color=red>No ArmyData found for type '{armyType}'!</color>";
                }
            }
            else
            {
                stats += $"\n<color=red>Invalid army type: '{data.targetAnimalType}'</color>";
            }
        }

        statsText.text = stats;

        string tips = "";
        if (data.type == StructureType.Silo)
            tips = "<color=#FFD700>Tip: Place silos near crops and animal pens for synergy bonuses!</color>";
        else if (data.type == StructureType.CropPlot)
            tips = "<color=#FFD700>Tip: Place crop plots near silos for increased yield synergy!</color>\n<color=#FFD700>Seasonal production boosts may increase harvests!</color>";
        else if (data.type == StructureType.Animal)
            tips = "<color=#FFD700>Tip: Place animal pens near silos for food efficiency synergy!</color>\n<color=#FFD700>Seasonal boosts can increase animal product output!</color>";
        else if (data.type == StructureType.Barracks)
            tips = "<color=#FFD700>Tip: Place barracks far away from animal pens for recruitment discounts!</color>";

        tipsText.text = tips;

        gameObject.SetActive(true);
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        panelRect.localScale = Vector3.one * 0.8f;
        canvasGroup.alpha = 0f;
        LeanTween.scale(panelRect, Vector3.one, 0.18f).setEase(LeanTweenType.easeOutBack);
        LeanTween.alphaCanvas(canvasGroup, 1f, 0.18f).setEase(LeanTweenType.easeOutQuad);
    }

    public void Hide()
    {
        LeanTween.cancel(gameObject);
        LeanTween.scale(panelRect, Vector3.one * 0.8f, 0.15f).setEase(LeanTweenType.easeInBack);
        LeanTween.alphaCanvas(canvasGroup, 0f, 0.15f).setEase(LeanTweenType.easeInQuad)
            .setOnComplete(() =>
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                gameObject.SetActive(false);
            });
    }

    public void HideImmediate()
    {
        LeanTween.cancel(gameObject);
        canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }
}
