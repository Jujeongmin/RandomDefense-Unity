using System.Collections.Generic;
using UnityEngine;

public class UnitDataManager : MonoBehaviour
{
    /// <summary>
    /// 독립체 데이터 리스트
    /// </summary>
    [SerializeField] List<EntityData> m_entityDataList = null;

    /// <summary>
    /// 데이터취득
    /// </summary>
    /// <param name="argEntityType">독립체 타입</param>
    /// <param name="argEntityIndex">독립체 인덱스</param>
    /// <returns></returns>
    public EntityData Get(EntityType.TYPE argEntityType, int argEntityIndex)
    {
        return m_entityDataList.Find(_data => _data.IsEntityType == argEntityType && _data.IsEntityIndex == argEntityIndex);
    }

    /// <summary>
    /// Returns a list of entity indices for the given entity type and species.
    /// Useful to pick a random appearance index within a species.
    /// </summary>
    public List<int> GetIndicesForSpecies(EntityType.TYPE argEntityType, SpeciesType.TYPE species)
    {
        var list = new List<int>();
        if (m_entityDataList == null) return list;
        foreach (var d in m_entityDataList)
        {
            if (d == null) continue;
            if (d.IsEntityType == argEntityType && d.IsSpeciesType == species)
            {
                list.Add(d.IsEntityIndex);
            }
        }
        return list;
    }

    public EntityData GetRandomForSpecies(EntityType.TYPE entityType, SpeciesType.TYPE species)
    {
        if (m_entityDataList == null) return null;

        EntityData selected = null;
        int matchCount = 0;
        foreach (EntityData data in m_entityDataList)
        {
            if (data == null || data.IsEntityType != entityType || data.IsSpeciesType != species) continue;

            matchCount++;
            // Reservoir sampling avoids allocating a temporary list every wave.
            if (Random.Range(0, matchCount) == 0) selected = data;
        }
        return selected;
    }
}
