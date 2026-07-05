using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GManager : MonoBehaviour
{
    [Header("Game Balance")]
    [SerializeField] GameBalanceData m_balanceData = null;

    [Header("Managers")]
    [SerializeField] UnitDataManager m_unitDataManager = null;
    [SerializeField] PoolManager m_poolManager = null;
    RegionManager m_regionManager = null;
    MobManager m_mobManager = null;
    EconomyManager m_economyManager = null;
    UpgradeManager m_upgradeManager = null;
    UnitSpawner m_unitSpawner = null;
    SpeedManager m_speedManager = null;
    SpawnTextManager m_spawnTextManager = null;
    ClassRarityDisplay m_classRarityDisplay = null;
    PlayerProgressManager m_playerProgress = null;
    ResearchManager m_researchManager = null;

    ResultPanel m_resultPanel = null;
    GameObject m_settingsPanel = null;
    Transform m_damageTextParent = null;

    TextMeshProUGUI m_selectedClassNameText = null;
    [SerializeField] Sprite[] m_wizardSp = null;
    [SerializeField] Sprite[] m_archerSp = null;
    [SerializeField] Sprite[] m_warriorSp = null;


    bool m_gameOver = false;
    bool m_runRewardGranted = false;

    [Header("Debug (Inspector)")]
    [SerializeField] bool m_testGameOver = false;
    [SerializeField] bool m_testGameClear = false;

    public static GManager Instance { get; private set; } = null;

    public UnitDataManager IsUnitData => m_unitDataManager;
    public PoolManager IsPool => m_poolManager;
    public RegionManager IsRegion => m_regionManager;
    public MobManager IsMob => m_mobManager;
    public EconomyManager IsEconomy => m_economyManager;
    public UpgradeManager IsUpgrade => m_upgradeManager;
    public UnitSpawner IsSpawner => m_unitSpawner;
    public SpeedManager IsSpeed => m_speedManager;
    public SpawnTextManager IsSpawnText => m_spawnTextManager;
    public Transform DamageTextParent => m_damageTextParent;
    public GameObject IsSettingPanel => m_settingsPanel;
    public ClassRarityDisplay IsClassRarityDisplay => m_classRarityDisplay;
    public GameBalanceData Balance => m_balanceData;
    public PlayerProgressManager IsProgress => m_playerProgress;
    public ResearchManager IsResearch => m_researchManager;

    // ── 런타임 등록 메서드 (각 게임씬 매니저가 Start에서 자기 자신을 등록) ──
    public void RegisterMobManager(MobManager mgr)
    {
        m_mobManager = mgr;
    }

    /// <summary>하위호환성 유지용 — RegisterMobManager 사용 권장</summary>
    public void SetMobManager(MobManager mgr) => RegisterMobManager(mgr);

    public void RegisterRegionManager(RegionManager mgr) { m_regionManager = mgr; }

    public void RegisterEconomyManager(EconomyManager mgr)
    {
        m_economyManager = mgr;
        if (mgr != null) mgr.Initialize();
    }

    public void RegisterUpgradeManager(UpgradeManager mgr)
    {
        m_upgradeManager = mgr;
        if (mgr != null) mgr.Initialize();
    }

    public void RegisterSpeedManager(SpeedManager mgr)
    {
        m_speedManager = mgr;
        if (mgr != null) mgr.Initialize();
    }

    public void RegisterSpawnTextManager(SpawnTextManager mgr)
    {
        m_spawnTextManager = mgr;
        if (mgr != null) mgr.Initialize();
    }

    public void RegisterUnitSpawner(UnitSpawner mgr) { m_unitSpawner = mgr; }

    public void RegisterClassRarityDisplay(ClassRarityDisplay display) { m_classRarityDisplay = display; }

    public void RegisterResultPanel(ResultPanel panel)
    {
        m_resultPanel = panel;
        if (m_resultPanel != null) m_resultPanel.gameObject.SetActive(false);
    }

    public void RegisterSettingsPanel(GameObject panel)
    {
        m_settingsPanel = panel;
        if (m_settingsPanel != null) m_settingsPanel.SetActive(false);
    }

    public void RegisterDamageTextParent(Transform parent)
    {
        m_damageTextParent = parent;
    }

    public void RegisterSelectedClassNameText(TextMeshProUGUI text)
    {
        m_selectedClassNameText = text;
        if (m_selectedClassNameText != null) m_selectedClassNameText.text = "직업 선택";
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        m_gameOver = false;
        m_runRewardGranted = false;
        // Ensure restart sequence runs but always hide the result panel even if an error occurs.
        try
        {
            // Clear regions and mobs first
            if (m_regionManager != null)
            {
                m_regionManager.ClearAllRegions();
                m_regionManager.InitializeRegions();
            }

            if (m_mobManager != null)
            {
                m_mobManager.ClearAllMobs();
            }

            // Cleanup any units that might be outside regions (player-summoned heroes)
            if (m_poolManager != null)
            {
                m_poolManager.CleanupAllUnits();
                m_poolManager.DestroyAllPooledObjects();
                // Recreate pool parents and prewarm
                m_poolManager.Initialize();
            }

            // Reinitialize other managers
            if (m_economyManager != null) m_economyManager.Initialize();
            if (m_upgradeManager != null) m_upgradeManager.Initialize();
            if (m_speedManager != null) m_speedManager.Initialize();
            if (m_spawnTextManager != null) m_spawnTextManager.Initialize();

            if (m_mobManager != null)
            {
                m_mobManager.ResetForRestart();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }
        finally
        {
            if (m_resultPanel != null) m_resultPanel.gameObject.SetActive(false);
        }
    }

    // Initialization via inspector-serialized references. Do not use runtime Find/registration when possible.

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (m_balanceData == null)
            {
                m_balanceData = ScriptableObject.CreateInstance<GameBalanceData>();
            }

            m_playerProgress = GetComponent<PlayerProgressManager>();
            if (m_playerProgress == null) m_playerProgress = gameObject.AddComponent<PlayerProgressManager>();
            m_playerProgress.Initialize();

            m_researchManager = GetComponent<ResearchManager>();
            if (m_researchManager == null) m_researchManager = gameObject.AddComponent<ResearchManager>();
            m_researchManager.Initialize(m_playerProgress, m_balanceData);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private IEnumerator DelayedInitializeManagers()
    {
        yield return null;
        if (m_poolManager != null) m_poolManager.Initialize();
        if (m_economyManager != null) m_economyManager.Initialize();
        if (m_upgradeManager != null) m_upgradeManager.Initialize();
        if (m_speedManager != null) m_speedManager.Initialize();
        if (m_spawnTextManager != null) m_spawnTextManager.Initialize();
        if (m_regionManager != null)
        {
            m_regionManager.ClearAllRegions();
            m_regionManager.InitializeRegions();
        }
        if (m_mobManager != null) m_mobManager.ResetForRestart();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>씬 이름 상수 — 빌드 세팅의 씬 이름과 반드시 일치해야 합니다.</summary>
    public const string SCENE_MAIN = "MainScene";
    public const string SCENE_GAME = "GameScene";

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        m_gameOver = false;
        m_runRewardGranted = false;
        Time.timeScale = 1f;

        if (scene.name == SCENE_MAIN)
        {
            // 메인씬: 게임씬 전용 매니저 참조를 초기화
            OnMainSceneLoaded();
        }
        else if (scene.name == SCENE_GAME)
        {
            // 게임씬: 풀 초기화 후 매니저들은 각자 Start에서 등록됨
            RegisterSceneReferences(scene);
            StartCoroutine(DelayedInitializeManagers());
            if (m_resultPanel != null) m_resultPanel.gameObject.SetActive(false);
        }
    }

    void RegisterSceneReferences(Scene scene)
    {
        if (m_resultPanel == null)
        {
            var resultPanel = FindInScene<ResultPanel>(scene);
            if (resultPanel != null) RegisterResultPanel(resultPanel);
        }

        if (m_settingsPanel == null)
        {
            var settingPanel = FindInScene<SettingPanel>(scene);
            if (settingPanel != null) RegisterSettingsPanel(settingPanel.gameObject);
        }

        if (m_damageTextParent == null)
        {
            var damageTextParent = FindTransformInScene(scene, "DamageTexts");
            if (damageTextParent != null) RegisterDamageTextParent(damageTextParent);
        }

        if (m_selectedClassNameText == null)
        {
            var classText = FindComponentByName<TextMeshProUGUI>(scene, "ClassText");
            if (classText != null) RegisterSelectedClassNameText(classText);
        }

        BindButton(scene, "Setting", ClickSettingBtn);
        BindButton(scene, "WizardSellBtn", () => SetClassToSell((int)EntityType.TYPE.Wizard));
        BindButton(scene, "ArcherSellBtn", () => SetClassToSell((int)EntityType.TYPE.Archer));
        BindButton(scene, "WarriorSellBtn", () => SetClassToSell((int)EntityType.TYPE.Warrior));
        BindButton(scene, "CommonSellBtn", () => SellCurrentClassByRarity((int)RarityType.TYPE.Common));
        BindButton(scene, "RareSellBtn", () => SellCurrentClassByRarity((int)RarityType.TYPE.Rare));
        BindButton(scene, "EliteSellBtn", () => SellCurrentClassByRarity((int)RarityType.TYPE.Elite));
        BindButton(scene, "LegendarySellBtn", () => SellCurrentClassByRarity((int)RarityType.TYPE.Legendary));
        BindButton(scene, "MythicSellBtn", () => SellCurrentClassByRarity((int)RarityType.TYPE.Mythic));
        BindButton(scene, "EternalSellBtn", () => SellCurrentClassByRarity((int)RarityType.TYPE.Eternal));
    }

    T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null) return component;
        }

        return null;
    }

    Transform FindTransformInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform result = FindTransformRecursive(root.transform, objectName);
            if (result != null) return result;
        }

        return null;
    }

    Transform FindTransformRecursive(Transform current, string objectName)
    {
        if (current.name == objectName) return current;

        foreach (Transform child in current)
        {
            Transform result = FindTransformRecursive(child, objectName);
            if (result != null) return result;
        }

        return null;
    }

    T FindComponentByName<T>(Scene scene, string objectName) where T : Component
    {
        Transform transform = FindTransformInScene(scene, objectName);
        return transform != null ? transform.GetComponent<T>() : null;
    }

    void BindButton(Scene scene, string buttonName, UnityEngine.Events.UnityAction action)
    {
        Button button = FindComponentByName<Button>(scene, buttonName);
        if (button == null) return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    void OnMainSceneLoaded()
    {
        // 게임씬 전용 매니저 참조를 해제 (다음 게임씬 진입 시 다시 등록됨)
        m_mobManager = null;
        m_regionManager = null;
        m_economyManager = null;
        m_upgradeManager = null;
        m_speedManager = null;
        m_spawnTextManager = null;
        m_unitSpawner = null;
        m_classRarityDisplay = null;
        m_resultPanel = null;
        m_settingsPanel = null;
        m_selectedClassNameText = null;
    }

    private void Start()
    {
        if (m_economyManager != null) m_economyManager.Initialize();
        if (m_upgradeManager != null) m_upgradeManager.Initialize();
        if (m_speedManager != null) m_speedManager.Initialize();
        if (m_poolManager != null) m_poolManager.Initialize();
        if (m_selectedClassNameText != null) m_selectedClassNameText.text = "직업 선택";

        if (m_spawnTextManager != null) m_spawnTextManager.Initialize();

        if (m_resultPanel != null) m_resultPanel.gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    // Allow triggering game over/clear from inspector at runtime
    void Update()
    {
        if (m_testGameOver)
        {
            m_testGameOver = false;
            ForceDefeat();
        }
        if (m_testGameClear)
        {
            m_testGameClear = false;
            ForceVictory();
        }
    }
#endif

    public void HandleDefeat()
    {
        if (m_gameOver) return;
        m_gameOver = true;
        GrantRunCrystalReward();
        Time.timeScale = 0f;
        if (m_resultPanel != null)
        {
            int currentWave = m_mobManager != null ? m_mobManager.CurrentWave : 1;
            m_resultPanel.Setup(false, currentWave);
        }
    }

    public void HandleVictory()
    {
        if (m_gameOver) return;
        m_gameOver = true;
        GrantRunCrystalReward();
        Time.timeScale = 0f;
        if (m_resultPanel != null)
        {
            int currentWave = m_mobManager != null ? m_mobManager.CurrentWave : 1;
            m_resultPanel.Setup(true, currentWave);
        }
    }

    public void ForceDefeat()
    {
        m_gameOver = false;
        HandleDefeat();
    }

    public void ForceVictory()
    {
        m_gameOver = false;
        HandleVictory();
    }

    void GrantRunCrystalReward()
    {
        if (m_runRewardGranted || m_playerProgress == null || m_balanceData == null) return;

        int reachedWave = m_mobManager != null ? m_mobManager.CurrentWave : 1;
        m_playerProgress.AddCrystals(m_balanceData.GetCrystalReward(reachedWave));
        m_runRewardGranted = true;
    }

    public void ClickSettingBtn()
    {
        if (m_settingsPanel == null || m_gameOver) return;

        m_settingsPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ShowSpawnText(float percent, EntityType.TYPE classType, RarityType.TYPE rarity, Vector3 worldPos)
    {
        string className = classType switch
        {
            EntityType.TYPE.Wizard => "마법사",
            EntityType.TYPE.Archer => "궁수",
            EntityType.TYPE.Warrior => "전사",
            _ => "영웅"
        };
        string rarityName = rarity switch
        {
            RarityType.TYPE.Common => "일반",
            RarityType.TYPE.Rare => "고급",
            RarityType.TYPE.Elite => "정예",
            RarityType.TYPE.Legendary => "전설",
            RarityType.TYPE.Mythic => "신화",
            RarityType.TYPE.Eternal => "태초",
            _ => rarity.ToString()
        };

        string msg = string.Format("{0}% {1} {2}등급 소환", percent.ToString("F1"), className, rarityName);

        if (rarity >= RarityType.TYPE.Mythic)
        {
            if (IsSpawnText != null)
            {
                IsSpawnText.ShowMythicNotice(msg);
                return;
            }
        }

        if (IsSpawnText != null)
        {
            IsSpawnText.Show(msg, worldPos, 10f);
            return;
        }

        if (IsPool != null)
        {
            var go = IsPool.GetDamageText(m_damageTextParent);
            if (go != null)
            {
                go.transform.position = worldPos;
                var dt = go.GetComponent<DamageText>();
                if (dt != null) dt.SetupMessage(msg, 10f, 0.6f, Color.white);
            }
            return;
        }

        var tmp = new GameObject("SpawnMessage");
        var tmpDt = tmp.AddComponent<DamageText>();
        tmp.transform.position = worldPos;
        tmpDt.SetupMessage(msg, 10f, 0.6f, Color.white);
    }

    public void SetClassToSell(int classType)
    {
        m_selectedClassToSell = classType;

        if (m_selectedClassNameText != null)
        {
            string className = (EntityType.TYPE)classType switch
            {
                EntityType.TYPE.Wizard => "마법사",
                EntityType.TYPE.Archer => "궁수",
                EntityType.TYPE.Warrior => "전사",
                _ => "알 수 없음"
            };
            m_selectedClassNameText.text = className;
        }

        if (m_classRarityDisplay != null)
        {
            m_classRarityDisplay.UpdateRarityImages((EntityType.TYPE)m_selectedClassToSell);
        }
    }

    int m_selectedClassToSell = 0;
    public void SellCurrentClassByRarity(int rarityType)
    {
        EntityType.TYPE type = (EntityType.TYPE)m_selectedClassToSell;
        RarityType.TYPE rarity = (RarityType.TYPE)rarityType;

        var region = IsRegion.GetRegionForType(type);
        if (region == null) return;

        GameObject unitGo = region.FindAndRemoveUnitByRarity(rarity);
        if (unitGo != null)
        {
            int price = GetSellPrice(rarity);
            IsEconomy.AddGold(price);

            if (IsPool != null) IsPool.ReturnUnit(unitGo);
            else Destroy(unitGo);

            m_classRarityDisplay.UpdateRarityImages(type);
        }
    }

    public Sprite GetSprite(EntityType.TYPE classType, RarityType.TYPE rarity)
    {
        Sprite[] arr = null;
        switch (classType)
        {
            case EntityType.TYPE.Wizard:
                arr = m_wizardSp;
                break;
            case EntityType.TYPE.Archer:
                arr = m_archerSp;
                break;
            case EntityType.TYPE.Warrior:
                arr = m_warriorSp;
                break;
            default:
                arr = null;
                break;
        }

        if (arr == null) return null;

        int idx = rarity switch
        {
            RarityType.TYPE.Common => 0,
            RarityType.TYPE.Rare => 1,
            RarityType.TYPE.Elite => 2,
            RarityType.TYPE.Legendary => 3,
            RarityType.TYPE.Mythic => 4,
            RarityType.TYPE.Eternal => 5,
            _ => -1
        };

        if (idx < 0 || idx >= arr.Length) return null;
        return arr[idx];
    }

    int GetSellPrice(RarityType.TYPE rarity)
    {
        return m_balanceData != null ? m_balanceData.GetSellPrice(rarity) : 0;
    }
}
