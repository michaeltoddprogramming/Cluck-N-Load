using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;

public class AnimalStructureUI : BaseStructureUI
{
    [SerializeField] private Button feedButton;
    [SerializeField] private Button collectButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Slider progressBar;
    
    // Fields to access private variables via reflection
    private FieldInfo isProducingField;
    private FieldInfo productReadyField;
    private FieldInfo productionProgressField;
    private FieldInfo productionSettingsField;
    
    public override void Initialize(Structure structure)
    {
        base.Initialize(structure);
        
        // Get references to private fields using reflection
        System.Type animalType = structure.GetType();
        isProducingField = animalType.GetField("isProducing", BindingFlags.Instance | BindingFlags.NonPublic);
        productReadyField = animalType.GetField("productReady", BindingFlags.Instance | BindingFlags.NonPublic);
        productionProgressField = animalType.GetField("productionProgress", BindingFlags.Instance | BindingFlags.NonPublic);
        productionSettingsField = animalType.GetField("productionSettings", BindingFlags.Instance | BindingFlags.NonPublic);
        
        // Update UI with current state
        UpdateUI();
        
        // Set up buttons
        if (feedButton != null)
        {
            feedButton.onClick.RemoveAllListeners();
            feedButton.onClick.AddListener(() => {
                MethodInfo feedMethod = animalType.GetMethod("Feed", BindingFlags.Instance | BindingFlags.Public);
                if (feedMethod != null)
                {
                    feedMethod.Invoke(structure, null);
                    UpdateUI();
                }
            });
        }
        
        if (collectButton != null)
        {
            collectButton.onClick.RemoveAllListeners();
            collectButton.onClick.AddListener(() => {
                MethodInfo collectMethod = animalType.GetMethod("Collect", BindingFlags.Instance | BindingFlags.Public);
                if (collectMethod != null)
                {
                    collectMethod.Invoke(structure, null);
                    UpdateUI();
                }
            });
        }
    }
    
    protected override void Update()
    {
        base.Update();
        
        // Update production progress in real-time
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        if (structure == null || isProducingField == null) return;
        
        bool isProducing = (bool)isProducingField.GetValue(structure);
        bool productReady = (bool)productReadyField.GetValue(structure);
        
        // Update buttons visibility
        if (feedButton != null)
            feedButton.gameObject.SetActive(!isProducing && !productReady);
            
        if (collectButton != null)
            collectButton.gameObject.SetActive(productReady);
            
        // Update status text
        if (statusText != null)
        {
            if (productReady)
            {
                statusText.text = "Ready to collect!";
                statusText.color = Color.green;
            }
            else if (isProducing)
            {
                float progress = (float)productionProgressField.GetValue(structure);
                object settings = productionSettingsField.GetValue(structure);
                float totalTime = (float)settings.GetType().GetField("productionTime").GetValue(settings);
                int secondsLeft = Mathf.CeilToInt(totalTime - progress);
                
                statusText.text = $"Producing... ({secondsLeft}s)";
                statusText.color = Color.yellow;
                
                // Update progress bar
                if (progressBar != null)
                {
                    progressBar.gameObject.SetActive(true);
                    progressBar.maxValue = totalTime;
                    progressBar.value = progress;
                }
            }
            else
            {
                statusText.text = "Needs feeding";
                statusText.color = Color.white;
                
                if (progressBar != null)
                    progressBar.gameObject.SetActive(false);
            }
        }
    }
}