using UnityEngine;

public class SynergyLineHover : MonoBehaviour 
{
    public BuildController buildController;
    private LineRenderer lineRenderer;

    void Start() 
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null) {
            Debug.LogError($"{name} missing LineRenderer component!");
            return;
        }

        // Configure the box collider to match the line
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider == null) return;
        
        // Calculate line parameters
        Vector3 point1 = lineRenderer.GetPosition(0);
        Vector3 point2 = lineRenderer.GetPosition(1);
        Vector3 direction = point2 - point1;
        float length = direction.magnitude;
        
        // Update collider shape and position
        transform.position = (point1 + point2) / 2;
        transform.forward = direction.normalized;
        collider.center = Vector3.zero;
        collider.size = new Vector3(0.3f, 0.3f, length);
    }
}