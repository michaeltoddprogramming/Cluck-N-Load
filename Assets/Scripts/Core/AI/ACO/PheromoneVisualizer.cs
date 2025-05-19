using UnityEngine;

public class PheromoneVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    public PheromoneManager pheromoneManager;
    public GridDataGenerator gridDataGenerator;
    public int pheromoneType = 0; // 0 = regular, 1 = fast, etc.
    public float maxPheromone = 5f;
    public Color color = Color.magenta;
    public bool show = true;

    [Header("Distribution Controls")]
    [Range(1, 15)] 
    public int testDistributionRange = 3;
    public bool applyDistribution = false;

    private void OnDrawGizmos()
    {
        if (!show || pheromoneManager == null || gridDataGenerator == null || !gridDataGenerator.IsInitialized)
            return;

        int width = gridDataGenerator.GetGridWidth();
        int height = gridDataGenerator.GetGridHeight();

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            float value = pheromoneManager.GetPheromone(new Vector2Int(x, y), pheromoneType);
            if (value > 0.01f)
            {
                float alpha = Mathf.Clamp01(value / maxPheromone);
                Gizmos.color = new Color(color.r, color.g, color.b, alpha * 0.7f);
                Vector3 pos = gridDataGenerator.GetCell(x, y).worldPosition;
                Gizmos.DrawCube(pos + Vector3.up * 0.1f, Vector3.one * 0.7f);
            }
        }
    }

    private void Update()
    {
        // Check for distribution test button
        if (applyDistribution)
        {
            applyDistribution = false;  // Reset flag
            if (pheromoneManager != null)
            {
                pheromoneManager.distributionRange = testDistributionRange;
                pheromoneManager.ApplyEvenDistribution();
                Debug.Log($"Applied even distribution with range {testDistributionRange}");
            }
        }
    }
}