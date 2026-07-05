using UnityEngine;
using UnityEngine.EventSystems;

// Attach to Region GameObjects. Click to select a region, then click another region to swap their contents.
[RequireComponent(typeof(Collider2D))]
public class RegionClickHandler : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    static RegionClickHandler s_selected = null;

    Region m_region;
    Camera m_cam;
    RegionClickHandler m_hovered;

    void Start()
    {
        m_region = GetComponent<Region>() ?? GetComponentInParent<Region>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (m_region == null) return;
        m_cam = Camera.main;
        s_selected = this;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // nothing special for begin drag
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
            m_hovered = null;
        }

        if (found != null && found != m_region)
        {
            var handler = found.GetComponent<RegionClickHandler>();
            if (handler != null && handler != m_hovered)
            {
                m_hovered = handler;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // if we have a hovered region, perform swap
        if (m_hovered != null && m_hovered.m_region != null && m_region != null)
        {
            m_region.SwapContentsWith(m_hovered.m_region);
            m_hovered = null;
        }

        // clear selection
        if (s_selected == this)
        {
            s_selected = null;
        }
    }

    void OnDisable()
    {
        if (s_selected == this) s_selected = null;
    }
}
