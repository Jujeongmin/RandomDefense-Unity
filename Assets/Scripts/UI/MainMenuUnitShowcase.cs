using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메인화면의 빈 Mid 영역을 유닛으로 채웁니다.
/// 메인화면에 들어올 때마다 직업·등급 조합이 랜덤으로 바뀌어 "운빨 랜덤" 컨셉을 드러냅니다.
/// 유닛은 정면 idle 포즈로 가만히 서 있습니다 (애니메이션 없음).
/// 스프라이트 목록은 에디터(MainScreenDecorator)에서 채워 넣습니다.
/// </summary>
public class MainMenuUnitShowcase : MonoBehaviour
{
    /// <summary>한 유닛의 정면 idle 포즈.</summary>
    [System.Serializable]
    public class UnitLook
    {
        public string Name;
        public Sprite Pose;
    }

    [Tooltip("직업×등급 조합별 정면 포즈 (에디터 빌더가 채웁니다)")]
    [SerializeField] List<UnitLook> m_looks = new List<UnitLook>();

    [Tooltip("유닛이 설 자리 (좌 -> 우)")]
    [SerializeField] Image[] m_slots;

    void Awake()
    {
        if (m_slots == null || m_slots.Length == 0) return;
        if (m_looks == null || m_looks.Count == 0) return;

        // 같은 조합이 두 번 나오지 않도록 후보를 복사해 뽑는다
        List<UnitLook> pool = new List<UnitLook>(m_looks);
        foreach (Image slot in m_slots)
        {
            if (slot == null) continue;

            if (pool.Count == 0) pool.AddRange(m_looks);
            int index = Random.Range(0, pool.Count);
            UnitLook look = pool[index];
            pool.RemoveAt(index);

            slot.enabled = look != null && look.Pose != null;
            if (slot.enabled) slot.sprite = look.Pose;
        }
    }
}
