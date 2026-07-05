using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;
using TMPro;

public class MobManager : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] int m_currentWave = 1;
    [SerializeField] int m_maxWave = 40;
    [SerializeField] int m_mobsPerWave = 40;
    [SerializeField] float m_mobSpawnInterval = 1.0f;
    [SerializeField] float m_waveInterval = 5.0f;

    int m_spawnCount = 0;
    int m_spawnedInWave = 0;
    bool m_gameOver = false;
    const int m_maxMobCount = 100;

    [Header("Boss Settings")]
    [SerializeField] float m_bossTimeLimit = 120.0f;
    [SerializeField] TextMeshProUGUI m_bossTimerText = null;

    MobController m_activeBoss = null;

    [Header("Active Mobs")]
    [SerializeField] List<MobController> m_activeMobs = new List<MobController>();
    public List<MobController> ActiveMobs => m_activeMobs;

    [Header("UI Settings")]
    [SerializeField] TextMeshProUGUI m_mobCountText;
    [SerializeField] TextMeshProUGUI m_waveText;
    [SerializeField] TextMeshProUGUI m_waveSpeciesText;
    [SerializeField] Image m_waveMobImage;
    [SerializeField] Slider m_mobCountSlider = null;

    [Header("Mob Path Settings")]
    [SerializeField] List<Vector3> m_mobWaypoints = new List<Vector3>();

    [Header("Runtime Parents")]
    [SerializeField] Transform m_mobParent = null;

    [Header("Mob Visuals")]
    [SerializeField] float m_mobWorldScale = 0.3f;

    public List<Vector3> MobWaypoints => m_mobWaypoints;
    public float MobWorldScale => m_mobWorldScale;
    public int MobCount => m_spawnCount;
    public int MaxMobCount => m_maxMobCount;
    public int CurrentWave => m_currentWave;

    void Start()
    {
        if (GManager.Instance != null) GManager.Instance.RegisterMobManager(this);

        // Ensure there is a parent container for spawned mobs to keep hierarchy tidy
        if (m_mobParent == null)
        {
            var go = new GameObject("Mobs");
            go.transform.SetParent(this.transform, false);
            m_mobParent = go.transform;
        }

        UpdateMobCountText();
        UpdateWaveText();
        m_mobCountSlider.value = 0f;
    }

    private void OnDestroy()
    {
        // GManager 참조 해제: 씬 전환 시 null 참조 방지
        if (GManager.Instance != null && GManager.Instance.IsMob == this)
        {
            GManager.Instance.RegisterMobManager(null);
        }
    }

    private void OnDisable()
    {
        if (m_waveCoroutine != null)
        {
            try { StopCoroutine(m_waveCoroutine); } catch { }
            m_waveCoroutine = null;
        }
    }

    public void ResetForRestart()
    {
        if (m_waveCoroutine != null)
        {
            try { StopCoroutine(m_waveCoroutine); } catch { }
            m_waveCoroutine = null;
        }

        m_spawnCount = 0;
        m_spawnedInWave = 0;
        m_currentWave = 1;
        m_gameOver = false;

        UpdateMobCountText();
        UpdateWaveText();

        if (!m_gameOver && m_waveCoroutine == null) m_waveCoroutine = StartCoroutine(WaveLoop());
    }

    private Coroutine m_waveCoroutine = null;

    void UpdateMobCountText()
    {
        if (m_mobCountText != null) m_mobCountText.text = $"{m_spawnCount} / {m_maxMobCount}";
        if (m_mobCountSlider != null)
        {
            m_mobCountSlider.maxValue = m_maxMobCount;
            m_mobCountSlider.value = m_spawnCount;
        }
    }

    // Called by GManager after scene reload to ensure spawning resumes
    public void Resume()
    {
        if (m_gameOver) m_gameOver = false;

        if (m_waveCoroutine == null) m_waveCoroutine = StartCoroutine(WaveLoop());
    }

    void UpdateWaveText()
    {
        if (m_waveText != null)
        {
            if (m_currentWave % 10 == 0)
            {
                m_waveText.text = $"Wave {m_currentWave} (BOSS)";
            }
            else
            {
                m_waveText.text = $"Wave {m_currentWave}";
            }
        }
    }

    void UpdateWaveInfoUI(int mobIndex)
    {
        var unitDataMgr = GManager.Instance != null ? GManager.Instance.IsUnitData : null;
        if (unitDataMgr == null)
        {
            if (m_waveSpeciesText != null) m_waveSpeciesText.text = string.Empty;
            if (m_waveMobImage != null) m_waveMobImage.gameObject.SetActive(false);
            return;
        }

        var data = unitDataMgr.Get(EntityType.TYPE.Mob, mobIndex);
        if (data != null)
        {
            if (m_waveSpeciesText != null) 
            { 
                switch(data.IsSpeciesType)
                {
                    case SpeciesType.TYPE.Orc:
                        m_waveSpeciesText.text = "오크";
                        break;
                    case SpeciesType.TYPE.Troll:
                        m_waveSpeciesText.text = "트롤";
                        break;
                    case SpeciesType.TYPE.Undead:
                        m_waveSpeciesText.text = "언데드";
                        break;
                    default:
                        m_waveSpeciesText.text = data.IsSpeciesType.ToString();
                        break;
                }
            }
            if (m_waveMobImage != null)
            {
                m_waveMobImage.sprite = data.IsIcon;
                m_waveMobImage.gameObject.SetActive(data.IsIcon != null);
            }
        }
        else
        {
            if (m_waveSpeciesText != null) m_waveSpeciesText.text = string.Empty;
            if (m_waveMobImage != null) m_waveMobImage.gameObject.SetActive(false);
        }
    }

    IEnumerator WaveLoop()
    {
        while (!m_gameOver)
        {

            m_spawnedInWave = 0;

            bool isBossWave = (m_currentWave % 10 == 0);
            UpdateWaveText();

            if (isBossWave)
            {
                if (m_bossTimerText != null)
                {
                    m_bossTimerText.gameObject.SetActive(true);
                }
                if (m_waveSpeciesText != null)
                {
                    m_waveSpeciesText.text = "BOSS WAVE";
                }
                if (m_waveMobImage != null)
                {
                    m_waveMobImage.gameObject.SetActive(false);
                }

                SpawnBoss();

                float timer = m_bossTimeLimit;
                while (timer > 0 && m_activeBoss != null)
                {
                    timer -= Time.deltaTime;
                    if (m_bossTimerText != null)
                    {
                        m_bossTimerText.text = $"Boss Time: {timer:F1}s";
                    }
                    yield return null;
                }

                if (m_bossTimerText != null)
                {
                    m_bossTimerText.gameObject.SetActive(false);
                }

                if (m_activeBoss != null)
                {
                    // Boss not killed in time -> Game Over!
                    Debug.LogError("Boss not defeated in time! Game Over.");
                    HandleDefeat();
                    yield break;
                }


                Debug.Log($"Boss Wave {m_currentWave} cleared! Waiting for next wave...");

                if (m_currentWave >= m_maxWave)
                {
                    Debug.Log("Final wave boss defeated! Game Clear.");
                    if (GManager.Instance != null)
                    {
                        GManager.Instance.HandleVictory();
                    }
                    m_gameOver = true;
                    yield break;
                }
            }
            else
            {
                SpeciesType.TYPE[] speciesPool = new SpeciesType.TYPE[] { SpeciesType.TYPE.Orc, SpeciesType.TYPE.Troll, SpeciesType.TYPE.Undead };
                SpeciesType.TYPE chosenSpecies = speciesPool[Random.Range(0, speciesPool.Length)];

                var unitDataMgr = GManager.Instance != null ? GManager.Instance.IsUnitData : null;
                int chosenIndex = 0;
                if (unitDataMgr != null)
                {
                    var indices = unitDataMgr.GetIndicesForSpecies(EntityType.TYPE.Mob, chosenSpecies);
                    if (indices != null && indices.Count > 0)
                    {
                        chosenIndex = indices[Random.Range(0, indices.Count)];
                    }
                }

                UpdateWaveInfoUI(chosenIndex);

                while (m_spawnedInWave < m_mobsPerWave)
                {
                    SpawnMob(chosenIndex);
                    m_spawnedInWave++;
                    yield return new WaitForSeconds(m_mobSpawnInterval);
                }
            }

            yield return new WaitForSeconds(m_waveInterval);
            m_currentWave++;
        }
    }

    void SpawnMob(int mobIndex)
    {
        if (!HasSpawnRegion()) return;

        var mobCtrl = CreateMobController($"Mob_W{m_currentWave}_{m_spawnedInWave}");

        var unitDataMgr = GManager.Instance != null ? GManager.Instance.IsUnitData : null;
        var data = unitDataMgr != null ? unitDataMgr.Get(EntityType.TYPE.Mob, mobIndex) : null;

        if (data != null)
        {
            mobCtrl.Setting(EntityType.TYPE.Mob, mobIndex);

            int baseHp = data.IsHp;
            int scaledHp = baseHp + (m_currentWave - 1) * (baseHp / 5);
            mobCtrl.SetMaxHp(scaledHp);
        }

        UpdateMobCountText();
    }

    bool HasSpawnRegion()
    {
        var regionMgr = GManager.Instance != null ? GManager.Instance.IsRegion : null;
        if (regionMgr == null) return false;
        if (regionMgr.RegionCount == 0) regionMgr.InitializeRegions();
        var regions = regionMgr.GetRegions();
        return regions != null && regions.Count > 0;
    }

    MobController CreateMobController(string objectName)
    {
        GameObject go = null;
        if (GManager.Instance != null && GManager.Instance.IsPool != null)
        {
            go = GManager.Instance.IsPool.GetMob(m_mobParent);
        }
        else
        {
            go = new GameObject();
            if (m_mobParent != null) go.transform.SetParent(m_mobParent);
        }

        go.name = objectName;
        m_spawnCount++;

        if (m_spawnCount >= m_maxMobCount)
        {
            HandleDefeat();
        }

        if (go.GetComponent<SpriteLibrary>() == null) go.AddComponent<SpriteLibrary>();
        if (go.GetComponent<SpriteResolver>() == null) go.AddComponent<SpriteResolver>();
        if (go.GetComponent<SpriteRenderer>() == null) go.AddComponent<SpriteRenderer>();
        if (go.GetComponent<Character>() == null) go.AddComponent<Character>();

        var mobCtrl = go.GetComponent<MobController>();
        if (mobCtrl == null) mobCtrl = go.AddComponent<MobController>();
        return mobCtrl;
    }

    public void HandleDefeat()
    {
        if (m_gameOver) return;
        m_gameOver = true;
        Time.timeScale = 0f;
        if (GManager.Instance != null)
        {
            GManager.Instance.HandleDefeat();
        }
    }

    public void OnMobDestroyed(bool giveReward)
    {
        m_spawnCount = Mathf.Max(0, m_spawnCount - 1);
        UpdateMobCountText();

        if (giveReward && GManager.Instance != null && GManager.Instance.IsEconomy != null)
        {
            // Reward: 1 Gold per mob kill
            GManager.Instance.IsEconomy.AddGold(1);
        }
    }

    public void RegisterMob(MobController mob)
    {
        if (mob == null) return;
        m_activeMobs.RemoveAll(item => item == null);
        if (!m_activeMobs.Contains(mob)) m_activeMobs.Add(mob);
    }

    public void UnregisterMob(MobController mob)
    {
        if (mob == null) return;
        if (m_activeMobs.Contains(mob)) m_activeMobs.Remove(mob);
        m_activeMobs.RemoveAll(item => item == null);
    }

    // Remove and return all active mobs to the pool (or destroy if no pool)
    public void ClearAllMobs()
    {
        var toClear = new List<MobController>(m_activeMobs);
        foreach (var m in toClear)
        {
            if (m == null) continue;
            var go = m.gameObject;
            // attempt to return to pool
            if (GManager.Instance != null && GManager.Instance.IsPool != null)
            {
                GManager.Instance.IsPool.ReturnMob(go);
            }
            else
            {
                Destroy(go);
            }
        }
        m_activeMobs.Clear();
    }

    void SpawnBoss()
    {
        if (!HasSpawnRegion()) return;

        var mobCtrl = CreateMobController($"Boss_W{m_currentWave}");

        var unitDataMgr = GManager.Instance != null ? GManager.Instance.IsUnitData : null;

        var data = unitDataMgr != null ? unitDataMgr.Get(EntityType.TYPE.Boss, 0) : null;
        bool usingFallback = false;
        if (data == null && unitDataMgr != null)
        {
            data = unitDataMgr.Get(EntityType.TYPE.Mob, 0);
            usingFallback = true;
            Debug.LogWarning("Boss data not found in UnitDataManager! Falling back to Mob index 0.");
        }

        if (data != null)
        {
            mobCtrl.Setting(usingFallback ? EntityType.TYPE.Mob : EntityType.TYPE.Boss, data.IsEntityIndex);

            int baseHp = data.IsHp;
            int scaledHp = baseHp * 15 + (m_currentWave - 1) * (baseHp * 3);
            mobCtrl.SetMaxHp(scaledHp);

            mobCtrl.SetWorldScale(Vector3.one * (m_mobWorldScale * 2f));
        }

        m_activeBoss = mobCtrl;
        UpdateMobCountText();
    }

    // Called by other systems when the active boss has been defeated (clears reference)
    public void NotifyBossDefeated(MobController boss)
    {
        if (boss == null) return;
        if (m_activeBoss == boss)
        {
            m_activeBoss = null;
            Debug.Log("MobManager: active boss cleared after defeat.");
        }
    }
}
