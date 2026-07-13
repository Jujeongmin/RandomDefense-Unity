using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 이펙트 오브젝트 풀. 프리팹 단위로 큐를 유지하고, 씬 전환 시 자동으로 비운다.
/// </summary>
public static class EffectPool
{
    static readonly Dictionary<GameObject, Queue<GameObject>> s_pools = new Dictionary<GameObject, Queue<GameObject>>();
    static Transform s_container;
    static bool s_sceneHooked;

    static Transform Container
    {
        get
        {
            if (s_container == null)
                s_container = new GameObject("EffectPool").transform;
            return s_container;
        }
    }

    public static GameObject Spawn(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return null;

        if (!s_sceneHooked)
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            s_sceneHooked = true;
        }

        if (!s_pools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            s_pools[prefab] = pool;
        }

        GameObject obj = null;
        while (pool.Count > 0 && obj == null) obj = pool.Dequeue();
        if (obj == null) obj = Object.Instantiate(prefab, Container);

        obj.transform.position = position;
        obj.SetActive(true);
        return obj;
    }

    public static void Despawn(GameObject prefab, GameObject obj)
    {
        if (obj == null) return;
        obj.SetActive(false);

        if (prefab == null || !s_pools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            Object.Destroy(obj);
            return;
        }
        pool.Enqueue(obj);
    }

    static void OnSceneUnloaded(Scene scene)
    {
        // 풀 오브젝트들은 씬과 함께 파괴되므로 참조만 정리
        s_pools.Clear();
        s_container = null;
    }
}
