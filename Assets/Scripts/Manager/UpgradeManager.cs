using UnityEngine;
using TMPro;

public class UpgradeManager : MonoBehaviour
{
    MobManager m_boundMobManager;
    SpeciesType.TYPE m_targetSpecies = SpeciesType.TYPE.None;
    [Header("Upgrade Levels")]
    [SerializeField] int m_wizardLevel = 1;
    [SerializeField] int m_archerLevel = 1;
    [SerializeField] int m_warriorLevel = 1;

    [Header("Upgrade UI")]
    [SerializeField] TextMeshProUGUI m_wizardLvText;
    [SerializeField] TextMeshProUGUI m_archerLvText;
    [SerializeField] TextMeshProUGUI m_warriorLvText;

    [Header("Cost Texts (직접 할당 — 없으면 라벨에 비용 포함 표기)")]
    [SerializeField] TextMeshProUGUI m_wizardCostText;
    [SerializeField] TextMeshProUGUI m_archerCostText;
    [SerializeField] TextMeshProUGUI m_warriorCostText;

    [Header("Upgrade Costs")]
    [SerializeField] int m_upgradeBaseCost = 20;
    [SerializeField] int m_upgradeCostStep = 20;

    private void Start()
    {
        if (GManager.Instance != null) GManager.Instance.RegisterUpgradeManager(this);
        else Initialize();
    }

    public void Initialize()
    {
        BindMobManager(GManager.Instance != null ? GManager.Instance.IsMob : null);
        UpdateUpgradeUI();
    }

    public void BindMobManager(MobManager mobManager)
    {
        if (m_boundMobManager == mobManager) return;
        if (m_boundMobManager != null) m_boundMobManager.SpeciesChanged -= OnSpeciesChanged;

        m_boundMobManager = mobManager;
        if (m_boundMobManager != null)
        {
            m_boundMobManager.SpeciesChanged += OnSpeciesChanged;
            m_targetSpecies = m_boundMobManager.CurrentSpecies;
        }
        else
        {
            m_targetSpecies = SpeciesType.TYPE.None;
        }
        UpdateUpgradeUI();
    }

    void OnDestroy()
    {
        if (m_boundMobManager != null) m_boundMobManager.SpeciesChanged -= OnSpeciesChanged;
    }

    void OnSpeciesChanged(SpeciesType.TYPE species)
    {
        m_targetSpecies = species;
        UpdateUpgradeUI();
    }

    public int GetClassLevel(EntityType.TYPE type)
    {
        return type switch
        {
            EntityType.TYPE.Wizard => m_wizardLevel,
            EntityType.TYPE.Archer => m_archerLevel,
            EntityType.TYPE.Warrior => m_warriorLevel,
            _ => 0
        };
    }

    public int GetUpgradeCost(EntityType.TYPE type)
    {
        int level = GetClassLevel(type);
        var balance = GManager.Instance != null ? GManager.Instance.Balance : null;
        int baseCost = balance != null ? balance.UpgradeBaseCost : m_upgradeBaseCost;
        int costStep = balance != null ? balance.UpgradeCostStep : m_upgradeCostStep;
        return baseCost + (level - 1) * costStep;
    }

    public void UpgradeWizard() => TryUpgrade(ref m_wizardLevel, EntityType.TYPE.Wizard);
    public void UpgradeArcher() => TryUpgrade(ref m_archerLevel, EntityType.TYPE.Archer);
    public void UpgradeWarrior() => TryUpgrade(ref m_warriorLevel, EntityType.TYPE.Warrior);

    void TryUpgrade(ref int level, EntityType.TYPE type)
    {
        int cost = GetUpgradeCost(type);
        EconomyManager economy = GManager.Instance != null ? GManager.Instance.IsEconomy : null;
        if (economy != null && economy.TrySpend(cost))
        {
            level++;
            GameAudioManager.Play(GameAudioManager.Sfx.Upgrade);
            UpdateUpgradeUI();
        }
    }

    public void UpdateUpgradeUI()
    {
        SetUpgradeText(m_wizardLvText, m_wizardCostText, GameLanguage.Choose("마법사", "WIZARD"), EntityType.TYPE.Wizard, m_wizardLevel);
        SetUpgradeText(m_archerLvText, m_archerCostText, GameLanguage.Choose("궁수", "ARCHER"), EntityType.TYPE.Archer, m_archerLevel);
        SetUpgradeText(m_warriorLvText, m_warriorCostText, GameLanguage.Choose("전사", "WARRIOR"), EntityType.TYPE.Warrior, m_warriorLevel);
    }

    void SetUpgradeText(TextMeshProUGUI label, TextMeshProUGUI costText, string className, EntityType.TYPE type, int level)
    {
        if (label == null) return;
        int cost = GetUpgradeCost(type);
        if (costText != null)
        {
            // 비용은 코인 아이콘 옆 별도 텍스트에 숫자만 표시
            label.text = $"{GetEffectivenessText(type)}\n{className} Lv.{level}";
            costText.text = cost.ToString("N0");
        }
        else
        {
            // CostRow 미생성 시 기존 3줄 표기 유지
            label.text = $"{GetEffectivenessText(type)}\n{className} Lv.{level}\n{cost}G";
        }
    }

    string GetEffectivenessText(EntityType.TYPE type)
    {
        if (m_targetSpecies == SpeciesType.TYPE.None) return "<color=#B8B8B8>--</color>";

        GameBalanceData balance = GManager.Instance != null ? GManager.Instance.Balance : null;
        float multiplier = balance != null ? balance.GetDamageMultiplier(type, m_targetSpecies) : 1f;
        int percent = Mathf.RoundToInt(multiplier * 100f);

        if (percent >= 100) return $"<color=#FFD447>{percent}%</color>";
        return $"<color=#FFFFFF>{percent}%</color>";
    }
}
