using UnityEngine;


using TMPro;
using UnityEngine.UI;

public class TextController : MonoBehaviour
{
    [Header("Building Note UI")]
    [SerializeField] private TextMeshProUGUI buildingNoteText;

    private Color originalColor;
    private string originalText;
    private bool wasPaused;

    private NightManager nightManager;


    void Start()
    {
        if (buildingNoteText != null)
        {
            originalColor = buildingNoteText.color;
            originalText = buildingNoteText.text;
        }
        wasPaused = false;

        // Find NightManager from the scene
        nightManager = FindFirstObjectByType<NightManager>();
        if (nightManager == null)
        {
            Debug.LogWarning("TextController: NightManager not found in scene!");
        }
    }

    void Update()
    {
        if (nightManager == null || buildingNoteText == null)
            return;

        bool isPaused = nightManager.getIsPaused();

        if (isPaused && !wasPaused)
        {
            buildingNoteText.color = Color.yellow;
            buildingNoteText.text = "Cannot place Building while Paused";
            wasPaused = true;
        }
        else if (!isPaused && wasPaused)
        {
            buildingNoteText.color = originalColor;
            buildingNoteText.text = originalText;
            wasPaused = false;
        }
    }
}
