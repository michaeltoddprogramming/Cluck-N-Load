using UnityEngine;

namespace FarmDefender.Core.AI.FlowField
{
    public class FlowFieldTargetManager : MonoBehaviour
    {
        [Header("Target Sources")]
        [SerializeField] private Transform targetTransform;
        [SerializeField] private Vector3 targetWorldPoint; // Specific point to target
        [SerializeField] private Vector2Int manualTargetCoord;
        [SerializeField] private bool useManualTarget = false;
        [SerializeField] private bool useWorldPoint = false;

        [Header("Dependencies")]
        [SerializeField] private GridController gridController;

        private Vector2Int currentTargetCoord;
        private bool targetChanged = false;

        private void Awake()
        {
            if (gridController == null)
                gridController = FindFirstObjectByType<GridController>();

            if (gridController == null)
                Debug.LogError("GridController not found. FlowFieldTargetManager cannot function properly.");
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                targetChanged = true;
        }

        public void SetManualTarget(Vector2Int newTarget)
        {
            if (IsValidTarget(newTarget))
            {
                manualTargetCoord = newTarget;
                targetChanged = true;
                useManualTarget = true;
                useWorldPoint = false;
            }
        }

        public void SetManualTarget(int x, int y)
        {
            SetManualTarget(new Vector2Int(x, y));
        }

        public void SetTargetTransform(Transform newTarget)
        {
            targetTransform = newTarget;
            useManualTarget = false;
            useWorldPoint = false;
            targetChanged = true;
        }

        public void SetTargetTransformWithPoint(Transform newTarget, Vector3 worldPoint)
        {
            targetTransform = newTarget;
            targetWorldPoint = worldPoint;
            useManualTarget = false;
            useWorldPoint = true;
            targetChanged = true;
        }

        public void ToggleManualTarget(bool useManual)
        {
            useManualTarget = useManual;
            useWorldPoint = false;
            targetChanged = true;
        }

        public Vector2Int GetTargetCoordinates()
        {
            if (useManualTarget)
                return manualTargetCoord;

            if (useWorldPoint)
                return gridController.WorldToGridCoords(targetWorldPoint);

            if (targetTransform != null)
                return gridController.WorldToGridCoords(targetTransform.position);

            return Vector2Int.zero;
        }

        public bool HasTargetChanged()
        {
            if (targetChanged)
            {
                targetChanged = false;
                return true;
            }

            Vector2Int newPos = GetTargetCoordinates();
            if (newPos != currentTargetCoord)
            {
                currentTargetCoord = newPos;
                return true;
            }

            return false;
        }

        public bool IsValidTarget(Vector2Int coord)
        {
            return gridController != null && gridController.IsValidCell(coord.x, coord.y);
        }

        public Vector2Int GetCurrentTargetCoord()
        {
            return currentTargetCoord;
        }
    }
}