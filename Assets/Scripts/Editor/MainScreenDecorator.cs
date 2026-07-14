#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// MainScene 타이틀 화면 꾸미기.
/// - 앱 콜드스타트 시 뜨는 타이틀 패널(루비 아트 + 로고 + "터치하여 시작"). 터치하면 사라진다.
/// - 메인화면 배경은 단순 다크 네이비 그라디언트.
/// - 비어 있던 Mid 영역을 랜덤 유닛 3명으로 채운다(진입할 때마다 조합이 바뀜).
/// - START/MODE/ODDS/RANKING 버튼을 Universal Stylized UI 스프라이트로 교체.
///
/// 여러 번 실행해도 안전합니다. 설정 버튼은 자체 아이콘이 있어 건드리지 않습니다.
/// </summary>
public static class MainScreenDecorator
{
    const string MainScenePath = "Assets/Scenes/MainScene.unity";
    const string RubyArtPath = "Assets/Store/ruby-portrait-1080x1920.png";
    const string GradientPath = "Assets/GData/Image/UI/main-bg-gradient.png";
    const string LogoPath = "Assets/GData/Image/UI/title-logo.png";
    const string ButtonAtlasPath = "Assets/Down/Universal Stylized UI/Atlases/Complete_Stylized_UI_elements_buttons.png";
    const string FontPath = "Assets/GData/Fonts/Paperlogy-9Black SDF.asset";
    const string CharacterDir = "Assets/GData/Image/Character";

    const string GoldSquare = "Complete_Stylized_UI_elements_buttons_36";
    const string NavyPill = "Complete_Stylized_UI_elements_buttons_37";

    static readonly Color GoldButtonLabel = new Color(0.11f, 0.08f, 0.30f, 1f);
    static readonly Color NavyButtonLabel = new Color(0.96f, 0.90f, 0.78f, 1f);
    static readonly Color TapTextColor = new Color(0.96f, 0.90f, 0.78f, 1f);

    static readonly string[] UnitClasses = { "Warrior", "Wizard", "Archer" };
    static readonly int[] IdleFrames = { 0, 1, 2 }; // 정면 idle 프레임 (뒷모습 9~11 제외)

    const int UiLayer = 5;

    [MenuItem("Tools/Random Defense/Decorate Main Screen")]
    public static void Decorate()
    {
        EnsureSpriteImport(RubyArtPath, SpriteImportMode.Single);
        EnsureSpriteImport(GradientPath, SpriteImportMode.Single);
        EnsureSpriteImport(LogoPath, SpriteImportMode.Single);

        Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

        MainMenuManager manager = Object.FindAnyObjectByType<MainMenuManager>(FindObjectsInactive.Include);
        if (manager == null) throw new System.InvalidOperationException("MainScene에서 MainMenuManager를 찾지 못했습니다.");
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) throw new System.InvalidOperationException("MainScene에서 Canvas를 찾지 못했습니다.");

        SerializedObject so = new SerializedObject(manager);
        Button start = so.FindProperty("m_startButton").objectReferenceValue as Button;
        Button mode = so.FindProperty("m_modeButton").objectReferenceValue as Button;
        Button odds = so.FindProperty("m_oddsButton").objectReferenceValue as Button;
        Button ranking = so.FindProperty("m_rankingButton").objectReferenceValue as Button;
        if (start == null) throw new System.InvalidOperationException("MainMenuManager의 Start 버튼이 비어 있습니다.");

        // 이전 버전이 남긴 오브젝트/컴포넌트 정리
        RemoveChild(canvas.transform, "Vignette");
        RemoveChild(canvas.transform, "TitleLogo");
        RectTransform midEarly = FindDeep(canvas.transform, "Mid");
        if (midEarly != null) RemoveChild(midEarly, "TitleLogo");
        // 구 버전이 붙였던 MainScreenPresenter는 스크립트가 삭제돼 Missing 컴포넌트로 남는다. 제거한다.
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(manager.gameObject);

        // 1) 단순 그라디언트 배경 (페이지들 뒤)
        BuildGradientBackground(canvas);

        // 2) 랜덤 유닛 쇼케이스 (빈 Mid 영역)
        RectTransform mid = FindDeep(canvas.transform, "Mid");
        if (mid == null) throw new System.InvalidOperationException("MainPage의 Mid 영역을 찾지 못했습니다.");
        BuildUnitShowcase(mid);

