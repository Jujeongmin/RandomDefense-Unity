using UnityEngine;
using UnityEngine.EventSystems;

// Attach to Region GameObjects. Click to select a region, then click another region to swap their contents.
[RequireComponent(typeof(Collider2D))]
public class RegionClickHandler : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    static RegionClickHandler s_selected = null;
    static readonly Color SelectedColor = new Color(1f, 0.75f, 0.1f, 1f);
    static readonly Color ValidTargetColor = new Color(0.2f, 1f, 0.45f, 1f);
    static readonly Color InvalidTargetColor = new Color(1f, 0.25f, 0.2f, 1f);

    Region m_region;
    Camera m_cam;
    RegionClickHandler m_hovered;
    LineRenderer m_outline;

    void Start()
    {
        m_region = GetComponent<Region>() ?? GetComponentInParent<Region>();
        CreateOutline();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (m_region == null) return;
        m_cam = Camera.main;
        s_selected = this;
        SetHighlight(SelectedColor, true);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        SetHighlight(SelectedColor, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (m_cam == null) m_cam = Camera.main;
        if (m_cam == null) return;
        Vector3 screenPos = new Vector3(eventData.position.x, eventData.position.y, m_cam.nearClipPlane);
        Vector3 worldPos = m_cam.ScreenToWorldPoint(screenPos);

        // find region under pointer
        var regionMgr = GManager.Instance != null ? GManager.Instance.IsRegion : null;
        Region found = null;
        if (regionMgr != null)
        {
            found = regionMgr.FindRegionAtPoint(new Vector2(worldPos.x, worldPos.y));
        }

        // update hovered highlight
        if (m_hovered != null && (found == null || m_hovered.m_region != found))
        {
            m_hovered.SetHighlight(Color.clear, false);
            m_hovered = null;
        }

        if (found != null && found != m_region)
        {
            found.TryGetComponent(out RegionClickHandler handler);
            if (handler != null && handler != m_hovered)
            {
                m_hovered = handler;
                m_hovered.SetHighlight(ValidTargetColor, true);
            }
        }

        SetHighlight(m_hovered != null ? SelectedColor : InvalidTargetColor, true);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // if we have a hovered region, perform swap
        if (m_hovered != null && m_hovered.m_region != null && m_region != null)
        {
            m_region.SwapContentsWith(m_hovered.m_region);
            m_hovered.SetHighlight(Color.clear, false);
            m_hovered = null;
        }

        SetHighlight(Color.clear, false);

        // clear selection
        if (s_selected == this)
        {
            s_selected = null;
        }
    }

    void OnDisable()
    {
        if (m_hovered != null) m_hovered.SetHighlight(Color.clear, false);
        m_hovered = null;
        SetHighlight(Color.clear, false);
        if (s_selected == this) s_selected = null;
    }

    void CreateOutline()
    {
        PolygonCollider2D polygon = GetComponent<PolygonCollider2D>();
        if (polygon == null || polygon.pathCount == 0) return;

        m_outline = GetComponent<LineRenderer>();
        if (m_outline == null) m_outline = gameObject.AddComponent<LineRenderer>();
        Vector2[] points = polygon.GetPath(0);
        m_outline.useWorldSpace = false;
        m_outline.loop = true;
        m_outline.positionCount = points.Length;
        m_outline.startWidth = 0.035f;
        m_outline.endWidth = 0.035f;
        m_outline.numCornerVertices = 2;
        m_outline.numCapVertices = 2;
        m_outline.sortingOrder = 100;
        for (int i = 0; i < points.Length; i++) m_outline.SetPosition(i, points[i]);
        SetHighlight(Color.clear, false);
    }

    void SetHighlight(Color color, bool visible)
    {
        if (m_outline == null) return;
        m_outline.startColor = color;
        m_outline.endColor = color;
        m_outline.enabled = visible;
    }
}
