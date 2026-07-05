using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 메인씬 UI 제어 및 게임씬 전환 관리
/// MainScene 전용 스크립트입니다.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("UI Buttons")]
    [SerializeField] Button m_startButton = null;
    [SerializeField] Button m_quitButton = null;

    private void Start()
    {
        if (m_startButton != null) m_startButton.onClick.AddListener(OnStartButtonClicked);
        if (m_quitButton != null) m_quitButton.onClick.AddListener(OnQuitButtonClicked);

        Time.timeScale = 1f;
    }

    private void OnStartButtonClicked()
    {
        SceneManager.LoadScene(GManager.SCENE_GAME);
    }

    private void OnQuitButtonClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
