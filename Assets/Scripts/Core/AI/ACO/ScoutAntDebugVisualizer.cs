using UnityEngine;

public class ScoutAntDebugVisualizer : MonoBehaviour
{
    public ScoutAntManager scoutAntManager;
    public GridDataGenerator gridDataGenerator;
    public Color color = Color.yellow;

    void OnDrawGizmos()
    {
        if (scoutAntManager == null || gridDataGenerator == null) return;
        foreach (var ant in scoutAntManager.GetActiveAnts())
        {
            Vector3 pos = gridDataGenerator.GetCell(ant.currentCell.x, ant.currentCell.y).worldPosition;
            Gizmos.color = color;
            Gizmos.DrawSphere(pos + Vector3.up * 0.2f, 0.2f);
        }
    }
}