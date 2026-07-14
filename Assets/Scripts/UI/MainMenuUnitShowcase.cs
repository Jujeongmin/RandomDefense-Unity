using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메인화면의 빈 Mid 영역을 유닛으로 채웁니다.
/// 메인화면에 들어올 때마다 직업·등급 조합이 랜덤으로 바뀌어 "운빨 랜덤" 컨셉을 드러냅니다.
/// 스프라이트 목록은 에디터(MainScreenDecorator)에서 채워 넣습니다.
/// </summary>
public class MainMenuUnitShowcase : MonoBehaviour
{
    /// <summary>한 유닛의 정면 idle 프레임들.</summary>
    [System.Serializable]
    public class UnitLook
    {
        public string Name;
        public Sprite[] Frames;
    }

    [Tooltip("직업×등급 조합별 정면 프레임 (에디터 빌더가 채웁니다)")]
    [SerializeField] List<UnitLook> m_looks = new List<UnitLook>();

    [Tooltip("유닛이 설 자리 (좌 -> 우)")]
    [SerializeField] Image[] m_slots;

    [Tooltip("idle 프레임 한 장당 유지 시간(초)")]
    [SerializeField] float m_frameInterval = 0.28f;

    [Tooltip("유닛이 위아래로 떠다니는 진폭(px)")]
    [SerializeField] float m_bobAmplitude = 5f;
    [SerializeField] float m_bobSpeed = 0.6f;

    readonly List<UnitLook> m_picked = new List<UnitLook>();
    Vector2[] m_slotHomes;
    float m_elapsed;

    void Awake()
    {
        if (m_slots == null || m_slots.Length == 0) return;

        m_slotHomes = new Vector2[m_slots.Length];
        for (int i = 0; i < m_slots.Length; i++)
            if (m_slots[i] != null)
                m_slotHomes[i] = m_slots[i].rectTransform.anchoredPosition;

        PickRandomUnits();
    }

    void PickRandomUnits()
    {
        m_picked.Clear();
        if (m_looks == null || m_looks.Count == 0) return;

        // 같은 조합이 두 번 나오지 않도록 후보를 복사해 뽑는다
        List<UnitLook> pool = new List<UnitLook>(m_looks);
        for (int i = 0; i < m_slots.Length; i++)
        {
            if (m_slots[i] == null) continue;

            if (pool.Count == 0) pool.AddRange(m_looks);
            int index = Random.Range(0, pool.Count);
            UnitLook look = pool[index];
            pool.RemoveAt(index);

            m_picked.Add(look);
            m_slots[i].enabled = look != null && look.Frames != null && look.Frames.Length > 0;
            if (m_slots[i].enabled) m_slots[i].sprite = look.Frames[0];
        }
    }

    void Update()
    {
        if (m_slots == null) return;

        m_elapsed += Time.unscaledDeltaTime;

        for (int i = 0; i < m_slots.Length && i < m_picked.Count; i++)
        {
            Image slot = m_slots[i];
            UnitLook look = m_picked[i];
            if (slot == null || look == null || look.Frames == null || look.Frames.Length == 0) continue;

            // 유닛마다 위상을 어긋나게 해서 셋이 동시에 같은 동작을 하지 않도록
            float phase = i * 0.37f;

            if (m_frameInterval > 0f)
            {
                int frame = Mathf.FloorToInt((m_elapsed / m_frameInterval) + phase * 3f) % look.Frames.Length;
                slot.sprite = look.Frames[frame];
            }

            if (m_slotHomes != null && i < m_slotHomes.Length)
            {
                float bob = Mathf.Sin((m_elapsed * m_bobSpeed + phase) * Mathf.PI * 2f) * m_bobAmplitude;
                slot.rectTransform.anchoredPosition = m_slotHomes[i] + new Vector2(0f, bob);
            }
        }
    }
}
