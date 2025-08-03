using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.VFX;
using FarmDefender.Core.AI.FlowField;


public class BuildController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridController gridController;
    [SerializeField] private FlowFieldManager flowFieldManager;
    [SerializeField] private OwnershipController ownershipController;
    [SerializeField] private GridMonitor gridMonitor;

    [Header("Build Settings")]
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private GameObject[] buildablePrefabs;

    [Header("Input Settings")]
    [SerializeField] private KeyCode rotateKey = KeyCode.R;
    [SerializeField] private KeyCode nextItemKey = KeyCode.N;
    [SerializeField] private KeyCode previousItemKey = KeyCode.P;
    [SerializeField] private KeyCode removeModifierKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode moveKey = KeyCode.M;

    [Header("UI References")]
    [SerializeField] private RectTransform itemDeleteIcon;
    [SerializeField] public GameObject dustPoof;

    [Header("Delete Mode Settings")]
    [SerializeField] private Vector2 cursorOffset = new Vector2(15f, 15f);

    [Header("Land Ownership")]
    [SerializeField] private bool enableLandBuying = true;

    [Header("Synergy Visualization")]
    [SerializeField] private GameObject rangeIndicatorPrefab;
    [SerializeField] private Material validSynergyMaterial;
    [SerializeField] private Material potentialSynergyMaterial;
    [SerializeField] private Material invalidSynergyMaterial;
    [SerializeField] private float synergyIndicatorHeight = 0.1f;
    [SerializeField] private GameObject synergyLineRendererPrefab;
    [SerializeField] private Canvas worldSpaceCanvas;

    [Header("Performance Settings")]
    [SerializeField] private bool enableSynergyVisuals = true;
    [SerializeField] private int maxSynergyLines = 10;

    private List<GameObject> synergyIndicators = new List<GameObject>();
    private List<LineRenderer> synergyLines = new List<LineRenderer>();
    private List<GameObject> activeSynergyLines = new List<GameObject>();
    private bool isHousePlaced;

    private GameObject currentGhost;
    private GameObject currentBuildTargetPrefab;
    private Structure movingStructure;
    private Vector3 originalPosition;
    private bool personallyHidden = false;
    private Quaternion originalRotation;
    private List<Vector2Int> originalFootprint;
    private bool isBuildModeActive;
    private bool isMoveModeActive;
    private bool isDeleteModeActive;
    private bool isInLandBuyMode;
    private bool isGhostTemporarilyHidden;
    private int currentPrefabIndex;
    private Quaternion currentRotation = Quaternion.identity;
    private ShopPanelUI shopPanelUI;
    private StructureData currentStructureData;

    void Start()
    {
        gridController = gridController ?? FindFirstObjectByType<GridController>();
        if (gridController == null)
        {
            enabled = false;
            return;
        }
        flowFieldManager = flowFieldManager ?? FindFirstObjectByType<FlowFieldManager>();
        ownershipController = ownershipController ?? FindFirstObjectByType<OwnershipController>();
        gridMonitor = gridMonitor ?? FindFirstObjectByType<GridMonitor>();
        shopPanelUI = FindFirstObjectByType<ShopPanelUI>(FindObjectsInactive.Include);
        if (shopPanelUI != null)
        {
            shopPanelUI.OnShopOpened.AddListener(HandleShopOpened);
            shopPanelUI.OnShopClosed.AddListener(HandleShopClosed);
        }
        if (buildablePrefabs.Length > 0) currentBuildTargetPrefab = buildablePrefabs[0];
        if (itemDeleteIcon != null) itemDeleteIcon.GetComponent<Graphic>().raycastTarget = false;
    }

    void OnDestroy()
    {
        if (shopPanelUI != null)
        {
            shopPanelUI.OnShopOpened.RemoveListener(HandleShopOpened);
            shopPanelUI.OnShopClosed.RemoveListener(HandleShopClosed);
        }
    }

    public void HandleShopOpened()
    {
        gridController.ShowGrid();
        EnableBuildMode();
    }

    public void HandleShopClosed()
    {
        DisableBuildMode();
        gridController.HideGrid();
    }

    void Update()
    {
        if (!isBuildModeActive && !isMoveModeActive) return;

        if (enableSynergyVisuals && activeSynergyLines.Count > maxSynergyLines)
        {
            while (activeSynergyLines.Count > maxSynergyLines)
            {
                var lineToRemove = activeSynergyLines[activeSynergyLines.Count - 1];
                activeSynergyLines.RemoveAt(activeSynergyLines.Count - 1);
                if (lineToRemove != null) Destroy(lineToRemove);
            }
        }

        bool deleteKeyPressed = Input.GetKey(removeModifierKey);
        if (deleteKeyPressed != isDeleteModeActive)
        {
            isDeleteModeActive = deleteKeyPressed;
            if (isDeleteModeActive)
            {
                if (currentGhost != null) currentGhost.SetActive(false);
                if (itemDeleteIcon != null) itemDeleteIcon.gameObject.SetActive(true);
            }
            else
            {
                if (currentGhost != null && !isGhostTemporarilyHidden) currentGhost.SetActive(true);
                if (itemDeleteIcon != null) itemDeleteIcon.gameObject.SetActive(false);
            }
        }

        if (isDeleteModeActive && itemDeleteIcon != null && itemDeleteIcon.gameObject.activeSelf)
            UpdateDeleteIconPosition();

        HandleBuildInput();
        if (!isDeleteModeActive && currentGhost != null && !isMoveModeActive) UpdateGhostPosition();
        if (isMoveModeActive && currentGhost != null) UpdateGhostPositionForMove();
    }

    public void EnableBuildMode()
    {
        isBuildModeActive = true;
        isMoveModeActive = false;
        gridController.ShowGrid();
        if (currentBuildTargetPrefab != null && currentGhost == null) CreateGhost(currentBuildTargetPrefab);
        flowFieldManager?.SetBuildModeActive(true);
    }

    public void DisableBuildMode()
    {
        ClearSynergyVisualization();
        isBuildModeActive = false;
        isMoveModeActive = false;
        gridController.HideGrid();
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }
        movingStructure = null;
        flowFieldManager?.SetBuildModeActive(false);
        if (itemDeleteIcon != null) itemDeleteIcon.gameObject.SetActive(false);
    }

    public void ToggleBuildMode()
    {
        if (isBuildModeActive) DisableBuildMode();
        else EnableBuildMode();
    }

    public bool IsBuildModeActive() => isBuildModeActive;

    public void ToggleMoveMode()
    {
        if (isMoveModeActive) CancelMove();
        else
        {
            isMoveModeActive = true;
            isBuildModeActive = false;
            if (currentGhost != null)
            {
                Destroy(currentGhost);
                currentGhost = null;
            }
        }
    }

    public void StartMoveModeForStructure(Structure structure)
    {
        if (structure == null) return;
        isMoveModeActive = true;
        isBuildModeActive = false;
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }
        movingStructure = structure;
        originalPosition = structure.transform.position;
        originalRotation = structure.transform.rotation;
        originalFootprint = GetStructureFootprint(structure.gameObject);
        currentBuildTargetPrefab = structure.structureData?.prefab;
        currentStructureData = structure.structureData;
        currentRotation = originalRotation;

        if (currentBuildTargetPrefab == null)
        {
            CancelMove();
            return;
        }

        CreateGhost(currentBuildTargetPrefab);
        if (structure is BarracksStructure barracks)
        {
            BarracksStructure ghostBarracks = currentGhost.GetComponent<BarracksStructure>() ?? currentGhost.AddComponent<BarracksStructure>();
            var targetField = typeof(BarracksStructure).GetField("targetAnimalType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            targetField?.SetValue(ghostBarracks, barracks.TargetAnimalType);
            ghostBarracks.synergyMinDist = barracks.synergyMinDist;
            ghostBarracks.synergyMaxDist = barracks.synergyMaxDist;
        }
        else if (structure is AnimalStructure animal)
        {
            AnimalStructure ghostAnimal = currentGhost.GetComponent<AnimalStructure>() ?? currentGhost.AddComponent<AnimalStructure>();
            var field = typeof(AnimalStructure).GetField("animalType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(ghostAnimal, animal.GetAnimalType);
        }
        structure.UnregisterFromGrid();
        structure.gameObject.SetActive(false);
        gridController.ShowGrid();
    }

    public void HideGhostTemporarily()
    {
        if (currentGhost != null && currentGhost.activeSelf)
        {
            isGhostTemporarilyHidden = true;
            currentGhost.SetActive(false);
        }
        if (itemDeleteIcon != null && isDeleteModeActive) itemDeleteIcon.gameObject.SetActive(false);
    }

    public void RestoreGhost()
    {
        isGhostTemporarilyHidden = personallyHidden = false;
        if (currentGhost != null && !isDeleteModeActive) currentGhost.SetActive(true);
        if (itemDeleteIcon != null && isDeleteModeActive && !isGhostTemporarilyHidden) itemDeleteIcon.gameObject.SetActive(true);
    }

    void HandleBuildInput()
    {
        if (isGhostTemporarilyHidden) return;
        isInLandBuyMode = enableLandBuying && currentBuildTargetPrefab == null;

        if (Input.GetKeyDown(moveKey) && !isDeleteModeActive)
        {
            ToggleMoveMode();
            return;
        }

        if (isMoveModeActive)
        {
            if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                if (movingStructure == null) SelectStructureToMove();
                else
                {
                    Vector2Int hoveredCell = GetGridCellUnderCursor(true);
                    PlaceMovedStructure(hoveredCell.x, hoveredCell.y);
                }
            }
            else if (Input.GetMouseButtonDown(1)) CancelMove();
            else if (Input.GetKeyDown(rotateKey))
            {
                currentRotation *= Quaternion.Euler(0, 90, 0);
                if (currentGhost != null) currentGhost.transform.rotation = currentRotation;
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(1) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) CancelCurrentBuilding();
            else if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                if (isDeleteModeActive)
                {
                    if (TryRemoveStructureByRaycast()) return;
                    Vector2Int hoveredCell = gridController.GetCurrentHoveredCell();
                    RemoveItem(hoveredCell.x, hoveredCell.y);
                }
                else if (currentBuildTargetPrefab == null || isInLandBuyMode)
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit)) ownershipController?.BuyLandAtPosition(hit.point);
                }
                else if (currentBuildTargetPrefab != null)
                {
                    Vector2Int hoveredCell = GetGridCellUnderCursor(true);
                    PlaceItem(hoveredCell.x, hoveredCell.y);
                }
            }
            else if (Input.GetKeyDown(rotateKey))
            {
                currentRotation *= Quaternion.Euler(0, 90, 0);
                if (currentGhost != null) currentGhost.transform.rotation = currentRotation;
            }
            else if (Input.GetKeyDown(nextItemKey) && buildablePrefabs.Length > 0)
            {
                currentPrefabIndex = (currentPrefabIndex + 1) % buildablePrefabs.Length;
                currentBuildTargetPrefab = buildablePrefabs[currentPrefabIndex];
                CreateGhost(currentBuildTargetPrefab);
            }
            else if (Input.GetKeyDown(previousItemKey) && buildablePrefabs.Length > 0)
            {
                currentPrefabIndex = (currentPrefabIndex - 1 + buildablePrefabs.Length) % buildablePrefabs.Length;
                currentBuildTargetPrefab = buildablePrefabs[currentPrefabIndex];
                CreateGhost(currentBuildTargetPrefab);
            }
        }
    }

    void SelectStructureToMove()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int layerMask = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, layerMask))
        {
            Transform hitTransform = hit.transform;
            while (hitTransform != null)
            {
                if (hitTransform.name.StartsWith("Item_"))
                {
                    Structure structure = hitTransform.GetComponent<Structure>();
                    if (structure != null)
                    {
                        movingStructure = structure;
                        originalPosition = structure.transform.position;
                        originalRotation = structure.transform.rotation;
                        originalFootprint = GetStructureFootprint(structure.gameObject);
                        currentBuildTargetPrefab = structure.structureData?.prefab;
                        currentRotation = originalRotation;
                        CreateGhost(currentBuildTargetPrefab);
                        structure.UnregisterFromGrid();
                        structure.gameObject.SetActive(false);
                        return;
                    }
                }
                hitTransform = hitTransform.parent;
            }
        }
    }

    void PlaceMovedStructure(int x, int y)
    {
        if (!IsValidPlacement(x, y)) return;
        Vector3 cellCenter = gridController.GetCellCenterFromTexture(x, y);
        movingStructure.transform.position = cellCenter;
        movingStructure.transform.rotation = currentRotation;
        movingStructure.gameObject.SetActive(true);
        List<Vector2Int> newFootprint = GetStructureFootprint(movingStructure.gameObject);
        foreach (Vector2Int cell in newFootprint) gridController.SetCellOccupied(cell.x, cell.y, true);
        movingStructure.RegisterWithGrid();
        AudioManager.Instance?.PlayPlaceSound();
        gridController.UpdateGridTexture();
        if (gridMonitor != null && newFootprint.Count > 0) gridMonitor.NotifyMultipleCellsChanged(newFootprint, GridChangeType.Structural);
        movingStructure = null;
        isMoveModeActive = false;
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }
        DisableBuildMode();
    }

    void CancelMove()
    {
        if (movingStructure != null)
        {
            movingStructure.transform.position = originalPosition;
            movingStructure.transform.rotation = originalRotation;
            movingStructure.gameObject.SetActive(true);
            movingStructure.RegisterWithGrid();
            movingStructure = null;
        }
        isMoveModeActive = false;
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }
        gridController.HideGrid();
        DisableBuildMode();
    }

    void UpdateGhostPosition()
    {
        if (currentGhost == null) return;
        if (isInLandBuyMode)
        {
            currentGhost.SetActive(false);
            return;
        }
        else if (!currentGhost.activeSelf && !isGhostTemporarilyHidden && !isDeleteModeActive) currentGhost.SetActive(true);

        Vector2Int hoveredCell = GetGridCellUnderCursor(true);
        if (!gridController.IsValidCell(hoveredCell.x, hoveredCell.y))
        {
            currentGhost.SetActive(false);
            return;
        }
        Vector3 cellCenter = gridController.GetCellCenterFromTexture(hoveredCell.x, hoveredCell.y);
        currentGhost.transform.position = cellCenter;
        bool isValidPlacement = IsValidPlacement(hoveredCell.x, hoveredCell.y);
        foreach (Renderer renderer in currentGhost.GetComponentsInChildren<Renderer>())
            renderer.material.color = isValidPlacement ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
        UpdateSynergyVisualization();
    }

    private void UpdateSynergyVisualization()
    {
        if (currentGhost == null || (!isBuildModeActive && !isMoveModeActive) || isDeleteModeActive || currentStructureData == null) return;
        ClearSynergyVisualization();
        switch (currentStructureData.type)
        {
            case StructureType.Silo: ShowSiloSynergyPreview(); break;
            case StructureType.CropPlot: ShowCropSynergyPreview(); break;
            case StructureType.Animal: ShowAnimalSynergyPreview(); break;
            case StructureType.Barracks: ShowBarracksSynergyPreview(); break;
        }
    }

    private void ShowSiloSynergyPreview()
    {
        CreateRangeIndicator(currentGhost.transform.position, 15f, potentialSynergyMaterial, "Animal Synergy Range");
        CreateRangeIndicator(currentGhost.transform.position, 10f, potentialSynergyMaterial, "Crop Synergy Range");
        foreach (var animal in FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None))
            if ((currentGhost.transform.position - animal.transform.position).sqrMagnitude <= 225f)
                CreateSynergyLine(currentGhost.transform.position, animal.transform.position, Color.green, "Silo-Animal");
        foreach (var crop in FindObjectsByType<CropStructure>(FindObjectsSortMode.None))
            if (crop != null && (currentGhost.transform.position - crop.transform.position).sqrMagnitude <= 100f)
                CreateSynergyLine(currentGhost.transform.position, crop.transform.position, Color.green, "Silo-Crop");
    }

    private void ShowAnimalSynergyPreview()
    {
        foreach (var silo in FindObjectsByType<SiloStructure>(FindObjectsSortMode.None))
            if ((currentGhost.transform.position - silo.transform.position).sqrMagnitude <= 225f)
                CreateSynergyLine(currentGhost.transform.position, silo.transform.position, Color.green, "Silo-Animal");
        AnimalStructure animalStructure = currentGhost.GetComponent<AnimalStructure>();
        if (animalStructure != null)
            foreach (var barrack in FindObjectsByType<BarracksStructure>(FindObjectsSortMode.None))
                if (barrack.TargetAnimalType == animalStructure.GetAnimalType.ToString())
                {
                    float sqrDistance = (currentGhost.transform.position - barrack.transform.position).sqrMagnitude;
                    if (sqrDistance <= barrack.synergyMaxDist * barrack.synergyMaxDist)
                    {
                        float distance = Mathf.Sqrt(sqrDistance);
                        Color lineColor = distance <= barrack.synergyMinDist ? Color.red : Color.green;
                        CreateSynergyLine(currentGhost.transform.position, barrack.transform.position, lineColor, $"Barracks-{animalStructure.GetAnimalType}");
                    }
                }
    }

    private void ShowBarracksSynergyPreview()
    {
        BarracksStructure barracksStructure = currentGhost.GetComponent<BarracksStructure>();
        if (barracksStructure == null) return;
        string targetType = barracksStructure.TargetAnimalType;
        CreateRangeIndicator(currentGhost.transform.position, barracksStructure.synergyMinDist, validSynergyMaterial, "Optimal Synergy Range");
        CreateRangeIndicator(currentGhost.transform.position, barracksStructure.synergyMaxDist, potentialSynergyMaterial, "Maximum Synergy Range");
        foreach (var animal in FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None))
            if (animal.GetAnimalType.ToString() == targetType)
            {
                float sqrDistance = (currentGhost.transform.position - animal.transform.position).sqrMagnitude;
                if (sqrDistance <= barracksStructure.synergyMaxDist * barracksStructure.synergyMaxDist)
                {
                    float distance = Mathf.Sqrt(sqrDistance);
                    Color lineColor = distance <= barracksStructure.synergyMinDist ? Color.red : Color.green;
                    CreateSynergyLine(currentGhost.transform.position, animal.transform.position, lineColor, $"Barracks-{targetType}");
                }
            }
    }

    private void ShowCropSynergyPreview()
    {
        foreach (var silo in FindObjectsByType<SiloStructure>(FindObjectsSortMode.None))
            if (silo != null && (currentGhost.transform.position - silo.transform.position).sqrMagnitude <= 100f)
                CreateSynergyLine(currentGhost.transform.position, silo.transform.position, Color.green, "Silo-Crop");
    }

    private GameObject CreateRangeIndicator(Vector3 position, float radius, Material material, string label = "")
    {
        GameObject indicator = rangeIndicatorPrefab == null ? new GameObject($"SynergyRange_{label}") : Instantiate(rangeIndicatorPrefab, new Vector3(position.x, position.y + synergyIndicatorHeight, position.z), Quaternion.Euler(90, 0, 0));
        indicator.transform.position = new Vector3(position.x, position.y + synergyIndicatorHeight, position.z);
        if (rangeIndicatorPrefab == null)
        {
            MeshFilter meshFilter = indicator.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = indicator.AddComponent<MeshRenderer>();
            Mesh mesh = new Mesh();
            int segments = 64;
            Vector3[] vertices = new Vector3[segments + 1];
            int[] triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;
            for (int i = 0; i < segments; i++)
            {
                float angle = 2 * Mathf.PI * i / segments;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % segments + 1;
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;
            meshRenderer.material = material;
        }
        else
        {
            float diameter = radius * 2;
            indicator.transform.localScale = new Vector3(diameter, diameter, 1);
            Renderer renderer = indicator.GetComponent<Renderer>();
            if (renderer != null) renderer.material = material;
        }
        synergyIndicators.Add(indicator);
        return indicator;
    }

    private LineRenderer CreateSynergyLine(Vector3 start, Vector3 end, Color color, string synergyType = "")
    {
        GameObject lineObj = synergyLineRendererPrefab != null ? Instantiate(synergyLineRendererPrefab) : new GameObject("SynergyLine_" + synergyType);
        if (synergyLineRendererPrefab == null) lineObj.AddComponent<LineRenderer>().material = new Material(Shader.Find("Sprites/Default"));
        start.y += 0.15f;
        end.y += 0.15f;
        LineRenderer line = lineObj.GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = line.endWidth = 0.15f;
        line.startColor = line.endColor = color;
        ShowSynergyText((start + end) / 2f, GetBonusSummary(synergyType, color), color);
        synergyLines.Add(line);
        activeSynergyLines.Add(lineObj);
        return line;
    }

    private void ShowSynergyText(Vector3 position, string text, Color color)
    {
        if (string.IsNullOrEmpty(text)) return;
        GameObject textObj = new GameObject("SynergyText");
        textObj.transform.position = new Vector3(position.x, position.y + 1f, position.z);
        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.color = color;
        textMesh.fontSize = 32;
        textMesh.characterSize = 0.1f;
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;
        MeshRenderer renderer = textObj.GetComponent<MeshRenderer>();
        renderer.material.shader = Shader.Find("GUI/Text Shader");
        textObj.AddComponent<SimpleBillboard>();
        synergyIndicators.Add(textObj);
    }

    private string GetBonusSummary(string synergyType, Color color)
    {
        if (color == Color.green)
        {
            if (synergyType.Contains("Silo-Animal")) return "+20% Food Efficiency";
            if (synergyType.Contains("Silo-Crop")) return "+50% Yield";
            if (synergyType.Contains("Barracks")) return "20% Discount on Recruitment";
        }
        else if (color == Color.red) return "No Discount on Recruitment";
        return "";
    }

    private void ClearSynergyVisualization()
    {
        foreach (var indicator in synergyIndicators) if (indicator != null) Destroy(indicator);
        synergyIndicators.Clear();
        foreach (var line in synergyLines) if (line != null && line.gameObject != null) Destroy(line.gameObject);
        synergyLines.Clear();
        activeSynergyLines.Clear();
    }

    void UpdateGhostPositionForMove()
    {
        if (currentGhost == null || movingStructure == null) return;
        Vector2Int hoveredCell = GetGridCellUnderCursor(true);
        if (!gridController.IsValidCell(hoveredCell.x, hoveredCell.y))
        {
            currentGhost.SetActive(false);
            return;
        }
        Vector3 cellCenter = gridController.GetCellCenterFromTexture(hoveredCell.x, hoveredCell.y);
        currentGhost.transform.position = cellCenter;
        currentGhost.SetActive(true);
        bool isValidPlacement = IsValidPlacement(hoveredCell.x, hoveredCell.y);
        foreach (Renderer renderer in currentGhost.GetComponentsInChildren<Renderer>())
            renderer.material.color = isValidPlacement ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
        UpdateSynergyVisualization();
    }

    void CreateGhost(GameObject prefab)
    {
        if (currentGhost != null) Destroy(currentGhost);
        if (prefab == null) return;
        currentGhost = Instantiate(prefab);
        currentGhost.name = "BuildGhost";
        ApplyGhostMaterial(currentGhost);
        currentGhost.transform.rotation = currentRotation;
    }

    void ApplyGhostMaterial(GameObject obj)
    {
        if (ghostMaterial == null)
        {
            ghostMaterial = new Material(Shader.Find("Standard"))
            {
                color = new Color(0, 1, 0, 0.5f)
            };
            ghostMaterial.SetFloat("_Mode", 3);
            ghostMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            ghostMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            ghostMaterial.SetInt("_ZWrite", 0);
            ghostMaterial.DisableKeyword("_ALPHATEST_ON");
            ghostMaterial.EnableKeyword("_ALPHABLEND_ON");
            ghostMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            ghostMaterial.renderQueue = 3000;
        }
        foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>())
            renderer.material = new Material(ghostMaterial);
    }

    public void SetBuildTarget(StructureData data)
    {
        if (data == null || data.prefab == null || (MoneyManager.Instance != null && !MoneyManager.Instance.CanAfford(data.cost))) return;
        currentBuildTargetPrefab = data.prefab;
        currentStructureData = data;
        EnableBuildMode();
        CreateGhost(currentBuildTargetPrefab);
    }

    public void SetBuildTarget(GameObject prefab)
    {
        if (prefab == null) return;
        currentBuildTargetPrefab = prefab;
        EnableBuildMode();
        CreateGhost(currentBuildTargetPrefab);
    }

    private List<Vector2Int> GetStructureFootprint(GameObject obj)
    {
        List<Vector2Int> occupiedCells = new List<Vector2Int>();
        Renderer renderer = obj.GetComponentInChildren<Renderer>();
        if (renderer == null) return occupiedCells;
        Bounds bounds = renderer.bounds;
        bounds.Expand(-0.1f);
        Vector2Int bottomLeft = gridController.WorldToGridCoords(bounds.min);
        Vector2Int topRight = gridController.WorldToGridCoords(bounds.max);
        for (int x = bottomLeft.x; x <= topRight.x; x++)
            for (int y = bottomLeft.y; y <= topRight.y; y++)
                if (gridController.IsValidCell(x, y))
                {
                    Vector3 cellCenter = gridController.GetCellCenterFromTexture(x, y);
                    if (bounds.Contains(new Vector3(cellCenter.x, bounds.center.y, cellCenter.z)))
                        occupiedCells.Add(new Vector2Int(x, y));
                }
        return occupiedCells;
    }

    bool IsValidPlacement(int x, int y)
    {
        if (!gridController.IsValidCell(x, y) || currentBuildTargetPrefab == null) return false;
        bool shopOpen = (shopPanelUI != null && shopPanelUI.gameObject.activeSelf && !isMoveModeActive);
        if (!shopOpen && !isMoveModeActive) return false;
        GameObject tempObj = Instantiate(currentBuildTargetPrefab, gridController.GetCellCenterFromTexture(x, y), currentRotation);
        List<Vector2Int> footprint, newFootprint = GetStructureFootprint(tempObj);
        Destroy(tempObj);
        foreach (Vector2Int cell in newFootprint)
        {
            if (!gridController.IsValidCell(cell.x, cell.y)) return false;
            GridCell gridCell = gridController.GetCell(cell.x, cell.y);
            if (gridCell == null || !gridCell.flags.isOwned || gridCell.flags.isObstacle) return false;
            if (gridCell.flags.isOccupied && isMoveModeActive && originalFootprint != null && !originalFootprint.Contains(cell)) return false;
        }
        return true;
    }

    void PlaceItem(int x, int y)
    {
        if (!IsValidPlacement(x, y) || (currentStructureData != null && MoneyManager.Instance != null && !MoneyManager.Instance.SpendMoney(currentStructureData.cost))) return;
        Vector3 cellCenter = gridController.GetCellCenterFromTexture(x, y);
        GameObject placedItem = Instantiate(currentBuildTargetPrefab, cellCenter, currentRotation);
        placedItem.name = $"Item_{x}_{y}";
        Structure structure = placedItem.GetComponent<Structure>();
        if (dustPoof != null)
        {
            Vector3 effectPosition = placedItem.transform.position + (structure != null && structure.GetStructureName() == "Cow Barn" ? new Vector3(0, 4f, 0) : new Vector3(0, 1f, 0));
            float totalMult = structure != null && structure.GetStructureName() == "Cow Barn" ? 1.5f : 1f;
            GameObject effect = Instantiate(dustPoof, effectPosition, Quaternion.identity);
            VisualEffect ps = effect.GetComponent<VisualEffect>();
            if (ps != null)
            {
                ps.SetVector3("posMult", new Vector3(totalMult, totalMult, totalMult));
                ps.SetFloat("totalMult", totalMult);
                ps.Play();
            }
            Destroy(effect, 3f);
        }
        if (structure != null)
        {
            structure.SetAllowSelectionAndUI(false);
            StartCoroutine(EnableSelectionAfterRelease(structure));
            HandleTutorialTriggers(structure);
        }
        List<Vector2Int> footprint = GetStructureFootprint(placedItem);
        foreach (Vector2Int cell in footprint) gridController.SetCellOccupied(cell.x, cell.y, true);
        AudioManager.Instance?.PlayPlaceSound();
        gridController.UpdateGridTexture();
        if (gridMonitor != null && footprint.Count > 0) gridMonitor.NotifyMultipleCellsChanged(footprint, GridChangeType.Structural);
    }

    private void HandleTutorialTriggers(Structure structure)
    {
        if (TutorialManager.Instance == null) return;
        string name = structure.GetStructureName().ToLower();
        if (structure is BarracksStructure barracks)
        {
            string targetAnimalType = barracks.TargetAnimalType.ToLower();
            TutorialTrigger barracksType = targetAnimalType switch
            {
                "chicken" => TutorialTrigger.BuiltChickenBarracks,
                "cow" => TutorialTrigger.BuiltCowBarracks,
                "sheep" => TutorialTrigger.BuiltSheepBarracks,
                "goat" => TutorialTrigger.BuiltGoatBarracks,
                "pig" => TutorialTrigger.BuiltPigBarracks,
                _ => TutorialTrigger.None
            };
            if (barracksType != TutorialTrigger.None) TutorialManager.Instance.Trigger(barracksType);
            return;
        }
        if (structure is AnimalStructure animalStructure)
        {
            string animalType = animalStructure.GetAnimalType.ToString().ToLower();
            TutorialTrigger animalTrigger = animalType switch
            {
                "chicken" => TutorialTrigger.BuiltChickenCoop,
                "cow" => TutorialTrigger.BuiltCowPen,
                "sheep" => TutorialTrigger.BuiltSheepPen,
                "goat" => TutorialTrigger.BuiltGoatPen,
                "pig" => TutorialTrigger.BuiltPigPen,
                _ => TutorialTrigger.None
            };
            if (animalTrigger != TutorialTrigger.None) TutorialManager.Instance.Trigger(animalTrigger);
            return;
        }
        TutorialTrigger trigger = name switch
        {
            var n when n.Contains("silo") || n.Contains("storage") => TutorialTrigger.BuiltSilo,
            var n when n.Contains("farm house") || n.Contains("farmhouse") => TutorialTrigger.BuiltFarmHouse,
            var n when n.Contains("crop") || n.Contains("plot") => TutorialTrigger.BuiltCropPlot,
            _ => TutorialTrigger.None
        };
        if (name.Contains("farm house") || name.Contains("farmhouse")) isHousePlaced = true;
        if (trigger != TutorialTrigger.None) TutorialManager.Instance.Trigger(trigger);
    }

    private IEnumerator EnableSelectionAfterRelease(Structure structure)
    {
        while (Input.GetMouseButton(0)) yield return null;
        if (structure != null) structure.SetAllowSelectionAndUI(true);
    }

    void RemoveItem(int x, int y)
    {
        if (!gridController.IsValidCell(x, y)) return;
        GridCell cell = gridController.GetCell(x, y);
        if (cell == null || !cell.flags.isOccupied) return;
        string itemName = $"Item_{x}_{y}";
        GameObject placedItem = GameObject.Find(itemName);
        if (placedItem != null)
        {
            Structure structure = placedItem.GetComponent<Structure>();
            if (structure is SiloStructure silo) InventoryManager.Instance.UnregisterSilo(silo);
            if (structure != null && structure.GetStructureName().ToLower().Contains("farm house")) isHousePlaced = false;
            List<Vector2Int> footprint = GetStructureFootprint(placedItem);
            Destroy(placedItem);
            AudioManager.Instance?.PlayRemoveSound();
            foreach (Vector2Int pos in footprint) gridController.SetCellOccupied(pos.x, pos.y, false);
            gridController.UpdateGridTexture();
            if (gridMonitor != null && footprint.Count > 0) gridMonitor.NotifyMultipleCellsChanged(footprint, GridChangeType.Structural);
        }
    }

    private bool TryRemoveStructureByRaycast()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int layerMask = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, layerMask) && hit.transform.name != "BuildGhost")
        {
            Transform hitTransform = hit.transform;
            while (hitTransform != null)
            {
                if (hitTransform.name.StartsWith("Item_"))
                {
                    GameObject placedItem = hitTransform.gameObject;
                    List<Vector2Int> footprint = GetStructureFootprint(placedItem);
                    if (footprint.Count == 0) footprint = GetExtendedStructureFootprint(placedItem);
                    string[] parts = placedItem.name.Split('_');
                    if (parts.Length >= 3 && int.TryParse(parts[1], out int gridX) && int.TryParse(parts[2], out int gridY))
                    {
                        Structure structure = placedItem.GetComponent<Structure>();
                        if (structure is SiloStructure silo) InventoryManager.Instance.UnregisterSilo(silo);
                        if (structure != null && structure.GetStructureName().ToLower().Contains("farm house")) isHousePlaced = false;
                        foreach (Vector2Int pos in footprint)
                            if (gridController.IsValidCell(pos.x, pos.y)) gridController.SetCellOccupied(pos.x, pos.y, false);
                        Destroy(placedItem);
                        AudioManager.Instance?.PlayRemoveSound();
                        gridController.UpdateGridTexture();
                        if (gridMonitor != null && footprint.Count > 0) gridMonitor.NotifyMultipleCellsChanged(footprint, GridChangeType.Structural);
                        return true;
                    }
                }
                hitTransform = hitTransform.parent;
            }
        }
        return false;
    }

    private List<Vector2Int> GetExtendedStructureFootprint(GameObject obj)
    {
        List<Vector2Int> occupiedCells = new List<Vector2Int>();
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return occupiedCells;
        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) combinedBounds.Encapsulate(renderers[i].bounds);
        combinedBounds.Expand(0.1f);
        Vector2Int bottomLeft = gridController.WorldToGridCoords(combinedBounds.min);
        Vector2Int topRight = gridController.WorldToGridCoords(combinedBounds.max);
        for (int x = bottomLeft.x - 1; x <= topRight.x + 1; x++)
            for (int y = bottomLeft.y - 1; y <= topRight.y + 1; y++)
                if (gridController.IsValidCell(x, y))
                {
                    GridCell cell = gridController.GetCell(x, y);
                    if (cell != null && cell.flags.isOccupied) occupiedCells.Add(new Vector2Int(x, y));
                }
        return occupiedCells;
    }

    private void UpdateDeleteIconPosition()
    {
        Vector2 mousePosition = Input.mousePosition;
        itemDeleteIcon.position = new Vector2(mousePosition.x + cursorOffset.x, mousePosition.y + cursorOffset.y);
        foreach (Graphic graphic in itemDeleteIcon.GetComponentsInChildren<Graphic>()) 
            graphic.raycastTarget = false;
    }

    public void CancelCurrentBuilding()
    {
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }
        currentBuildTargetPrefab = null;
    }

    private Vector2Int GetGridCellUnderCursor(bool ignoreStructures = false)
    {
        Vector2Int hoveredCell = gridController.GetCurrentHoveredCell();
        if (ignoreStructures && !isDeleteModeActive)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float gridHeight = 0f;
            if (gridController.TextureHeight > 0)
                for (int x = 0; x < gridController.TextureWidth; x++)
                    for (int y = 0; y < gridController.TextureHeight; y++)
                    {
                        GridCell cell = gridController.GetCell(x, y);
                        if (cell != null)
                        {
                            gridHeight = cell.worldPosition.y;
                            break;
                        }
                    }
            Plane gridPlane = new Plane(Vector3.up, new Vector3(0, gridHeight, 0));
            if (gridPlane.Raycast(ray, out float distance)) hoveredCell = gridController.WorldToGridCoords(ray.GetPoint(distance));
        }
        return hoveredCell;
    }

    public void SetRemovalModifierKey(KeyCode newKey) => removeModifierKey = newKey;
    public KeyCode GetRemovalModifierKey() => removeModifierKey;
    public void HideDeleteIcon() => itemDeleteIcon.gameObject.SetActive(false);
    public Vector2 DeleteIconOffset { get => cursorOffset; set => cursorOffset = value; }
    public bool IsHousePlaced() => isHousePlaced;
}