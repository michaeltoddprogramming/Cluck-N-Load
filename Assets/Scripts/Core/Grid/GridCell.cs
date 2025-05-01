using System;
using UnityEngine;

[Serializable]
public class GridCell
{
    public int x;
    public int y;
    public float height;
    public Vector3 worldPosition;
    public float[] pheromones = {0f, 0f, 0f}; //each index represents an enemy type; 0 = regular, 1 = fast, 2 = strong
    public GridCellFlags flags;

    public GameObject placedObject; // e.g., building or item prefab instance
    public string itemID; // optional, for referencing items by type

    public int integrationCost = int.MaxValue; // Start at max
    
    // Change this from Vector2Int to Vector2 for continuous direction
    public Vector2 flowDirection = Vector2.zero;

    // Color getter for this cell (based on flags)
    public Color GetDebugColor() => flags.GetColor();
}

[Serializable]
public struct GridCellFlags
{
    public bool isOwned;
    public bool isOccupied;
    public bool isObstacle;
    public bool isVisible; // New visibility flag

    public Color GetColor()
    {
        return GridCellColorResolver.Resolve(this);
    }

    public void Set(bool owned, bool occupied, bool obstacle, bool visible = true)
    {
        isOwned = owned;
        isOccupied = occupied;
        isObstacle = obstacle;
        isVisible = visible;
    }
}

public static class GridCellColorResolver
{
    public static GridColors Colors { get; set; } = new GridColors(); // Allow external assignment

    public static Color Resolve(GridCellFlags flags)
    {
        // Invisible cells get a fully transparent color (highest priority)
        if (!flags.isVisible)
            return new Color(0, 0, 0, 0);
            
        if (!flags.isOwned)
        {
            if (flags.isObstacle) return Colors.notOwnedObstacle;
            return Colors.notOwned;
        }

        if (flags.isOwned && flags.isObstacle) return Colors.ownedObstacle;
        if (flags.isOwned && flags.isOccupied) return Colors.ownedOccupied;
        if (flags.isOwned) return Colors.owned;

        return Colors.unavailable;
    }
}

[System.Serializable]
public class GridColors
{
    public Color notOwned = new Color(0.0f, 0.5f, 0.0f, 0.3f);
    public Color owned = new Color(0f, 1f, 0f, 0.3f);
    public Color ownedOccupied = new Color(1f, 1f, 0f, 0.3f);
    public Color ownedObstacle = new Color(0.1f, 0f, 1f, 0.3f);
    public Color notOwnedObstacle = new Color(0f, 0.2f, 0f, 0.3f);
    public Color unavailable = new Color(0.5f, 0f, 0.5f, 0.3f);
}
