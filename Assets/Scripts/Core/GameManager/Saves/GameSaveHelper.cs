using BayatGames.SaveGameFree;

public static class GameSaveHelper
{
    public static void SaveToSlot(int slot, GameSaveData data)
    {
        string key = $"SaveSlot_{slot}";
        SaveGame.Save(key, data);
    }

    public static GameSaveData LoadFromSlot(int slot)
    {
        string key = $"SaveSlot_{slot}";
        return SaveGame.Load<GameSaveData>(key, default(GameSaveData));
    }

    public static bool HasSave(int slot)
    {
        string key = $"SaveSlot_{slot}";
        return SaveGame.Exists(key);
    }

    public static void DeleteSlot(int slot)
    {
        string key = $"SaveSlot_{slot}";
        BayatGames.SaveGameFree.SaveGame.Delete(key);
    }
}