using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : MonoBehaviour
{
    [SerializeField] Button m_restartButton = null;
    [SerializeField] Button m_exitButton = null;


    void Start()
    {
        if (GManager.Instance != null) GManager.Instance.RegisterSettingsPanel(gameObject);
        if (m_restartButton != null) m_restartButton.onClick.AddListener(OnRestartButtonClicked);
        if (m_exitButton != null) m_exitButton.onClick.AddListener(OnExitButtonClicked);
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
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
