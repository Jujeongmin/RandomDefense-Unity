using UnityEngine;

/// <summary>
/// 제자리에서 한 번 터지고 풀로 돌아가는 버스트 이펙트.
/// delay를 주면 그만큼 기다렸다가 재생돼서, 경로를 따라 순차적으로 터지는 연출이 가능하다.
/// </summary>
public class PooledBurstEffect : MonoBehaviour
{
    [SerializeField] float m_duration = 1.2f;

    GameObject m_poolKey;
    float m_timer;
    float m_delay;
    bool m_played;

    public void Play(GameObject argPoolKey, float argDelay = 0.0f)
    {
        m_poolKey = argPoolKey;
        m_delay = argDelay;
        m_timer = 0.0f;
        m_played = false;
    }

    void Update()
    {
        m_timer += Time.deltaTime;

        if (!m_played && m_timer >= m_delay)
        {
            m_played = true;
            foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Clear();
                ps.Play();
            }
        }

        if (m_timer >= m_delay + m_duration)
            EffectPool.Despawn(m_poolKey, gameObject);
    }
}
