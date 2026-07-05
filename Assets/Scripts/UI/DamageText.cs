using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    [SerializeField] float m_moveSpeed = 1.0f;
    [SerializeField] float m_lifeTime = 0.5f;

    TMP_Text m_text;
    Color m_color;
    float m_timer;

    void Start()
    {
        EnsureText();
    }

    void EnsureText()
    {
        if (m_text != null) return;

        var parentCanvas = GetComponentInParent<Canvas>();
        m_text = GetComponent<TMP_Text>();
        if (m_text == null)
        {
            if (parentCanvas != null)
            {
                m_text = gameObject.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                m_text = gameObject.AddComponent<TextMeshPro>();
            }
        }

        m_text.alignment = TextAlignmentOptions.Center;
        m_text.fontSize = 4;
        m_color = Color.red;
        m_text.color = m_color;
    }

    public void Setup(int damage)
    {
        EnsureText();
        if (m_text == null) return;

        m_text.text = damage.ToString();
        m_timer = m_lifeTime;
        m_color = Color.red;
        m_color.a = 1.0f;
        m_text.color = m_color;
    }

    public void SetupMessage(string message, float lifeTime = 5.0f, float moveSpeed = 0.6f, Color color = default)
    {
        EnsureText();
        if (m_text == null) return;

        m_text.text = message;
        m_lifeTime = lifeTime;
        m_moveSpeed = moveSpeed;
        m_timer = m_lifeTime;

        if (color.a == 0f) color = Color.white;
        m_color = color;
        m_color.a = 1f;
        m_text.color = m_color;
    }

    void Update()
    {
        transform.position += Vector3.up * m_moveSpeed * Time.deltaTime;
        m_timer -= Time.deltaTime;

        if (m_timer <= 0)
        {
            if (GManager.Instance != null && GManager.Instance.IsPool != null)
            {
                GManager.Instance.IsPool.ReturnDamageText(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        else
        {
            m_color.a = Mathf.Lerp(0, 1, m_timer / m_lifeTime);
            if (m_text != null) m_text.color = m_color;
        }
    }

    public void ResetState()
    {
        m_timer = 0f;
        m_moveSpeed = 1.0f;
        m_lifeTime = 0.5f;
        m_color = Color.white;
        if (m_text != null) m_text.color = m_color;
        if (m_text != null) m_text.text = string.Empty;
    }
}
