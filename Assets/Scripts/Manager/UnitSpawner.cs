using UnityEngine;
using UnityEngine.U2D.Animation;
using TMPro;

public class UnitSpawner : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI m_spawnButtonText;
    int m_spawnCount = 0;

    [Header("Hold Spawn Settings")]
    [SerializeField] float m_holdInitialDelay = 0.5f;
    [SerializeField] float m_holdRepeatInterval = 0.12f;
    bool m_spawnHeld = false;
    float m_holdTimer = 0f;

    private void Start()
    {
        if (GManager.Instance != null) GManager.Instance.RegisterUnitSpawner(this);
        if (m_spawnButtonText != null) m_spawnButtonText.text = GameLanguage.Choose("소환", "Spawn");
    }

    private void OnDestroy()
    {
        if (GManager.Instance != null && GManager.Instance.IsSpawner == this)
            GManager.Instance.RegisterUnitSpawner(null);
    }

    void Update()
    {
        if (!m_spawnHeld) return;

        m_holdTimer -= Time.deltaTime;
        if (m_holdTimer <= 0f)
        {
            Spawn();
            m_holdTimer = m_holdRepeatInterval;
        }
    }

    public void Spawn()
    {
        GManager game = GManager.Instance;
        if (game == null || game.IsEconomy == null || game.IsUnitData == null) return;
        EconomyManager economy = game.IsEconomy;

        // 클래스 선택
        int classRoll = Random.Range(0, 3);
        EntityType.TYPE spawnType = classRoll switch
        {
            0 => EntityType.TYPE.Wizard,
            1 => EntityType.TYPE.Archer,
            _ => EntityType.TYPE.Warrior,
        };

        // 등급 (비율: 50 일반, 33 고급, 10 정예, 6.5 전설, 0.4 신화, 0.1 태초)
        int rareResearchLevel = game.IsResearch != null
            ? game.IsResearch.GetLevel(ResearchType.RareSummon)
            : 0;
        RarityType.TYPE rarity = game.Balance != null
            ? game.Balance.RollRarity(rareResearchLevel)
            : RarityType.TYPE.Common;

        if (game.IsUnitData.Get(spawnType, (int)rarity) == null) return;

        // 비용 지불
        if (!economy.TrySpend(economy.SummonCost)) return;

        ExecuteSpawn(spawnType, rarity, 0f);
    }

    public void ForceSpawnWithRarity(RarityType.TYPE forcedRarity)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Play Mode에서만 테스트 소환이 가능합니다.");
            return;
        }

        int classRoll = Random.Range(0, 3);
        EntityType.TYPE spawnType = classRoll switch
        {
            0 => EntityType.TYPE.Wizard,
            1 => EntityType.TYPE.Archer,
            _ => EntityType.TYPE.Warrior,
        };

        float mockRar = forcedRarity switch
        {
            RarityType.TYPE.Common => 25f,
            RarityType.TYPE.Rare => 60f,
            RarityType.TYPE.Elite => 88f,
            RarityType.TYPE.Legendary => 95f,
            RarityType.TYPE.Mythic => 99.7f,
            RarityType.TYPE.Eternal => 99.95f,
            _ => 0f
        };

        ExecuteSpawn(spawnType, forcedRarity, mockRar);
    }

    private void ExecuteSpawn(EntityType.TYPE spawnType, RarityType.TYPE rarity, float rar)
    {
        int index = (int)rarity;

        var unitData = GManager.Instance.IsUnitData;
        var checkData = unitData.Get(spawnType, index);
        if (checkData == null) return;

        // 생성
        GameObject go = null;
        if (GManager.Instance != null && GManager.Instance.IsPool != null)
        {
            go = GManager.Instance.IsPool.GetUnit(spawnType);
        }
        else
        {
            go = new GameObject();
        }
        go.name = $"Unit_{spawnType}_{m_spawnCount}";
        m_spawnCount++;

        SpriteLibrary spriteLibrary = GetOrAddComponent<SpriteLibrary>(go);
        GetOrAddComponent<SpriteResolver>(go);
        GetOrAddComponent<SpriteRenderer>(go);

        var regionMgr = GManager.Instance.IsRegion;
        var region = regionMgr.GetRegionForType(spawnType);
        if (region == null)
        {
            if (GManager.Instance != null && GManager.Instance.IsPool != null)
            {
                GManager.Instance.IsPool.ReturnUnit(go);
            }
            else
            {
                Destroy(go);
            }
            return;
        }

        // 초기화
        if (checkData.IsSpLibAsset != null)
            spriteLibrary.spriteLibraryAsset = checkData.IsSpLibAsset;

        if (go.transform.localScale == Vector3.one) go.transform.localScale = Vector3.one * 0.3f;
        Character character = GetOrAddComponent<Character>(go);

        ParentsController controller = go.GetComponent<ParentsController>();
        if (controller == null)
        {
            controller = spawnType switch
            {
                EntityType.TYPE.Wizard => go.AddComponent<WizardController>(),
                EntityType.TYPE.Archer => go.AddComponent<ArcherController>(),
                EntityType.TYPE.Warrior => go.AddComponent<WarriorController>(),
                _ => go.AddComponent<ParentsController>()
            };
        }

        controller.Setting(spawnType, index);
        controller.IsRarity = rarity;
        character.CacheController();
        region.AddUnit(go);

        // Show spawn message above the unit using pooled DamageText
        Vector3 spawnPos = go.transform.position + Vector3.up * 0.5f;
        
        // Use fixed display probabilities per rarity
        float percent = rar; // default fallback
        switch (rarity)
        {
            case RarityType.TYPE.Common: percent = 50f; break;
            case RarityType.TYPE.Rare: percent = 33f; break;
            case RarityType.TYPE.Elite: percent = 10f; break;
            case RarityType.TYPE.Legendary: percent = 6.5f; break;
            case RarityType.TYPE.Mythic: percent = 0.4f; break;
            case RarityType.TYPE.Eternal: percent = 0.1f; break;
        }

        if (GManager.Instance != null)
        {
            GManager.Instance.ShowSpawnText(percent, spawnType, rarity, spawnPos);
        }
        GameAudioManager.Play(GameAudioManager.Sfx.Summon);
    }

    public void BeginSpawnHold()
    {
        Spawn();
        m_spawnHeld = true;
        m_holdTimer = m_holdInitialDelay;
    }

    public void EndSpawnHold()
    {
        m_spawnHeld = false;
    }

    static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        return target.TryGetComponent(out T component) ? component : target.AddComponent<T>();
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(UnitSpawner))]
public class UnitSpawnerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        UnitSpawner spawner = (UnitSpawner)target;

        GUILayout.Space(15);
        GUILayout.Label("Test Spawns (Requires Play Mode)", UnityEditor.EditorStyles.boldLabel);

        if (GUILayout.Button("Spawn Common (일반)"))
        {
            spawner.ForceSpawnWithRarity(RarityType.TYPE.Common);
        }
        if (GUILayout.Button("Spawn Rare (고급)"))
        {
            spawner.ForceSpawnWithRarity(RarityType.TYPE.Rare);
        }
        if (GUILayout.Button("Spawn Elite (정예)"))
        {
            spawner.ForceSpawnWithRarity(RarityType.TYPE.Elite);
        }
        if (GUILayout.Button("Spawn Legendary (전설)"))
        {
            spawner.ForceSpawnWithRarity(RarityType.TYPE.Legendary);
        }
        if (GUILayout.Button("Spawn Mythic (신화)"))
        {
            spawner.ForceSpawnWithRarity(RarityType.TYPE.Mythic);
        }
        if (GUILayout.Button("Spawn Eternal (태초)"))
        {
            spawner.ForceSpawnWithRarity(RarityType.TYPE.Eternal);
        }
    }
}
#endif
