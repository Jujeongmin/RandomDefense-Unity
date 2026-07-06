using TMPro;
using UnityEngine;

public class GameHudTimer : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI m_timerText;
    float m_elapsedTime;

    void OnEnable()
    {
        m_elapsedTime = 0f;
        RefreshText();
    }

    void Update()
    {
        m_elapsedTime += Time.deltaTime;
        RefreshText();
    }

    void RefreshText()
    {
        if (m_timerText == null) return;
        int minutes = Mathf.FloorToInt(m_elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(m_elapsedTime % 60f);
        int centiseconds = Mathf.FloorToInt((m_elapsedTime * 100f) % 100f);
        m_timerText.text = $"{minutes:00}:{seconds:00}.{centiseconds:00}";
    }
}
