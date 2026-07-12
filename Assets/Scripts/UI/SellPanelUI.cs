using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SellPanelUI : MonoBehaviour
{
    static readonly RarityType.TYPE[] Rarities =
    {
        RarityType.TYPE.Common, RarityType.TYPE.Rare, RarityType.TYPE.Elite,
        RarityType.TYPE.Legendary, RarityType.TYPE.Mythic, RarityType.TYPE.Eternal
    };

    EntityType.TYPE m_selectedClass = EntityType.TYPE.Wizard;
    int m_sellAmount = 1;

    [Header("View References")]
    [SerializeField] Button[] m_rarityButtons;
    [SerializeField] Image[] m_rarityImages;
    [SerializeField] TextMeshProUGUI[] m_countTexts;
    [SerializeField] TextMeshProUGUI[] m_priceTexts;
    [SerializeField] Button[] m_classButtons;
    [SerializeField] Button[] m_amountButtons;
    [SerializeField] Button m_closeButton;

    [Header("Auto-Sell Toggle (optional — 미할당 시 런타임 자동 생성)")]
    [SerializeField] Button m_autoSellToggle;

    void Awake()
    {
        for (int i = 0; i < Rarities.Length; i++)
        {
            int captured = i;
            Button button = GetAt(m_rarityButtons, i);
            button?.onClick.AddListener(() => SellRarity(captured));
        }

        BindButtons(m_classButtons, SetClass);
        BindButtons(m_amountButtons, SetAmount);
        m_closeButton?.onClick.AddListener(() => gameObject.SetActive(false));

        if (m_autoSellToggle == null)
        {
            // 씬에 배치한 토글을 이름으로 먼저 찾고, 정말 없을 때만 생성 (배치한 위치를 건드리지 않음)
            RectTransform content = m_closeButton != null ? m_closeButton.transform.parent as RectTransform : transform as RectTransform;
            Transform existing = content != null ? content.Find("AutoSellToggle") : null;
            m_autoSellToggle = existing != null ? existing.GetComponent<Button>() : CreateAutoSellButton();
        }
        m_autoSellToggle?.onClick.AddListener(ToggleAutoSell);
    }

    void OnEnable()
    {
        ApplyLanguage();
        Refresh();
        UpdateAutoSellVisual();
    }

    void ToggleAutoSell()
    {
        if (GManager.Instance == null) return;
        GManager.Instance.SetAutoSellLowGrade(!GManager.Instance.AutoSellLowGradeEnabled);
        UpdateAutoSellVisual();
        Refresh();
    }

    void UpdateAutoSellVisual()
    {
        if (m_autoSellToggle == null) return;
        bool on = GManager.Instance != null && GManager.Instance.AutoSellLowGradeEnabled;

        TextMeshProUGUI label = m_autoSellToggle.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            string state = on ? "ON" : "OFF";
            label.text = GameLanguage.Choose($"고급이하 자동판매: {state}", $"AUTO-SELL ≤RARE: {state}");
        }
        if (m_autoSellToggle.targetGraphic != null)
            m_autoSellToggle.targetGraphic.color = on
                ? new Color(1f, 0.65f, 0.12f, 1f)
                : new Color(0.22f, 0.24f, 0.3f, 1f);
    }

    Button CreateAutoSellButton()
    {
        RectTransform content = m_closeButton != null ? m_closeButton.transform.parent as RectTransform : transform as RectTransform;
        if (content == null) return null;

        var go = new GameObject("AutoSellToggle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(content, false);
        // 판매 패널 자식들은 중앙 앵커 좌표계를 사용 (HintText/CloseButton과 같은 행의 왼쪽)
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-255f, 340f);
        rt.sizeDelta = new Vector2(180f, 40f);

        var img = go.GetComponent<Image>();
        img.color = new Color(0.22f, 0.24f, 0.3f, 1f);

        var button = go.GetComponent<Button>();
        button.targetGraphic = img;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.SetParent(rt, false);
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        GameFont.Apply(tmp);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 10f;
        tmp.fontSizeMax = 22f;
        tmp.text = "AUTO-SELL";

        return button;
    }

    void ApplyLanguage()
    {
        string[] classNames = GameLanguage.IsEnglish
            ? new[] { "WIZARD", "ARCHER", "WARRIOR" }
            : new[] { "마법사", "궁수", "전사" };
        for (int i = 0; i < classNames.Length; i++) SetButtonLabel(GetAt(m_classButtons, i), classNames[i]);

        string[] rarityNames = GameLanguage.IsEnglish
            ? new[] { "COMMON", "RARE", "ELITE", "LEGEND", "MYTHIC", "ETERNAL" }
            : new[] { "일반", "고급", "정예", "전설", "신화", "태초" };
        for (int i = 0; i < Rarities.Length; i++)
        {
            Button button = GetAt(m_rarityButtons, i);
            if (button == null) continue;
            TextMeshProUGUI[] texts = button.GetComponentsInChildren<TextMeshProUGUI>(true);
            TextMeshProUGUI count = GetAt(m_countTexts, i);
            TextMeshProUGUI price = GetAt(m_priceTexts, i);
            foreach (TextMeshProUGUI text in texts)
                if (text != count && text != price) { text.text = rarityNames[i]; break; }
        }

        RectTransform content = m_closeButton != null ? m_closeButton.transform.parent as RectTransform : null;
        if (content == null) return;
        TextMeshProUGUI topText = null;
        TextMeshProUGUI lowerText = null;
        foreach (Transform child in content)
        {
            TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
            if (text == null) continue;
            if (topText == null || text.rectTransform.anchoredPosition.y > topText.rectTransform.anchoredPosition.y)
            {
                lowerText = topText;
                topText = text;
            }
            else if (lowerText == null || text.rectTransform.anchoredPosition.y > lowerText.rectTransform.anchoredPosition.y)
                lowerText = text;
        }
        if (topText != null) topText.text = GameLanguage.Choose("한 번에 판매할 개수를 선택할 수 있어요", "CHOOSE HOW MANY TO SELL AT ONCE");
        if (lowerText != null) lowerText.text = GameLanguage.Choose("영웅 판매", "SELL HEROES");
    }

    static void SetButtonLabel(Button button, string value)
    {
        if (button == null) return;
        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null) text.text = value;
    }

    void BindButtons(Button[] buttons, System.Action<int> clicked)
    {
        if (buttons == null) return;
        for (int i = 0; i < buttons.Length; i++)
        {
            int captured = i;
            buttons[i]?.onClick.AddListener(() => clicked(captured));
        }
    }

    static T GetAt<T>(T[] items, int index) where T : Object =>
        items != null && index >= 0 && index < items.Length ? items[index] : null;

