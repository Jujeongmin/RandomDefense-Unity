#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// MainScene 타이틀 화면 꾸미기.
/// - 루비 키아트를 풀스크린 배경으로 (페이지들보다 뒤)
/// - 가독성용 세로 그라디언트 오버레이
/// - 스토어 피처 그래픽에서 추출한 타이틀 로고를 빈 Mid 영역에
/// - START/MODE/ODDS/RANKING 버튼을 Universal Stylized UI 스프라이트로 교체
/// - 로고 등장·버튼 펄스·배경 호흡을 담당하는 MainScreenPresenter 연결
///
/// 여러 번 실행해도 안전합니다(기존 결과를 찾아 갱신). 설정 버튼은 자체 아이콘이 있어 건드리지 않습니다.
/// </summary>
public static class MainScreenDecorator
{
    const string MainScenePath = "Assets/Scenes/MainScene.unity";
    const string BackgroundArtPath = "Assets/Store/ruby-portrait-1080x1920.png";
    const string LogoPath = "Assets/GData/Image/UI/title-logo.png";
    const string VignettePath = "Assets/GData/Image/UI/vignette-overlay.png";
    const string ButtonAtlasPath = "Assets/Down/Universal Stylized UI/Atlases/Complete_Stylized_UI_elements_buttons.png";

    const string GoldSquare = "Complete_Stylized_UI_elements_buttons_36"; // 골드 라운드 스퀘어
    const string NavyPill = "Complete_Stylized_UI_elements_buttons_37";   // 네이비 라운드 필

    static readonly Color GoldButtonLabel = new Color(0.11f, 0.08f, 0.30f, 1f); // 골드 위에 얹는 진한 네이비
    static readonly Color NavyButtonLabel = new Color(0.96f, 0.90f, 0.78f, 1f); // 네이비 위에 얹는 크림

    const int UiLayer = 5;

    [MenuItem("Tools/Random Defense/Decorate Main Screen")]
    public static void Decorate()
    {
        EnsureSpriteImport(BackgroundArtPath);
        EnsureSpriteImport(LogoPath);
        EnsureSpriteImport(VignettePath);

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

        Sprite goldSquare = LoadAtlasSprite(GoldSquare);
        Sprite navyPill = LoadAtlasSprite(NavyPill);

        // 1) 배경 — 페이지들보다 뒤에 깔리도록 첫 형제로
        RectTransform background = BuildBackground(canvas);

        // 2) 가독성 오버레이 — 배경 바로 위, 페이지들보다는 아래
        BuildVignette(canvas);

        // 3) 타이틀 로고 — 비어 있는 Mid 영역 상단
        RectTransform mid = FindDeep(canvas.transform, "Mid");
        if (mid == null) throw new System.InvalidOperationException("MainPage의 Mid 영역을 찾지 못했습니다.");
        (RectTransform logo, CanvasGroup logoGroup) = BuildLogo(mid);

        // 4) 버튼 스타일 — 설정 버튼은 자체 아이콘이 있으므로 제외
        StyleButton(start, goldSquare, 2.0f, GoldButtonLabel);
        StyleButton(mode, navyPill, 1.0f, NavyButtonLabel);
        StyleButton(odds, navyPill, 1.5f, NavyButtonLabel);
        StyleButton(ranking, navyPill, 1.5f, NavyButtonLabel);

        // 5) 연출 컴포넌트
        WirePresenter(manager.gameObject, logo, logoGroup, start.transform as RectTransform, background);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[MainScreenDecorator] 메인화면 꾸미기 완료 — 배경 / 오버레이 / 로고 / 버튼 4종 / 연출");
    }

    static RectTransform BuildBackground(Canvas canvas)
    {
        RectTransform rect = EnsureChild(canvas.transform, "Background");
        rect.SetSiblingIndex(0);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        Image image = Ensure<Image>(rect.gameObject);
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundArtPath);
        image.color = Color.white;
        image.raycastTarget = false;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;

        // 기기 화면비가 9:16보다 길어져도 왜곡 없이 화면을 덮도록
        AspectRatioFitter fitter = Ensure<AspectRatioFitter>(rect.gameObject);
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 1080f / 1920f;

        return rect;
    }

    static void BuildVignette(Canvas canvas)
    {
        RectTransform rect = EnsureChild(canvas.transform, "Vignette");
        rect.SetSiblingIndex(1); // 배경 위, 페이지 아래
        Stretch(rect);

        Image image = Ensure<Image>(rect.gameObject);
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(VignettePath);
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.raycastTarget = false;
    }

    static (RectTransform, CanvasGroup) BuildLogo(RectTransform mid)
    {
        RectTransform rect = EnsureChild(mid, "TitleLogo");
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(600f, 196f);
        rect.anchoredPosition = new Vector2(0f, -19f); // 화면 상단 기준 약 140px — 루비와 겹치지 않는다
        rect.localScale = Vector3.one;

        Image image = Ensure<Image>(rect.gameObject);
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;

        CanvasGroup group = Ensure<CanvasGroup>(rect.gameObject);
        group.blocksRaycasts = false;
        group.interactable = false;

        return (rect, group);
    }

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

        // 눌림 반응이 보이도록 컬러 틴트를 살짝
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.86f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
        button.colors = colors;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null) label.color = labelColor; // 텍스트 내용은 건드리지 않는다
    }

    static void WirePresenter(GameObject host, RectTransform logo, CanvasGroup logoGroup, RectTransform start, RectTransform background)
    {
        MainScreenPresenter presenter = Ensure<MainScreenPresenter>(host);
        SerializedObject so = new SerializedObject(presenter);
        so.FindProperty("m_logo").objectReferenceValue = logo;
        so.FindProperty("m_logoGroup").objectReferenceValue = logoGroup;
        so.FindProperty("m_startButton").objectReferenceValue = start;
        so.FindProperty("m_background").objectReferenceValue = background;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---- helpers ----

    static Sprite LoadAtlasSprite(string spriteName)
    {
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(ButtonAtlasPath).OfType<Sprite>()
            .FirstOrDefault(s => s.name == spriteName);
        if (sprite == null)
            throw new System.InvalidOperationException($"버튼 아틀라스에서 '{spriteName}' 스프라이트를 찾지 못했습니다: {ButtonAtlasPath}");
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

    static void EnsureSpriteImport(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new System.InvalidOperationException($"텍스처를 찾지 못했습니다: {path}");

        // 이 프로젝트의 기본 임포트는 Sprite(Multiple)이라 서브에셋이 없는 단일 스프라이트로는
        // 로드되지 않는다. Single로 강제하지 않으면 Image.sprite가 null이 된다.
        bool alreadyCorrect = importer.textureType == TextureImporterType.Sprite
            && importer.spriteImportMode == SpriteImportMode.Single
            && !importer.mipmapEnabled
            && importer.alphaIsTransparency;
        if (alreadyCorrect) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }
}
#endif
