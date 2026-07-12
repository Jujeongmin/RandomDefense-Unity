using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 소환 등급별 확률을 보여주는 표기 화면(확률형 아이템 확률 공개용).
/// 확률 값은 GameBalanceData.GetRarityPercent에서 가져와 실제 뽑기와 항상 일치합니다.
/// 런타임에 UI를 직접 생성하므로 씬에 미리 배치할 필요가 없습니다.
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

    // 씬에 미리 배치할 수 있도록 빌더가 할당하는 직렬화 참조들
    [Header("References (에디터 빌더가 할당)")]
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

    public static RarityOddsPanel Create(Transform canvasParent)
    {
        GameObject go = new GameObject("RarityOddsPanel", typeof(RectTransform));
        go.transform.SetParent(canvasParent, false);
        RarityOddsPanel panel = go.AddComponent<RarityOddsPanel>();
        panel.Build();
        panel.gameObject.SetActive(false);
        return panel;
    }

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

    /// <summary>UI를 생성하고 직렬화 참조를 채웁니다. 에디터 빌더 또는 런타임 폴백에서 호출됩니다.</summary>
    public void Build()
    {
        RectTransform root = GetComponent<RectTransform>();
        Stretch(root);

        // 반투명 배경 (바깥 클릭 시 닫힘)
        Image bg = gameObject.GetComponent<Image>();
        if (bg == null) bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.72f);
        m_bgButton = gameObject.GetComponent<Button>();
        if (m_bgButton == null) m_bgButton = gameObject.AddComponent<Button>();
        m_bgButton.targetGraphic = bg;

        // 카드
        RectTransform card = CreateBox("Card", root, new Vector2(360f, 580f), new Color(0.12f, 0.13f, 0.17f, 1f));
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.anchoredPosition = Vector2.zero;

        m_titleText = CreateText(card, "Title", 26f, TextAlignmentOptions.Center);
        PlaceTop(m_titleText.rectTransform, 0f, -20f, new Vector2(320f, 40f));

        m_headerName = CreateText(card, "HeaderName", 15f, TextAlignmentOptions.Left);
        m_headerName.color = new Color(0.7f, 0.72f, 0.78f);
        PlaceTopLeft(m_headerName.rectTransform, 28f, -70f, new Vector2(180f, 26f));

        m_headerPct = CreateText(card, "HeaderPct", 15f, TextAlignmentOptions.Right);
        m_headerPct.color = new Color(0.7f, 0.72f, 0.78f);
        PlaceTopRight(m_headerPct.rectTransform, -28f, -70f, new Vector2(140f, 26f));

        const float rowTop = -100f;
        const float rowHeight = 46f;
        m_nameTexts = new TextMeshProUGUI[Rarities.Length];
        m_pctTexts = new TextMeshProUGUI[Rarities.Length];
        for (int i = 0; i < Rarities.Length; i++)
        {
            float y = rowTop - i * rowHeight;

            TextMeshProUGUI name = CreateText(card, $"Name{i}", 20f, TextAlignmentOptions.Left);
            PlaceTopLeft(name.rectTransform, 28f, y, new Vector2(190f, 40f));
            m_nameTexts[i] = name;

            TextMeshProUGUI pct = CreateText(card, $"Pct{i}", 20f, TextAlignmentOptions.Right);
            PlaceTopRight(pct.rectTransform, -28f, y, new Vector2(150f, 40f));
            m_pctTexts[i] = pct;
        }

        m_noteText = CreateText(card, "Note", 14f, TextAlignmentOptions.TopLeft);
        m_noteText.color = new Color(0.78f, 0.80f, 0.86f);
        PlaceTop(m_noteText.rectTransform, 0f, rowTop - Rarities.Length * rowHeight - 14f, new Vector2(316f, 96f));

        RectTransform closeRect = CreateBox("Close", card, new Vector2(220f, 52f), new Color(1f, 0.65f, 0.12f, 1f));
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 18f);
        m_closeButton = closeRect.gameObject.AddComponent<Button>();
        m_closeButton.targetGraphic = closeRect.GetComponent<Image>();
        m_closeText = CreateText(closeRect, "Label", 20f, TextAlignmentOptions.Center);
        Stretch(m_closeText.rectTransform);
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

    // ── UI 생성 헬퍼 ──
    static RectTransform CreateBox(string name, Transform parent, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        return rt;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, float fontSize, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        GameFont.Apply(text);
        text.fontSize = fontSize;
        text.alignment = align;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void PlaceTop(RectTransform rt, float x, float y, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = size;
    }

    static void PlaceTopLeft(RectTransform rt, float x, float y, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = size;
    }

    static void PlaceTopRight(RectTransform rt, float x, float y, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = size;
    }
}
