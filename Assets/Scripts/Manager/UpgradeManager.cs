using UnityEngine;
using TMPro;

public class UpgradeManager : MonoBehaviour
{
    [Header("Upgrade Levels")]
    [SerializeField] int m_wizardLevel = 1;
    [SerializeField] int m_archerLevel = 1;
    [SerializeField] int m_warriorLevel = 1;

    [Header("Upgrade UI")]
    [SerializeField] TextMeshProUGUI m_wizardLvText;
    [SerializeField] TextMeshProUGUI m_archerLvText;
    [SerializeField] TextMeshProUGUI m_warriorLvText;

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
        if (GManager.Instance.IsEconomy.CanAfford(cost))
        {
            GManager.Instance.IsEconomy.SpendGold(cost);
            level++;
            UpdateUpgradeUI();
        }
    }

    public void UpdateUpgradeUI()
    {
        if (m_wizardLvText != null) m_wizardLvText.text = $"마법사 Lv.{m_wizardLevel}\n{GetUpgradeCost(EntityType.TYPE.Wizard)}G";
        if (m_archerLvText != null) m_archerLvText.text = $"궁수 Lv.{m_archerLevel}\n{GetUpgradeCost(EntityType.TYPE.Archer)}G";
        if (m_warriorLvText != null) m_warriorLvText.text = $"전사 Lv.{m_warriorLevel}\n{GetUpgradeCost(EntityType.TYPE.Warrior)}G";
    }
}
