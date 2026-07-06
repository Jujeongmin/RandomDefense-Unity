using UnityEngine;

public class Character : MonoBehaviour
{
    /// <summary>
    /// 컨트롤러
    /// </summary>
    ParentsController m_controller;

    void Awake()
    {
        CacheController();
    }

    void Start()
    {
        // Pooled objects can receive their controller after Character.Awake.
        CacheController();
    }

    void Update()
    {
        if (m_controller != null && m_controller.enabled)
            m_controller.Tick();
    }

    public void CacheController()
    {
        if (m_controller == null)
            TryGetComponent(out m_controller);
    }
}
