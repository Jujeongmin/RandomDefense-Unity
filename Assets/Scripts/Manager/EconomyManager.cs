using UnityEngine;
using TMPro;
using System;

public class EconomyManager : MonoBehaviour
{
    [Header("Gold Settings")]
    [SerializeField] int m_startGold = 100;
    [SerializeField] int m_summonCost = 20;

    [Header("UI")]
    [SerializeField] TextMeshProUGUI m_goldText;

    int m_gold = 0;
    int m_summonCount = 0;

    public int Gold => m_gold;
    public int SummonCount => m_summonCount;
    public event Action<int> GoldChanged;

    /// <summary>이번 판 소환 횟수에 따라 증가하는 현재 소환 비용.</summary>
    public int SummonCost
    {
        get
        {
            GameBalanceData balance = GManager.Instance != null ? GManager.Instance.Balance : null;
            return balance != null ? balance.GetSummonCost(m_summonCount) : m_summonCost;
        }
    }

    /// <summary>소환이 성공적으로 이뤄졌을 때 호출하여 비용 증가 카운터를 올립니다.</summary>
    public void RegisterSummon()
    {
        m_summonCount++;
    }

    private void Start()
    {
        // GManager가 있으면 등록 (Register 내부에서 Initialize 호출됨)
        if (GManager.Instance != null) GManager.Instance.RegisterEconomyManager(this);
        else Initialize(); // GManager 없이 독립적으로 실행할 경우 대비
    }

    public void Initialize()
    {
        int baseGold = GManager.Instance != null && GManager.Instance.Balance != null
            ? GManager.Instance.Balance.StartGold
            : m_startGold;
        int researchBonus = GManager.Instance != null && GManager.Instance.IsResearch != null
            ? GManager.Instance.IsResearch.StartGoldBonus
            : 0;
        m_gold = baseGold + researchBonus;
        m_summonCount = 0;
        UpdateGoldUI();
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        SetGold(m_gold + amount);
    }

    public void AddKillGold(int baseAmount)
    {
        float multiplier = GManager.Instance != null && GManager.Instance.IsResearch != null
            ? GManager.Instance.IsResearch.GoldGainMultiplier
            : 1f;
        AddGold(Mathf.RoundToInt(baseAmount * multiplier));
    }

    public bool CanAfford(int cost)
    {
        return cost >= 0 && m_gold >= cost;
    }

    public bool TrySpend(int amount)
    {
        if (!CanAfford(amount)) return false;
        SetGold(m_gold - amount);
        return true;
    }

    public void SpendGold(int amount)
    {
        TrySpend(amount);
    }

    public void UpdateGoldUI()
    {
        if (m_goldText != null)
        {
            m_goldText.text = $"{m_gold}";
        }
    }


    void SetGold(int value)
    {
        int nextGold = Mathf.Max(0, value);
        if (m_gold == nextGold) return;
        m_gold = nextGold;
        UpdateGoldUI();
        GoldChanged?.Invoke(m_gold);
    }
}
