using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class RegionClickHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    static RegionClickHandler s_selected;
    static readonly Color SelectedColor = new Color(1f, 0.75f, 0.1f, 1f);
    static readonly Color ValidTargetColor = new Color(0.2f, 1f, 0.45f, 1f);
    static readonly Color InvalidTargetColor = new Color(1f, 0.25f, 0.2f, 1f);
    // 평상시에도 지역 경계를 은은한 점선으로 표시
    static readonly Color IdleColor = new Color(1f, 1f, 1f, 0.3f);

    Region m_region;
    Camera m_cam;
    RegionClickHandler m_hovered;
    LineRenderer m_outline;
    MeshRenderer m_selectionFill;
    LineRenderer m_path;
    LineRenderer m_arrow;
    LineRenderer m_targetRing;
    Material m_dragMaterial;
    Material m_solidMaterial;
    Material m_outlineMaterial;
    Texture2D m_dashTexture;
    Vector3 m_dragStartWorld;
    bool m_dragging;

    void Start()
    {
        m_region = GetComponent<Region>() ?? GetComponentInParent<Region>();
        CreateDashTexture();
        CreateOutline();
        CreateSelectionFill();
        CreateDragVisual();
    }

    void CreateDashTexture()
    {
        m_dashTexture = new Texture2D(16, 2, TextureFormat.RGBA32, false);
        m_dashTexture.wrapMode = TextureWrapMode.Repeat;
        m_dashTexture.filterMode = FilterMode.Point;
        for (int y = 0; y < m_dashTexture.height; y++)
            for (int x = 0; x < m_dashTexture.width; x++)
                m_dashTexture.SetPixel(x, y, x < 9 ? Color.white : Color.clear);
        m_dashTexture.Apply();
    }

    void ShowIdle()
    {
        SetHighlight(IdleColor, true);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (m_region == null) return;
        m_cam = Camera.main;
        m_dragStartWorld = ScreenToWorld(eventData.position);
        s_selected = this;
        SetSelectionFillVisible(true);
        SetHighlight(SelectedColor, true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 드래그 없이 탭만 한 경우 OnEndDrag가 호출되지 않아 어두운 상태가 남는 문제 방지
        if (m_dragging) return;
        SetSelectionFillVisible(false);
        ShowIdle();
        if (s_selected == this) s_selected = null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        m_dragging = true;
        SetHighlight(SelectedColor, true);
        SetSelectionFillVisible(true);
        SetDragVisualVisible(true);
        UpdateDragVisual(ScreenToWorld(eventData.position), false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 worldPos = ScreenToWorld(eventData.position);
        Region found = GManager.Instance?.IsRegion?.FindRegionAtPoint(worldPos);

        if (m_hovered != null && (found == null || m_hovered.m_region != found))
        {
            m_hovered.ShowIdle();
            m_hovered.SetSelectionFillVisible(false);
            m_hovered = null;
        }

        if (found != null && found != m_region && found.TryGetComponent(out RegionClickHandler handler))
        {
            m_hovered = handler;
            m_hovered.SetHighlight(ValidTargetColor, true);
            m_hovered.SetSelectionFillVisible(true);
        }

        bool valid = m_hovered != null;
        SetHighlight(valid ? SelectedColor : InvalidTargetColor, true);
        // Keep the guide attached to the pointer. Entering another region only
        // changes its target state; it must not snap the visual to that region's centre.
        UpdateDragVisual(worldPos, valid);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        m_dragging = false;
        if (m_hovered != null && m_hovered.m_region != null && m_region != null)
        {
            m_region.SwapContentsWith(m_hovered.m_region);
            m_hovered.ShowIdle();
            m_hovered.SetSelectionFillVisible(false);
            m_hovered = null;
        }

        ShowIdle();
        SetSelectionFillVisible(false);
        SetDragVisualVisible(false);
        if (s_selected == this) s_selected = null;
    }

    void OnDisable()
    {
        m_dragging = false;
        if (m_hovered != null)
        {
            m_hovered.ShowIdle();
            m_hovered.SetSelectionFillVisible(false);
        }
        m_hovered = null;
        SetHighlight(Color.clear, false);
        SetSelectionFillVisible(false);
        SetDragVisualVisible(false);
        if (s_selected == this) s_selected = null;
    }

    void Update()
    {
        if (m_path != null && m_path.enabled && m_dragMaterial != null)
        {
            Vector2 offset = m_dragMaterial.mainTextureOffset;
            offset.x -= Time.unscaledDeltaTime * 1.8f;
            m_dragMaterial.mainTextureOffset = offset;
        }
    }

    Vector3 ScreenToWorld(Vector2 screenPosition)
    {
        if (m_cam == null) m_cam = Camera.main;
        if (m_cam == null) return transform.position;
        Vector3 world = m_cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -m_cam.transform.position.z));
        world.z = transform.position.z - 0.1f;
        return world;
    }

    void CreateOutline()
    {
        PolygonCollider2D polygon = GetComponent<PolygonCollider2D>();
        if (polygon == null || polygon.pathCount == 0) return;
        m_outline = GetComponent<LineRenderer>() ?? gameObject.AddComponent<LineRenderer>();
        ConfigureLine(m_outline, 0.035f, 100);
        m_outline.useWorldSpace = false;
        m_outline.loop = true;
        Vector2[] points = polygon.GetPath(0);
        m_outline.positionCount = points.Length;
        float perimeter = 0f;
        for (int i = 0; i < points.Length; i++)
        {
            m_outline.SetPosition(i, points[i]);
            perimeter += Vector2.Distance(points[i], points[(i + 1) % points.Length]);
        }

        // 점선 재질 적용
        m_outlineMaterial = new Material(Shader.Find("Sprites/Default")) { color = Color.white };
        m_outlineMaterial.mainTexture = m_dashTexture;
        m_outlineMaterial.mainTextureScale = new Vector2(Mathf.Max(1f, perimeter * 5f), 1f);
        m_outline.material = m_outlineMaterial;
        m_outline.textureMode = LineTextureMode.Tile;

        ShowIdle();
    }

    void CreateDragVisual()
    {
        Shader shader = Shader.Find("Sprites/Default");
        m_dragMaterial = new Material(shader) { color = Color.white };
        m_solidMaterial = new Material(shader) { color = Color.white };
        m_dragMaterial.mainTexture = m_dashTexture;

        m_path = CreateVisualLine("SwapPath", 0.09f, 110, m_dragMaterial);
        m_path.textureMode = LineTextureMode.Tile;
        m_arrow = CreateVisualLine("SwapArrow", 0.11f, 111, m_solidMaterial);
        m_targetRing = CreateVisualLine("SwapTarget", 0.07f, 110, m_solidMaterial);
        m_targetRing.loop = true;
        SetDragVisualVisible(false);
    }

    void CreateSelectionFill()
    {
        PolygonCollider2D polygon = GetComponent<PolygonCollider2D>();
        if (polygon == null || polygon.pathCount == 0) return;

        Vector2[] points = polygon.GetPath(0);
        if (points.Length < 3) return;

        GameObject child = new GameObject("SelectionFill");
        child.transform.SetParent(transform, false);
        child.transform.localPosition = new Vector3(0f, 0f, -0.05f);

        Vector3[] vertices = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++) vertices[i] = points[i];

        int[] triangles = new int[(points.Length - 2) * 3];
        for (int i = 0; i < points.Length - 2; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        Mesh mesh = new Mesh { name = "RegionSelectionMesh" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        child.AddComponent<MeshFilter>().sharedMesh = mesh;
        m_selectionFill = child.AddComponent<MeshRenderer>();
        Material fillMaterial = new Material(Shader.Find("Sprites/Default"));
        fillMaterial.color = new Color(0.22f, 0.24f, 0.26f, 0.42f);
        m_selectionFill.material = fillMaterial;
        m_selectionFill.sortingOrder = 90;
        SetSelectionFillVisible(false);
    }

    LineRenderer CreateVisualLine(string objectName, float width, int order, Material material)
    {
        GameObject child = new GameObject(objectName);
        child.transform.SetParent(transform, false);
        LineRenderer line = child.AddComponent<LineRenderer>();
        ConfigureLine(line, width, order);
        line.useWorldSpace = true;
        line.material = material;
        return line;
    }

    static void ConfigureLine(LineRenderer line, float width, int order)
    {
        line.startWidth = width;
        line.endWidth = width;
        line.numCornerVertices = 3;
        line.numCapVertices = 3;
        line.sortingOrder = order;
    }

    void UpdateDragVisual(Vector3 end, bool valid)
    {
        if (m_path == null) return;
        Vector3 start = m_dragStartWorld;
        start.z = end.z;
        Vector3 delta = end - start;
        Vector3 direction = delta.sqrMagnitude > 0.001f ? delta.normalized : Vector3.down;
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f);

        m_path.positionCount = 2;
        m_path.SetPosition(0, start);
        m_path.SetPosition(1, end - direction * 0.18f);
        m_dragMaterial.mainTextureScale = new Vector2(Mathf.Max(1f, delta.magnitude * 7f), 1f);

        m_arrow.positionCount = 3;
        m_arrow.SetPosition(0, end - direction * 0.42f + perpendicular * 0.24f);
        m_arrow.SetPosition(1, end);
        m_arrow.SetPosition(2, end - direction * 0.42f - perpendicular * 0.24f);

        const int segments = 32;
        m_targetRing.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            m_targetRing.SetPosition(i, end + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.23f);
        }
        Color color = valid ? ValidTargetColor : Color.white;
        m_targetRing.startColor = m_targetRing.endColor = color;
    }

    void SetDragVisualVisible(bool visible)
    {
        if (m_path != null) m_path.enabled = visible;
        if (m_arrow != null) m_arrow.enabled = visible;
        if (m_targetRing != null) m_targetRing.enabled = visible;
    }

    void SetSelectionFillVisible(bool visible)
    {
        if (m_selectionFill != null) m_selectionFill.enabled = visible;
    }

    void SetHighlight(Color color, bool visible)
    {
        if (m_outline == null) return;
        m_outline.startColor = color;
        m_outline.endColor = color;
        m_outline.enabled = visible;
    }
}
