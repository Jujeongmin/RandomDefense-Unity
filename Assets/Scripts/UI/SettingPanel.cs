using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SettingPanel : MonoBehaviour
{
    [SerializeField] Button m_restartButton;
    [SerializeField] Button m_exitButton;
    [FormerlySerializedAs("m_soundButton"), SerializeField] Button m_bgmButton;
    [FormerlySerializedAs("m_soundButtonText"), SerializeField] TextMeshProUGUI m_bgmButtonText;
    [SerializeField] Button m_sfxButton;
    [SerializeField] TextMeshProUGUI m_sfxButtonText;

    void Start()
    {
        if (GManager.Instance != null) GManager.Instance.RegisterSettingsPanel(gameObject);
        EnsureSeparateSoundButtons();
        m_restartButton?.onClick.AddListener(ResumeGame);
        m_exitButton?.onClick.AddListener(ExitToLobby);
        m_bgmButton?.onClick.AddListener(ToggleBgm);
        m_sfxButton?.onClick.AddListener(ToggleSfx);
        ApplyLanguage();
    }

    void ResumeGame()
    {
        if (GManager.Instance == null) return;
        int speed = GManager.Instance.IsSpeed != null ? GManager.Instance.IsSpeed.IsCurrentSpeed : 0;
        Time.timeScale = speed + 1f;
        if (GManager.Instance.IsSettingPanel != null) GManager.Instance.IsSettingPanel.SetActive(false);
    }

    void ExitToLobby()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GManager.SCENE_MAIN);
    }

    void ToggleBgm()
    {
        GameAudioSettings.ToggleBgm();
        RefreshSoundButtons();
    }

    void ToggleSfx()
    {
        GameAudioSettings.ToggleSfx();
        RefreshSoundButtons();
    }

    void EnsureSeparateSoundButtons()
    {
        if (m_bgmButton == null || m_sfxButton != null) return;
        RectTransform bgmRect = m_bgmButton.transform as RectTransform;
        float y = bgmRect != null ? bgmRect.anchoredPosition.y : 145f;
        if (bgmRect != null)
        {
            bgmRect.anchoredPosition = new Vector2(-75f, y);
            bgmRect.sizeDelta = new Vector2(140f, bgmRect.sizeDelta.y);
        }

        m_sfxButton = Instantiate(m_bgmButton, m_bgmButton.transform.parent);
        m_sfxButton.name = "SfxButton";
        RectTransform sfxRect = m_sfxButton.transform as RectTransform;
        if (sfxRect != null) sfxRect.anchoredPosition = new Vector2(75f, y);
        m_sfxButtonText = m_sfxButton.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    void ApplyLanguage()
    {
        SetButtonText(m_restartButton, GameLanguage.Choose("계속하기", "RESUME"));
        SetButtonText(m_exitButton, GameLanguage.Choose("로비", "LOBBY"));
        RefreshSoundButtons();
    }

    void RefreshSoundButtons()
    {
        if (m_bgmButtonText != null) m_bgmButtonText.text = $"BGM  {(GameAudioSettings.BgmEnabled ? "ON" : "OFF")}";
        if (m_sfxButtonText != null) m_sfxButtonText.text = $"SFX  {(GameAudioSettings.SfxEnabled ? "ON" : "OFF")}";
    }

    static void SetButtonText(Button button, string value)
    {
        if (button == null) return;
        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null) text.text = value;
    }
}

public static class GameLanguage
{
    public const string PreferenceKey = "RandomDefense.Language";
    public static bool IsEnglish => PlayerPrefs.GetInt(PreferenceKey, 0) == 1;
    public static string Choose(string korean, string english) => IsEnglish ? english : korean;
}
