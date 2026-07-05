using UnityEngine;

[CreateAssetMenu(fileName = "GameBalanceData", menuName = "Random Defense/Game Balance")]
public class GameBalanceData : ScriptableObject
{
    [Header("Economy")]
    [SerializeField] int m_startGold = 100;
    [SerializeField] int m_summonCost = 20;
    [SerializeField] int m_normalKillGold = 2;
    [SerializeField] int m_bossKillGold = 100;
    [SerializeField] int m_upgradeBaseCost = 20;
    [SerializeField] int m_upgradeCostStep = 20;

    [Header("Progression")]
    [SerializeField] int m_crystalPerReachedWave = 2;
    [SerializeField] int m_researchBaseCost = 50;
    [SerializeField] float m_rareSummonBonusPerLevel = 0.1f;

    [Header("Combat")]
    [SerializeField] float m_attackInterval = 1f;

    [Header("Waves")]
    [SerializeField] int m_maxWave = 50;
    [SerializeField] int m_mobsPerWave = 40;
    [SerializeField] float m_mobSpawnInterval = 1f;
    [SerializeField] float m_waveInterval = 5f;
    [SerializeField] float m_bossTimeLimit = 120f;

    public int StartGold => m_startGold;
    public int SummonCost => m_summonCost;
    public int NormalKillGold => m_normalKillGold;
    public int BossKillGold => m_bossKillGold;
    public int UpgradeBaseCost => m_upgradeBaseCost;
    public int UpgradeCostStep => m_upgradeCostStep;
    public float AttackInterval => m_attackInterval;
    public int MaxWave => m_maxWave;
    public int MobsPerWave => m_mobsPerWave;
    public float MobSpawnInterval => m_mobSpawnInterval;
    public float WaveInterval => m_waveInterval;
    public float BossTimeLimit => m_bossTimeLimit;

    public int GetCrystalReward(int reachedWave) => Mathf.Max(0, reachedWave) * m_crystalPerReachedWave;
    public int GetResearchCost(int currentLevel) => m_researchBaseCost * (currentLevel + 1);
    public float GetRareSummonBonus(int level) => Mathf.Max(0, level) * m_rareSummonBonusPerLevel;

    public int GetSellPrice(RarityType.TYPE rarity)
    {
        return rarity switch
        {
            RarityType.TYPE.Common => 10,
            RarityType.TYPE.Rare => 25,
            RarityType.TYPE.Elite => 60,
            RarityType.TYPE.Legendary => 150,
            RarityType.TYPE.Mythic => 400,
            RarityType.TYPE.Eternal => 1000,
            _ => 0
        };
    }

    public float GetDamageMultiplier(EntityType.TYPE attacker, SpeciesType.TYPE target)
    {
        if (target == SpeciesType.TYPE.None) return 1f;

        return attacker switch
        {
            EntityType.TYPE.Archer when target == SpeciesType.TYPE.Orc => 1f,
            EntityType.TYPE.Archer => 0.8f,
            EntityType.TYPE.Wizard when target == SpeciesType.TYPE.Undead => 1f,
            EntityType.TYPE.Wizard when target == SpeciesType.TYPE.Orc => 0.8f,
            EntityType.TYPE.Wizard => 0.6f,
            EntityType.TYPE.Warrior when target == SpeciesType.TYPE.Troll => 1f,
            EntityType.TYPE.Warrior when target == SpeciesType.TYPE.Orc => 0.8f,
            EntityType.TYPE.Warrior => 0.6f,
            _ => 1f
        };
    }

    public RarityType.TYPE RollRarity(int rareResearchLevel)
    {
        float bonus = Mathf.Min(49f, GetRareSummonBonus(rareResearchLevel));
        float roll = Random.Range(0f, 100f);
        float commonEnd = 50f - bonus;

        if (roll < commonEnd) return RarityType.TYPE.Common;
        if (roll < 83f - bonus) return RarityType.TYPE.Rare;
        if (roll < 93f - bonus) return RarityType.TYPE.Elite;
        if (roll < 99.5f) return RarityType.TYPE.Legendary;
        if (roll < 99.9f) return RarityType.TYPE.Mythic;
        return RarityType.TYPE.Eternal;
    }
}
