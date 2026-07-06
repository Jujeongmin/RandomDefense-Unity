using System;
using UnityEngine;

public static class GameAudioSettings
{
    const string LegacySoundKey = "RandomDefense.SoundEnabled";
    const string BgmKey = "RandomDefense.BgmEnabled";
    const string SfxKey = "RandomDefense.SfxEnabled";

    public static event Action Changed;
    public static bool BgmEnabled => Get(BgmKey);
    public static bool SfxEnabled => Get(SfxKey);

    public static void ToggleBgm() => Set(BgmKey, !BgmEnabled);
    public static void ToggleSfx() => Set(SfxKey, !SfxEnabled);

    static bool Get(string key) => PlayerPrefs.GetInt(key, PlayerPrefs.GetInt(LegacySoundKey, 1)) == 1;

    static void Set(string key, bool enabled)
    {
        PlayerPrefs.SetInt(key, enabled ? 1 : 0);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
