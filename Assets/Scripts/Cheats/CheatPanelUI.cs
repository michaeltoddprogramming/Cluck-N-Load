using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CheatPanelUI : MonoBehaviour
{
    [Header("Auto-Setup")]
    public bool autoSetupReferences = true;
    
    private void Start()
    {
        if (autoSetupReferences)
        {
            SetupReferences();
        }
    }
    
    private void SetupReferences()
    {
        CheatManager cheatManager = FindFirstObjectByType<CheatManager>();
        if (cheatManager == null) return;
        
        // You can use this to automatically assign UI references
        // This is optional - you can also assign them manually in the inspector
    }
}