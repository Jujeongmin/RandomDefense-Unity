#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuCarouselBuilder
{
    const string ScenePath = "Assets/Scenes/MainScene.unity";
    const string KoreanFontPath = "Assets/GData/Fonts/Paperlogy-9Black SDF.asset";
    const float PageWidth = 720f;
    static TMP_FontAsset s_koreanFont;

    [MenuItem("Tools/Random Defense/Rebuild Main Menu Carousel")]
    public static void BuildMainScene()
    {
        s_koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        if (s_koreanFont == null) throw new System.InvalidOperationException("Paperlogy-9Black SDF font was not found.");

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) throw new System.InvalidOperationException("MainScene Canvas was not found.");

        Transform oldCarousel = canvas.transform.Find("MainCarousel");
        if (oldCarousel != null)
        {
            Transform oldMainPage = oldCarousel.Find("Pages/MainPage");
            if (oldMainPage != null)
            {
                var children = new List<Transform>();
                foreach (Transform child in oldMainPage) children.Add(child);
                foreach (Transform child in children) child.SetParent(canvas.transform, false);
            }
            Object.DestroyImmediate(oldCarousel.gameObject);
        }

        GameObject viewportObject = new GameObject("MainCarousel", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(MainMenuCarouselUI));
        viewportObject.layer = 5;
        viewportObject.transform.SetParent(canvas.transform, false);
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        Stretch(viewport);
        viewportObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);

        RectTransform content = CreateRect("Pages", viewport);
        content.anchorMin = new Vector2(0f, 0f);
        content.anchorMax = new Vector2(0f, 1f);
        content.pivot = new Vector2(0f, 0.5f);
        content.sizeDelta = new Vector2(PageWidth * 3f, 0f);
        content.anchoredPosition = new Vector2(-PageWidth, 0f);

        RectTransform shop = CreatePage(content, "ShopPage", 0);
        RectTransform main = CreatePage(content, "MainPage", 1);
        RectTransform research = CreatePage(content, "ResearchPage", 2);

        var existing = new List<Transform>();
        foreach (Transform child in canvas.transform)
        {
            if (child != viewport) existing.Add(child);
        }
        foreach (Transform child in existing) child.SetParent(main, false);

        var products = new List<MainMenuCarouselUI.ProductBinding>();
        TextMeshProUGUI shopStatus;
        Button rewardButton;
        TextMeshProUGUI rewardText;
        BuildShop(shop, products, out shopStatus, out rewardButton, out rewardText);

        var researchRows = new List<MainMenuCarouselUI.ResearchBinding>();
        BuildResearch(research, researchRows);
        TextMeshProUGUI crystalText = BuildTopBar(viewport);

        MainMenuCarouselUI controller = viewportObject.GetComponent<MainMenuCarouselUI>();
        controller.Configure(viewport, content, crystalText, products.ToArray(), rewardButton, rewardText, shopStatus, researchRows.ToArray());
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("MainScene carousel was rebuilt as editable scene objects.");
    }

    [MenuItem("Tools/Random Defense/Configure Result Reward UI")]
    public static void BuildResultRewardUI()
    {
        s_koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
        ResultPanel[] panels = Object.FindObjectsByType<ResultPanel>(FindObjectsInactive.Include);
        if (panels.Length == 0) throw new System.InvalidOperationException("GameScene ResultPanel was not found.");

        SerializedObject serializedPanel = new SerializedObject(panels[0]);
        Button basicButton = serializedPanel.FindProperty("m_restartButton").objectReferenceValue as Button;
        Button doubleButton = serializedPanel.FindProperty("m_exitButton").objectReferenceValue as Button;
        TextMeshProUGUI title = serializedPanel.FindProperty("m_titleText").objectReferenceValue as TextMeshProUGUI;
        TextMeshProUGUI details = serializedPanel.FindProperty("m_detailsText").objectReferenceValue as TextMeshProUGUI;

        ConfigureRewardButton(basicButton, "BasicRewardButton", "보상받기", -150f);
        ConfigureRewardButton(doubleButton, "DoubleRewardButton", "광고 보고 2배 받기", 150f);
        if (title != null) title.font = s_koreanFont;
        if (details != null) details.font = s_koreanFont;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("GameScene result reward UI was configured.");
    }

    [MenuItem("Tools/Random Defense/Configure Main Settings UI")]
    public static void BuildMainSettingsUI()
    {
        s_koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        if (s_koreanFont == null) throw new System.InvalidOperationException("Paperlogy-9Black SDF font was not found.");

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        MainMenuManager manager = Object.FindAnyObjectByType<MainMenuManager>();
        if (canvas == null || manager == null)
            throw new System.InvalidOperationException("MainScene Canvas or MainMenuManager was not found.");

        Transform oldPanel = canvas.transform.Find("MainSettingsPanel");
        if (oldPanel != null) Object.DestroyImmediate(oldPanel.gameObject);

        RectTransform overlay = CreatePanel("MainSettingsPanel", canvas.transform, new Color(0f, 0f, 0f, 0.72f));
        Stretch(overlay);
        RectTransform card = CreatePanel("SettingsCard", overlay, new Color(0.07f, 0.1f, 0.16f, 1f));
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(500f, 610f);

        TextMeshProUGUI title = CreateText("SettingsTitle", card, "설정", 40f, TextAlignmentOptions.Center);
        Position(title.rectTransform, new Vector2(0f, 245f), new Vector2(420f, 70f));

        Button sound = CreateSettingsButton("SoundButton", card, "사운드  켜짐", new Vector2(0f, 155f), new Vector2(380f, 72f));
        TextMeshProUGUI soundText = sound.GetComponentInChildren<TextMeshProUGUI>(true);

        TextMeshProUGUI languageLabel = CreateText("LanguageLabel", card, "LANGUAGE", 22f, TextAlignmentOptions.Center);
        Position(languageLabel.rectTransform, new Vector2(0f, 85f), new Vector2(380f, 40f));
        Button korean = CreateSettingsButton("KoreanButton", card, "한국어", new Vector2(-100f, 30f), new Vector2(180f, 64f));
        Button english = CreateSettingsButton("EnglishButton", card, "English", new Vector2(100f, 30f), new Vector2(180f, 64f));

        Button reset = CreateSettingsButton("ResetDataButton", card, "데이터 초기화", new Vector2(0f, -80f), new Vector2(380f, 72f));
        reset.targetGraphic.color = new Color(0.55f, 0.16f, 0.18f, 1f);
        TextMeshProUGUI resetText = reset.GetComponentInChildren<TextMeshProUGUI>(true);
        Button close = CreateSettingsButton("CloseButton", card, "닫기", new Vector2(0f, -185f), new Vector2(380f, 72f));
        TextMeshProUGUI closeText = close.GetComponentInChildren<TextMeshProUGUI>(true);

        Button setting = FindNamedComponent<Button>("Setting");
        TextMeshProUGUI highWave = FindNamedComponent<TextMeshProUGUI>("HighWave");
        SerializedObject serializedManager = new SerializedObject(manager);
        serializedManager.FindProperty("m_settingButton").objectReferenceValue = setting;
        serializedManager.FindProperty("m_highWaveText").objectReferenceValue = highWave;
        serializedManager.FindProperty("m_settingPanel").objectReferenceValue = overlay.gameObject;
        serializedManager.FindProperty("m_settingsTitleText").objectReferenceValue = title;
        serializedManager.FindProperty("m_soundButton").objectReferenceValue = sound;
        serializedManager.FindProperty("m_soundButtonText").objectReferenceValue = soundText;
        serializedManager.FindProperty("m_koreanButton").objectReferenceValue = korean;
        serializedManager.FindProperty("m_englishButton").objectReferenceValue = english;
        serializedManager.FindProperty("m_resetButton").objectReferenceValue = reset;
        serializedManager.FindProperty("m_resetButtonText").objectReferenceValue = resetText;
        serializedManager.FindProperty("m_closeButton").objectReferenceValue = close;
        serializedManager.FindProperty("m_closeButtonText").objectReferenceValue = closeText;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();

        overlay.gameObject.SetActive(false);
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("MainScene settings UI was created and assigned as scene objects.");
    }

    static Button CreateSettingsButton(string name, Transform parent, string label, Vector2 position, Vector2 size)
    {
        Button button = CreateButton(name, parent, label);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout != null) Object.DestroyImmediate(layout);
        return button;
    }

    static void Position(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static T FindNamedComponent<T>(string objectName) where T : Component
    {
        T[] components = Object.FindObjectsByType<T>(FindObjectsInactive.Include);
        foreach (T component in components)
            if (component.name == objectName) return component;
        return null;
    }

    static void ConfigureRewardButton(Button button, string objectName, string label, float x)
    {
        if (button == null) return;
        button.gameObject.name = objectName;
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(260f, 100f);
        rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text == null) return;
        text.text = label;
        text.font = s_koreanFont;
        text.fontSize = 24f;
    }

    static RectTransform CreatePage(RectTransform content, string name, int index)
    {
        RectTransform page = CreateRect(name, content);
        page.anchorMin = new Vector2(0f, 0f);
        page.anchorMax = new Vector2(0f, 1f);
        page.pivot = new Vector2(0f, 0.5f);
        page.anchoredPosition = new Vector2(index * PageWidth, 0f);
        page.sizeDelta = new Vector2(PageWidth, 0f);
        return page;
    }

    static void BuildShop(
        RectTransform page,
        List<MainMenuCarouselUI.ProductBinding> products,
        out TextMeshProUGUI status,
        out Button rewardButton,
        out TextMeshProUGUI rewardText)
    {
        RectTransform panel = CreateContentPanel(page, "상점");
        AddProduct(panel, products, "작은 크리스탈", "크리스탈 500개  /  ₩1,100", "crystal_500", 500, false);
        AddProduct(panel, products, "중간 크리스탈", "크리스탈 1,200개  /  ₩2,200", "crystal_1200", 1200, false);
        AddProduct(panel, products, "큰 크리스탈", "크리스탈 3,000개  /  ₩4,900", "crystal_3000", 3000, false);
        AddProduct(panel, products, "특대 크리스탈", "크리스탈 6,500개  /  ₩9,900", "crystal_6500", 6500, false);
        AddProduct(panel, products, "광고 제거 + 3배속", "영구 적용  /  ₩4,400", "remove_ads", 0, true);
        rewardButton = CreateButton("RewardAd", panel, "광고 시청  +50 크리스탈");
        rewardText = rewardButton.GetComponentInChildren<TextMeshProUGUI>();
        status = CreateText("ShopStatus", panel, "오른쪽으로 밀어 메인 화면으로 이동", 20, TextAlignmentOptions.Center);
        status.rectTransform.sizeDelta = new Vector2(0f, 56f);
    }

    static void AddProduct(RectTransform parent, List<MainMenuCarouselUI.ProductBinding> products, string title, string description, string id, int crystals, bool removesAds)
    {
        Button button = CreateButton(title, parent, $"{title}\n<size=70%>{description}</size>");
        products.Add(new MainMenuCarouselUI.ProductBinding
        {
            button = button,
            productId = id,
            crystals = crystals,
            removesAds = removesAds
        });
    }

    static void BuildResearch(RectTransform page, List<MainMenuCarouselUI.ResearchBinding> rows)
    {
        RectTransform panel = CreateContentPanel(page, "연구소");
        AddResearch(panel, rows, ResearchType.Attack, "공격력 연구", "현재 증가: 공격력 +0%");
        AddResearch(panel, rows, ResearchType.StartGold, "시작 골드 연구", "현재 증가: 시작 골드 +0");
        AddResearch(panel, rows, ResearchType.GoldGain, "골드 획득 연구", "현재 증가: 처치 골드 +0%");
        AddResearch(panel, rows, ResearchType.RareSummon, "희귀 소환 연구", "현재 증가: 전설 이상 확률 +0.0%p");
        AddResearch(panel, rows, ResearchType.BossDamage, "보스 피해 연구", "현재 증가: 보스 피해 +0%");
        TextMeshProUGUI hint = CreateText("ResearchHint", panel, "왼쪽으로 밀어 메인 화면으로 이동", 20, TextAlignmentOptions.Center);
        hint.rectTransform.sizeDelta = new Vector2(0f, 56f);
    }

    static void AddResearch(RectTransform parent, List<MainMenuCarouselUI.ResearchBinding> rows, ResearchType type, string title, string effect)
    {
        Button button = CreateButton(type.ToString(), parent, $"{title}\n<size=70%>{effect}</size>");
        rows.Add(new MainMenuCarouselUI.ResearchBinding
        {
            type = type,
            button = button,
            info = button.GetComponentInChildren<TextMeshProUGUI>()
        });
    }

    static TextMeshProUGUI BuildTopBar(RectTransform viewport)
    {
        RectTransform bar = CreatePanel("CurrencyBar", viewport, new Color(0.06f, 0.08f, 0.12f, 0.9f));
        bar.anchorMin = new Vector2(0.5f, 1f);
        bar.anchorMax = new Vector2(0.5f, 1f);
        bar.pivot = new Vector2(0.5f, 1f);
        bar.anchoredPosition = new Vector2(0f, -20f);
        bar.sizeDelta = new Vector2(260f, 64f);
        TextMeshProUGUI text = CreateText("CrystalText", bar, "0", 28, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        return text;
    }

    static RectTransform CreateContentPanel(RectTransform page, string title)
    {
        RectTransform panel = CreatePanel(title + "Panel", page, new Color(0.08f, 0.11f, 0.18f, 0.96f));
        panel.anchorMin = new Vector2(0.08f, 0.08f);
        panel.anchorMax = new Vector2(0.92f, 0.9f);
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 34, 24);
        layout.spacing = 14f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        CreateText("Title", panel, title, 38, TextAlignmentOptions.Center).rectTransform.sizeDelta = new Vector2(0f, 60f);
        return panel;
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        rect.gameObject.AddComponent<CanvasRenderer>();
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return rect;
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, string value, float size, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        rect.gameObject.AddComponent<CanvasRenderer>();
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = s_koreanFont;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    static Button CreateButton(string name, Transform parent, string label)
    {
        RectTransform rect = CreatePanel(name, parent, new Color(0.16f, 0.24f, 0.38f, 1f));
        LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = 94f;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        TextMeshProUGUI text = CreateText("Label", rect, label, 24, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(14f, 8f);
        text.rectTransform.offsetMax = new Vector2(-14f, -8f);
        return button;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
#endif
