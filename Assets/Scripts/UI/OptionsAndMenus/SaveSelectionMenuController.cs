using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SaveSelectionMenuController : MonoBehaviour
{
    public GameObject slotTemplate;
    public Transform slotsContainer; 

    public int SelectedSlot { get; private set; } = -1;

    private void OnEnable()
    {
        PopulateSlots();
    }

    public void PopulateSlots()
    {
        foreach (Transform child in slotsContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < 4; i++)
        {
            var slot = Instantiate(slotTemplate, slotsContainer);
            slot.SetActive(true);

            var timeText = slot.transform.Find("Time")?.GetComponent<TMP_Text>();
            var actionButton = slot.transform.Find("ActionButton")?.GetComponent<Button>();
            var actionButtonText = slot.transform.Find("ActionButton/ActionButtonText")?.GetComponent<TMP_Text>();
            var deleteButton = slot.transform.Find("DeleteButton")?.GetComponent<Button>();

            bool exists = GameSaveHelper.HasSave(i);
            GameSaveData saveData = exists ? GameSaveHelper.LoadFromSlot(i) : null;
            if (exists && saveData != null)
            {
                if (timeText != null)
                    timeText.text = $"Save Slot {i + 1}: Day {saveData.day}, Money {saveData.money}";
                if (actionButtonText != null)
                    actionButtonText.text = "Resume";
            }
            else
            {
                if (timeText != null)
                    timeText.text = $"Empty Slot {i + 1}";
                if (actionButtonText != null)
                    actionButtonText.text = "Begin";
            }

            int slotIndex = i;
            if (actionButton != null)
            {
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(() => LoadSlot(slotIndex));
            }
            if (deleteButton != null)
            {
                deleteButton.gameObject.SetActive(exists);
                deleteButton.onClick.RemoveAllListeners();
                deleteButton.onClick.AddListener(() => DeleteSlot(slotIndex));
            }
        }
    }

    public void LoadSlot(int slot)
    {
        SelectedSlot = slot;
        PlayerPrefs.SetInt("SelectedSaveSlot", slot);

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadSceneWithLoading("MainScene");
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
    }

    public void DeleteSlot(int slot)
    {
        GameSaveHelper.DeleteSlot(slot);
        PopulateSlots();
        Debug.Log($"Deleted save slot {slot}");
    }
}