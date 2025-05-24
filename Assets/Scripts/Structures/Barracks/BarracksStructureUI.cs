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

    private BarracksStructure barracksStructure;
    private bool isBarracksStructure = false;
    private bool isPlacingFlag = false;

    public override void Initialize(Structure structure)
    {
        base.Initialize(structure);
        barracksStructure = structure as BarracksStructure;
        isBarracksStructure = barracksStructure != null;

        if (!isBarracksStructure)
        {
            Debug.LogWarning($"BarracksStructureUI used with non-barracks structure: {structure.GetType().Name}");
            HideBarracksUI();
            return;
        }

        barracksStructure.OnArmyChanged += UpdateUI;

        if (recruitButton != null)
        {
            recruitButton.onClick.RemoveAllListeners();
            recruitButton.onClick.AddListener(() =>
            {
                Debug.Log("Recruit button clicked!");
                barracksStructure.RecruitAnimals(recruitAmount);
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
    }

    private void StartFlagPlacement()
    {
        if (!isBarracksStructure || barracksStructure.ArmyAnimalCount <= 0) return;
        isPlacingFlag = true;
        if (recruitButton != null) recruitButton.interactable = false;
        if (setFlagColorButton != null) setFlagColorButton.interactable = false;
        if (placeFlagButton != null && placeFlagButton.GetComponentInChildren<TextMeshProUGUI>() != null)
        {
            placeFlagButton.GetComponentInChildren<TextMeshProUGUI>().text = "Click to Place Flag";
        }
        if (flagPlacementIndicator != null)
        {
            flagPlacementIndicator.SetActive(true);
        }
        StartCoroutine(HandleFlagPlacement());
    }

    private IEnumerator HandleFlagPlacement()
    {
        while (isPlacingFlag && !Input.GetMouseButtonDown(0))
        {
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
            Ray finalRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit finalHit;
            LayerMask groundLayer = LayerMask.GetMask("Ground", "Default");
            if (Physics.Raycast(finalRay, out finalHit, 1000f, groundLayer))
            {
                Vector3 position = finalHit.point;
                position.y = barracksStructure.transform.position.y;
                barracksStructure.PlaceFlag(position);
            }
        }
        // flagPlacementSounds();
        EndFlagPlacement();
    }

    private void EndFlagPlacement()
    {
        isPlacingFlag = false;
        UpdateUI();
        if (placeFlagButton != null && placeFlagButton.GetComponentInChildren<TextMeshProUGUI>() != null)
        {
            placeFlagButton.GetComponentInChildren<TextMeshProUGUI>().text = "Place Flag";
        }
        if (flagPlacementIndicator != null)
        {
            flagPlacementIndicator.SetActive(false);
        }

        
    }

    private void UpdateUI()
    {
        if (!isBarracksStructure || barracksStructure == null)
        {
            HideBarracksUI();
            return;
        }

        bool canRecruit = barracksStructure.CanRecruit(recruitAmount);
        bool hasArmy = barracksStructure.ArmyAnimalCount > 0;

        if (statusText != null)
        {
            if (barracksStructure.GetTargetStructure != null)
            {
                AnimalStructure target = barracksStructure.GetTargetStructure;
                statusText.text = $"Target: {target.GetStructureName()} (Chicken)\n" +
                                  $"Animals: {target.AnimalCount}/{target.MaxAnimalCount}\n" +
                                  $"Army: {barracksStructure.ArmyAnimalCount}/{barracksStructure.MaxArmyAnimals}";
                statusText.color = Color.white;
            }
            else
            {
                statusText.text = "No Chicken structure found!";
                statusText.color = Color.red;
            }
        }

        if (armyCountText != null)
        {
            armyCountText.text = $"Army: {barracksStructure.ArmyAnimalCount}/{barracksStructure.MaxArmyAnimals}";
            armyCountText.color = hasArmy ? Color.green : Color.white;
        }

        if (recruitButton != null && !isPlacingFlag)
        {
            recruitButton.interactable = canRecruit;
            TextMeshProUGUI buttonText = recruitButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                int cost = barracksStructure.GetRecruitmentCost() * recruitAmount;
                buttonText.text = $"Recruit ({cost} gold)";
            }
            Debug.Log($"Recruit button interactable: {canRecruit}");
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
    }

    private void HideBarracksUI()
    {
        if (statusText != null)
        {
            statusText.text = "Not a barracks structure";
            statusText.color = Color.red;
        }
        if (armyCountText != null) armyCountText.gameObject.SetActive(false);
        if (recruitButton != null) recruitButton.gameObject.SetActive(false);
        if (placeFlagButton != null) placeFlagButton.gameObject.SetActive(false);
        if (setFlagColorButton != null) setFlagColorButton.gameObject.SetActive(false);
        if (flagPlacementIndicator != null) flagPlacementIndicator.SetActive(false);
    }

    private void OnDestroy()
    {
        if (isBarracksStructure && barracksStructure != null)
        {
            barracksStructure.OnArmyChanged -= UpdateUI;
        }
    }

    
}