        // 3) 버튼 스타일 (설정 버튼 제외)
        Sprite goldSquare = LoadAtlasSprite(GoldSquare);
        Sprite navyPill = LoadAtlasSprite(NavyPill);
        StyleButton(start, goldSquare, 2.0f, GoldButtonLabel);
        StyleButton(mode, navyPill, 1.0f, NavyButtonLabel);
        StyleButton(odds, navyPill, 1.5f, NavyButtonLabel);
        StyleButton(ranking, navyPill, 1.5f, NavyButtonLabel);

        // 4) 콜드스타트 타이틀 패널 (최상단, 모든 것을 덮음)
        BuildTitlePanel(canvas);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[MainScreenDecorator] 메인화면 갱신 완료 — 그라디언트 배경 / 랜덤 유닛 / 버튼 / 타이틀 패널");
    }

    // ---- 배경 ----

    static void BuildGradientBackground(Canvas canvas)
    {
        RectTransform rect = EnsureChild(canvas.transform, "Background");
        rect.SetSiblingIndex(0);
        Stretch(rect);

        Image image = Ensure<Image>(rect.gameObject);
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GradientPath);
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = false;

        // 이전 버전이 붙여 놓은 화면비 피터는 그라디언트엔 불필요
        AspectRatioFitter fitter = rect.GetComponent<AspectRatioFitter>();
        if (fitter != null) Object.DestroyImmediate(fitter);
    }

    // ---- 유닛 쇼케이스 ----

    static void BuildUnitShowcase(RectTransform mid)
    {
        RectTransform root = EnsureChild(mid, "UnitShowcase");
        Stretch(root);

        // 좌·중·우 3자리. 가운데를 살짝 크고 높게 배치해 주인공처럼.
        Image left = EnsureSlot(root, "Slot_L", new Vector2(-190f, -30f), 150f);
        Image center = EnsureSlot(root, "Slot_C", new Vector2(0f, 10f), 180f);
        Image right = EnsureSlot(root, "Slot_R", new Vector2(190f, -30f), 150f);

        MainMenuUnitShowcase showcase = Ensure<MainMenuUnitShowcase>(root.gameObject);
        SerializedObject so = new SerializedObject(showcase);

        SerializedProperty slots = so.FindProperty("m_slots");
        slots.arraySize = 3;
        slots.GetArrayElementAtIndex(0).objectReferenceValue = left;
        slots.GetArrayElementAtIndex(1).objectReferenceValue = center;
        slots.GetArrayElementAtIndex(2).objectReferenceValue = right;

        List<(string name, Sprite[] frames)> looks = CollectUnitLooks();
        SerializedProperty looksProp = so.FindProperty("m_looks");
        looksProp.arraySize = looks.Count;
        for (int i = 0; i < looks.Count; i++)
        {
            SerializedProperty element = looksProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("Name").stringValue = looks[i].name;
            SerializedProperty frames = element.FindPropertyRelative("Frames");
            frames.arraySize = looks[i].frames.Length;
            for (int f = 0; f < looks[i].frames.Length; f++)
                frames.GetArrayElementAtIndex(f).objectReferenceValue = looks[i].frames[f];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log($"[MainScreenDecorator] 유닛 룩 {looks.Count}종 수집");
    }

    static Image EnsureSlot(RectTransform parent, string name, Vector2 pos, float size)
    {
        RectTransform rect = EnsureChild(parent, name);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(size, size);
        rect.localScale = Vector3.one;

        Image image = Ensure<Image>(rect.gameObject);
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.type = Image.Type.Simple;
        return image;
    }

    static List<(string, Sprite[])> CollectUnitLooks()
    {
        var looks = new List<(string, Sprite[])>();
        foreach (string unitClass in UnitClasses)
        {
            for (int tier = 0; tier <= 5; tier++)
            {
                string path = $"{CharacterDir}/{unitClass}_{tier}.png";
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                if (assets == null || assets.Length == 0) continue;

                var byName = assets.OfType<Sprite>().ToDictionary(s => s.name, s => s);
                var frames = new List<Sprite>();
                foreach (int f in IdleFrames)
                    if (byName.TryGetValue($"{unitClass}_{tier}_{f}", out Sprite sprite))
                        frames.Add(sprite);

                if (frames.Count > 0)
                    looks.Add(($"{unitClass}_{tier}", frames.ToArray()));
            }
        }
        if (looks.Count == 0)
            throw new System.InvalidOperationException($"{CharacterDir}에서 유닛 스프라이트를 찾지 못했습니다.");
        return looks;
    }

    // ---- 타이틀 패널 ----

    static void BuildTitlePanel(Canvas canvas)
    {
        RectTransform panel = EnsureChild(canvas.transform, "TitleScreen");
        panel.SetAsLastSibling(); // 모든 UI 위에
        Stretch(panel);

        Image bg = Ensure<Image>(panel.gameObject);
        bg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RubyArtPath);
        bg.color = Color.white;
        bg.type = Image.Type.Simple;
        bg.preserveAspect = false;
        bg.raycastTarget = true; // 탭을 받아 패널을 닫는다

        AspectRatioFitter fitter = Ensure<AspectRatioFitter>(panel.gameObject);
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 1080f / 1920f;

        Ensure<CanvasGroup>(panel.gameObject);

        // 로고
        RectTransform logo = EnsureChild(panel, "Logo");
        logo.anchorMin = logo.anchorMax = new Vector2(0.5f, 1f);
        logo.pivot = new Vector2(0.5f, 1f);
        logo.sizeDelta = new Vector2(600f, 196f);
        logo.anchoredPosition = new Vector2(0f, -159f);
        logo.localScale = Vector3.one;
        Image logoImage = Ensure<Image>(logo.gameObject);
        logoImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
        logoImage.color = Color.white;
        logoImage.preserveAspect = true;
        logoImage.raycastTarget = false;

        // "터치하여 시작"
        RectTransform tap = EnsureChild(panel, "TapText");
        tap.anchorMin = tap.anchorMax = new Vector2(0.5f, 0f);
        tap.pivot = new Vector2(0.5f, 0f);
        tap.sizeDelta = new Vector2(600f, 80f);
        tap.anchoredPosition = new Vector2(0f, 150f);
        tap.localScale = Vector3.one;
        TextMeshProUGUI tapText = Ensure<TextMeshProUGUI>(tap.gameObject);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font != null) tapText.font = font;
        tapText.fontSize = 40f;
        tapText.alignment = TextAlignmentOptions.Center;
        tapText.color = TapTextColor;
        tapText.raycastTarget = false;
        tapText.text = GameLanguage.IsEnglish ? "TAP TO START" : "터치하여 시작";

        TitleScreenPanel component = Ensure<TitleScreenPanel>(panel.gameObject);
        SerializedObject so = new SerializedObject(component);
        so.FindProperty("m_logo").objectReferenceValue = logo;
        so.FindProperty("m_tapText").objectReferenceValue = tapText;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---- 버튼 ----

    static void StyleButton(Button button, Sprite sprite, float pixelsPerUnitMultiplier, Color labelColor)
    {
        if (button == null || sprite == null) return;

        Image image = button.GetComponent<Image>();
        if (image == null) return;

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
        image.color = Color.white;
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.82f, 0.82f, 0.86f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
        button.colors = colors;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null) label.color = labelColor; // 텍스트 내용은 건드리지 않는다
    }

    // ---- helpers ----

    static Sprite LoadAtlasSprite(string spriteName)
    {
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(ButtonAtlasPath).OfType<Sprite>()
            .FirstOrDefault(s => s.name == spriteName);
        if (sprite == null)
            throw new System.InvalidOperationException($"버튼 아틀라스에서 '{spriteName}'를 찾지 못했습니다: {ButtonAtlasPath}");
        return sprite;
    }

    static RectTransform EnsureChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return (RectTransform)existing;

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = UiLayer;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        Stretch(rect);
        return rect;
    }

    static void RemoveChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    static T Ensure<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    static RectTransform FindDeep(Transform root, string name)
    {
        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
            if (rect.name == name) return rect;
        return null;
    }

    static void EnsureSpriteImport(string path, SpriteImportMode mode)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new System.InvalidOperationException($"텍스처를 찾지 못했습니다: {path}");

        bool ok = importer.textureType == TextureImporterType.Sprite
            && importer.spriteImportMode == mode
            && !importer.mipmapEnabled
            && importer.alphaIsTransparency;
        if (ok) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = mode;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }
}
#endif
