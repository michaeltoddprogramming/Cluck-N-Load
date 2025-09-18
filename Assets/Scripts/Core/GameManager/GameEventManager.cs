using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Central event manager for game-wide events to improve performance
/// and reduce coupling between systems
/// </summary>
public class GameEventManager : MonoBehaviour
{
    public static GameEventManager Instance { get; private set; }

    [Header("Structure Events")]
    public UnityEvent<Structure> OnStructurePlaced;
    public UnityEvent<Structure> OnStructureDestroyed;
    public UnityEvent<Structure> OnStructureSelected;
    public UnityEvent<Structure> OnStructureDeselected;

    [Header("Game State Events")]
    public UnityEvent OnDayStarted;
    public UnityEvent OnNightStarted;
    public UnityEvent OnGamePaused;
    public UnityEvent OnGameResumed;

    [Header("UI Events")]
    public UnityEvent OnShopOpened;
    public UnityEvent OnShopClosed;
    public UnityEvent OnInventoryChanged;

    [Header("Tutorial Events")]
    public UnityEvent<string> OnFeatureUnlocked;

    [Header("Combat Events")]
    // public UnityEvent<Wolf> OnWolfSpawned;
    // public UnityEvent<Wolf> OnWolfDestroyed;
    public UnityEvent OnDefenseSuccessful;

    public event System.Action<int> OnSeasonChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region Event Trigger Methods

    public void TriggerStructurePlaced(Structure structure)
    {
        OnStructurePlaced?.Invoke(structure);
    }

    public void TriggerStructureDestroyed(Structure structure)
    {
        OnStructureDestroyed?.Invoke(structure);
    }

    public void TriggerStructureSelected(Structure structure)
    {
        OnStructureSelected?.Invoke(structure);
    }

    public void TriggerStructureDeselected(Structure structure)
    {
        OnStructureDeselected?.Invoke(structure);
    }

    public void TriggerDayStarted()
    {
        OnDayStarted?.Invoke();
    }

    public void TriggerNightStarted()
    {
        OnNightStarted?.Invoke();
    }

    public void TriggerGamePaused()
    {
        OnGamePaused?.Invoke();
    }

    public void TriggerGameResumed()
    {
        OnGameResumed?.Invoke();
    }

    public void TriggerShopOpened()
    {
        OnShopOpened?.Invoke();
    }

    public void TriggerShopClosed()
    {
        OnShopClosed?.Invoke();
    }

    public void TriggerInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }

    public void TriggerFeatureUnlocked(string featureName)
    {
        OnFeatureUnlocked?.Invoke(featureName);
    }

    // public void TriggerWolfSpawned(Wolf wolf)
    // {
    //     OnWolfSpawned?.Invoke(wolf);
    // }

    // public void TriggerWolfDestroyed(Wolf wolf)
    // {
    //     OnWolfDestroyed?.Invoke(wolf);
    // }

    public void TriggerDefenseSuccessful()
    {
        OnDefenseSuccessful?.Invoke();
    }

    public void TriggerSeasonChanged(int season)
    {
        OnSeasonChanged?.Invoke(season);
    }

    #endregion

    #region Subscription Helpers

    public void SubscribeToStructureEvents(
        System.Action<Structure> onPlaced = null,
        System.Action<Structure> onDestroyed = null,
        System.Action<Structure> onSelected = null,
        System.Action<Structure> onDeselected = null)
    {
        if (onPlaced != null) OnStructurePlaced.AddListener(onPlaced.Invoke);
        if (onDestroyed != null) OnStructureDestroyed.AddListener(onDestroyed.Invoke);
        if (onSelected != null) OnStructureSelected.AddListener(onSelected.Invoke);
        if (onDeselected != null) OnStructureDeselected.AddListener(onDeselected.Invoke);
    }

    public void UnsubscribeFromStructureEvents(
        System.Action<Structure> onPlaced = null,
        System.Action<Structure> onDestroyed = null,
        System.Action<Structure> onSelected = null,
        System.Action<Structure> onDeselected = null)
    {
        if (onPlaced != null) OnStructurePlaced.RemoveListener(onPlaced.Invoke);
        if (onDestroyed != null) OnStructureDestroyed.RemoveListener(onDestroyed.Invoke);
        if (onSelected != null) OnStructureSelected.RemoveListener(onSelected.Invoke);
        if (onDeselected != null) OnStructureDeselected.RemoveListener(onDeselected.Invoke);
    }

    #endregion
}
