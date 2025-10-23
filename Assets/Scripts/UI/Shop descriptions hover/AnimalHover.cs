using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AnimalHover : MonoBehaviour
{
    private StructureData database;

    [SerializeField] private RectTransform panelRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI tipsText;

    [Header("All stats")]
    [SerializeField] private Image[] animalIcons;
    [SerializeField] private Sprite[] animalsSprites;
    [SerializeField] private Sprite[] foodSprites;
    [SerializeField] private Sprite[] productionSprites;

    [Header("Health and cost")]
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI heathText;

    [Header("Unit cost")]
    [SerializeField] private TextMeshProUGUI unitCostText;

    [Header("Feed info")]
    [SerializeField] private Image[] foodIcons;
    [SerializeField] private TextMeshProUGUI feedAmountText;

    [Header("Production info")]
    [SerializeField] private Image productIcon;
    [SerializeField] private TextMeshProUGUI produceAmountText;

    [Header("Capacity info")]
    [SerializeField] private TextMeshProUGUI capacityText;


    private void Awake()
    {
        // Instance = this;
        HideImmediate();
    }

    public void Show(StructureData data)
    {
        if (data == null)
        {
            Debug.LogWarning("ItemHoverPanel.Show: StructureData is null");
            return;
        }

        database = data;

        LeanTween.cancel(gameObject);

        setupAnimalIcons();

        if (nameText != null)
        {
            // Debug.Log("here are the name: " + data.name);
            nameText.text = database.structureName;
        }

        // Set description if available
        if (descriptionText != null)
        {
            descriptionText.text = database.description;
            // Debug.Log("here are the description: " + database.description);
        }

        costText.text = database.cost.ToString();
        heathText.text = database.health.ToString();

        unitCostText.text = database.costPerAnimal.ToString();

        FoodIcons();

        feedAmountText.text = database.baseFoodRequired.ToString();

        productionIcon();
        produceAmountText.text = database.moneyPerProduct.ToString();

        capacityText.text = "10";

        // Shorter, more concise tips
        if (tipsText != null)
        {
            tipsText.text = "<color=#FFD700>Placing this near a Silo reduces the amount of feed each animal needs.</color>";
        }

        gameObject.SetActive(true);
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        panelRect.localScale = Vector3.one * 0.8f;
        canvasGroup.alpha = 0f;
        LeanTween.scale(panelRect, Vector3.one, 0.18f).setEase(LeanTweenType.easeOutBack).setIgnoreTimeScale(true);
        LeanTween.alphaCanvas(canvasGroup, 1f, 0.18f).setEase(LeanTweenType.easeOutQuad).setIgnoreTimeScale(true);
    }

    private void setupAnimalIcons()
    {
        foreach (var icon in animalIcons)
        {
            if (database.structureName == "Chicken Coop")
            {
                icon.sprite = animalsSprites[0];
            }
            else if (database.structureName == "Cow Barn")
            {
                icon.sprite = animalsSprites[1];
            }
            else if (database.structureName == "Goat Pen")
            {
                icon.sprite = animalsSprites[2];
            }
            else if (database.structureName == "Pig Sty")
            {
                icon.sprite = animalsSprites[3];
            }
            else
            {
                icon.sprite = animalsSprites[4];
            }
        }
    }

    private void FoodIcons()
    {
        foreach (var icon in foodIcons)
        {
            if (database.structureName == "Chicken Coop")
            {
                icon.sprite = foodSprites[0];
            }
            else if (database.structureName == "Cow Barn")
            {
                icon.sprite = foodSprites[1];
            }
            else if (database.structureName == "Goat Pen")
            {
                icon.sprite = foodSprites[2];
            }
            else if (database.structureName == "Pig Sty")
            {
                icon.sprite = foodSprites[2];
            }
            else
            {
                icon.sprite = foodSprites[1];
            }
        }
    }

    private void productionIcon()
    {
        if (database.structureName == "Chicken Coop")
        {
            productIcon.sprite = productionSprites[0];
        }
        else if (database.structureName == "Cow Barn")
        {
            productIcon.sprite = productionSprites[1];
        }
        else if (database.structureName == "Goat Pen")
        {
            productIcon.sprite = productionSprites[2];
        }
        else if (database.structureName == "Pig Sty")
        {
            productIcon.sprite = productionSprites[3];
        }
        else
        {
            productIcon.sprite = productionSprites[4];
        }
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