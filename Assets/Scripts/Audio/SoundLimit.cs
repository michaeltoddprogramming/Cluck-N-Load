using UnityEngine;

[CreateAssetMenu(fileName = "GlobalSoundManager", menuName = "Audio/GlobalSoundManager")]
public class SoundLimit : ScriptableObject
{
    public int maxAttackSounds = 5;
    public int maxHurtSounds = 5;
    public int maxDeathSounds = 3;
}
