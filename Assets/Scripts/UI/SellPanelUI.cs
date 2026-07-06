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
    }

    void OnEnable()
    {
        ApplyLanguage();
        Refresh();
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

    void OnRectTransformDimensionsChange()
    {
        RectTransform content = m_closeButton != null ? m_closeButton.transform.parent as RectTransform : null;
        MobileSafeAreaLayout.ApplyBottom(content);
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
