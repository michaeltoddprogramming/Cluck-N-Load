using System;
using UnityEngine;

[Serializable]
public class GridCell
{
    public int x;
    public int y;
    public Vector3 worldPosition;

    public GridCellFlags flags;

    // Color getter for this cell (based on flags)
    public Color GetDebugColor() => flags.GetColor();
}

[Serializable]
public struct GridCellFlags
{
    public bool isOwned;
    public bool isOccupied;
    public bool isObstacle;

    public Color GetColor()
    {
        return GridCellColorResolver.Resolve(this);
    }

    public void Set(bool owned, bool occupied, bool obstacle)
    {
        isOwned = owned;
        isOccupied = occupied;
        isObstacle = obstacle;
    }
}

public static class GridCellColorResolver
{
    public static GridColors Colors { get; set; } = new GridColors(); // Allow external assignment

    public static Color Resolve(GridCellFlags flags)
    {
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
