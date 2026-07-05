using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ResultPanel : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI m_titleText = null;
    [SerializeField] TextMeshProUGUI m_detailsText = null;
    [SerializeField] Button m_restartButton = null;
    [SerializeField] Button m_exitButton = null;

    [Header("Color Settings")]
    [SerializeField] Color m_clearColor = Color.green;
    [SerializeField] Color m_gameOverColor = Color.red;


    private void Start()
    {
        if (GManager.Instance != null) GManager.Instance.RegisterResultPanel(this);
        if (m_restartButton != null) m_restartButton.onClick.AddListener(OnRestartButtonClicked);
        if (m_exitButton != null) m_exitButton.onClick.AddListener(OnExitButtonClicked);
    }

    public void Setup(bool isClear, int finalWave)
    {
        gameObject.SetActive(true);

        if (isClear)
        {
            if (m_titleText != null)
            {
                m_titleText.text = "GAME CLEAR";
                m_titleText.color = m_clearColor;
            }
            if (m_detailsText != null)
            {
                m_detailsText.text = $"축하합니다!\n웨이브를 모두 클리어했습니다!";
            }
        }
        else
        {
            if (m_titleText != null)
            {
                m_titleText.text = "GAME OVER";
                m_titleText.color = m_gameOverColor;
            }
            if (m_detailsText != null)
            {
                m_detailsText.text = $"{finalWave} 웨이브에서 패배했습니다.";
            }
        }
    }

    private void OnRestartButtonClicked()
    {
        Time.timeScale = 1f;
        if (GManager.Instance != null)
        {
            GManager.Instance.RestartGame();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void OnExitButtonClicked()
    {
        Time.timeScale = 1f;
        // GManager가 있으면 메인씨으로 복귀, 없으면 바로 종료
        if (GManager.Instance != null)
        {
            SceneManager.LoadScene(GManager.SCENE_MAIN);
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
