using UnityEngine;
using TMPro;

public class EconomyManager : MonoBehaviour
{
    [Header("Gold Settings")]
    [SerializeField] int m_startGold = 100;
    [SerializeField] int m_summonCost = 10;

    [Header("UI")]
    [SerializeField] TextMeshProUGUI m_goldText;

    int m_gold = 0;

    public int Gold => m_gold;
    public int SummonCost => m_summonCost;

    private void Start()
    {
        // GManager가 있으면 등록 (Register 내부에서 Initialize 호출됨)
        if (GManager.Instance != null) GManager.Instance.RegisterEconomyManager(this);
        else Initialize(); // GManager 없이 독립적으로 실행할 경우 대비
    }

    public void Initialize()
    {
        m_gold = m_startGold;
        UpdateGoldUI();
    }

    public void AddGold(int amount)
    {
        m_gold += amount;
        UpdateGoldUI();
    }

    public bool CanAfford(int cost)
    {
        return m_gold >= cost;
    }

    public void SpendGold(int amount)
    {
        m_gold -= amount;
        UpdateGoldUI();
    }

    public void UpdateGoldUI()
    {
        if (m_goldText != null)
        {
            m_goldText.text = $"{m_gold}";
        }
    }
}
