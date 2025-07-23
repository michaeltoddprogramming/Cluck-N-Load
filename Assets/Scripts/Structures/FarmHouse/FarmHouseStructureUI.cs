using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FarmHouseStructureUI : BaseStructureUI
{
    [Header("Farm House UI Elements")]
    [SerializeField] private TextMeshProUGUI farmStatusText;
    [SerializeField] private TextMeshProUGUI workerCountText;
    [SerializeField] private TextMeshProUGUI efficiencyText;
    [SerializeField] private Button addWorkerButton;
    [SerializeField] private Button removeWorkerButton;
    [SerializeField] private Slider efficiencySlider;

    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip workerHiredSound;
    [SerializeField] private AudioClip errorSound;
    [SerializeField] private float soundVolume = 0.7f;
    private AudioSource audioSource;

    private FarmHouseStructure farmHouseStructure;
    private bool isFarmHouseStructure = false;

    public override void Initialize(Structure structure)
    {
        base.Initialize(structure);

        farmHouseStructure = structure as FarmHouseStructure;
        isFarmHouseStructure = farmHouseStructure != null;

        if (!isFarmHouseStructure)
        {
            HideFarmHouseSpecificUI();
            return;
        }

        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.volume = soundVolume;

        SetupButtonListeners();
        UpdateUI();

        Debug.Log("FarmHouse UI initialized successfully!");
    }

    private void SetupButtonListeners()
    {
        if (addWorkerButton != null)
        {
            addWorkerButton.onClick.RemoveAllListeners();
            addWorkerButton.onClick.AddListener(() =>
            {
                PlaySound(buttonClickSound);
                if (farmHouseStructure.CanAddWorker())
                {
                    // Check if player has enough money (assuming 100 per worker)
                    if (MoneyManager.Instance != null && MoneyManager.Instance.GetCurrentMoney() >= 100)
                    {
                        MoneyManager.Instance.SpendMoney(100);
                        farmHouseStructure.AddWorker();
                        PlaySound(workerHiredSound);
                        UpdateUI();
                    }
                    else
                    {
                        PlaySound(errorSound);
                        Debug.Log("Not enough money to hire worker!");
                    }
                }
                else
                {
                    PlaySound(errorSound);
                    Debug.Log("Cannot add more workers - farm house is at capacity!");
                }
            });
        }

        if (removeWorkerButton != null)
        {
            removeWorkerButton.onClick.RemoveAllListeners();
            removeWorkerButton.onClick.AddListener(() =>
            {
                PlaySound(buttonClickSound);
                farmHouseStructure.RemoveWorker();
                UpdateUI();
            });
        }
    }

    private void UpdateUI()
    {
        if (!isFarmHouseStructure || farmHouseStructure == null)
            return;

        if (farmStatusText != null)
        {
            if (farmHouseStructure.IsMainBuilding)
            {
                farmStatusText.text = "Main Farm House\nOperational";
            }
            else
            {
                farmStatusText.text = "Secondary Building\nOperational";
            }
        }

        if (workerCountText != null)
        {
            workerCountText.text = $"Workers: {farmHouseStructure.CurrentWorkers}/{farmHouseStructure.MaxWorkers}";
        }

        if (efficiencyText != null)
        {
            float efficiency = farmHouseStructure.GetFarmEfficiency();
            efficiencyText.text = $"Farm Efficiency: {efficiency:P0}";
        }

        if (efficiencySlider != null)
        {
            float efficiency = farmHouseStructure.GetFarmEfficiency();
            efficiencySlider.value = (efficiency - 1f) / 0.5f; // Normalize to 0-1
        }

        if (addWorkerButton != null)
        {
            bool canAdd = farmHouseStructure.CanAddWorker();
            bool hasEnoughMoney = MoneyManager.Instance != null && MoneyManager.Instance.GetCurrentMoney() >= 100;
            addWorkerButton.interactable = canAdd && hasEnoughMoney;

            var buttonText = addWorkerButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                if (!canAdd)
                {
                    buttonText.text = "Max Workers";
                }
                else if (!hasEnoughMoney)
                {
                    buttonText.text = "Need $100";
                }
                else
                {
                    buttonText.text = "Hire Worker ($100)";
                }
            }
        }

        if (removeWorkerButton != null)
        {
            removeWorkerButton.interactable = farmHouseStructure.CurrentWorkers > 0;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void HideFarmHouseSpecificUI()
    {
        if (farmStatusText != null) farmStatusText.gameObject.SetActive(false);
        if (workerCountText != null) workerCountText.gameObject.SetActive(false);
        if (efficiencyText != null) efficiencyText.gameObject.SetActive(false);
        if (addWorkerButton != null) addWorkerButton.gameObject.SetActive(false);
        if (removeWorkerButton != null) removeWorkerButton.gameObject.SetActive(false);
        if (efficiencySlider != null) efficiencySlider.gameObject.SetActive(false);
    }

    protected override void OnDestroy()
    {
        // Clean up button listeners
        if (addWorkerButton != null)
        {
            addWorkerButton.onClick.RemoveAllListeners();
        }
        if (removeWorkerButton != null)
        {
            removeWorkerButton.onClick.RemoveAllListeners();
        }

        base.OnDestroy();
    }
}
