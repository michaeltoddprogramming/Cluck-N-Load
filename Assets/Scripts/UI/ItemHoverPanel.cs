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
        if (data == null)
        {
            Debug.LogWarning("ItemHoverPanel.Show: StructureData is null");
            return;
        }

        LeanTween.cancel(gameObject);

        // Hide name text entirely since it's redundant with the icon
        if (nameText != null)
        {
            Debug.Log("here are the name: " + data.name);
            nameText.text = data.name;
            // nameText.gameObject.SetActive(false);
        }

        // Set description if available
        if (descriptionText != null)
        {
            descriptionText.text = data.description;
            Debug.Log("here are the description: " + data.description);
        }

        // Make stats more concise for the small box
        string stats = $"${data.cost} • HP: {data.health}";

        // Compact stats for different structure types
        if (data.type == StructureType.Silo)
        {
            int perSilo = 0;
            if (InventoryManager.Instance != null)
            {
                perSilo = InventoryManager.Instance.totalPerSilo;
            }
            stats += $" • Cap: {perSilo}";
        }
        else if (data.type == StructureType.CropPlot)
        {
            stats += " • Grows Crops";
        }
        else if (data.type == StructureType.Animal)
        {
            if (data.prefab != null)
            {
                var animalStructure = data.prefab.GetComponent<AnimalStructure>();
                if (animalStructure != null)
                {
                    stats += $" • Max: {animalStructure.MaxAnimalCount} • ${animalStructure.baseMoneyPerProduct}/prod";
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
                    stats += $" • DMG: {armyData.AttackDamage} • SPD: {armyData.MovementSpeed}";
                }
            }
        }

        // Set stats text if available
        if (statsText != null)
        {
            statsText.text = stats;
            Debug.Log("here are the stats: " + stats);
        }

        // Shorter, more concise tips
        if (tipsText != null)
        {
            string tips = "";
            if (data.type == StructureType.Silo)
                tips = "<color=#FFD700>Near crops & animals for synergy</color>";
            else if (data.type == StructureType.CropPlot)
                tips = "<color=#FFD700>Near silos for yield bonus</color>";
            else if (data.type == StructureType.Animal)
                tips = "<color=#FFD700>Near silos for efficiency</color>";
            else if (data.type == StructureType.Barracks)
                tips = "<color=#FFD700>Far from animals for discounts</color>";

            Debug.Log("here are the tips: " + tips);

            tipsText.text = tips;
        }

        gameObject.SetActive(true);
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        panelRect.localScale = Vector3.one * 0.8f;
        canvasGroup.alpha = 0f;
        LeanTween.scale(panelRect, Vector3.one, 0.18f).setEase(LeanTweenType.easeOutBack).setIgnoreTimeScale(true);
        LeanTween.alphaCanvas(canvasGroup, 1f, 0.18f).setEase(LeanTweenType.easeOutQuad).setIgnoreTimeScale(true);
    }

    public void Hide()
    {
        LeanTween.cancel(gameObject);
        LeanTween.scale(panelRect, Vector3.one * 0.8f, 0.15f).setEase(LeanTweenType.easeInBack).setIgnoreTimeScale(true);
        LeanTween.alphaCanvas(canvasGroup, 0f, 0.15f).setEase(LeanTweenType.easeInQuad).setIgnoreTimeScale(true)
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
