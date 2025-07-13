using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class StructureUIManager : MonoBehaviour
{
    public static StructureUIManager Instance { get; private set; }

    [SerializeField] private Transform uiParent;
    [SerializeField] private GameObject defaultStructureUI;
    [SerializeField] private float uiOffset = 0.5f;
    [SerializeField] private Vector2 screenOffset = new Vector2(0, 20f);

    [Header("UI SFX")]
    [SerializeField] public AudioSource closeSound;
    [SerializeField] public AudioSource openSound;

    private Structure currentSelectedStructure;
    private GameObject activeUI;
    private RectTransform activeUIRect;
    private bool isHidingUI = false;

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
        // Only update if we have an active UI to manage
        if (activeUI == null || isHidingUI) return;

        // Check for destroyed structure less frequently for performance
        if (currentSelectedStructure == null || 
            (Time.frameCount % 10 == 0 && currentSelectedStructure.GetCurrentHealth() <= 0))
        {
            HideStructureUI();
            return;
        }

        if (currentSelectedStructure == null || activeUIRect == null)
            return;

        // Update UI position following the structure
        Vector3 screenPos = GetScreenPositionAboveStructure(currentSelectedStructure);
        screenPos += new Vector3(screenOffset.x, screenOffset.y, 0);
        activeUIRect.position = screenPos;

        // Only check visibility every few frames for performance
        if (Time.frameCount % 5 == 0)
        {
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

        HideStructureUI(); // Ensure previous UI is closed

        currentSelectedStructure = structure;
        currentSelectedStructure.OnStructureDestroyed += OnSelectedStructureDestroyed;

        GameObject prefab = structure.structureData?.uiPrefab ?? defaultStructureUI;
        if (prefab == null)
        {
            Debug.LogWarning("No UI prefab assigned, using default.");
            prefab = defaultStructureUI;
            if (prefab == null)
            {
                Debug.LogWarning("No default structure UI prefab assigned!");
                return;
            }
        }

        activeUI = Instantiate(prefab, uiParent);
        activeUIRect = activeUI.GetComponent<RectTransform>();
        if (activeUIRect != null)
        {
            activeUIRect.anchorMin = new Vector2(0.5f, 0.5f);
            activeUIRect.anchorMax = new Vector2(0.5f, 0.5f);
            activeUIRect.pivot = new Vector2(0.5f, 0.5f);
            activeUIRect.localScale = Vector3.one;

            Vector3 screenPos = GetScreenPositionAboveStructure(structure);
            screenPos += new Vector3(screenOffset.x, screenOffset.y, 0);
            activeUIRect.position = screenPos;
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

        playOpenSFX();
    }

    public void HideStructureUI()
    {
        isHidingUI = true;

        if (activeUI != null)
        {
            playClosingSFX();
            Destroy(activeUI);
            activeUI = null;
            activeUIRect = null;
        }

        if (currentSelectedStructure != null)
        {
            currentSelectedStructure.Deselect(); // Explicitly deselect
            currentSelectedStructure.OnStructureDestroyed -= OnSelectedStructureDestroyed;
            currentSelectedStructure = null;
        }

        StartCoroutine(ResetHidingFlag());
    }

    private IEnumerator ResetHidingFlag()
    {
        yield return new WaitForSeconds(0.1f);
        isHidingUI = false;
    }

    private void OnSelectedStructureDestroyed(Structure destroyedStructure)
    {
        if (destroyedStructure == currentSelectedStructure)
        {
            Debug.Log($"Selected structure {destroyedStructure.GetStructureName()} was destroyed - hiding UI");
            if (activeUI != null)
            {
                activeUI.SetActive(false);
            }
            HideStructureUI();
        }
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

        screenPos.x = Mathf.Clamp(screenPos.x, 0, Screen.width);
        screenPos.y = Mathf.Clamp(screenPos.y, 0, Screen.height);

        return screenPos;
    }

    public void playClosingSFX()
    {
        if (closeSound != null)
        {
            closeSound.Play();
        }
    }

    public void playOpenSFX()
    {
        if (openSound != null)
        {
            openSound.Play();
        }
    }

    private void OnDestroy()
    {
        if (currentSelectedStructure != null)
        {
            currentSelectedStructure.OnStructureDestroyed -= OnSelectedStructureDestroyed;
            currentSelectedStructure.Deselect();
            currentSelectedStructure = null;
        }
    }
}