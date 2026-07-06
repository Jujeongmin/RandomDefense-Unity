using UnityEngine;

[CreateAssetMenu(menuName = "Random Defense/Audio Library")]
public sealed class GameAudioLibrary : ScriptableObject
{
    [Header("BGM")]
    public AudioClip mainBgm;
    public AudioClip battleBgm;
    public AudioClip bossBgm;

    [Header("SFX")]
    public AudioClip buttonClick;
    public AudioClip confirm;
    public AudioClip error;
    public AudioClip summon;
    public AudioClip sell;
    public AudioClip upgrade;
    public AudioClip mobDeath;
    public AudioClip bossDeath;
    public AudioClip victory;
    public AudioClip defeat;
}
