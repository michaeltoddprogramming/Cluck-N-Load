using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SiloHover : MonoBehaviour
{
    private StructureData database;

    [SerializeField] private RectTransform panelRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI tipsText;

    [Header("Health and cost")]
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI heathText;

    [Header("Stats Display")]
    [SerializeField] private TextMeshProUGUI totalAmount;

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


        totalAmount.text = database.totalPerSilo.ToString();
        

        // Shorter, more concise tips
        if (tipsText != null)
        {
            tipsText.text = "<color=#FFD700>Silos near crops improve your harvest. Close to Animal Pens, they cut down on how much feed the animals need.</color>";
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