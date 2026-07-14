using UnityEngine;

/// <summary>
/// 메인화면 타이틀 연출. 씬에 배치된 값을 영구 변경하지 않도록,
/// 시작 시점의 위치·스케일을 기준값으로 캐시하고 그 위에 오프셋만 얹습니다.
/// </summary>
public class MainScreenPresenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] RectTransform m_logo;
    [SerializeField] CanvasGroup m_logoGroup;
    [SerializeField] RectTransform m_startButton;
    [SerializeField] RectTransform m_background;

    [Header("Intro")]
    [Tooltip("로고가 떨어지며 나타나는 시간(초)")]
    [SerializeField] float m_introDuration = 0.55f;
    [Tooltip("로고가 얼마나 위에서부터 떨어지는지(px)")]
    [SerializeField] float m_introDropDistance = 70f;

    [Header("Idle")]
    [Tooltip("로고가 떠다니는 진폭(px)")]
    [SerializeField] float m_logoFloatAmplitude = 6f;
    [SerializeField] float m_logoFloatSpeed = 0.9f;
    [Tooltip("START 버튼 펄스 진폭(스케일 비율)")]
    [SerializeField] float m_startPulseAmplitude = 0.04f;
    [SerializeField] float m_startPulseSpeed = 2.2f;
    [Tooltip("배경 호흡 진폭(스케일 비율)")]
    [SerializeField] float m_backgroundBreathAmplitude = 0.02f;
    [SerializeField] float m_backgroundBreathSpeed = 0.15f;

    Vector2 m_logoHome;
    Vector3 m_startHome;
    Vector3 m_backgroundHome;
    float m_elapsed;

    void Awake()
    {
        if (m_logo != null) m_logoHome = m_logo.anchoredPosition;
        if (m_startButton != null) m_startHome = m_startButton.localScale;
        if (m_background != null) m_backgroundHome = m_background.localScale;
    }

    void OnEnable()
    {
        m_elapsed = 0f;
        Apply();
    }

    void Update()
    {
        m_elapsed += Time.unscaledDeltaTime;
        Apply();
    }

    void Apply()
    {
        float intro = m_introDuration <= 0f ? 1f : Mathf.Clamp01(m_elapsed / m_introDuration);
        float eased = 1f - Mathf.Pow(1f - intro, 3f); // ease-out cubic

        if (m_logo != null)
        {
            float drop = Mathf.Lerp(m_introDropDistance, 0f, eased);
            float float_ = Mathf.Sin(m_elapsed * m_logoFloatSpeed * Mathf.PI * 2f) * m_logoFloatAmplitude * eased;
            m_logo.anchoredPosition = m_logoHome + new Vector2(0f, drop + float_);
        }

        if (m_logoGroup != null) m_logoGroup.alpha = eased;

        if (m_startButton != null)
        {
            // 인트로가 끝난 뒤에만 펄스 시작
            float pulse = 1f + Mathf.Sin(m_elapsed * m_startPulseSpeed * Mathf.PI * 2f) * m_startPulseAmplitude * eased;
            m_startButton.localScale = m_startHome * pulse;
        }

        if (m_background != null)
        {
            float breath = 1f + (Mathf.Sin(m_elapsed * m_backgroundBreathSpeed * Mathf.PI * 2f) * 0.5f + 0.5f) * m_backgroundBreathAmplitude;
            m_background.localScale = m_backgroundHome * breath;
        }
    }

    void OnDisable()
    {
        // 씬에 저장된 원래 값으로 되돌려 놓는다 (에디터에서 값이 오염되지 않도록)
        if (m_logo != null) m_logo.anchoredPosition = m_logoHome;
        if (m_logoGroup != null) m_logoGroup.alpha = 1f;
        if (m_startButton != null) m_startButton.localScale = m_startHome;
        if (m_background != null) m_background.localScale = m_backgroundHome;
    }
}
