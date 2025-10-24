using UnityEngine;

[CreateAssetMenu(fileName = "TooltipData", menuName = "Tooltip/ToolTipData")]
public class TooltipData : ScriptableObject
{
    [Header("Title")]
    public string Title;
    public TooltipType Type;


    [Header("Description")]
    public string Description;
}

public enum TooltipType
{
    coin,
    enemies,
    season,
    time,
    crops,
    pricePanel,
    animals,
}