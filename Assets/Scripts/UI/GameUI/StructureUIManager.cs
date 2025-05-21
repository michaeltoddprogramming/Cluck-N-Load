using UnityEngine;
using System.Collections.Generic;

public class StructureUIManager : MonoBehaviour
{
    public static StructureUIManager Instance { get; private set; }
    
    [SerializeField] private Transform uiParent;
    [SerializeField] private GameObject defaultStructureUI;
    [SerializeField] private float uiOffset = 0.5f;
    [SerializeField] private Vector2 screenOffset = new Vector2(0, 20f);
    
    private Structure currentSelectedStructure;
    private GameObject activeUI;
    private RectTransform activeUIRect;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (uiParent == null)
        {
            GameObject parent = new GameObject("Structure UI Parent");
            uiParent = parent.transform;
            parent.transform.SetParent(transform);
        }
    }
    
    private void Update()
    {
        if (currentSelectedStructure != null && activeUI != null && activeUIRect != null)
        {
            // Calculate the target screen position including the offset
            Vector3 screenPos = GetScreenPositionAboveStructure(currentSelectedStructure);
            screenPos += new Vector3(screenOffset.x, screenOffset.y, 0);

            // Directly set the position for an anchored feel (no Lerp)
            activeUIRect.position = screenPos;

            // Scale based on distance (optional, remove if not needed)
            float distance = Vector3.Distance(Camera.main.transform.position, currentSelectedStructure.transform.position);
            float scale = Mathf.Clamp(1f / (distance * 0.1f), 0.5f, 1.5f);
            activeUIRect.localScale = new Vector3(scale, scale, 1f);

            // Hide UI if the structure is off-screen
            Vector3 viewportPos = Camera.main.WorldToViewportPoint(currentSelectedStructure.transform.position);
            bool isOnScreen = viewportPos.x >= 0 && viewportPos.x <= 1 && viewportPos.y >= 0 && viewportPos.y <= 1 && viewportPos.z > 0;
            activeUI.SetActive(isOnScreen);


        }
    }
    
    public void ShowStructureUI(Structure structure)
    {
        if (structure == null)
        {
            Debug.LogWarning("Cannot show UI: Structure is null");
            return;
        }

        Debug.Log($"ShowStructureUI called for {structure.GetStructureName()}");
        
        HideStructureUI();
        
        currentSelectedStructure = structure;
        
        GameObject prefab = null;
        if (structure.structureData != null && structure.structureData.uiPrefab != null)
        {
            prefab = structure.structureData.uiPrefab;
            Debug.Log($"Using specific UI prefab: {prefab.name}");
        }
        else
        {
            prefab = defaultStructureUI;
            if (prefab == null)
            {
                Debug.LogWarning("No default structure UI prefab assigned!");
                return;
            }
            Debug.Log($"Using default UI prefab: {prefab.name}");
        }
        
        // Instantiate the UI under the Canvas
        activeUI = Instantiate(prefab, uiParent);
        
        // Set up the RectTransform for positioning
        activeUIRect = activeUI.GetComponent<RectTransform>();
        if (activeUIRect != null)
        {
            activeUIRect.anchorMin = new Vector2(0.5f, 0.5f);
            activeUIRect.anchorMax = new Vector2(0.5f, 0.5f);
            activeUIRect.pivot = new Vector2(0.5f, 0.5f);
            
            // Set initial position with the same logic as Update
            Vector3 screenPos = GetScreenPositionAboveStructure(structure);
            screenPos += new Vector3(screenOffset.x, screenOffset.y, 0);
            activeUIRect.position = screenPos;

            // Set initial scale (optional, remove if not needed)
            float distance = Vector3.Distance(Camera.main.transform.position, structure.transform.position);
            float scale = Mathf.Clamp(1f / (distance * 0.1f), 0.5f, 1.5f);
            activeUIRect.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            Debug.LogWarning("UI prefab does not have a RectTransform!");
        }
        
        IStructureUI structureUI = activeUI.GetComponent<IStructureUI>();
        if (structureUI != null)
        {
            structureUI.Initialize(structure);
            Debug.Log("UI initialized successfully");
        }
        else
        {
            Debug.LogWarning($"UI prefab for {structure.GetStructureName()} doesn't implement IStructureUI interface");
        }
    }
    
    public void HideStructureUI()
    {
        if (activeUI != null)
        {
            Destroy(activeUI);
            activeUI = null;
            activeUIRect = null;
        }
        
        currentSelectedStructure = null;
    }
    
    private Vector3 GetScreenPositionAboveStructure(Structure structure)
    {
        if (structure == null || Camera.main == null)
            return Vector3.zero;

        float structureHeight = 1f;
        Renderer renderer = structure.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            structureHeight = renderer.bounds.size.y;
        }

        Vector3 worldPos = structure.transform.position + Vector3.up * (structureHeight + uiOffset);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        // Clamp to screen bounds to prevent the UI from going off-screen
        screenPos.x = Mathf.Clamp(screenPos.x, 0, Screen.width);
        screenPos.y = Mathf.Clamp(screenPos.y, 0, Screen.height);

        return screenPos;
    }
}