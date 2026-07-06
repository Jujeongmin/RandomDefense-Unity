using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHudTimer : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI m_timerText;
    [SerializeField] Button m_settingButton;
    [SerializeField] SettingPanel m_settingPanel;
    Vector2Int m_screenSize;

    void Start()
    {
        if (m_settingButton != null) m_settingButton.onClick.AddListener(OpenSettings);
    }

    void OnDestroy()
    {
        if (m_settingButton != null) m_settingButton.onClick.RemoveListener(OpenSettings);
    }

    void OpenSettings()
    {
        if (m_settingPanel == null) return;
        if (GManager.Instance != null) GManager.Instance.RegisterSettingsPanel(m_settingPanel.gameObject);
        m_settingPanel.gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    void Update()
    {
        Vector2Int currentSize = new Vector2Int(Screen.width, Screen.height);
        if (currentSize != m_screenSize)
        {
            MobileSafeAreaLayout.ApplyTop(transform as RectTransform);
            m_screenSize = currentSize;
        }
        RefreshText();
    }

    void RefreshText()
    {
        if (m_timerText == null) return;
        MobManager mobManager = GManager.Instance != null ? GManager.Instance.IsMob : null;
        if (mobManager == null)
        {
            m_timerText.text = "--:--";
            return;
        }

        m_timerText.text = mobManager.WaveTimeRemaining.ToString("F2");
    }
}
