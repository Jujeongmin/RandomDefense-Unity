using UnityEngine;

public enum GameMode
{
    Normal,   // 50웨이브 클리어 시 승리
    Endless   // 클리어 없이 패배할 때까지 무한 진행
}

/// <summary>
/// 게임 모드 설정. PlayerPrefs에 저장되며 메인메뉴에서 선택합니다.
/// </summary>
public static class GameModeSettings
{
    const string ModeKey = "RandomDefense.GameMode";

    static GameMode s_mode;
    static bool s_loaded;

    static void EnsureLoaded()
    {
        if (s_loaded) return;
        s_mode = (GameMode)PlayerPrefs.GetInt(ModeKey, (int)GameMode.Normal);
        s_loaded = true;
    }

    public static GameMode Mode
    {
        get { EnsureLoaded(); return s_mode; }
        set
        {
            EnsureLoaded();
            s_mode = value;
            PlayerPrefs.SetInt(ModeKey, (int)value);
            PlayerPrefs.Save();
        }
    }

    public static bool IsEndless => Mode == GameMode.Endless;
}
