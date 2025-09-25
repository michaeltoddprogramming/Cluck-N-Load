using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BarracksStructureUI : BaseStructureUI
{
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI armyCountText;
    [SerializeField] private Button recruitButton;
    [SerializeField] private Button placeFlagButton;
    [SerializeField] private Button setFlagColorButton;
    [SerializeField] private int recruitAmount = 1;
    [SerializeField] private GameObject flagPlacementIndicator;
    [SerializeField] private Button addAnimal;
    [SerializeField] private Button minusAnimal;
    [SerializeField] private TextMeshProUGUI animalCountText;
    [SerializeField] private TextMeshProUGUI costText;

    private BarracksStructure barracksStructure;
    private bool isBarracksStructure = false;
    private bool isPlacingFlag = false;
    private int newAnimalCount = 0;
    private int animalCount = 0;
    private int maxAnimalCount = 0;
    
    // Public property to check if this barracks is currently placing a flag
    public bool IsPlacingFlag => isPlacingFlag;

    [SerializeField] public Image animalIcon1;
    [SerializeField] public Image animalIcon2;
    [SerializeField] public Sprite cowIcon;
    [SerializeField] public Sprite chickenIcon;
    [SerializeField] public Sprite goatIcon;
    [SerializeField] public Sprite pigIcon;
    [SerializeField] public Sprite sheepIcon;

    // private BarracksStructure barrackStructure;

    public override void Initialize(Structure structure)
    {
        base.Initialize(structure);
        barracksStructure = structure as BarracksStructure;
        isBarracksStructure = barracksStructure != null;

        if (!isBarracksStructure)
        {
            HideBarracksUI();
            return;
        }

        barracksStructure.OnArmyChanged += UpdateUI;

        if (recruitButton != null)
        {
            recruitButton.onClick.RemoveAllListeners();
            recruitButton.onClick.AddListener(() =>
            {
                recruitAnimals();
                UpdateUI();
            });
        }
        else
        {
            Debug.LogError("Recruit Button is not assigned in BarracksStructureUI!");
        }

        if (placeFlagButton != null)
        {
            placeFlagButton.onClick.RemoveAllListeners();
            placeFlagButton.onClick.AddListener(StartFlagPlacement);
        }

        if (setFlagColorButton != null)
        {
            setFlagColorButton.onClick.RemoveAllListeners();
            setFlagColorButton.onClick.AddListener(() =>
            {
                Color[] presetColors = { Color.red, Color.blue, Color.green, Color.yellow, Color.white };
                Color currentColor = barracksStructure.GetFlagColor;
                int currentIndex = System.Array.IndexOf(presetColors, currentColor);
                Color newColor = presetColors[(currentIndex + 1) % presetColors.Length];
                barracksStructure.SetFlagColor(newColor);
                UpdateUI();
            });
        }

        if (flagPlacementIndicator != null)
        {
            flagPlacementIndicator.SetActive(false);
        }

        UpdateUI();

        barracksStructure.playBackgroundSound();

        SetupButtonListeners();

        animalCount = barracksStructure.GetAnimalCount();
        maxAnimalCount = barracksStructure.GetMaxAnimalCount();
    }

    private float lastUIUpdate;
    private const float UI_UPDATE_INTERVAL = 0.5f; // Update UI twice per second

    private void Update()
    {
        if (Time.time - lastUIUpdate > UI_UPDATE_INTERVAL)
        {
            UpdateUI();
            lastUIUpdate = Time.time;
        }

        // Additional safeguard for sheep flag button at night
        if (barracksStructure != null && barracksStructure.GetAnimalType() == "Sheep")
        {
            NightManager nightManager = NightManager.Instance;
            if (nightManager != null && !nightManager.IsDay && placeFlagButton != null)
            {
                placeFlagButton.interactable = false;
                Debug.LogWarning("[Sheep Barracks UI] Force-disabled placeFlagButton at night");
            }
        }
    }

    // Removed Update() method for better performance - using event-driven updates instead

    private void SetupButtonListeners()
    {
        if (addAnimal != null)
        {
            addAnimal.onClick.RemoveAllListeners();
            addAnimal.onClick.AddListener(() =>
            {
                // animalStructure.Feed();
                animalChange(0);
                UpdateUI();
            });
        }
        else
        {
        }
        if (minusAnimal != null)
        {
            minusAnimal.onClick.RemoveAllListeners();
            minusAnimal.onClick.AddListener(() =>
            {
                // animalStructure.Feed();
                animalChange(1);
                UpdateUI();
            });
        }
        else
        {
        }
        // if (recruitButton != null)
        // {
        //     recruitButton.onClick.RemoveAllListeners();
        //     recruitButton.onClick.AddListener(() =>
        //     {
        //         // animalStructure.Feed();
        //         UpdateUI();
        //     });
        // }
        // else
        // {
        // }
    }

    private void StartFlagPlacement()
    {
        if (!isBarracksStructure || barracksStructure.ArmyAnimalCount <= 0) return;
        
        // Check if it's sheep and if it's nighttime - prevent flag placement
        if (barracksStructure.GetAnimalType() == "Sheep")
        {
            NightManager nightManager = NightManager.Instance;
            if (nightManager != null && !nightManager.IsDay)
            {
                updateStatusText("Sheep flags can only be placed during the day");
                // Optional: Add visual feedback, e.g., flash the button red
                if (placeFlagButton != null)
                {
                    var buttonImage = placeFlagButton.GetComponent<Image>();
                    if (buttonImage != null) buttonImage.color = Color.red;
                    LeanTween.color(buttonImage.rectTransform, Color.white, 1f);  // Reset after 1 second
                }
                return; // Exit early, don't start flag placement
            }
        }
        
        isPlacingFlag = true;
        updateStatusText("Click on the ground to place flag");
        
        if (recruitButton != null) recruitButton.interactable = false;
        if (setFlagColorButton != null) setFlagColorButton.interactable = false;
        if (placeFlagButton != null)
        {
            placeFlagButton.interactable = false;
            placeFlagButton.GetComponentInChildren<TextMeshProUGUI>().text = "Placing...";
        }
        if (flagPlacementIndicator != null) flagPlacementIndicator.SetActive(true);
        
        // Add a small delay to prevent input conflicts
        StartCoroutine(HandleFlagPlacementWithDelay());
    }
    
    private IEnumerator HandleFlagPlacementWithDelay()
    {
        // Wait a frame to prevent immediate input consumption
        yield return null;
        yield return StartCoroutine(HandleFlagPlacement());
    }

    private IEnumerator HandleFlagPlacement()
    {
        while (isPlacingFlag && !Input.GetMouseButtonDown(0))
        {
            // Additional check during placement - if night starts, cancel placement
            if (barracksStructure.GetAnimalType() == "Sheep")
            {
                NightManager nightManager = NightManager.Instance;
                if (nightManager != null && !nightManager.IsDay)
                {
                    updateStatusText("Night started - sheep flag placement cancelled");
                    EndFlagPlacement();
                    yield break;
                }
            }
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            LayerMask groundLayer = LayerMask.GetMask("Ground", "Default");
            if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
            {
                Vector3 position = hit.point;
                if (flagPlacementIndicator != null)
                {
                    position.y += 0.1f;
                    flagPlacementIndicator.transform.position = position;
                }
            }
            yield return null;
        }

        if (isPlacingFlag)
        {
            // Make sure we don't place on UI elements
            if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                Ray finalRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit finalHit;
                LayerMask groundLayer = LayerMask.GetMask("Ground", "Default");
                
                if (Physics.Raycast(finalRay, out finalHit, 1000f, groundLayer))
                {
                    // Final check before placing the flag
                    if (barracksStructure.GetAnimalType() == "Sheep")
                    {
                        NightManager nightManager = NightManager.Instance;
                        if (nightManager != null && !nightManager.IsDay)
                        {
                            updateStatusText("Cannot place sheep flags at night");
                            EndFlagPlacement();
                            yield break;
                        }
                    }
                    
                    barracksStructure.PlaceFlag(finalHit.point);
                    updateStatusText("Flag placed successfully!");
                }
                else
                {
                    updateStatusText("Cannot place flag here - try clicking on the ground");
                }
            }
            else
            {
                updateStatusText("Cannot place flag on UI - click on the ground");
            }
        }
        
        EndFlagPlacement();
    }

    private void EndFlagPlacement()
    {
        isPlacingFlag = false;
        UpdateUI();
        if (placeFlagButton != null && placeFlagButton.GetComponentInChildren<TextMeshProUGUI>() != null)
        {
            updateStatusText("Click anywhere to place flag");
            // statusText.text = "Click anywhere to place flag";
            // placeFlagButton.GetComponentInChildren<TextMeshProUGUI>().text = "Place Flag";
        }
        if (flagPlacementIndicator != null)
        {
            flagPlacementIndicator.SetActive(false);
        }

    }

    private void UpdateUI()
    {
        if (!isBarracksStructure || barracksStructure == null) return;

        animalCountText.text = $"{newAnimalCount}";

        animalCount = barracksStructure.GetAnimalCount();
        maxAnimalCount = barracksStructure.GetMaxAnimalCount();

        bool canRecruit = barracksStructure.CanRecruit(newAnimalCount);
        bool hasArmy = barracksStructure.ArmyAnimalCount > 0;

        if (statusText != null)
        {
            if (isPlacingFlag)
            {
                statusText.text = "Click anywhere to place flag";
            }
            else if (barracksStructure.GetTargetStructure != null)
            {
                AnimalStructure target = barracksStructure.GetTargetStructure;
                statusText.text = $"Animals: {target.AnimalCount}/{target.MaxAnimalCount}\n" +
                                  $"Army: {barracksStructure.ArmyAnimalCount}/{barracksStructure.MaxArmyAnimals}";
                statusText.color = Color.white;
            }
            else
            {
                statusText.text = $"No {barracksStructure.GetAnimalType()}s nearby!";
                statusText.color = Color.yellow;
            }
        }

        if (armyCountText != null)
        {
            armyCountText.text = $"{barracksStructure.ArmyAnimalCount}/{barracksStructure.MaxArmyAnimals}";
            armyCountText.color = hasArmy ? Color.green : Color.white;
        }

        if (recruitButton != null && !isPlacingFlag)
        {
            recruitButton.interactable = canRecruit;
            // TextMeshProUGUI buttonText = recruitButton.GetComponentInChildren<TextMeshProUGUI>();
            if (costText != null)
            {
                int cost = barracksStructure.GetRecruitmentCost() * newAnimalCount;
                costText.text = cost.ToString();
            }
        }

        if (placeFlagButton != null && !isPlacingFlag)
        {
            placeFlagButton.interactable = hasArmy;
            ColorBlock colors = placeFlagButton.colors;
            colors.normalColor = hasArmy ? new Color(0.8f, 1f, 0.8f) : Color.grey;
            placeFlagButton.colors = colors;
        }

        if (setFlagColorButton != null && !isPlacingFlag)
        {
            setFlagColorButton.interactable = true;
            Image buttonImage = setFlagColorButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = Color.Lerp(barracksStructure.GetFlagColor, Color.white, 0.7f);
            }
        }

        if (addAnimal != null)
        {
            if ((newAnimalCount + animalCount) < maxAnimalCount && MoneyManager.Instance.CanAfford(newAnimalCount + 1 * barracksStructure.GetAnimalRecruitPrice()) && barracksStructure.CanRecruit(newAnimalCount + 1))
            {
                addAnimal.interactable = true;
            }
            else
            {
                addAnimal.interactable = false;
            }
        }

        if (minusAnimal != null)
        {
            if (newAnimalCount > 0)
            {
                minusAnimal.interactable = true;
            }
            else
            {
                minusAnimal.interactable = false;
            }
        }

        if (recruitButton != null)
        {
            if (minusAnimal != null && (minusAnimal.interactable == false || !MoneyManager.Instance.CanAfford(newAnimalCount * barracksStructure.GetAnimalRecruitPrice())))
            {
                recruitButton.interactable = false;
            }
        }

        if (MoneyManager.Instance != null && !MoneyManager.Instance.CanAfford(newAnimalCount * barracksStructure.GetAnimalRecruitPrice()))
        {
            updateStatusText($"Cannot afford {maxAnimalCount} many animals!");
        }

        // Add day/night check for sheep flag placement
        if (barracksStructure.GetAnimalType() == "Sheep")
        {
            NightManager nightManager = NightManager.Instance;
            bool isDay = nightManager != null ? nightManager.IsDay : true;
            Debug.Log($"[Sheep Barracks UI] IsDay: {isDay}, HasArmy: {hasArmy}");  // Debug log to check values
            
            if (placeFlagButton != null)
            {
                placeFlagButton.interactable = isDay && hasArmy;
                Debug.Log($"[Sheep Barracks UI] PlaceFlagButton interactable: {placeFlagButton.interactable}");  // Debug log for button state
                
                if (!isDay && statusText != null)
                {
                    updateStatusText("Sheep flags can only be placed during the day");
                }
                else if (isDay && statusText != null && !hasArmy)
                {
                    updateStatusText("No sheep army to place flags for");
                }
                else if (isDay && hasArmy && statusText != null)
                {
                    updateStatusText("Ready to place sheep flags");
                }
            }
        }
        else
        {
            // For non-sheep animals, flags can be placed anytime (existing behavior)
            if (placeFlagButton != null)
            {
                placeFlagButton.interactable = hasArmy;
            }
        }

        if (animalIcon1 != null)
        {
            if (barracksStructure.GetAnimalType() == "Cow")
            {
                animalIcon1.sprite = cowIcon;
            }
            else if (barracksStructure.GetAnimalType() == "Chicken")
            {
                animalIcon1.sprite = chickenIcon;
            }
            else if (barracksStructure.GetAnimalType() == "Goat")
            {
                animalIcon1.sprite = goatIcon;
            }
            else if (barracksStructure.GetAnimalType() == "Pig")
            {
                animalIcon1.sprite = pigIcon;
            }
            else if (barracksStructure.GetAnimalType() == "Sheep")
            {
                animalIcon1.sprite = sheepIcon;
            }
        }

        // if (animalIcon2 != null)
        // {
        //     if (barracksStructure.GetAnimalType() == "Cow")
        //     {
        //         animalIcon2.sprite = cowIcon;
        //     }
        //     else if (barracksStructure.GetAnimalType() == "Chicken")
        //     {
        //         animalIcon2.sprite = chickenIcon;
        //     }
        //     else if (barracksStructure.GetAnimalType() == "Goat")
        //     {
        //         animalIcon2.sprite = goatIcon;
        //     }
        //     else if (barracksStructure.GetAnimalType() == "Pig")
        //     {
        //         animalIcon2.sprite = pigIcon;
        //     }
        //     else if (barracksStructure.GetAnimalType() == "Sheep")
        //     {
        //         animalIcon2.sprite = sheepIcon;
        //     }
        // }
    }

    private void updateStatusText(string message)
    {
        // Update status text
        if (statusText != null)
        {
            string animalStatus = "";

            statusText.text = message;
            statusText.color = Color.yellow;
        }
    }

    private void HideBarracksUI()
    {

        if (statusText != null)
        {
            statusText.text = "Not a barracks structure";
            statusText.color = Color.yellow;
        }
        if (armyCountText != null) armyCountText.gameObject.SetActive(false);
        if (recruitButton != null) recruitButton.gameObject.SetActive(false);
        if (placeFlagButton != null) placeFlagButton.gameObject.SetActive(false);
        if (setFlagColorButton != null) setFlagColorButton.gameObject.SetActive(false);
        if (flagPlacementIndicator != null) flagPlacementIndicator.SetActive(false);

    }

    protected override void OnDestroy()
    {
        if (isBarracksStructure && barracksStructure != null)
        {
            barracksStructure.OnArmyChanged -= UpdateUI;
            barracksStructure.stopBackgroundSound();
        }

        // Call base OnDestroy
        base.OnDestroy();
    }

    private void animalChange(int flag)
    {
        if (flag == 0)
        {
            newAnimalCount += 1;
        }
        else if (flag == 1 && newAnimalCount > 0)
        {
            newAnimalCount -= 1;
        }
    }

    // private void BuyAnimals()
    // {
    //     if (newAnimalCount > 0)
    //     {
    //         animalStructure.AddAnimals(newAnimalCount);
    //         newAnimalCount = 0;
    //     }
    // }

    private void recruitAnimals()
    {
        if (newAnimalCount > 0)
        {
            barracksStructure.RecruitAnimals(newAnimalCount);
            newAnimalCount = 0;
        }
    }
}