using UnityEngine;
using TMPro;

public class SaveSelectionMenuController : MonoBehaviour
{
    public GameObject slotTemplate; // Assign in Inspector
    public Transform slotsContainer; // Assign in Inspector

    private void OnEnable()
    {
        PopulateSlots();
    }

    public void PopulateSlots()
    {
        if (slotTemplate == null || slotsContainer == null)
        {
            Debug.LogError("slotTemplate or slotsContainer not assigned!");
            return;
        }

        // Remove old slots
        foreach (Transform child in slotsContainer)
            Destroy(child.gameObject);

        // Dummy slots
        for (int i = 0; i < 4; i++)
        {
            var slot = Instantiate(slotTemplate, slotsContainer);
            slot.SetActive(true); // Ensure slot itself is enabled
            foreach (Transform child in slot.transform)
                child.gameObject.SetActive(true); // Enable all children

            var timeText = slot.transform.Find("Time")?.GetComponent<TMP_Text>();
            if (timeText != null)
                timeText.text = $"Save Slot {i + 1}: Day {i * 5 + 1}";
        }
    }
}