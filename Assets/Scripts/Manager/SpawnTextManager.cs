using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SpawnTextManager : MonoBehaviour
{
    [System.Serializable]
    public class PreplacedSlot
    {
        public TextMeshProUGUI m_textUI; // 선택된 UI 텍스트 (월드 스페이스 캔버스 권장)
        public Transform m_transform; // 위치를 기준으로 사용할 대상 Transform
        public float m_lifeTime = 10f; // 기본 지속 시간 (초)
        [System.NonSerialized] public Coroutine runningCoroutine = null;
        [System.NonSerialized] public Vector3 targetPos;
        [System.NonSerialized] public Vector3 initialPos;
        [System.NonSerialized] public Vector3 defaultStartPos; // 시작 시의 원래 위치를 캐시
    }

    [Header("Normal Spawn Settings")]
    [SerializeField] PreplacedSlot[] m_slots = null;

    [Header("신화(특수) 알림 UI")]
    [SerializeField] TextMeshProUGUI m_mythicNoticeText = null;
    [SerializeField] float m_mythicNoticeDuration = 30f;
    [SerializeField] float m_flashSpeed = 6f; // 깜박임 주기

    private Coroutine m_mythicNoticeCoroutine = null;

    // 활성화된 슬롯을 순서대로 추적합니다 (선입선출)
    private List<PreplacedSlot> m_activeSlots = new List<PreplacedSlot>();

    private void Start()
    {
        if (GManager.Instance != null) GManager.Instance.RegisterSpawnTextManager(this);
        else Initialize();
    }

    public void Initialize()
    {
        m_activeSlots.Clear();

        if (m_slots != null)
        {
            foreach (var s in m_slots)
            {
                if (s == null) continue;
                if (s.m_textUI != null)
                {
                    s.m_textUI.gameObject.SetActive(false);
                    // 이동 애니메이션이 원위치를 손상시키지 않도록 시작 위치를 캐시합니다
                    s.defaultStartPos = s.m_transform != null ? s.m_transform.position : s.m_textUI.transform.position;
                }
            }
        }

        if (m_mythicNoticeText != null)
        {
            m_mythicNoticeText.gameObject.SetActive(false);
        }
    }

    public void Show(string message, Vector3 worldPos, float lifeTime = 10f)
    {
        if (m_slots == null || m_slots.Length == 0)
        {
            return;
        }

        // 기존 슬롯들의 내용을 한 칸씩 위로 이동시킵니다 (마지막 슬롯은 이전 슬롯의 내용을 받음)
        int n = m_slots.Length;
        for (int i = n - 1; i >= 1; i--)
        {
            var dst = m_slots[i];
            var src = m_slots[i - 1];
            if (dst == null) continue;

            // 덮어쓰기 전에 대상 슬롯에서 실행 중인 코루틴이 있으면 중지
            if (dst.runningCoroutine != null)
            {
                StopCoroutine(dst.runningCoroutine);
                dst.runningCoroutine = null;
            }

            if (src != null && src.m_textUI != null && src.m_textUI.gameObject.activeInHierarchy)
            {
                // 원본 슬롯의 코루틴을 중지하고 대상 슬롯에서 새 코루틴을 시작함
                if (src.runningCoroutine != null)
                {
                    StopCoroutine(src.runningCoroutine);
                    src.runningCoroutine = null;
                }

                if (dst.m_textUI != null)
                {
                    // 텍스트 복사
                    dst.m_textUI.text = src.m_textUI.text;

                    // 대상 슬롯의 기준 위치 결정 (Transform이 있으면 캐시된 기본 위치 사용)
                    Vector3 dstPos = dst.m_transform != null ? dst.defaultStartPos : dst.m_textUI.transform.position;
                    dst.targetPos = dstPos;
                    dst.initialPos = dstPos;
                    dst.m_textUI.transform.position = dstPos;
                    dst.m_textUI.gameObject.SetActive(true);

                    // 대상 슬롯에서 코루틴을 재시작 (슬롯의 설정된 lifeTime 또는 전달된 lifeTime 사용)
                    float lt = (dst.m_lifeTime > 0f) ? dst.m_lifeTime : lifeTime;
                    dst.runningCoroutine = StartCoroutine(AnimateAndHideUI(dst, lt));

                    // maintain active list: remove source and add destination
                    m_activeSlots.Remove(src);
                    if (!m_activeSlots.Contains(dst)) m_activeSlots.Add(dst);
                }

                // 원본 슬롯은 즉시 숨김
                if (src.m_textUI != null)
                {
                    src.m_textUI.gameObject.SetActive(false);
                }
                src.runningCoroutine = null;
            }
            else
            {
                // 원본 내용이 없으면 대상은 숨겨둠
                if (dst.m_textUI != null) dst.m_textUI.gameObject.SetActive(false);
                dst.runningCoroutine = null;
                m_activeSlots.Remove(dst);
            }
        }

        var first = m_slots[0];
        if (first != null && first.m_textUI != null)
        {
            if (first.runningCoroutine != null)
            {
                StopCoroutine(first.runningCoroutine);
                first.runningCoroutine = null;
            }

            first.m_textUI.text = message;
            Vector3 pos0 = first.m_transform != null ? first.defaultStartPos : worldPos;
            first.targetPos = pos0;
            first.initialPos = pos0;
            first.m_textUI.transform.position = pos0;
            first.m_textUI.gameObject.SetActive(true);

            float lt0 = (first.m_lifeTime > 0f) ? first.m_lifeTime : lifeTime;
            first.runningCoroutine = StartCoroutine(AnimateAndHideUI(first, lt0));

            // 활성 슬롯 목록에 반영
            if (!m_activeSlots.Contains(first)) m_activeSlots.Add(first);
        }
    }

    private IEnumerator AnimateAndHideUI(PreplacedSlot slot, float life)
    {
        float timer = life;
        while (timer > 0f)
        {
            // 슬롯 또는 UI가 외부에서 파괴되었으면 안전하게 종료
            if (slot == null || slot.m_textUI == null)
            {
                yield break;
            }

            slot.m_textUI.transform.position = Vector3.Lerp(slot.m_textUI.transform.position, slot.targetPos, Time.deltaTime * 8f);
            timer -= Time.deltaTime;
            yield return null;
        }

        if (slot != null && slot.m_textUI != null)
        {
            slot.m_textUI.gameObject.SetActive(false);
        }
        if (slot != null)
        {
            m_activeSlots.Remove(slot);
            slot.runningCoroutine = null;
        }
    }

    void OnDisable()
    {
        if (m_slots != null)
        {
            foreach (var s in m_slots)
            {
                if (s == null) continue;
                if (s.runningCoroutine != null)
                {
                    try { StopCoroutine(s.runningCoroutine); } catch { }
                    s.runningCoroutine = null;
                }
            }
        }
        if (m_mythicNoticeCoroutine != null)
        {
            try { StopCoroutine(m_mythicNoticeCoroutine); } catch { }
            m_mythicNoticeCoroutine = null;
        }
    }

    public void ShowMythicNotice(string message)
    {
        if (m_mythicNoticeText == null) return;
        if (m_mythicNoticeCoroutine != null) StopCoroutine(m_mythicNoticeCoroutine);
        m_mythicNoticeCoroutine = StartCoroutine(MythicNoticeRoutine(message));
    }

    private IEnumerator MythicNoticeRoutine(string message)
    {
        m_mythicNoticeText.text = message;
        m_mythicNoticeText.gameObject.SetActive(true);

        float timer = m_mythicNoticeDuration;
        Color baseColor = m_mythicNoticeText.color;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            float alpha = 0.65f + Mathf.Sin(Time.time * m_flashSpeed) * 0.35f;
            m_mythicNoticeText.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            yield return null;
        }

        m_mythicNoticeText.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
        m_mythicNoticeText.gameObject.SetActive(false);
        m_mythicNoticeCoroutine = null;
    }
}
