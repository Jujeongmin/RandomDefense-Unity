using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    private GameObject m_mobPoolParent;
    private GameObject m_damageTextPoolParent;

    private GameObject m_wizardPoolParent;
    private GameObject m_archerPoolParent;
    private GameObject m_warriorPoolParent;

    private Queue<GameObject> m_mobPool = new Queue<GameObject>();
    private Queue<GameObject> m_damageTextPool = new Queue<GameObject>();

    private Queue<GameObject> m_wizardPool = new Queue<GameObject>();
    private Queue<GameObject> m_archerPool = new Queue<GameObject>();
    private Queue<GameObject> m_warriorPool = new Queue<GameObject>();

    private HashSet<GameObject> m_activeDamageTexts = new HashSet<GameObject>();

    [Header("Prewarm Settings")]
    [SerializeField] int m_initialMobPoolCount = 0;
    [SerializeField] int m_initialDamageTextPoolCount = 0;
    [SerializeField] int m_initialUnitPerType = 0;

    public void Initialize()
    {
        m_mobPoolParent = EnsurePoolParent(m_mobPoolParent, "Pool_Mobs");
        m_damageTextPoolParent = EnsurePoolParent(m_damageTextPoolParent, "Pool_DamageTexts");
        m_wizardPoolParent = EnsurePoolParent(m_wizardPoolParent, "Pool_Wizards");
        m_archerPoolParent = EnsurePoolParent(m_archerPoolParent, "Pool_Archers");
        m_warriorPoolParent = EnsurePoolParent(m_warriorPoolParent, "Pool_Warriors");

        RemoveDestroyedReferences(m_mobPool);
        RemoveDestroyedReferences(m_damageTextPool);
        RemoveDestroyedReferences(m_wizardPool);
        RemoveDestroyedReferences(m_archerPool);
        RemoveDestroyedReferences(m_warriorPool);

        RebuildQueueIfEmpty(m_mobPool, m_mobPoolParent.transform);
        RebuildQueueIfEmpty(m_damageTextPool, m_damageTextPoolParent.transform);
        RebuildQueueIfEmpty(m_wizardPool, m_wizardPoolParent.transform);
        RebuildQueueIfEmpty(m_archerPool, m_archerPoolParent.transform);
        RebuildQueueIfEmpty(m_warriorPool, m_warriorPoolParent.transform);

        // Optionally pre-create pool objects to avoid runtime spikes
        PrewarmPools();
    }

    private GameObject EnsurePoolParent(GameObject current, string name)
    {
        if (current != null) return current;

        var go = new GameObject(name);
        go.transform.SetParent(transform);
        return go;
    }

    // 완전 삭제: 재시작 시 풀에 있는 모든 오브젝트(프리웜 포함)와 풀 부모를 파괴합니다.
    // 이후 Initialize()가 호출되면 새 부모가 생성됩니다.
    public void DestroyAllPooledObjects()
    {
        void DestroyParentAndClear(GameObject parent, Queue<GameObject> queue)
        {
            if (queue != null) queue.Clear();
            if (parent != null)
            {
                var t = parent.transform;
                for (int i = t.childCount - 1; i >= 0; i--)
                {
                    var child = t.GetChild(i).gameObject;
                    if (child == null) continue;
                    if (Application.isPlaying)
                    {
                        Destroy(child);
                    }
                    else
                    {
#if UNITY_EDITOR
                        UnityEngine.Object.DestroyImmediate(child);
#else
                        Destroy(child);
#endif
                    }
                }

                if (Application.isPlaying)
                {
                    Destroy(parent);
                }
                else
                {
#if UNITY_EDITOR
                    UnityEngine.Object.DestroyImmediate(parent);
#else
                    Destroy(parent);
#endif
                }
            }
        }

        DestroyParentAndClear(m_mobPoolParent, m_mobPool);
        DestroyParentAndClear(m_damageTextPoolParent, m_damageTextPool);
        DestroyParentAndClear(m_wizardPoolParent, m_wizardPool);
        DestroyParentAndClear(m_archerPoolParent, m_archerPool);
        DestroyParentAndClear(m_warriorPoolParent, m_warriorPool);

        // clear active damage text tracking
        if (m_activeDamageTexts != null) m_activeDamageTexts.Clear();

        // Null out references so Initialize will recreate parents
        m_mobPoolParent = null;
        m_damageTextPoolParent = null;
        m_wizardPoolParent = null;
        m_archerPoolParent = null;
        m_warriorPoolParent = null;
        // Also clear any queued references
        m_mobPool.Clear();
        m_damageTextPool.Clear();
        m_wizardPool.Clear();
        m_archerPool.Clear();
        m_warriorPool.Clear();
    }

    // Cleanup all units currently present in the scene by returning them to pools or destroying.
    // This searches for ParentsController instances and returns their GameObjects to the appropriate pool.
    public void CleanupAllUnits()
    {
        // Find all ParentsController in scene (includes inactive if available)
        ParentsController[] controllers = Resources.FindObjectsOfTypeAll<ParentsController>();
        foreach (var pc in controllers)
        {
            if (pc == null) continue;
            var go = pc.gameObject;
            // Only handle scene-rooted objects (ignore assets, prefabs)
            if (!go.scene.IsValid()) continue;

            // Skip mobs (they are handled by MobManager.ClearAllMobs)
            if (go.GetComponent<MobController>() != null) continue;

            // Return unit to pool if possible, otherwise destroy
            if (Application.isPlaying)
            {
                // Only return if the corresponding pool parent still exists. Otherwise destroy to avoid null refs.
                var type = GetUnitType(go);
                bool hasParent = (type == EntityType.TYPE.Wizard && m_wizardPoolParent != null) ||
                                 (type == EntityType.TYPE.Archer && m_archerPoolParent != null) ||
                                 (type == EntityType.TYPE.Warrior && m_warriorPoolParent != null);

                if (hasParent)
                {
                    ReturnUnit(go);
                }
                else
                {
                    Destroy(go);
                }
            }
            else
            {
#if UNITY_EDITOR
                UnityEngine.Object.DestroyImmediate(go);
#else
                Destroy(go);
#endif
            }
        }
    }

    private void PrewarmPools()
    {
        int mobNeed = Mathf.Max(0, m_initialMobPoolCount - m_mobPool.Count);
        int damageTextNeed = Mathf.Max(0, m_initialDamageTextPoolCount - m_damageTextPool.Count);
        int wizardNeed = Mathf.Max(0, m_initialUnitPerType - m_wizardPool.Count);
        int archerNeed = Mathf.Max(0, m_initialUnitPerType - m_archerPool.Count);
        int warriorNeed = Mathf.Max(0, m_initialUnitPerType - m_warriorPool.Count);

        // Prewarm mob pool with empty GameObjects (components are added on spawn)
        for (int i = 0; i < mobNeed; i++)
        {
            var go = new GameObject($"PooledMob_{m_mobPool.Count}");
            go.SetActive(false);
            go.transform.SetParent(m_mobPoolParent.transform);
            m_mobPool.Enqueue(go);
        }

        // Prewarm damage text pool with DamageText components
        for (int i = 0; i < damageTextNeed; i++)
        {
            var go = new GameObject("DamageText");
            go.AddComponent<DamageText>();
            go.SetActive(false);
            go.transform.SetParent(m_damageTextPoolParent.transform);
            m_damageTextPool.Enqueue(go);
        }

        // Prewarm unit pools per type with empty GameObjects
        for (int i = 0; i < wizardNeed; i++)
        {
            var w = new GameObject($"PooledWizard_{m_wizardPool.Count}");
            w.SetActive(false);
            w.transform.SetParent(m_wizardPoolParent.transform);
            m_wizardPool.Enqueue(w);
        }

        for (int i = 0; i < archerNeed; i++)
        {
            var a = new GameObject($"PooledArcher_{m_archerPool.Count}");
            a.SetActive(false);
            a.transform.SetParent(m_archerPoolParent.transform);
            m_archerPool.Enqueue(a);
        }

        for (int i = 0; i < warriorNeed; i++)
        {
            var r = new GameObject($"PooledWarrior_{m_warriorPool.Count}");
            r.SetActive(false);
            r.transform.SetParent(m_warriorPoolParent.transform);
            m_warriorPool.Enqueue(r);
        }
    }

    // Pre-placed SpawnText slots will be used from inspector. No spawn pool methods.

    // --- Mob Pool ---
    public GameObject GetMob()
    {
        return GetMob(null);
    }

    // Get a mob and optionally set its parent in the hierarchy before activation.
    public GameObject GetMob(Transform parent)
    {
        // If internal queue is empty but there are children under the pool parent (prewarmed in editor), rebuild queue
        if (m_mobPool.Count == 0 && m_mobPoolParent != null && m_mobPoolParent.transform.childCount > 0)
        {
            RebuildQueueFromParent(m_mobPool, m_mobPoolParent.transform);
        }

        GameObject go;
        if (TryDequeueLiveObject(m_mobPool, out go))
        {
            if (parent != null) go.transform.SetParent(parent);
            else go.transform.SetParent(null);
            go.SetActive(true);
        }
        else
        {
            go = new GameObject();
            if (parent != null) go.transform.SetParent(parent);
        }
        return go;
    }

    public void ReturnMob(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        if (m_mobPoolParent != null) go.transform.SetParent(m_mobPoolParent.transform);
        if (!m_mobPool.Contains(go)) m_mobPool.Enqueue(go);
    }

    // --- DamageText Pool ---
    public GameObject GetDamageText()
    {
        return GetDamageText(null);
    }

    // Get a damage text optionally parented under the provided Transform before activation
    public GameObject GetDamageText(Transform parent)
    {
        if (m_damageTextPool.Count == 0 && m_damageTextPoolParent != null && m_damageTextPoolParent.transform.childCount > 0)
        {
            RebuildQueueFromParent(m_damageTextPool, m_damageTextPoolParent.transform);
        }

        // If caller didn't provide a parent, default to the damage text pool parent (keeps hierarchy tidy)
        if (parent == null && m_damageTextPoolParent != null)
        {
            parent = m_damageTextPoolParent.transform;
        }

        GameObject go;
        if (TryDequeueLiveObject(m_damageTextPool, out go))
        {
            if (parent != null) go.transform.SetParent(parent);
            else go.transform.SetParent(null);
            go.SetActive(true);
        }
        else
        {
            go = new GameObject("DamageText");
            go.AddComponent<DamageText>();
            if (parent != null) go.transform.SetParent(parent);
        }

        if (go != null) m_activeDamageTexts.Add(go);
        return go;
    }

    public void ReturnDamageText(GameObject go)
    {
        if (go == null) return;

        // Reset transient damage text state if component exists
        var dt = go.GetComponent<DamageText>();
        if (dt != null) dt.ResetState();

        go.SetActive(false);
        if (m_damageTextPoolParent != null) go.transform.SetParent(m_damageTextPoolParent.transform);
        if (!m_damageTextPool.Contains(go)) m_damageTextPool.Enqueue(go);

        if (m_activeDamageTexts.Contains(go)) m_activeDamageTexts.Remove(go);
    }

    // --- Unit Pool ---
    public GameObject GetUnit(EntityType.TYPE type)
    {
        Queue<GameObject> pool = GetUnitPool(type);
        var parent = GetUnitPoolParent(type);
        if (pool != null && pool.Count == 0 && parent != null && parent.childCount > 0)
        {
            RebuildQueueFromParent(pool, parent);
        }
        GameObject go;
        if (TryDequeueLiveObject(pool, out go))
        {
            go.transform.SetParent(null);
            go.SetActive(true);
        }
        else
        {
            go = new GameObject();
        }
        return go;
    }

    public void ReturnUnit(GameObject go)
    {
        if (go == null) return;

        EntityType.TYPE type = GetUnitType(go);
        Queue<GameObject> pool = GetUnitPool(type);
        Transform parent = GetUnitPoolParent(type);

        if (pool != null && parent != null)
        {
            go.SetActive(false);
            go.transform.SetParent(parent);
            if (!pool.Contains(go)) pool.Enqueue(go);
        }
        else
        {
            Destroy(go); // Fallback
        }
    }

    private Queue<GameObject> GetUnitPool(EntityType.TYPE type)
    {
        return type switch
        {
            EntityType.TYPE.Wizard => m_wizardPool,
            EntityType.TYPE.Archer => m_archerPool,
            EntityType.TYPE.Warrior => m_warriorPool,
            _ => null
        };
    }

    private Transform GetUnitPoolParent(EntityType.TYPE type)
    {
        return type switch
        {
            EntityType.TYPE.Wizard => m_wizardPoolParent != null ? m_wizardPoolParent.transform : null,
            EntityType.TYPE.Archer => m_archerPoolParent != null ? m_archerPoolParent.transform : null,
            EntityType.TYPE.Warrior => m_warriorPoolParent != null ? m_warriorPoolParent.transform : null,
            _ => null
        };
    }

    private EntityType.TYPE GetUnitType(GameObject go)
    {
        if (go.GetComponent<WizardController>() != null) return EntityType.TYPE.Wizard;
        if (go.GetComponent<ArcherController>() != null) return EntityType.TYPE.Archer;
        if (go.GetComponent<WarriorController>() != null) return EntityType.TYPE.Warrior;

        // Fallback check by name
        if (go.name.Contains("Wizard")) return EntityType.TYPE.Wizard;
        if (go.name.Contains("Archer")) return EntityType.TYPE.Archer;
        if (go.name.Contains("Warrior")) return EntityType.TYPE.Warrior;

        return EntityType.TYPE.Wizard;
    }

    private void RebuildQueueFromParent(Queue<GameObject> queue, Transform parent)
    {
        // Move children into queue preserving order
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i).gameObject;
            child.SetActive(false);
            queue.Enqueue(child);
        }
    }

    private void RebuildQueueIfEmpty(Queue<GameObject> queue, Transform parent)
    {
        if (queue == null || queue.Count > 0 || parent == null || parent.childCount == 0) return;
        RebuildQueueFromParent(queue, parent);
    }

    private bool TryDequeueLiveObject(Queue<GameObject> queue, out GameObject go)
    {
        go = null;
        if (queue == null) return false;

        while (queue.Count > 0)
        {
            go = queue.Dequeue();
            if (go != null) return true;
        }

        return false;
    }

    private void RemoveDestroyedReferences(Queue<GameObject> queue)
    {
        if (queue == null || queue.Count == 0) return;

        int count = queue.Count;
        for (int i = 0; i < count; i++)
        {
            var go = queue.Dequeue();
            if (go != null) queue.Enqueue(go);
        }
    }

    // 게임오버 등에서 모든 풀에 있는 객체들을 비활성화하고 큐를 재구성합니다.
    // 재시작 시 풀 상태를 예측 가능하게 유지하기 위해 사용하세요.
    public void DeactivateAllPooledObjects()
    {
        DeactivateAndRebuild(m_mobPoolParent, m_mobPool);
        DeactivateAndRebuild(m_damageTextPoolParent, m_damageTextPool);
        DeactivateAndRebuild(m_wizardPoolParent, m_wizardPool);
        DeactivateAndRebuild(m_archerPoolParent, m_archerPool);
        DeactivateAndRebuild(m_warriorPoolParent, m_warriorPool);

        // 활성 DamageText 추적 집합을 사용해 FindObjectsOfType 호출 없이 비활성화 후 풀로 반환
        if (m_activeDamageTexts != null && m_damageTextPoolParent != null)
        {
            var toReturn = new List<GameObject>(m_activeDamageTexts);
            foreach (var go in toReturn)
            {
                if (go == null) continue;
                go.SetActive(false);
                go.transform.SetParent(m_damageTextPoolParent.transform);
                if (!m_damageTextPool.Contains(go)) m_damageTextPool.Enqueue(go);
                m_activeDamageTexts.Remove(go);
            }
        }
    }

    private void DeactivateAndRebuild(GameObject parentObj, Queue<GameObject> queue)
    {
        if (parentObj == null) return;
        var parent = parentObj.transform;
        // First, ensure any child is inactive
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i).gameObject;
            if (child != null) child.SetActive(false);
        }

        // Clear queue and repopulate from children
        queue.Clear();
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i).gameObject;
            if (child != null)
            {
                queue.Enqueue(child);
            }
        }
    }
}
