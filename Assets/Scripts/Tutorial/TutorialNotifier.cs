using UnityEngine;

/// <summary>
/// Helper class to notify the tutorial system of game events
/// Add this to structure prefabs or use from placement systems
/// </summary>
public class TutorialNotifier : MonoBehaviour
{
    [Header("Notification Settings")]
    [SerializeField] private bool notifyOnStart = true;
    [SerializeField] private bool notifyOnEnable = false;
    
    private void Start()
    {
        if (notifyOnStart)
        {
            NotifyTutorial();
        }
    }
    
    private void OnEnable()
    {
        if (notifyOnEnable)
        {
            NotifyTutorial();
        }
    }
    
    /// <summary>
    /// Manually notify the tutorial system about this structure
    /// </summary>
    public void NotifyTutorial()
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnStructurePlaced(gameObject);
            TutorialManager.Instance.CheckTutorialConditions();
        }
    }
    
    /// <summary>
    /// Call this when the structure is fully placed and functional
    /// </summary>
    public void OnStructureCompleted()
    {
        NotifyTutorial();
    }
}
