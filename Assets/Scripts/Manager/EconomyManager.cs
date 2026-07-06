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

    public int Gold => m_gold;
    public event Action<int> GoldChanged;
    public int SummonCost => GManager.Instance != null && GManager.Instance.Balance != null
        ? GManager.Instance.Balance.SummonCost
        : m_summonCost;

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
