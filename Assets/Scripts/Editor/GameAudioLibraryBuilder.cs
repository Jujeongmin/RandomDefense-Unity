using System.IO;
using UnityEditor;
using UnityEngine;

public static class GameAudioLibraryBuilder
{
    const string AssetPath = "Assets/Resources/GameAudioLibrary.asset";

    [MenuItem("Tools/Random Defense/Build Audio Library")]
    public static void Build()
    {
        Directory.CreateDirectory("Assets/Resources");
        GameAudioLibrary library = AssetDatabase.LoadAssetAtPath<GameAudioLibrary>(AssetPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<GameAudioLibrary>();
            AssetDatabase.CreateAsset(library, AssetPath);
        }

        library.mainBgm = Load("Assets/GData/Audio/TownTheme.mp3");
        library.battleBgm = Load("Assets/GData/Audio/heartfelt-battle_loop.ogg");
        library.bossBgm = Load("Assets/GData/Audio/ambient3(ominous).mp3");
        library.buttonClick = Load("Assets/GData/Audio/kenney_interface-sounds/Audio/click_002.ogg");
        library.confirm = Load("Assets/GData/Audio/kenney_interface-sounds/Audio/confirmation_002.ogg");
        library.error = Load("Assets/GData/Audio/kenney_interface-sounds/Audio/error_003.ogg");
        library.summon = Load("Assets/GData/Audio/kenney_interface-sounds/Audio/pluck_002.ogg");
        library.rareSummon = Load("Assets/GData/Audio/kenney_interface-sounds/Audio/bong_001.ogg");
        library.sell = Load("Assets/GData/Audio/kenney_rpg-audio/Audio/handleCoins.ogg");
        library.upgrade = Load("Assets/GData/Audio/kenney_rpg-audio/Audio/bookOpen.ogg");
        library.mobDeath = Load("Assets/GData/Audio/kenney_impact-sounds/Audio/impactSoft_heavy_002.ogg");
        library.bossDeath = Load("Assets/GData/Audio/kenney_impact-sounds/Audio/impactMetal_medium_002.ogg");
        library.attackWarrior = Load("Assets/GData/Audio/kenney_rpg-audio/Audio/chop.ogg");
        library.attackArcher = Load("Assets/GData/Audio/kenney_impact-sounds/Audio/impactWood_medium_001.ogg");
        library.attackWizard = Load("Assets/GData/Audio/kenney_interface-sounds/Audio/glitch_002.ogg");
        library.victory = Load("Assets/GData/Audio/kenney_interface-sounds/Audio/confirmation_004.ogg");
        library.defeat = Load("Assets/GData/Audio/kenney_interface-sounds/Audio/error_006.ogg");

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        Debug.Log("Game audio library built.");
    }

    static AudioClip Load(string path)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null) Debug.LogError($"Audio clip not found: {path}");
        return clip;
    }
}
