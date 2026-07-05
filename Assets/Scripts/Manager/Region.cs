using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Region : MonoBehaviour
{
    public int RegionId { get; set; } = -1;

    // 이 지역에 허용된 직업 타입 (한 지역에 한 직업만)
    public EntityType.TYPE? OccupiedType { get; private set; } = null;

    List<GameObject> m_units = new List<GameObject>();

    [Header("Triangle Layout")]
    [SerializeField] float m_triangleBase = 2f;
    [SerializeField] float m_triangleHeight = 2f;
    [Tooltip("-1 = use all four sub-triangles randomly. 0=top,1=bottom,2=left,3=right")]
    [SerializeField] int m_triangleIndex = -1;
    public int TriangleIndex { get { return m_triangleIndex; } set { m_triangleIndex = value; } }
    [Header("Unit Orientation")]
    [Tooltip("월드 Z 회전(도) - 이 값을 유닛의 월드 회전으로 설정합니다.")]
    [SerializeField] float m_unitRotationDeg = 0f;

    [Tooltip("스폰 시 유닛의 목표 월드 스케일")] 
    [SerializeField] float m_desiredWorldScale = 0.3f;

    // collider reference (optional)
    PolygonCollider2D m_polygonCollider;

    void OnValidate()
    {
        // keep collider shape in sync in editor when properties change
        EnsurePolygonCollider();
        UpdateColliderShape();
    }

    void Start()
    {
        EnsurePolygonCollider();
        UpdateColliderShape();
    }

    void EnsurePolygonCollider()
    {
        if (m_polygonCollider == null)
            m_polygonCollider = GetComponent<PolygonCollider2D>();
        if (m_polygonCollider == null)
            m_polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
        m_polygonCollider.isTrigger = true;
    }

    void UpdateColliderShape()
    {
        if (m_polygonCollider == null) return;

        float halfW = m_triangleBase * 0.5f;
        float halfH = m_triangleHeight * 0.5f;

        if (m_triangleIndex >= 0 && m_triangleIndex <= 3)
        {
            // triangle polygon (3 points)
            var verts = GetTriangleVerticesLocal(m_triangleIndex);
            Vector2[] path = new Vector2[3];
            for (int i = 0; i < 3; i++) path[i] = verts[i];
            m_polygonCollider.pathCount = 1;
            m_polygonCollider.SetPath(0, path);
        }
        else
        {
            // full rectangle
            Vector2[] rect = new Vector2[4]
            {
                new Vector2(-halfW, -halfH),
                new Vector2(halfW, -halfH),
                new Vector2(halfW, halfH),
                new Vector2(-halfW, halfH)
            };
            m_polygonCollider.pathCount = 1;
            m_polygonCollider.SetPath(0, rect);
        }
    }

    // 이 지역에 유닛 추가. 타입이 없으면 설정. 다른 타입이면 교환.
    public void AddUnit(GameObject unit)
    {
        if (unit == null) return;

        // 이전 부모 저장(스왑 시 사용)
        var incomingPrevParent = unit.transform.parent;
        // 원하는 월드 스케일 보존을 위해 들어오는 유닛의 현재 월드 스케일을 저장
        Vector3 desiredWorldScaleIncoming = unit.transform.lossyScale;
        var charComp = unit.GetComponent<Character>();
        if (charComp == null)
        {
            // Character 컴포넌트 없으면 그냥 부모화
            unit.transform.SetParent(transform, false);
            // 유닛 월드 회전을 Region에 맞춤
            unit.transform.rotation = Quaternion.Euler(0f, 0f, m_unitRotationDeg);
            unit.transform.localRotation = Quaternion.identity;
            // set desired world scale
            var pc = unit.GetComponent<ParentsController>();
            if (pc != null) pc.SetWorldScale(Vector3.one * m_desiredWorldScale);
            else unit.transform.localScale = ComputeLocalScaleForDesiredWorld(Vector3.one * m_desiredWorldScale, transform);
            m_units.Add(unit);
            // notify region manager about count change
            if (GManager.Instance != null && GManager.Instance.IsRegion != null)
                GManager.Instance.IsRegion.RecalculateAndNotify();
            return;
        }

        var goType = GetEntityTypeFromGameObject(unit);

        if (!OccupiedType.HasValue)
        {
            OccupiedType = goType;
            unit.transform.SetParent(transform, false);
            // 유닛 월드 회전을 Region에 맞춤
            unit.transform.rotation = Quaternion.Euler(0f, 0f, m_unitRotationDeg);
            unit.transform.localRotation = Quaternion.identity;
            // set desired world scale
            var pc2 = unit.GetComponent<ParentsController>();
            if (pc2 != null) pc2.SetWorldScale(Vector3.one * m_desiredWorldScale);
            else unit.transform.localScale = ComputeLocalScaleForDesiredWorld(Vector3.one * m_desiredWorldScale, transform);
            PlaceUnitInGrid(unit);
            m_units.Add(unit);
            if (GManager.Instance != null && GManager.Instance.IsRegion != null)
                GManager.Instance.IsRegion.RecalculateAndNotify();
            return;
        }

        if (OccupiedType.HasValue && OccupiedType.Value == goType)
        {
            // 같은 타입이면 추가
            unit.transform.SetParent(transform, false);
            // 유닛 월드 회전 및 스케일을 Region에 맞춤
            unit.transform.rotation = Quaternion.Euler(0f, 0f, m_unitRotationDeg);
            unit.transform.localRotation = Quaternion.identity;
            // set desired world scale
            var pc3 = unit.GetComponent<ParentsController>();
            if (pc3 != null) pc3.SetWorldScale(Vector3.one * m_desiredWorldScale);
            else unit.transform.localScale = ComputeLocalScaleForDesiredWorld(Vector3.one * m_desiredWorldScale, transform);
            PlaceUnitInGrid(unit);
            m_units.Add(unit);
            if (GManager.Instance != null && GManager.Instance.IsRegion != null)
                GManager.Instance.IsRegion.RecalculateAndNotify();
            return;
        }

        // 다른 타입이면 첫 번째 유닛과 위치 교환
        if (m_units.Count > 0)
        {
            var other = m_units[0];
            var otherParent = other.transform.parent;
            // 다른 유닛을 들어온 유닛의 이전 부모로 옮김
            Vector3 desiredWorldScaleOther = other.transform.lossyScale;
            other.transform.SetParent(incomingPrevParent, false);
            // other가 부모 밖으로 나가면 회전은 그대로 두고 로컬스케일만 조정
            other.transform.localRotation = Quaternion.identity;

            unit.transform.SetParent(this.transform, false);
            unit.transform.rotation = Quaternion.Euler(0f, 0f, m_unitRotationDeg);
            unit.transform.localRotation = Quaternion.identity;
            // set desired world scale for incoming unit
            var pc4 = unit.GetComponent<ParentsController>();
            if (pc4 != null) pc4.SetWorldScale(Vector3.one * m_desiredWorldScale);
            else unit.transform.localScale = ComputeLocalScaleForDesiredWorld(Vector3.one * m_desiredWorldScale, transform);

            // update lists
            m_units.RemoveAt(0);
            m_units.Add(unit);
            if (GManager.Instance != null && GManager.Instance.IsRegion != null)
                GManager.Instance.IsRegion.RecalculateAndNotify();
            OccupiedType = goType;
            PlaceUnitInGrid(unit);
        }
        else
        {
            // 유닛이 없는데 OccupiedType이 잘못되어 있는 경우(정상적이지 않음). 초기화
            m_units.Clear();
            OccupiedType = goType;
            unit.transform.SetParent(transform, false);
            unit.transform.rotation = Quaternion.identity;
            unit.transform.localRotation = Quaternion.identity;
            m_units.Add(unit);
            if (GManager.Instance != null && GManager.Instance.IsRegion != null)
                GManager.Instance.IsRegion.RecalculateAndNotify();
            PlaceUnitInGrid(unit);
        }
    }

    public GameObject GetUnitAtSlot(int slot)
    {
        if (slot < 0 || slot >= m_units.Count) return null;
        return m_units[slot];
    }

    public int UnitCount => m_units.Count;

    // Move a unit at slot index from this region to target region
    public bool MoveUnitToRegion(int slot, Region target)
    {
        var u = GetUnitAtSlot(slot);
        if (u == null || target == null) return false;
        // remove from this region list (but do not destroy)
        bool removed = RemoveUnit(u);
        // Add to target regardless of removal result
        target.AddUnit(u);
        return true;
    }

    void PlaceUnitInGrid(GameObject unit)
    {
        // 삼각형 영역 내부에 랜덤 위치 샘플링 (유닛 겹침 허용)
        Vector3 p = SamplePointInTriangleLocal();
        unit.transform.localPosition = p;
    }

    Vector3 SamplePointInTriangleLocal()
    {
        float halfW = m_triangleBase * 0.5f;
        float halfH = m_triangleHeight * 0.5f;

        Vector3 center = new Vector3(0f, 0f, 0f);
        Vector3 bl = new Vector3(-halfW, -halfH, 0f);
        Vector3 br = new Vector3(halfW, -halfH, 0f);
        Vector3 tl = new Vector3(-halfW, halfH, 0f);
        Vector3 tr = new Vector3(halfW, halfH, 0f);

        int tri = (m_triangleIndex >= 0 && m_triangleIndex <= 3) ? m_triangleIndex : Random.Range(0, 4);
        Vector3 A, B, C;
        switch (tri)
        {
            case 0: // top
                A = center; B = tl; C = tr; break;
            case 1: // bottom
                A = center; B = bl; C = br; break;
            case 2: // left
                A = center; B = bl; C = tl; break;
            default: // right
                A = center; B = br; C = tr; break;
        }

        // barycentric random sampling inside triangle A,B,C
        float r1 = Random.value;
        float r2 = Random.value;
        if (r1 + r2 > 1f)
        {
            r1 = 1f - r1;
            r2 = 1f - r2;
        }
        Vector3 p = A + r1 * (B - A) + r2 * (C - A);
        return p;
    }

    static float SafeDivide(float a, float b)
    {
        if (Mathf.Approximately(b, 0f)) return 1f;
        return a / b;
    }

    Vector3 ComputeLocalScaleForDesiredWorld(Vector3 desiredWorldScale, Transform parentTransform)
    {
        if (parentTransform == null) return desiredWorldScale;
        Vector3 pLossy = parentTransform.lossyScale;
        return new Vector3(SafeDivide(desiredWorldScale.x, pLossy.x), SafeDivide(desiredWorldScale.y, pLossy.y), SafeDivide(desiredWorldScale.z, pLossy.z));
    }

    EntityType.TYPE GetEntityTypeFromGameObject(GameObject go)
    {
        // 컨트롤러로 타입 유추 시도
        var parent = go.GetComponent<ParentsController>();
        if (parent != null)
        {
            // 직접 속성이 없으면 이름으로 판별
            if (go.name.Contains("Wizard")) return EntityType.TYPE.Wizard;
            if (go.name.Contains("Archer")) return EntityType.TYPE.Archer;
            if (go.name.Contains("Warrior")) return EntityType.TYPE.Warrior;
        }
        // 기본값
        return EntityType.TYPE.Wizard;
    }

    // 지역의 모든 유닛 제거
    public void ClearRegion()
    {
        foreach (var u in m_units)
        {
            if (u != null)
            {
                if (GManager.Instance != null && GManager.Instance.IsPool != null)
                {
                    GManager.Instance.IsPool.ReturnUnit(u);
                }
                else
                {
                    Destroy(u);
                }
            }
        }
        m_units.Clear();
        OccupiedType = null;
    }

    // 지역 간 내용(유닛) 전체를 교환합니다. 월드 스케일/회전은 가능한 보존합니다.
    public void SwapContentsWith(Region other)
    {
        // perform animated swap so units walk to their new regions
        if (other == null) return;
        // start coroutine to animate swap
        StartCoroutine(SwapContentsCoroutine(other));
    }

    System.Collections.IEnumerator SwapContentsCoroutine(Region other)
    {
        if (other == null) yield break;

        var myUnits = new List<GameObject>(m_units);
        var otherUnits = new List<GameObject>(other.m_units);

        // Clear both lists to prepare for reassigning after animation
        m_units.Clear();
        other.m_units.Clear();

        float duration = 0.6f;

        // compute targets
        var myTargets = new List<Vector3>();
        foreach (var u in myUnits)
        {
            if (u == null) { myTargets.Add(Vector3.zero); continue; }
            Vector3 local = other.SamplePointInTriangleLocal();
            myTargets.Add(other.transform.TransformPoint(local));
        }

        var otherTargets = new List<Vector3>();
        foreach (var u in otherUnits)
        {
            if (u == null) { otherTargets.Add(Vector3.zero); continue; }
            Vector3 local = SamplePointInTriangleLocal();
            otherTargets.Add(transform.TransformPoint(local));
        }

        // capture starts
        var myStarts = new List<Vector3>();
        foreach (var u in myUnits) myStarts.Add(u != null ? u.transform.position : Vector3.zero);
        var otherStarts = new List<Vector3>();
        foreach (var u in otherUnits) otherStarts.Add(u != null ? u.transform.position : Vector3.zero);

        float t = 0f;
        // set controllers to Walk and compute direction for animation
        for (int i = 0; i < myUnits.Count; i++)
        {
            var u = myUnits[i];
            if (u == null) continue;
            var pc = u.GetComponent<ParentsController>();
            if (pc != null)
            {
                pc.IsMoveType = MoveType.TYPE.Walk;
                // compute direction from start to target
                Vector3 delta = myTargets[i] - myStarts[i];
                pc.IsDirType = GetDirFromVector(delta);
            }
        }

        for (int i = 0; i < otherUnits.Count; i++)
        {
            var u = otherUnits[i];
            if (u == null) continue;
            var pc = u.GetComponent<ParentsController>();
            if (pc != null)
            {
                pc.IsMoveType = MoveType.TYPE.Walk;
                Vector3 delta = otherTargets[i] - otherStarts[i];
                pc.IsDirType = GetDirFromVector(delta);
            }
        }

        while (t < duration)
        {
            float f = t / duration;
            for (int i = 0; i < myUnits.Count; i++)
            {
                var u = myUnits[i];
                if (u == null) continue;
                u.transform.position = Vector3.Lerp(myStarts[i], myTargets[i], f);
            }
            for (int i = 0; i < otherUnits.Count; i++)
            {
                var u = otherUnits[i];
                if (u == null) continue;
                u.transform.position = Vector3.Lerp(otherStarts[i], otherTargets[i], f);
            }
            t += Time.deltaTime;
            yield return null;
        }

        // finalize positions and reparent, apply rotation/scale, set idle
        for (int i = 0; i < myUnits.Count; i++)
        {
            var u = myUnits[i];
            if (u == null) continue;
            u.transform.position = myTargets[i];
            u.transform.SetParent(other.transform, true);
            u.transform.rotation = Quaternion.Euler(0f, 0f, other.m_unitRotationDeg);
            var pc = u.GetComponent<ParentsController>();
            if (pc != null) pc.SetWorldScale(Vector3.one * other.m_desiredWorldScale);
            else u.transform.localScale = ComputeLocalScaleForDesiredWorld(Vector3.one * other.m_desiredWorldScale, other.transform);
            if (pc != null) pc.IsMoveType = MoveType.TYPE.Idle;
            other.m_units.Add(u);
        }

        for (int i = 0; i < otherUnits.Count; i++)
        {
            var u = otherUnits[i];
            if (u == null) continue;
            u.transform.position = otherTargets[i];
            u.transform.SetParent(this.transform, true);
            u.transform.rotation = Quaternion.Euler(0f, 0f, m_unitRotationDeg);
            var pc = u.GetComponent<ParentsController>();
            if (pc != null) pc.SetWorldScale(Vector3.one * m_desiredWorldScale);
            else u.transform.localScale = ComputeLocalScaleForDesiredWorld(Vector3.one * m_desiredWorldScale, this.transform);
            if (pc != null) pc.IsMoveType = MoveType.TYPE.Idle;
            m_units.Add(u);
        }

        // swap occupied types
        var tmp = this.OccupiedType;
        this.OccupiedType = other.OccupiedType;
        other.OccupiedType = tmp;

        yield break;
    }

    // derive cardinal direction from vector for animation selection
    DirType.TYPE GetDirFromVector(Vector3 v)
    {
        if (v.magnitude < 0.001f) return DirType.TYPE.Down;
        // project to X/Y plane
        Vector2 d = new Vector2(v.x, v.y).normalized;
        if (Mathf.Abs(d.x) > Mathf.Abs(d.y))
        {
            return d.x > 0 ? DirType.TYPE.Right : DirType.TYPE.Left;
        }
        else
        {
            return d.y > 0 ? DirType.TYPE.Up : DirType.TYPE.Down;
        }
    }

    // If using a 2D PolygonCollider set as trigger on this Region, these handlers
    // will add/remove units automatically when they enter/exit the region area.
    void OnTriggerEnter2D(Collider2D col)
    {
        var unitGo = ResolveUnitFromCollider(col);
        if (unitGo == null) return;
        // avoid double-adding
        if (m_units.Contains(unitGo)) return;
        AddUnit(unitGo);
    }

    void OnTriggerExit2D(Collider2D col)
    {
        var unitGo = ResolveUnitFromCollider(col);
        if (unitGo == null) return;
        RemoveUnit(unitGo);
    }

    GameObject ResolveUnitFromCollider(Collider2D col)
    {
        if (col == null) return null;
        // prefer the GameObject that has a Character or ParentsController component
        var go = col.gameObject;
        var charComp = go.GetComponentInParent<Character>();
        if (charComp != null) return charComp.gameObject;
        var parentCtrl = go.GetComponentInParent<ParentsController>();
        if (parentCtrl != null) return parentCtrl.gameObject;
        return go;
    }

    // Remove a unit from this region (called on trigger exit or explicit removal)
    public bool RemoveUnit(GameObject unit)
    {
        if (unit == null) return false;
        bool removed = m_units.Remove(unit);
        if (!removed) return false;

        // detach from this region to world root while preserving world transform
        if (unit.transform.parent == this.transform)
        {
            unit.transform.SetParent(null, true);
        }

        if (m_units.Count == 0)
        {
            OccupiedType = null;
        }

        if (GManager.Instance != null && GManager.Instance.IsRegion != null)
            GManager.Instance.IsRegion.RecalculateAndNotify();

        return true;
    }

    /// <summary>
    /// 해당 등급의 유닛을 찾아 리스트에서 제거하고 반환합니다.
    /// </summary>
    public GameObject FindAndRemoveUnitByRarity(RarityType.TYPE rarity)
    {
        for (int i = 0; i < m_units.Count; i++)
        {
            var unitGo = m_units[i];
            if (unitGo == null) continue;
            var controller = unitGo.GetComponent<ParentsController>();
            if (controller != null && controller.IsRarity == rarity)
            {
                m_units.RemoveAt(i);
                if (m_units.Count == 0) OccupiedType = null;
                if (GManager.Instance != null && GManager.Instance.IsRegion != null)
                    GManager.Instance.IsRegion.RecalculateAndNotify();
                
                // 부모 해제
                unitGo.transform.SetParent(null, true);
                
                return unitGo;
            }
        }
        return null;
    }

    // Return per-type counts for this region
    public UnitTypeCounts GetUnitTypeCounts()
    {
        UnitTypeCounts counts = new UnitTypeCounts();
        foreach (var u in m_units)
        {
            if (u == null) continue;
            var pc = u.GetComponent<ParentsController>();
            if (pc == null) continue;
            switch (pc.IsData != null ? pc.IsData.IsEntityType : EntityType.TYPE.Wizard)
            {
                case EntityType.TYPE.Wizard: counts.Wizards++; break;
                case EntityType.TYPE.Archer: counts.Archers++; break;
                case EntityType.TYPE.Warrior: counts.Warriors++; break;
            }
        }
        return counts;
    }

    // Return count of units in this region matching given class type and rarity
    public int GetCountByTypeAndRarity(EntityType.TYPE classType, RarityType.TYPE rarity)
    {
        int cnt = 0;
        foreach (var u in m_units)
        {
            if (u == null) continue;
            var pc = u.GetComponent<ParentsController>();
            if (pc == null) continue;
            var ut = pc.IsData != null ? pc.IsData.IsEntityType : EntityType.TYPE.Wizard;
            if (ut == classType && pc.IsRarity == rarity) cnt++;
        }
        return cnt;
    }

    // Editor visualization removed. Use the helper below to get triangle vertices (local space)
    // The rectangle (width = m_triangleBase, height = m_triangleHeight) is split into 4
    // triangles around the center: top(0), bottom(1), left(2), right(3).
    public Vector3[] GetTriangleVerticesLocal(int tri)
    {
        float halfW = m_triangleBase * 0.5f;
        float halfH = m_triangleHeight * 0.5f;
        Vector3 center = new Vector3(0f, 0f, 0f);
        Vector3 bl = new Vector3(-halfW, -halfH, 0f);
        Vector3 br = new Vector3(halfW, -halfH, 0f);
        Vector3 tl = new Vector3(-halfW, halfH, 0f);
        Vector3 tr = new Vector3(halfW, halfH, 0f);

        switch (tri)
        {
            case 0: // top
                return new Vector3[] { center, tl, tr };
            case 1: // bottom
                return new Vector3[] { center, bl, br };
            case 2: // left
                return new Vector3[] { center, bl, tl };
            default: // right
                return new Vector3[] { center, br, tr };
        }
    }
}
