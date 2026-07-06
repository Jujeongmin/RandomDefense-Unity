using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Simple mob controller: moves the mob in a square path inside its Region.
// Also performs periodic "dice rolls" that temporarily change behavior (speed/idle).
public class MobController : ParentsController
{
    [Header("Movement")]
    [SerializeField] float m_speed = 1.2f;

    Coroutine m_moveRoutine;

    public override void Setting(EntityType.TYPE argEntityType, int argEntityIndex)
    {
        base.Setting(argEntityType, argEntityIndex);
        RestartMovement();
    }

    public override void Setting(EntityData data)
    {
        base.Setting(data);
        RestartMovement();
    }

    void OnEnable()
    {
        // apply desired world scale from MobManager
        float scale = (GManager.Instance != null && GManager.Instance.IsMob != null) ? GManager.Instance.IsMob.MobWorldScale : 0.3f;
        SetWorldScale(Vector3.one * scale);

        // safe-start if Setting wasn't called
        if (m_moveRoutine == null) RestartMovement();
        
        // place at first waypoint if available
        if (GManager.Instance != null && GManager.Instance.IsMob != null && 
            GManager.Instance.IsMob.MobWaypoints != null && GManager.Instance.IsMob.MobWaypoints.Count > 0)
        {
            transform.position = GManager.Instance.IsMob.MobWaypoints[0];
        }

        if (GManager.Instance != null && GManager.Instance.IsMob != null)
        {
            GManager.Instance.IsMob.RegisterMob(this);
        }
    }

    void OnDisable()
    {
        if (m_moveRoutine != null) StopCoroutine(m_moveRoutine);
        m_moveRoutine = null;

        if (GManager.Instance != null && GManager.Instance.IsMob != null)
        {
            GManager.Instance.IsMob.UnregisterMob(this);
        }
    }

    void OnTransformParentChanged()
    {
        RestartMovement();
    }

    void RestartMovement()
    {
        if (m_moveRoutine != null) StopCoroutine(m_moveRoutine);
        m_moveRoutine = null;

        // Don't start coroutines on inactive GameObjects or disabled components
        if (!gameObject.activeInHierarchy || !enabled) return;

        m_moveRoutine = StartCoroutine(MoveLoop());
    }

    System.Collections.IEnumerator MoveLoop()
    {
        int idx = 0;
        while (true)
        {
            var waypoints = (GManager.Instance != null && GManager.Instance.IsMob != null) ? GManager.Instance.IsMob.MobWaypoints : null;
            if (waypoints == null || waypoints.Count == 0)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            var target = waypoints[idx % waypoints.Count];
            // set move type and direction
            IsMoveType = MoveType.TYPE.Walk;
            IsDirType = GetDirFromVector(target - transform.position);

            while (Vector3.Distance(transform.position, target) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, m_speed * Time.deltaTime);
                yield return null;
            }

            idx++;
            // small pause at corner
            IsMoveType = MoveType.TYPE.Idle;
            yield return new WaitForSeconds(0.25f);
        }
    }

    System.Collections.IEnumerator DiceLoop()
    {
        // dice behavior removed for automatic mobs
        yield break;
    }

}
