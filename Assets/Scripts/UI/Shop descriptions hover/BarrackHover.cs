using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BarrackHover : MonoBehaviour
{
    private StructureData database;
    private ArmyType armyType;
    private ArmyData armyData;

    [SerializeField] private RectTransform panelRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI tipsText;

    [Header("All stats")]
    [SerializeField] private Image[] animalIcons;
    [SerializeField] private Sprite[] animalsSprites;

    [Header("Health and cost")]
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI heathText;

    [Header("Unit cost")]
    [SerializeField] private TextMeshProUGUI unitCostText;

    [Header("Capacity info")]
    [SerializeField] private TextMeshProUGUI capacityText;

    [Header("Stats Display")]
    [SerializeField] public GameObject[] healthAmount;
    [SerializeField] public GameObject[] damageAmount;
    [SerializeField] public GameObject[] rangeAmount;
    [SerializeField] public GameObject[] speedAmount;


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

        if (System.Enum.TryParse(data.targetAnimalType, out armyType))
        {
            armyData = Resources.Load<ArmyData>($"Prefabs/Units/All new stuff/Army/{armyType}");
            setUpStats();
            // if (armyData != null)
            // {
            //     stats += $" • DMG: {armyData.AttackDamage} • SPD: {armyData.MovementSpeed}";
            // }
        }
              

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


        unitCostText.text = database.recruitmentCostPerAnimal.ToString();

        capacityText.text = "5";
        

        // Shorter, more concise tips
        if (tipsText != null)
        {
            tipsText.text = "<color=#FFD700>The farther your Barracks is from the animals, the cheaper it is to recruit new troops.</color>";
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
            if (database.structureName == "Chicken Barrack")
            {
                icon.sprite = animalsSprites[0];
            }
            else if (database.structureName == "Cow Barrack")
            {
                icon.sprite = animalsSprites[1];
            }
            else if (database.structureName == "Goat Barrack")
            {
                icon.sprite = animalsSprites[2];
            }
            else if (database.structureName == "Pig Barrack")
            {
                icon.sprite = animalsSprites[3];
            }
            else
            {
                icon.sprite = animalsSprites[4];
            }
        }
    }

    public void setUpStats()
    {
        hideAllStatAmounts();
        if (armyType == ArmyType.Chicken)
        {
            healthAmount[0].SetActive(true);

            damageAmount[0].SetActive(true);

            rangeAmount[0].SetActive(true);

            speedAmount[0].SetActive(true);
            speedAmount[1].SetActive(true);
            speedAmount[2].SetActive(true);
            speedAmount[3].SetActive(true);
            speedAmount[4].SetActive(true);
        }
        else if (armyType == ArmyType.Cow)
        {
            healthAmount[0].SetActive(true);
            healthAmount[1].SetActive(true);
            healthAmount[2].SetActive(true);
            healthAmount[3].SetActive(true);
            healthAmount[4].SetActive(true);

            damageAmount[0].SetActive(true);
            damageAmount[1].SetActive(true);
            // damageAmount[2].SetActive(true);
            // damageAmount[3].SetActive(true);

            rangeAmount[0].SetActive(true);
            rangeAmount[1].SetActive(true);
            rangeAmount[2].SetActive(true);
            // rangeAmount[3].SetActive(true);

            speedAmount[0].SetActive(true);
            speedAmount[1].SetActive(true);
            // speedAmount[2].SetActive(true);
        }
        else if (armyType == ArmyType.Pig)
        {
            healthAmount[0].SetActive(true);
            healthAmount[1].SetActive(true);
            healthAmount[2].SetActive(true);

            damageAmount[0].SetActive(true);
            damageAmount[1].SetActive(true);
            damageAmount[2].SetActive(true);

            rangeAmount[0].SetActive(true);
            rangeAmount[1].SetActive(true);

            speedAmount[0].SetActive(true);
            speedAmount[1].SetActive(true);
            speedAmount[2].SetActive(true);
            // speedAmount[3].SetActive(true);
        }
        else if (armyType == ArmyType.Sheep)
        {
            healthAmount[0].SetActive(true);
            healthAmount[1].SetActive(true);
            healthAmount[2].SetActive(true);
            healthAmount[3].SetActive(true);

            damageAmount[0].SetActive(true);
            damageAmount[1].SetActive(true);
            damageAmount[2].SetActive(true);
            damageAmount[3].SetActive(true);
            damageAmount[4].SetActive(true);

            rangeAmount[0].SetActive(true);
            // rangeAmount[1].SetActive(true);

            speedAmount[0].SetActive(true);
            // speedAmount[1].SetActive(true);
        }
        else if (armyType == ArmyType.Goat)
        {
            healthAmount[0].SetActive(true);
            healthAmount[1].SetActive(true);
            healthAmount[2].SetActive(true);
            healthAmount[3].SetActive(true);

            damageAmount[0].SetActive(true);
            damageAmount[1].SetActive(true);
            damageAmount[2].SetActive(true);
            damageAmount[3].SetActive(true);
            // damageAmount[4].SetActive(true);

            rangeAmount[0].SetActive(true);
            rangeAmount[1].SetActive(true);
            rangeAmount[2].SetActive(true);
            rangeAmount[3].SetActive(true);
            rangeAmount[4].SetActive(true);

            speedAmount[0].SetActive(true);
        }
    }

    public void hideAllStatAmounts()
    {
        for (int k = 0; k < healthAmount.Length; k++)
        {
            healthAmount[k].SetActive(false);
        }
        for (int k = 0; k < damageAmount.Length; k++)
        {
            damageAmount[k].SetActive(false);
        }
        for (int k = 0; k < rangeAmount.Length; k++)
        {
            rangeAmount[k].SetActive(false);
        }
        for (int k = 0; k < speedAmount.Length; k++)
        {
            speedAmount[k].SetActive(false);
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