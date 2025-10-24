using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using TMPro;

public static class NotificationPrefabInspector
{
    [MenuItem("Tools/Debug Notification Prefab")] 
    public static void DebugNotificationPrefabs()
    {
        var mgr = Object.FindObjectOfType<NotificationManager>();
        if (mgr == null)
        {
            Debug.LogWarning("NotificationPrefabInspector: No NotificationManager found in the open scenes.");
            return;
        }

        Debug.Log($"NotificationPrefabInspector: notificationPrefab = {mgr.notificationPrefab}, blockingNotificationPrefab = {mgr.blockingNotificationPrefab}");

        InspectPrefabInstance(mgr.notificationPrefab, "notificationPrefab");
        InspectPrefabInstance(mgr.blockingNotificationPrefab, "blockingNotificationPrefab");
    }

    private static void InspectPrefabInstance(GameObject prefab, string label)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"NotificationPrefabInspector: {label} is null.");
            return;
        }

        Debug.Log($"--- Inspecting {label} ({prefab.name}) ---");
        // Walk hierarchy
        var transforms = prefab.GetComponentsInChildren<Transform>(true);
        foreach (var t in transforms)
        {
            // Build full path
            string path = GetTransformPath(t, prefab.transform);
            string comps = "";
            var tmps = t.GetComponents<TextMeshProUGUI>();
            if (tmps != null && tmps.Length > 0)
            {
                for (int i = 0; i < tmps.Length; i++)
                {
                    comps += $"TMP('{tmps[i].text}') ";
                }
            }
            var texts = t.GetComponents<UnityEngine.UI.Text>();
            if (texts != null && texts.Length > 0)
            {
                for (int i = 0; i < texts.Length; i++)
                {
                    comps += $"Text('{texts[i].text}') ";
                }
            }
            var imgs = t.GetComponents<UnityEngine.UI.Image>();
            if (imgs != null && imgs.Length > 0) comps += "Image ";

            if (string.IsNullOrEmpty(comps)) comps = "(no text/image components)";
            Debug.Log($"{label}: {path} -> {comps}");
        }
    }

    private static string GetTransformPath(Transform t, Transform root)
    {
        if (t == null) return "";
        if (t == root) return t.name;
        string path = t.name;
        Transform parent = t.parent;
        while (parent != null && parent != root && parent != parent.root)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}
