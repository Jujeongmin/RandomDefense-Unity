using UnityEngine;

public enum ResearchType
{
    Attack,
    StartGold,
    GoldGain,
    RareSummon,
    BossDamage
}

public class ResearchManager : MonoBehaviour
{
    PlayerProgressManager m_progress;
    GameBalanceData m_balance;

    public void Initialize(PlayerProgressManager progress, GameBalanceData balance)
    {
        m_progress = progress;
        m_balance = balance;
    }

    public int GetLevel(ResearchType type)
    {
        if (m_progress == null) return 0;
        PlayerProgressData data = m_progress.Data;
        return type switch
        {
            ResearchType.Attack => data.attackResearchLevel,
            ResearchType.StartGold => data.startGoldResearchLevel,
            ResearchType.GoldGain => data.goldGainResearchLevel,
            ResearchType.RareSummon => data.rareSummonResearchLevel,
            ResearchType.BossDamage => data.bossDamageResearchLevel,
            _ => 0
        };
    }

    public int GetMaxLevel(ResearchType type) => type == ResearchType.RareSummon ? 10 : 20;
    public int GetCost(ResearchType type) => m_balance.GetResearchCost(GetLevel(type));

    public bool TryUpgrade(ResearchType type)
    {
        int level = GetLevel(type);
        if (level >= GetMaxLevel(type) || !m_progress.SpendCrystals(GetCost(type))) return false;

        PlayerProgressData data = m_progress.Data;
        switch (type)
        {
            case ResearchType.Attack: data.attackResearchLevel++; break;
            case ResearchType.StartGold: data.startGoldResearchLevel++; break;
            case ResearchType.GoldGain: data.goldGainResearchLevel++; break;
            case ResearchType.RareSummon: data.rareSummonResearchLevel++; break;
            case ResearchType.BossDamage: data.bossDamageResearchLevel++; break;
        }
        m_progress.Save();
        return true;
    }

    public float AttackMultiplier => 1f + GetLevel(ResearchType.Attack) * 0.02f;
    public float BossDamageMultiplier => 1f + GetLevel(ResearchType.BossDamage) * 0.02f;
    public float GoldGainMultiplier => 1f + GetLevel(ResearchType.GoldGain) * 0.02f;
    public int StartGoldBonus => GetLevel(ResearchType.StartGold) * 5;
}
