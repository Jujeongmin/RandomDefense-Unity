using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 앱을 켰을 때 메인화면을 덮는 타이틀 화면. 화면을 터치하면 페이드아웃되며 메인화면이 드러납니다.
/// 게임을 마치고 MainScene으로 돌아올 때마다 다시 뜨면 성가시므로, 앱 실행당 한 번만 표시합니다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class TitleScreenPanel : MonoBehaviour, IPointerClickHandler
{
    // 씬을 다시 로드해도 유지된다(앱 프로세스 단위). 게임 -> 메인 복귀 시 타이틀이 다시 뜨지 않게 한다.
    static bool s_shownThisSession;

    [SerializeField] RectTransform m_logo;
    [SerializeField] TextMeshProUGUI m_tapText;

    [Header("Intro")]
    [SerializeField] float m_logoIntroDuration = 0.6f;
    [SerializeField] float m_logoDropDistance = 70f;

    [Header("Idle")]
    [SerializeField] float m_logoFloatAmplitude = 6f;
    [SerializeField] float m_logoFloatSpeed = 0.9f;
    [Tooltip("'터치하여 시작' 문구가 깜빡이는 속도(초당 사이클)")]
    [SerializeField] float m_tapBlinkSpeed = 0.8f;

    [Header("Dismiss")]
    [SerializeField] float m_fadeOutDuration = 0.35f;

    CanvasGroup m_group;
    Vector2 m_logoHome;
    float m_elapsed;
    bool m_dismissing;
    float m_fadeElapsed;

    void Awake()
    {
        m_group = GetComponent<CanvasGroup>();
        if (m_logo != null) m_logoHome = m_logo.anchoredPosition;

        if (s_shownThisSession)
        {
            gameObject.SetActive(false);
            return;
        }
        s_shownThisSession = true;

        if (m_tapText != null)
            m_tapText.text = GameLanguage.IsEnglish ? "TAP TO START" : "터치하여 시작";
    }

    void Update()
    {
        if (m_dismissing)
        {
            m_fadeElapsed += Time.unscaledDeltaTime;
            float t = m_fadeOutDuration <= 0f ? 1f : Mathf.Clamp01(m_fadeElapsed / m_fadeOutDuration);
            m_group.alpha = 1f - t;
            if (t >= 1f) gameObject.SetActive(false);
            return;
        }

        m_elapsed += Time.unscaledDeltaTime;

        float intro = m_logoIntroDuration <= 0f ? 1f : Mathf.Clamp01(m_elapsed / m_logoIntroDuration);
        float eased = 1f - Mathf.Pow(1f - intro, 3f);

        if (m_logo != null)
        {
            float drop = Mathf.Lerp(m_logoDropDistance, 0f, eased);
            float bob = Mathf.Sin(m_elapsed * m_logoFloatSpeed * Mathf.PI * 2f) * m_logoFloatAmplitude * eased;
            m_logo.anchoredPosition = m_logoHome + new Vector2(0f, drop + bob);
        }

        if (m_tapText != null)
        {
            // 로고 등장이 끝난 뒤부터 깜빡인다
            float blink = 0.35f + 0.65f * (Mathf.Sin(m_elapsed * m_tapBlinkSpeed * Mathf.PI * 2f) * 0.5f + 0.5f);
            Color c = m_tapText.color;
            c.a = blink * eased;
            m_tapText.color = c;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (m_dismissing) return;
        m_dismissing = true;
        m_fadeElapsed = 0f;
    }
}
