using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class TutorialHighlighter : MonoBehaviour
{
    [Header("Highlight Effects")]
    [SerializeField] private GameObject highlightPrefab;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.3f;
    
    [Header("World Pointer")]
    [SerializeField] private GameObject worldPointerPrefab;
    [SerializeField] private float worldPointerHeight = 2f;
    [SerializeField] private float bounceSpeed = 3f;
    [SerializeField] private float bounceAmount = 0.5f;
    
    [Header("UI Arrow")]
    [SerializeField] private GameObject uiArrowPrefab;
    [SerializeField] private float arrowDistance = 100f;
    [SerializeField] private Canvas uiCanvas;
    
    private List<GameObject> activeHighlights = new List<GameObject>();
    private GameObject currentWorldPointer;
    private GameObject currentUIArrow;
    private Vector3 originalWorldPointerPos;

    public void HighlightUIElement(string tag)
    {
        GameObject targetUI = GameObject.FindGameObjectWithTag(tag);
        if (targetUI != null)
        {
            HighlightUIElement(targetUI);
        }
    }

    public void HighlightUIElement(GameObject uiElement)
    {
        if (uiElement == null) return;

        // Create highlight overlay
        GameObject highlight = CreateUIHighlight(uiElement);
        if (highlight != null)
        {
            activeHighlights.Add(highlight);
            StartCoroutine(PulseHighlight(highlight));
        }

        // Create pointing arrow
        CreateUIArrow(uiElement);
    }

    public void HighlightWorldPosition(Vector3 worldPosition)
    {
        CreateWorldPointer(worldPosition);
    }

    public void HighlightWorldObject(GameObject worldObject)
    {
        if (worldObject == null) return;

        Vector3 position = worldObject.transform.position;
        position.y += worldPointerHeight;
        
        CreateWorldPointer(position);
        
        // Also add outline to the object if it has a renderer
        AddObjectOutline(worldObject);
    }

    private GameObject CreateUIHighlight(GameObject uiElement)
    {
        if (uiCanvas == null)
        {
            uiCanvas = FindFirstObjectByType<Canvas>();
        }

        if (uiCanvas == null) return null;

        RectTransform targetRect = uiElement.GetComponent<RectTransform>();
        if (targetRect == null) return null;

        // Create highlight GameObject
        GameObject highlight = new GameObject("UI_Highlight");
        highlight.transform.SetParent(uiCanvas.transform, false);
        
        // Position and size to match target
        RectTransform highlightRect = highlight.AddComponent<RectTransform>();
        highlightRect.position = targetRect.position;
        highlightRect.sizeDelta = targetRect.sizeDelta;
        highlightRect.anchorMin = targetRect.anchorMin;
        highlightRect.anchorMax = targetRect.anchorMax;
        highlightRect.anchoredPosition = targetRect.anchoredPosition;

        // Add visual components
        Image highlightImage = highlight.AddComponent<Image>();
        highlightImage.color = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0.3f);
        highlightImage.raycastTarget = false;

        // Add outline
        Outline outline = highlight.AddComponent<Outline>();
        outline.effectColor = highlightColor;
        outline.effectDistance = new Vector2(2, 2);

        return highlight;
    }

    private void CreateUIArrow(GameObject uiElement)
    {
        if (uiArrowPrefab == null || uiCanvas == null) return;

        // Remove existing arrow
        if (currentUIArrow != null)
        {
            Destroy(currentUIArrow);
        }

        currentUIArrow = Instantiate(uiArrowPrefab, uiCanvas.transform);
        
        RectTransform targetRect = uiElement.GetComponent<RectTransform>();
        RectTransform arrowRect = currentUIArrow.GetComponent<RectTransform>();
        
        if (targetRect != null && arrowRect != null)
        {
            // Position arrow near the target
            Vector3 targetPos = targetRect.position;
            Vector3 direction = (targetPos - uiCanvas.transform.position).normalized;
            
            arrowRect.position = targetPos + direction * arrowDistance;
            
            // Rotate arrow to point at target
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            arrowRect.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            
            // Animate arrow
            StartCoroutine(AnimateUIArrow(arrowRect, targetPos));
        }
    }

    private void CreateWorldPointer(Vector3 worldPosition)
    {
        if (worldPointerPrefab == null) return;

        // Remove existing pointer
        if (currentWorldPointer != null)
        {
            Destroy(currentWorldPointer);
        }

        currentWorldPointer = Instantiate(worldPointerPrefab, worldPosition, Quaternion.identity);
        originalWorldPointerPos = worldPosition;
        
        // Start bounce animation
        StartCoroutine(BounceWorldPointer());
    }

    private void AddObjectOutline(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && highlightMaterial != null)
        {
            // Store original materials
            Material[] originalMaterials = renderer.materials;
            
            // Add highlight material
            Material[] newMaterials = new Material[originalMaterials.Length + 1];
            for (int i = 0; i < originalMaterials.Length; i++)
            {
                newMaterials[i] = originalMaterials[i];
            }
            newMaterials[originalMaterials.Length] = highlightMaterial;
            
            renderer.materials = newMaterials;
            
            // Store reference for cleanup
            GameObject highlight = new GameObject("Outline_Highlight");
            highlight.transform.SetParent(obj.transform);
            highlight.AddComponent<TutorialHighlightMarker>().originalMaterials = originalMaterials;
            highlight.GetComponent<TutorialHighlightMarker>().targetRenderer = renderer;
            
            activeHighlights.Add(highlight);
        }
    }

    private IEnumerator PulseHighlight(GameObject highlight)
    {
        Image image = highlight.GetComponent<Image>();
        if (image == null) yield break;

        Color originalColor = image.color;
        
        while (highlight != null)
        {
            float pulse = Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount + 1f;
            Color newColor = originalColor;
            newColor.a = originalColor.a * pulse;
            image.color = newColor;
            
            yield return null;
        }
    }

    private IEnumerator AnimateUIArrow(RectTransform arrowRect, Vector3 targetPos)
    {
        Vector3 originalPos = arrowRect.position;
        
        while (currentUIArrow != null)
        {
            float oscillation = Mathf.Sin(Time.unscaledTime * bounceSpeed) * 10f;
            Vector3 direction = (targetPos - originalPos).normalized;
            arrowRect.position = originalPos + direction * oscillation;
            
            yield return null;
        }
    }

    private IEnumerator BounceWorldPointer()
    {
        while (currentWorldPointer != null)
        {
            float bounce = Mathf.Sin(Time.time * bounceSpeed) * bounceAmount;
            Vector3 newPos = originalWorldPointerPos;
            newPos.y += bounce;
            currentWorldPointer.transform.position = newPos;
            
            yield return null;
        }
    }

    public void ClearAllHighlights()
    {
        // Clear UI highlights
        foreach (GameObject highlight in activeHighlights)
        {
            if (highlight != null)
            {
                TutorialHighlightMarker marker = highlight.GetComponent<TutorialHighlightMarker>();
                if (marker != null)
                {
                    // Restore original materials
                    marker.targetRenderer.materials = marker.originalMaterials;
                }
                
                Destroy(highlight);
            }
        }
        activeHighlights.Clear();

        // Clear world pointer
        if (currentWorldPointer != null)
        {
            Destroy(currentWorldPointer);
            currentWorldPointer = null;
        }

        // Clear UI arrow
        if (currentUIArrow != null)
        {
            Destroy(currentUIArrow);
            currentUIArrow = null;
        }
    }

    public void ClearUIHighlights()
    {
        for (int i = activeHighlights.Count - 1; i >= 0; i--)
        {
            GameObject highlight = activeHighlights[i];
            if (highlight != null && highlight.name.Contains("UI_Highlight"))
            {
                Destroy(highlight);
                activeHighlights.RemoveAt(i);
            }
        }

        if (currentUIArrow != null)
        {
            Destroy(currentUIArrow);
            currentUIArrow = null;
        }
    }

    public void ClearWorldHighlights()
    {
        if (currentWorldPointer != null)
        {
            Destroy(currentWorldPointer);
            currentWorldPointer = null;
        }

        for (int i = activeHighlights.Count - 1; i >= 0; i--)
        {
            GameObject highlight = activeHighlights[i];
            if (highlight != null && highlight.name.Contains("Outline_Highlight"))
            {
                TutorialHighlightMarker marker = highlight.GetComponent<TutorialHighlightMarker>();
                if (marker != null)
                {
                    marker.targetRenderer.materials = marker.originalMaterials;
                }
                
                Destroy(highlight);
                activeHighlights.RemoveAt(i);
            }
        }
    }
}

public class TutorialHighlightMarker : MonoBehaviour
{
    public Material[] originalMaterials;
    public Renderer targetRenderer;
}
