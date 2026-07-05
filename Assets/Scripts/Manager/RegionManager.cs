using System.Collections.Generic;
using UnityEngine;
using TMPro;

// 직업별 유닛 수를 보관하는 간단한 데이터 구조
public struct UnitTypeCounts
{
    public int Wizards;
    public int Archers;
    public int Warriors;

    public int GetByType(EntityType.TYPE t)
    {
        return t switch
        {
            EntityType.TYPE.Wizard => Wizards,
            EntityType.TYPE.Archer => Archers,
            EntityType.TYPE.Warrior => Warriors,
            _ => 0
        };
    }

    public void Add(UnitTypeCounts o)
    {
        Wizards += o.Wizards; Archers += o.Archers; Warriors += o.Warriors;
    }
}

public class RegionManager : MonoBehaviour
{
    // 삼각형 형태로 배치되는 4개의 지역
    [SerializeField] List<Region> m_regions = new List<Region>(4);

    // 현재 등록된 지역 수 (런타임에서 확인용)
    public int RegionCount { get { return m_regions != null ? m_regions.Count : 0; } }

    private void Start()
    {
        if (GManager.Instance != null) GManager.Instance.RegisterRegionManager(this);
    }

    private void OnDestroy()
    {
        if (GManager.Instance != null && GManager.Instance.IsRegion == this)
        {
            GManager.Instance.RegisterRegionManager(null);
        }
    }

    // Initialize regions. 인스펙터에서 할당된 Region들을 등록하고 부모로 설정합니다.
    // 인스펙터에 지역이 설정되지 않은 경우 자식으로 4개의 지역을 자동 생성합니다.
    public void InitializeRegions()
    {
        // If no regions configured in inspector, auto-create 4 child regions
        if (m_regions == null) m_regions = new List<Region>(4);
        if (m_regions.Count == 0)
        {
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject($"Region_{i}");
                go.transform.SetParent(this.transform, false);
                var reg = go.AddComponent<Region>();
                reg.RegionId = i;
                reg.TriangleIndex = i;
                m_regions.Add(reg);
            }

            return;
        }

        for (int i = 0; i < m_regions.Count; i++)
        {
            var region = m_regions[i];
            if (region == null) continue;
            region.transform.SetParent(this.transform, false);
            if (region.RegionId < 0) region.RegionId = i;
            if (region.TriangleIndex < 0) region.TriangleIndex = i % 4;
        }
    }

    // 포함된 유닛을 풀로 반환하거나 파괴하여 모든 지역을 비웁니다.
    public void ClearAllRegions()
    {
        if (m_regions == null) return;
        for (int i = 0; i < m_regions.Count; i++)
        {
            var region = m_regions[i];
            if (region == null) continue;
            region.ClearRegion();
        }
    }

    public Region GetRegionForType(EntityType.TYPE type)
    {
        // 이미 해당 타입을 포함한 지역이 있는지 확인
        foreach (var r in m_regions)
        {
            if (r.OccupiedType.HasValue && r.OccupiedType.Value == type) return r;
        }

        // 없으면 빈 지역을 반환
        foreach (var r in m_regions)
        {
            if (!r.OccupiedType.HasValue) return r;
        }

        // 사용 가능한 지역이 없으면 null을 반환합니다
        return null;
    }

    public Region GetRegionById(int id)
    {
        return m_regions.Find(r => r.RegionId == id);
    }

    // UI/툴용으로 지역 목록을 반환합니다
    public List<Region> GetRegions()
    {
        return m_regions;
    }

    [Header("Unit Count UI")]
    [SerializeField] TextMeshProUGUI m_wizardCountText = null;
    [SerializeField] TextMeshProUGUI m_archerCountText = null;
    [SerializeField] TextMeshProUGUI m_warriorCountText = null;

    // 모든 지역의 유닛 수를 재계산하고 인스펙터에 연결된 UI를 업데이트합니다.
    public UnitTypeCounts RecalculateAndNotify()
    {
        UnitTypeCounts totals = new UnitTypeCounts();
        if (m_regions != null)
        {
            foreach (var r in m_regions)
            {
                if (r == null) continue;
                totals.Add(r.GetUnitTypeCounts());
            }
        }

        if (m_wizardCountText != null) m_wizardCountText.text = totals.Wizards.ToString();
        if (m_archerCountText != null) m_archerCountText.text = totals.Archers.ToString();
        if (m_warriorCountText != null) m_warriorCountText.text = totals.Warriors.ToString();
        return totals;
    }

    public int GetUnitCountByType(EntityType.TYPE type)
    {
        var t = RecalculateAndNotify();
        return t.GetByType(type);
    }

    // Return total count across all regions for a given class type and rarity
    public int GetCountByClassAndRarity(EntityType.TYPE classType, RarityType.TYPE rarity)
    {
        int total = 0;
        if (m_regions == null) return 0;
        foreach (var r in m_regions)
        {
            if (r == null) continue;
            total += r.GetCountByTypeAndRarity(classType, rarity);
        }
        return total;
    }

    // 주어진 월드 좌표를 포함하는 PolygonCollider2D를 가진 지역을 찾습니다
    public Region FindRegionAtPoint(Vector2 worldPoint)
    {
        foreach (var r in m_regions)
        {
            if (r == null) continue;
            var pc = r.GetComponent<PolygonCollider2D>();
            if (pc == null) continue;
            if (pc.OverlapPoint(worldPoint)) return r;
        }
        return null;
    }

    // 지역을 월드 위치로 이동. 다른 지역과 겹치면 위치를 교환
    public void MoveRegion(int regionId, Vector3 newPosition)
    {
        var region = GetRegionById(regionId);
        if (region == null) return;

        // 새 위치에 다른 지역이 있는지 검사(간단한 거리 체크)
        foreach (var other in m_regions)
        {
            if (other.RegionId == regionId) continue;
            if (Vector3.Distance(other.transform.position, newPosition) < 0.5f)
            {
                // 위치와 내용을 교환합니다
                var tmpPos = other.transform.position;
                other.transform.position = region.transform.position;
                region.transform.position = tmpPos;

                // 내용(유닛)을 서로 교환합니다
                other.SwapContentsWith(region);
                return;
            }
        }

        // 그렇지 않으면 단순히 이동합니다
        region.transform.position = newPosition;
    }
}
