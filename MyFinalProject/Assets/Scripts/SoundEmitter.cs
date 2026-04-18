using UnityEngine;

public class SoundEmitter : MonoBehaviour
{
    public static Vector3 LastSoundPos;
    public static float LastSoundVolume;

    // this lets the predator know what the sound is coming from
    public enum SoundSource { None, Player, Prey, Animal, Familiar }
    public static SoundSource LastSoundSource = SoundSource.None;

    public static void Emit(Vector3 pos, float volume, SoundSource source = SoundSource.Player)
    {
        // prevents the quiet sounds from overriding louder sounds
        if (source == SoundSource.Player || volume >= LastSoundVolume)
        {
            LastSoundPos = pos;
            LastSoundVolume = Mathf.Clamp01(volume);
            LastSoundSource = source;
        }
    }

    public static void ResetSound()
    {
        LastSoundPos = Vector3.zero;
        LastSoundVolume = 0f;
        LastSoundSource = SoundSource.None;
    }

    private void OnDrawGizmos()
    {
        // colour coded the types of sounds to show what the predator ml agent is hearing
        switch (LastSoundSource)
        {
            case SoundSource.Player:
                Gizmos.color = Color.red;
                break;
            case SoundSource.Prey:
                Gizmos.color = Color.green;
                break;
            case SoundSource.Animal:
                Gizmos.color = Color.yellow;
                break;
            case SoundSource.Familiar:
                Gizmos.color = Color.cyan;
                break;
            default:
                Gizmos.color = Color.blue;
                break;
        }
        Gizmos.DrawSphere(LastSoundPos, 0.3f);
        Gizmos.DrawWireSphere(LastSoundPos, LastSoundVolume * 5f); // shows volume radius
    }
}
