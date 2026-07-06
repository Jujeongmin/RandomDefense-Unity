using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Main UI")]
    [SerializeField] Button m_startButton;
    [SerializeField] Button m_settingButton;
    [SerializeField] TextMeshProUGUI m_highWaveText;

    [Header("Settings UI")]
    [SerializeField] GameObject m_settingPanel;
    [SerializeField] TextMeshProUGUI m_settingsTitleText;
    [FormerlySerializedAs("m_soundButton"), SerializeField] Button m_bgmButton;
    [FormerlySerializedAs("m_soundButtonText"), SerializeField] TextMeshProUGUI m_bgmButtonText;
    [SerializeField] Button m_sfxButton;
    [SerializeField] TextMeshProUGUI m_sfxButtonText;
    [SerializeField] Button m_koreanButton;
    [SerializeField] Button m_englishButton;
    [SerializeField] Button m_resetButton;
    [SerializeField] TextMeshProUGUI m_resetButtonText;
    [SerializeField] Button m_closeButton;
    [SerializeField] TextMeshProUGUI m_closeButtonText;

    bool m_resetConfirmationPending;

    bool IsEnglish => GameLanguage.IsEnglish;

    void Start()
    {
        m_startButton?.onClick.AddListener(OnStartButtonClicked);
        m_settingButton?.onClick.AddListener(OpenSettings);
        EnsureSeparateSoundButtons();
        m_bgmButton?.onClick.AddListener(ToggleBgm);
        m_sfxButton?.onClick.AddListener(ToggleSfx);
        m_koreanButton?.onClick.AddListener(() => SetLanguage(false));
        m_englishButton?.onClick.AddListener(() => SetLanguage(true));
        m_resetButton?.onClick.AddListener(OnResetButtonClicked);
        m_closeButton?.onClick.AddListener(CloseSettings);

        if (m_settingPanel != null) m_settingPanel.SetActive(false);
        ApplyLanguage();
        Time.timeScale = 1f;
    }

    void OpenSettings()
    {
        if (m_settingPanel == null) return;
        m_resetConfirmationPending = false;
        m_settingPanel.SetActive(true);
        m_settingPanel.transform.SetAsLastSibling();
        ApplyLanguage();
    }

    void CloseSettings()
    {
        m_resetConfirmationPending = false;
        if (m_settingPanel != null) m_settingPanel.SetActive(false);
    }

    void ToggleBgm()
    {
        GameAudioSettings.ToggleBgm();
        ApplyLanguage();
    }

    void ToggleSfx()
    {
        GameAudioSettings.ToggleSfx();
        ApplyLanguage();
    }

    void EnsureSeparateSoundButtons()
    {
        if (m_bgmButton == null || m_sfxButton != null) return;
        RectTransform bgmRect = m_bgmButton.transform as RectTransform;
        float y = bgmRect != null ? bgmRect.anchoredPosition.y : 155f;
        if (bgmRect != null)
        {
            bgmRect.anchoredPosition = new Vector2(-100f, y);
            bgmRect.sizeDelta = new Vector2(180f, bgmRect.sizeDelta.y);
        }
        m_sfxButton = Instantiate(m_bgmButton, m_bgmButton.transform.parent);
        m_sfxButton.name = "SfxButton";
        RectTransform sfxRect = m_sfxButton.transform as RectTransform;
        if (sfxRect != null) sfxRect.anchoredPosition = new Vector2(100f, y);
        m_sfxButtonText = m_sfxButton.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    void SetLanguage(bool english)
    {
        PlayerPrefs.SetInt(GameLanguage.PreferenceKey, english ? 1 : 0);
        PlayerPrefs.Save();
        m_resetConfirmationPending = false;
        ApplyLanguage();
    }

    void OnResetButtonClicked()
    {
        if (!m_resetConfirmationPending)
        {
            m_resetConfirmationPending = true;
            RefreshResetLabel();
            return;
        }

        if (GManager.Instance != null && GManager.Instance.IsProgress != null)
            GManager.Instance.IsProgress.ResetProgress();
        SceneManager.LoadScene(GManager.SCENE_MAIN);
    }

    void ApplyLanguage()
    {
        bool english = IsEnglish;
        int highestWave = GManager.Instance != null && GManager.Instance.IsProgress != null
            ? GManager.Instance.IsProgress.HighestWave
            : 0;

        if (m_highWaveText != null)
            m_highWaveText.text = english
                ? $"HIGHEST WAVE\n{highestWave:N0} Wave"
                : $"최고 도달 웨이브\n{highestWave:N0} Wave";
        SetButtonText(m_startButton, english ? "START" : "게임시작");
        if (m_settingsTitleText != null) m_settingsTitleText.text = english ? "SETTINGS" : "설정";
        if (m_bgmButtonText != null)
            m_bgmButtonText.text = $"BGM  {(GameAudioSettings.BgmEnabled ? "ON" : "OFF")}";
        if (m_sfxButtonText != null)
            m_sfxButtonText.text = $"SFX  {(GameAudioSettings.SfxEnabled ? "ON" : "OFF")}";
        /*
                : (AudioListener.volume > 0f ? "사운드  켜짐" : "사운드  꺼짐");
        if (m_closeButtonText != null) m_closeButtonText.text = english ? "CLOSE" : "닫기";
        */
        if (m_closeButtonText != null) m_closeButtonText.text = english ? "CLOSE" : "닫기";
        RefreshResetLabel();

        SetButtonSelected(m_koreanButton, !english);
        SetButtonSelected(m_englishButton, english);
    }

    void RefreshResetLabel()
    {
        if (m_resetButtonText == null) return;
        m_resetButtonText.text = IsEnglish
            ? (m_resetConfirmationPending ? "TAP AGAIN TO CONFIRM" : "RESET DATA")
            : (m_resetConfirmationPending ? "한 번 더 눌러 초기화" : "데이터 초기화");
    }

    static void SetButtonText(Button button, string value)
    {
        if (button == null) return;
        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null) text.text = value;
    }

    static void SetButtonSelected(Button button, bool selected)
    {
        if (button == null || button.targetGraphic == null) return;
        button.targetGraphic.color = selected
            ? new Color(0.2f, 0.55f, 0.8f, 1f)
            : new Color(0.16f, 0.24f, 0.38f, 1f);
    }

    void OnStartButtonClicked() => SceneManager.LoadScene(GManager.SCENE_GAME);

}
