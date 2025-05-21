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
    [SerializeField] private GameObject flagPlacementIndicator; // Add this to your prefab - a visual indicator

    private BarracksStructure barracksStructure;
    private bool isBarracksStructure = false;
    private bool isPlacingFlag = false;

    public override void Initialize(Structure structure)
    {
        base.Initialize(structure);

        // Cast structure to BarracksStructure
        barracksStructure = structure as BarracksStructure;
        isBarracksStructure = barracksStructure != null;
        
        if (!isBarracksStructure)
        {
            Debug.LogWarning($"BarracksStructureUI used with non-barracks structure: {structure.GetType().Name}");
            HideBarracksUI();
            return;
        }

        // Subscribe to army changes
        if (barracksStructure.OnArmyChanged == null)
            barracksStructure.OnArmyChanged = new System.Action(() => {});
            
        barracksStructure.OnArmyChanged += OnArmyChanged;
        
        // Set up recruit button
        if (recruitButton != null)
        {
            recruitButton.onClick.RemoveAllListeners();
            recruitButton.onClick.AddListener(() =>
            {
                Debug.Log($"Recruit button clicked, attempting to recruit {recruitAmount} animals...");
                barracksStructure.RecruitAnimals(recruitAmount);
                // UI will be updated via the OnArmyChanged event
            });
        }

        // Set up place flag button
        if (placeFlagButton != null)
        {
            placeFlagButton.onClick.RemoveAllListeners();
            placeFlagButton.onClick.AddListener(() =>
            {
                Debug.Log("Place flag button clicked");
                StartFlagPlacement();
            });
        }

        // Set up color button
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

        // Initialize the flag placement indicator if available
        if (flagPlacementIndicator != null)
        {
            flagPlacementIndicator.SetActive(false);
        }

        UpdateUI();
    }

    private void StartFlagPlacement()
    {
        if (!isBarracksStructure || barracksStructure.ArmyAnimalCount <= 0) return;
        
        Debug.Log("Starting flag placement mode");
        isPlacingFlag = true;
        
        // Disable other UI elements during placement
        if (recruitButton != null) recruitButton.interactable = false;
        if (setFlagColorButton != null) setFlagColorButton.interactable = false;
        
        // Change the button text to indicate we're in placement mode
        if (placeFlagButton != null && placeFlagButton.GetComponentInChildren<TextMeshProUGUI>() != null)
        {
            placeFlagButton.GetComponentInChildren<TextMeshProUGUI>().text = "Click to Place Flag";
        }
        
        // Show the indicator if available
        if (flagPlacementIndicator != null)
        {
            flagPlacementIndicator.SetActive(true);
        }
        
        StartCoroutine(HandleFlagPlacement());
    }

    private IEnumerator HandleFlagPlacement()
    {
        // Wait until the player clicks
        while (isPlacingFlag && !Input.GetMouseButtonDown(0))
        {
            // Update indicator position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            LayerMask groundLayer = LayerMask.GetMask("Ground", "Default"); // Adjust these layer names as needed
            
            if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
            {
                Vector3 position = hit.point;
                
                // Update indicator position
                if (flagPlacementIndicator != null)
                {
                    position.y += 0.1f; // Slightly above ground to avoid z-fighting
                    flagPlacementIndicator.transform.position = position;
                }
            }
            
            yield return null;
        }
        
        // Handle the click if we're still in placement mode
        if (isPlacingFlag)
        {
            Ray finalRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit finalHit;
            LayerMask groundLayer = LayerMask.GetMask("Ground", "Default");
            
            if (Physics.Raycast(finalRay, out finalHit, 1000f, groundLayer))
            {
                Vector3 position = finalHit.point;
                position.y = barracksStructure.transform.position.y; // Keep at same height as barracks
                
                Debug.Log($"Placing flag at {position}");
                barracksStructure.PlaceFlag(position);
            }
        }
        
        // Cleanup
        EndFlagPlacement();
    }
    
    private void EndFlagPlacement()
    {
        isPlacingFlag = false;
        
        // Re-enable UI elements
        UpdateUI();
        
        // Reset button text
        if (placeFlagButton != null && placeFlagButton.GetComponentInChildren<TextMeshProUGUI>() != null)
        {
            placeFlagButton.GetComponentInChildren<TextMeshProUGUI>().text = "Place Flag";
        }
        
        // Hide the indicator
        if (flagPlacementIndicator != null)
        {
            flagPlacementIndicator.SetActive(false);
        }
    }

    // Event handler for army changes
    private void OnArmyChanged()
    {
        Debug.Log($"OnArmyChanged event received! Army count: {barracksStructure.ArmyAnimalCount}");
        UpdateUI();
    }

    // Remove this Update method - it's calling UpdateUI every frame which is inefficient
    protected override void Update()
    {
        base.Update();
        
        // Only handle flag placement preview here, not full UI updates
        if (isPlacingFlag && flagPlacementIndicator != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                flagPlacementIndicator.transform.position = hit.point + Vector3.up * 0.1f;
            }
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

        Debug.Log($"UpdateUI - CanRecruit: {canRecruit}, HasArmy: {hasArmy}, ArmyCount: {barracksStructure.ArmyAnimalCount}");

        // Update status text
        if (statusText != null)
        {
            if (barracksStructure.GetTargetStructure != null)
            {
                AnimalStructure target = barracksStructure.GetTargetStructure;
                statusText.text = $"Target: {target.GetStructureName()} ({barracksStructure.TargetAnimalType})\n" +
                                  $"Animals: {target.AnimalCount}/{target.MaxAnimalCount}\n" +
                                  $"Army: {barracksStructure.ArmyAnimalCount}/{barracksStructure.MaxArmyAnimals}";
                statusText.color = Color.white;
            }
            else
            {
                statusText.text = $"No {barracksStructure.TargetAnimalType} structure found!";
                statusText.color = Color.red;
            }
        }

        // Update army count text
        if (armyCountText != null)
        {
            armyCountText.text = $"Army: {barracksStructure.ArmyAnimalCount}/{barracksStructure.MaxArmyAnimals}";
            armyCountText.color = hasArmy ? Color.green : Color.white;
        }

        // Update button states
        if (recruitButton != null && !isPlacingFlag)
        {
            recruitButton.interactable = canRecruit;
            
            // Add text to show cost
            TextMeshProUGUI buttonText = recruitButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                int cost = barracksStructure.GetRecruitmentCost() * recruitAmount;
                buttonText.text = $"Recruit ({cost} gold)";
            }
        }

        if (placeFlagButton != null && !isPlacingFlag)
        {
            placeFlagButton.interactable = hasArmy;
            
            // Visual feedback for flag button
            ColorBlock colors = placeFlagButton.colors;
            colors.normalColor = hasArmy ? new Color(0.8f, 1f, 0.8f) : Color.grey;
            placeFlagButton.colors = colors;
        }

        if (setFlagColorButton != null && !isPlacingFlag)
        {
            setFlagColorButton.interactable = true;
            
            // Show current color
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
        // Clean up event subscriptions
        if (isBarracksStructure && barracksStructure != null)
        {
            barracksStructure.OnArmyChanged -= OnArmyChanged;
        }
    }
}