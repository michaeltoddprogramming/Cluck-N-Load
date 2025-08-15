using System.IO;
using UnityEngine;

public static class SaveManager
{
    private static string SavePath => Application.persistentDataPath + "/gamesave.json";

    public static void SaveGame(GameSaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
    }

    public static GameSaveData LoadGame()
    {
        if (!File.Exists(SavePath)) return null;
        string json = File.ReadAllText(SavePath);
        return JsonUtility.FromJson<GameSaveData>(json);
    }
}