#if UNITY_EDITOR
    public void ConfigureView(
        Button[] rarityButtons, Image[] rarityImages, TextMeshProUGUI[] countTexts,
        TextMeshProUGUI[] priceTexts, Button[] classButtons, Button[] amountButtons, Button closeButton)
    {
        m_rarityButtons = rarityButtons;
        m_rarityImages = rarityImages;
        m_countTexts = countTexts;
        m_priceTexts = priceTexts;
        m_classButtons = classButtons;
        m_amountButtons = amountButtons;
        m_closeButton = closeButton;
    }
#endif

    void SetClass(int index)
    {
        m_selectedClass = index switch
        {
            1 => EntityType.TYPE.Archer,
            2 => EntityType.TYPE.Warrior,
            _ => EntityType.TYPE.Wizard
        };
        Refresh();
    }

    void SetAmount(int index)
    {
        m_sellAmount = index switch { 1 => 10, 2 => 0, _ => 1 };
        Refresh();
    }

    void SellRarity(int index)
    {
        if (GManager.Instance == null || index < 0 || index >= Rarities.Length) return;
        GManager.Instance.SellUnits(m_selectedClass, Rarities[index], m_sellAmount);
        Refresh();
    }

    void Refresh()
    {
        GManager gm = GManager.Instance;
        if (gm == null) return;

        for (int i = 0; i < Rarities.Length; i++)
        {
            int owned = gm.GetUnitCount(m_selectedClass, Rarities[i]);
            int quantity = m_sellAmount <= 0 ? owned : Mathf.Min(m_sellAmount, owned);
            Image rarityImage = GetAt(m_rarityImages, i);
            TextMeshProUGUI countText = GetAt(m_countTexts, i);
            TextMeshProUGUI priceText = GetAt(m_priceTexts, i);
            Button rarityButton = GetAt(m_rarityButtons, i);
            if (rarityImage != null) rarityImage.sprite = gm.GetSprite(m_selectedClass, Rarities[i]);
            if (countText != null) countText.text = owned.ToString();
            if (priceText != null) priceText.text = $"+{gm.GetUnitSellPrice(Rarities[i]) * quantity:N0}";
            if (rarityButton != null) rarityButton.interactable = owned > 0;
        }

        Highlight(m_classButtons, (int)m_selectedClass - (int)EntityType.TYPE.Wizard);
        Highlight(m_amountButtons, m_sellAmount == 1 ? 0 : m_sellAmount == 10 ? 1 : 2);
    }

    static void Highlight(Button[] buttons, int selected)
    {
        if (buttons == null) return;
        for (int i = 0; i < buttons.Length; i++)
            if (buttons[i] != null && buttons[i].targetGraphic != null)
                buttons[i].targetGraphic.color = i == selected
                    ? new Color(1f, 0.65f, 0.12f, 1f)
                    : new Color(0.22f, 0.24f, 0.3f, 1f);
    }
}
