using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class SpeedManager : MonoBehaviour
{
    [Header("Speed Settings")]
    [SerializeField] float[] m_speeds = new float[] { 1f, 2f, 3f };
    [SerializeField] int m_currentSpeedIndex = 0;

    [Header("UI Reference")]
    [SerializeField] TextMeshProUGUI m_speedText = null;
    [SerializeField] Button m_speedButton = null; // Button to cycle speed

    public int IsCurrentSpeed { get { return m_currentSpeedIndex; } }

    private void Start()
    {
        if (GManager.Instance != null) GManager.Instance.RegisterSpeedManager(this);
        else Initialize();
    }

    public void Initialize()
    {
        if (m_speedButton != null)
        {
            // 중복 등록 방지: 이미 리스너가 추가되어 있으면 제거한 뒤 추가
            m_speedButton.onClick.RemoveListener(CycleSpeed);
            m_speedButton.onClick.AddListener(CycleSpeed);
        }
        // 인스펙터에서 배열 순서를 바꿨을 수 있으므로 안전하게 오름차순 정렬
        if (m_speeds != null && m_speeds.Length > 1)
        {
            Array.Sort(m_speeds);
        }

        // 인덱스가 범위를 벗어나지 않도록 보정
        if (m_speeds == null || m_speeds.Length == 0)
        {
            m_speeds = new float[] { 1f };
        }
        m_currentSpeedIndex = Mathf.Clamp(m_currentSpeedIndex, 0, m_speeds.Length - 1);
        if (!IsSpeedUnlocked(m_currentSpeedIndex)) m_currentSpeedIndex = 0;

        ApplySpeed();

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) SetSpeedIndex(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) SetSpeedIndex(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) SetSpeedIndex(2);
    }

    public void CycleSpeed()
    {
        int nextIndex = (m_currentSpeedIndex + 1) % m_speeds.Length;
        if (!IsSpeedUnlocked(nextIndex)) nextIndex = 0;
        m_currentSpeedIndex = nextIndex;
        ApplySpeed();
    }

    public void SetSpeedIndex(int index)
    {
        if (index < 0 || index >= m_speeds.Length) return;
        if (!IsSpeedUnlocked(index)) return;
        m_currentSpeedIndex = index;
        ApplySpeed();
    }

    bool IsSpeedUnlocked(int index)
    {
        if (index < 0 || index >= m_speeds.Length) return false;
        if (m_speeds[index] < 3f) return true;

        return GManager.Instance != null
            && GManager.Instance.IsProgress != null
            && GManager.Instance.IsProgress.AdsRemoved;
    }

    void ApplySpeed()
    {
        float targetSpeed = m_speeds[m_currentSpeedIndex];

        // If the game is already paused/ended, do not override the timescale of 0
        if (Time.timeScale > 0.001f || Mathf.Approximately(targetSpeed, 0f))
        {
            Time.timeScale = targetSpeed;
        }

        if (m_speedText != null)
        {
            m_speedText.text = $"{targetSpeed}x";
        }
    }
}
