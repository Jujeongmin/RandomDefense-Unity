using UnityEngine;

public class Character : MonoBehaviour
{
    /// <summary>
    /// 컨트롤러
    /// </summary>
    ParentsController m_controller;

    void Start()
    {
        // try to find controller on this GameObject (added in GManager.Spawn)
        m_controller = GetComponent<ParentsController>();
    }

    void Update()
    {
        if (m_controller == null) return;
        // let controller handle movement/animation logic per-frame
        m_controller.Tick();
    }
}
