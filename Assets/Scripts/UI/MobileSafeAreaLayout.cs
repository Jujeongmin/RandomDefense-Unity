using UnityEngine;

public sealed class MobileSafeAreaLayout : MonoBehaviour
{
    [Header("Safe Area Targets")]
    [SerializeField] RectTransform m_topBar;
    [SerializeField] RectTransform m_bottomBar;
    [SerializeField] RectTransform m_sellContent;

    Rect m_lastSafeArea;
    Vector2Int m_lastScreenSize;

    void OnEnable() => Apply();

    void Update()
    {
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
        if (m_lastSafeArea != Screen.safeArea || m_lastScreenSize != screenSize) Apply();
    }

    void Apply()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;
        Rect safe = Screen.safeArea;
        Vector2 min = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        Vector2 max = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);

        AnchorHorizontalEdge(m_topBar, min.x, max.x, max.y, true);
        AnchorVerticalEdge(m_bottomBar, min.y, false);
        AnchorHorizontalEdge(m_sellContent, min.x, max.x, min.y, false);

        m_lastSafeArea = safe;
        m_lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }

    static void AnchorHorizontalEdge(RectTransform rect, float minX, float maxX, float y, bool top)
    {
        if (rect == null) return;
        rect.anchorMin = new Vector2(minX, y);
        rect.anchorMax = new Vector2(maxX, y);
        rect.pivot = new Vector2(0.5f, top ? 1f : 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(-20f, rect.sizeDelta.y);
    }

    public static void ApplyTop(RectTransform rect) => ApplyEdge(rect, true);
    public static void ApplyBottom(RectTransform rect)
    {
        if (rect == null || Screen.height <= 0) return;
        AnchorVerticalEdge(rect, Screen.safeArea.yMin / Screen.height, false);
    }

    /// <summary>하단 안전영역에 붙이되 lift만큼 위로 띄웁니다.</summary>
    public static void ApplyBottom(RectTransform rect, float lift)
    {
        ApplyBottom(rect);
        if (rect != null) rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, lift);
    }

    static void ApplyEdge(RectTransform rect, bool top)
    {
        if (rect == null || Screen.width <= 0 || Screen.height <= 0) return;
        Rect safe = Screen.safeArea;
        float minX = safe.xMin / Screen.width;
        float maxX = safe.xMax / Screen.width;
        float y = top ? safe.yMax / Screen.height : safe.yMin / Screen.height;
        AnchorHorizontalEdge(rect, minX, maxX, y, top);
    }

    static void AnchorVerticalEdge(RectTransform rect, float y, bool top)
    {
        if (rect == null) return;
        rect.anchorMin = new Vector2(rect.anchorMin.x, y);
        rect.anchorMax = new Vector2(rect.anchorMax.x, y);
        rect.pivot = new Vector2(rect.pivot.x, top ? 1f : 0f);
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 0f);
    }
}
