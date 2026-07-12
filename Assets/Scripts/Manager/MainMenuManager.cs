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
    [Tooltip("모드(일반/무한) 선택 버튼 (미할당 시 자동 생성)")]
    [SerializeField] Button m_modeButton;
    [Tooltip("확률 표기 화면을 여는 버튼 (미할당 시 자동 생성)")]
    [SerializeField] Button m_oddsButton;
    [Tooltip("확률 표기 패널 (씬에 미리 배치 후 할당, 미할당 시 자동 생성)")]
    [SerializeField] RarityOddsPanel m_oddsPanel;
    [Tooltip("무한모드 랭킹(리더보드) 버튼 (미할당 시 자동 생성)")]
    [SerializeField] Button m_rankingButton;

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

        if (m_modeButton == null) m_modeButton = CloneMenuButton("ModeButton", -1);
        if (m_modeButton != null)
        {
            m_modeButton.onClick.RemoveAllListeners();
            m_modeButton.onClick.AddListener(OnModeButtonClicked);
        }

        Transform canvasParent = ResolveCanvasParent();
        if (m_oddsPanel == null) m_oddsPanel = FindAnyObjectByType<RarityOddsPanel>(FindObjectsInactive.Include);
        if (m_oddsPanel == null) m_oddsPanel = RarityOddsPanel.Create(canvasParent);
        if (m_oddsButton == null) m_oddsButton = CreateOddsButton(canvasParent);
        if (m_oddsButton != null)
        {
            m_oddsButton.onClick.RemoveAllListeners();
            m_oddsButton.onClick.AddListener(() => { if (m_oddsPanel != null) m_oddsPanel.Open(); });
        }

        if (m_rankingButton == null) m_rankingButton = CreateRankingButton(canvasParent);
        if (m_rankingButton != null)
        {
            m_rankingButton.onClick.RemoveAllListeners();
            m_rankingButton.onClick.AddListener(() => LeaderboardService.ShowLeaderboard());
        }
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

        RefreshModeButtons();
        if (m_oddsButton != null) SetButtonText(m_oddsButton, english ? "ODDS" : "확률 정보");
        if (m_rankingButton != null) SetButtonText(m_rankingButton, english ? "RANKING" : "랭킹");
    }

    Transform ResolveCanvasParent()
    {
        Canvas canvas = m_startButton != null ? m_startButton.GetComponentInParent<Canvas>() : GetComponentInParent<Canvas>();
        return canvas != null ? canvas.transform : transform;
    }

    Button CreateOddsButton(Transform canvasParent)
        => CreateCornerButton(canvasParent, "OddsButton", new Vector2(16f, 110f), IsEnglish ? "ODDS" : "확률 정보");

    Button CreateRankingButton(Transform canvasParent)
        => CreateCornerButton(canvasParent, "RankingButton", new Vector2(16f, 166f), IsEnglish ? "RANKING" : "랭킹");

    // 좌하단 코너에 128x48 버튼 생성 (확률/랭킹 등)
    Button CreateCornerButton(Transform canvasParent, string cornerName, Vector2 position, string labelText)
    {
        if (canvasParent == null) return null;

        GameObject go = new GameObject(cornerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(Button));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(canvasParent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(128f, 48f);

        UnityEngine.UI.Image image = go.GetComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.16f, 0.24f, 0.38f, 1f);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        GameObject labelGo = new GameObject("Label", typeof(RectTransform));
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
        GameFont.Apply(label);
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 10f;
        label.fontSizeMax = 20f;
        label.text = labelText;

        return button;
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

    void OnModeButtonClicked()
    {
        GameModeSettings.Mode = GameModeSettings.Mode == GameMode.Normal ? GameMode.Endless : GameMode.Normal;
        ApplyLanguage();
    }

    void RefreshModeButtons()
    {
        bool english = IsEnglish;

        if (m_modeButton != null)
        {
            string mode = GameModeSettings.Mode == GameMode.Endless
                ? (english ? "ENDLESS" : "무한")
                : (english ? "NORMAL" : "일반");
            SetButtonText(m_modeButton, (english ? "MODE: " : "모드: ") + mode);
        }
    }

    // START 버튼을 복제해 같은 스타일의 메뉴 버튼을 만듭니다. step이 양수면 아래, 음수면 위에 배치합니다.
    Button CloneMenuButton(string cloneName, int stepDown)
    {
        if (m_startButton == null) return null;

        Button clone = Instantiate(m_startButton, m_startButton.transform.parent);
        clone.name = cloneName;
        clone.onClick.RemoveAllListeners();

        RectTransform source = m_startButton.transform as RectTransform;
        RectTransform rect = clone.transform as RectTransform;
        if (source != null && rect != null)
            rect.anchoredPosition = source.anchoredPosition - new Vector2(0f, (source.sizeDelta.y + 24f) * stepDown);

        return clone;
    }
}
