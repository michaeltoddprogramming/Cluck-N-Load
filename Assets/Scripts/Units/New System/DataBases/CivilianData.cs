using UnityEngine;

[CreateAssetMenu(fileName = "NewCivilianData", menuName = "Units/CivilianData")]
public class CivilianData : UnitData
{
    [Header("Wander Settings")]
    public float minWait = 1f;
    public float maxWait = 3f;
    public float stopThreshold = 0.05f;
}
