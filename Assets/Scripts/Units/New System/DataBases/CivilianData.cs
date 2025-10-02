using UnityEngine;

[CreateAssetMenu(fileName = "NewCivilianData", menuName = "Units/CivilianData")]
public class CivilianData : UnitData
{
    [Header("Wander Settings")]
    public float minWait = 3f;
    public float maxWait = 10f;
    public float stopThreshold = 0.05f;
}
