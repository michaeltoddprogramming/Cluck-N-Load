using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ItemHoverPanel : MonoBehaviour
{
    public static ItemHoverPanel Instance { get; private set; }

    private StructureData database;

    [SerializeField] private RectTransform panelRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI tipsText;

    [Header("All panels")]
    [SerializeField] private AnimalHover animalHover;
    [SerializeField] private BarrackHover barrackHover;
    [SerializeField] private SiloHover siloHover;
    [SerializeField] private CropHover cropHover;
    [SerializeField] private BaseHover baseHover;



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

        database = data;

        LeanTween.cancel(gameObject);

        if (data.type == StructureType.Animal)
        {
            animalHover.Show(data);
        }
        else if(data.type == StructureType.Barracks)
        {
            barrackHover.Show(data);
        }
        else if(data.type == StructureType.Silo)
        {
            siloHover.Show(data);
        }
        else if(data.type == StructureType.CropPlot)
        {
            cropHover.Show(data);
        }
        else
        {
            baseHover.Show(data);
        }

        // Shorter, more concise tips
        // if (tipsText != null)
        // {
        //     string tips = "";
        //     if (data.type == StructureType.Silo)
        //         tips = "<color=#FFD700>Near crops & animals for synergy</color>";
        //     else if (data.type == StructureType.CropPlot)
        //         tips = "<color=#FFD700>Near silos for yield bonus</color>";
        //     else if (data.type == StructureType.Animal)
        //         tips = "<color=#FFD700>Near silos for efficiency</color>";
        //     else if (data.type == StructureType.Barracks)
        //         tips = "<color=#FFD700>Far from animals for discounts</color>";

        //     Debug.Log("here are the tips: " + tips);

        //     tipsText.text = tips;
        // }

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
        if(database != null)
        {
            if (database.type == StructureType.Animal)
            {
                animalHover.Hide();
            }
            else if(database.type == StructureType.Barracks)
            {
                barrackHover.Hide();
            }
            else if(database.type == StructureType.Silo)
            {
                siloHover.Hide();
            }
            else if(database.type == StructureType.CropPlot)
            {
                cropHover.Hide();
            }
            else
            {
                baseHover.Hide();
            }
        }


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
        if(database != null)
        {
            if (database.type == StructureType.Animal)
            {
                animalHover.HideImmediate();
            }
            else if(database.type == StructureType.Barracks)
            {
                barrackHover.HideImmediate();
            }
            else if(database.type == StructureType.Silo)
            {
                siloHover.HideImmediate();
            }
            else if(database.type == StructureType.CropPlot)
            {
                cropHover.HideImmediate();
            }
            else
            {
                baseHover.HideImmediate();
            }
        }


        LeanTween.cancel(gameObject);
        canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }
}
