using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingPanel : MonoBehaviour
{
    [SerializeField] Button m_restartButton = null;
    [SerializeField] Button m_exitButton = null;
    [SerializeField] Button m_soundButton = null;
    [SerializeField] TextMeshProUGUI m_soundButtonText = null;

    const string SoundEnabledKey = "RandomDefense.SoundEnabled";

    void Start()
    {
        if (GManager.Instance != null) GManager.Instance.RegisterSettingsPanel(gameObject);
        if (m_restartButton != null) m_restartButton.onClick.AddListener(OnRestartButtonClicked);
        if (m_exitButton != null) m_exitButton.onClick.AddListener(OnExitButtonClicked);
        if (m_soundButton != null) m_soundButton.onClick.AddListener(OnSoundButtonClicked);
        ApplySavedSoundSetting();
    }

    void OnRestartButtonClicked()
    {
        if (GManager.Instance == null) return;

        int _gameSpeed = GManager.Instance.IsSpeed != null ? GManager.Instance.IsSpeed.IsCurrentSpeed : 0;

        Time.timeScale = (float)_gameSpeed + 1;
        if (GManager.Instance.IsSettingPanel != null) GManager.Instance.IsSettingPanel.SetActive(false);
    }

    void OnExitButtonClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GManager.SCENE_MAIN);
    }

    void OnSoundButtonClicked()
    {
        bool enabled = AudioListener.volume <= 0f;
        AudioListener.volume = enabled ? 1f : 0f;
        PlayerPrefs.SetInt(SoundEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        RefreshSoundButton();
    }

    void ApplySavedSoundSetting()
    {
        AudioListener.volume = PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1 ? 1f : 0f;
        RefreshSoundButton();
    }

    void RefreshSoundButton()
    {
        if (m_soundButtonText != null)
            m_soundButtonText.text = AudioListener.volume > 0f ? "사운드  켜짐" : "사운드  꺼짐";
    }
}
