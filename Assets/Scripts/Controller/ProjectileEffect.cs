using UnityEngine;

/// <summary>
/// 유닛 공격 발사체. 대상을 향해 유도 비행하다가 도달하면 풀로 반환된다.
/// 데미지는 발사 시점에 이미 적용되므로 순수 연출용.
/// </summary>
public class ProjectileEffect : MonoBehaviour
{
    [SerializeField] float m_speed = 15f;
    [SerializeField] float m_hitDistance = 0.15f;
    [SerializeField] float m_maxLifetime = 1.5f;

    GameObject m_poolKey;
    Transform m_target;
    Vector3 m_targetPos;
    float m_age;

    public void Launch(GameObject argPoolKey, Transform argTarget)
    {
        m_poolKey = argPoolKey;
        m_target = argTarget;
        if (argTarget != null) m_targetPos = argTarget.position;
        m_age = 0f;
        FaceTarget();

        // 풀 재사용 시 이전 재생 흔적(파티클/트레일) 제거 후 재시작
        foreach (TrailRenderer trail in GetComponentsInChildren<TrailRenderer>(true))
            trail.Clear();
        foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Clear();
            ps.Play();
        }
    }

    void Update()
    {
        m_age += Time.deltaTime;
        if (m_target != null) m_targetPos = m_target.position;

        Vector3 to = m_targetPos - transform.position;
        to.z = 0.0f;
        float step = m_speed * Time.deltaTime;

        if (to.magnitude <= Mathf.Max(step, m_hitDistance) || m_age >= m_maxLifetime)
        {
            EffectPool.Despawn(m_poolKey, gameObject);
            return;
        }

        transform.position += to.normalized * step;
        FaceTarget();
    }

    void FaceTarget()
    {
        Vector3 dir = m_targetPos - transform.position;
        if (dir.sqrMagnitude < 0.0001f) return;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0.0f, 0.0f, angle);
    }
}
