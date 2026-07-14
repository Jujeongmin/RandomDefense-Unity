using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 소환 등급별 확률을 보여주는 표기 화면(확률형 아이템 확률 공개용).
/// 확률 값은 GameBalanceData.GetRarityPercent에서 가져와 실제 뽑기와 항상 일치합니다.
/// UI는 씬에 미리 배치돼 있고, 직렬화 참조로 연결됩니다.
/// </summary>
public class RarityOddsPanel : MonoBehaviour
{
    static readonly RarityType.TYPE[] Rarities =
    {
        RarityType.TYPE.Common, RarityType.TYPE.Rare, RarityType.TYPE.Elite,
        RarityType.TYPE.Legendary, RarityType.TYPE.Mythic, RarityType.TYPE.Eternal
    };

    static readonly Color[] RarityColors =
    {
        new Color(0.72f, 0.72f, 0.72f), // Common
        new Color(0.36f, 0.84f, 0.36f), // Rare
        new Color(0.30f, 0.65f, 1.00f), // Elite
        new Color(0.78f, 0.49f, 1.00f), // Legendary
        new Color(1.00f, 0.36f, 0.36f), // Mythic
        new Color(1.00f, 0.83f, 0.28f), // Eternal
    };

    [Header("References (씬에서 할당)")]
    [SerializeField] Button m_bgButton;
    [SerializeField] Button m_closeButton;
    [SerializeField] TextMeshProUGUI m_titleText;
    [SerializeField] TextMeshProUGUI m_headerName;
    [SerializeField] TextMeshProUGUI m_headerPct;
    [SerializeField] TextMeshProUGUI[] m_nameTexts;
    [SerializeField] TextMeshProUGUI[] m_pctTexts;
    [SerializeField] TextMeshProUGUI m_noteText;
    [SerializeField] TextMeshProUGUI m_closeText;

    bool m_wired;

    public void Open()
    {
        EnsureWired();
        Populate();
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Close() => gameObject.SetActive(false);

    void EnsureWired()
    {
        if (m_wired) return;
        m_wired = true;
        if (m_bgButton != null) m_bgButton.onClick.AddListener(Close);
        if (m_closeButton != null) m_closeButton.onClick.AddListener(Close);
    }

    void Populate()
    {
        bool english = GameLanguage.IsEnglish;

        GManager gm = GManager.Instance;
        GameBalanceData balance = gm != null ? gm.Balance : null;
        bool tempBalance = false;
        if (balance == null)
        {
            balance = ScriptableObject.CreateInstance<GameBalanceData>();
            tempBalance = true;
        }
        int rareLevel = gm != null && gm.IsResearch != null ? gm.IsResearch.GetLevel(ResearchType.RareSummon) : 0;

        string[] names = english
            ? new[] { "COMMON", "RARE", "ELITE", "LEGENDARY", "MYTHIC", "ETERNAL" }
            : new[] { "일반", "고급", "정예", "전설", "신화", "태초" };

        m_titleText.text = english ? "SUMMON ODDS" : "소환 확률";
        m_headerName.text = english ? "GRADE" : "등급";
        m_headerPct.text = english ? "CHANCE" : "확률";

        for (int i = 0; i < Rarities.Length; i++)
        {
            float pct = balance.GetRarityPercent(Rarities[i], rareLevel);
            m_nameTexts[i].text = names[i];
            m_nameTexts[i].color = RarityColors[i];
            m_pctTexts[i].text = pct.ToString("0.##") + "%";
            m_pctTexts[i].color = RarityColors[i];
        }

        m_noteText.text = english
            ? "Class (Wizard / Archer / Warrior) is chosen at 33.3% each. Values above are grade odds per summon. Rare-summon research adjusts these odds."
            : "직업(마법사·궁수·전사)은 각 33.3% 확률로 정해집니다. 위 확률은 1회 소환 시 등급이 정해질 확률이며, 고급소환 연구 시 변동됩니다.";
        m_closeText.text = english ? "CLOSE" : "닫기";

        if (tempBalance) Destroy(balance);
    }

}
