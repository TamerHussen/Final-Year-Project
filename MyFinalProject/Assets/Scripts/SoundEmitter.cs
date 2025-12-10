using UnityEngine;

public class SoundEmitter : MonoBehaviour
{
    public static Vector3 LastSoundPos;
    public static float LastSoundVolume;

    public static void Emit(Vector3 pos, float volume)
    {
        LastSoundPos = pos;
        LastSoundVolume = volume;
    }
}
