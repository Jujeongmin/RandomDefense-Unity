#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 런타임 생성 대신 씬에 UI를 미리 배치하는 빌더.
/// - MainScene: 모드/난이도 버튼(START 위·아래), 확률 정보 버튼, 확률 표기 패널
/// - GameScene: 판매 패널의 '고급이하 자동판매' 토글 버튼
/// 실행 후 각 컴포넌트의 직렬화 필드에 자동으로 연결되므로 런타임 폴백 생성은 동작하지 않습니다.
/// </summary>
public static class PreplacedUiBuilder
{
    const string MainScenePath = "Assets/Scenes/MainScene.unity";
    const string GameScenePath = "Assets/Scenes/GameScene.unity";
    const string KoreanFontPath = "Assets/GData/Fonts/Paperlogy-9Black SDF.asset";
    const int UiLayer = 5;

    [MenuItem("Tools/Random Defense/Place Main Menu Extra UI (Mode·Odds)")]
    public static void PlaceMainMenuUi()
    {
        TMP_FontAsset font = LoadFont();
        Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

        MainMenuManager manager = Object.FindAnyObjectByType<MainMenuManager>(FindObjectsInactive.Include);
        if (manager == null) throw new System.InvalidOperationException("MainScene에서 MainMenuManager를 찾지 못했습니다.");
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) throw new System.InvalidOperationException("MainScene에서 Canvas를 찾지 못했습니다.");

        SerializedObject so = new SerializedObject(manager);
        Button start = so.FindProperty("m_startButton").objectReferenceValue as Button;
        if (start == null) throw new System.InvalidOperationException("MainMenuManager의 Start 버튼이 비어 있습니다.");

        // 난이도 기능 제거: 이전 빌드로 배치된 DifficultyButton이 있으면 삭제
        Transform oldDifficulty = start.transform.parent.Find("DifficultyButton");
        if (oldDifficulty != null) Object.DestroyImmediate(oldDifficulty.gameObject);

        Button mode = CloneButton(start, "ModeButton", -1, "모드: 일반");
        Button odds = BuildOddsButton(canvas.transform, font);
        Button ranking = BuildCornerButton(canvas.transform, font, "RankingButton", new Vector2(16f, 166f), "랭킹");
        RarityOddsPanel panel = BuildOddsPanel(canvas.transform, font);

        so.FindProperty("m_modeButton").objectReferenceValue = mode;
        so.FindProperty("m_oddsButton").objectReferenceValue = odds;
        so.FindProperty("m_rankingButton").objectReferenceValue = ranking;
        so.FindProperty("m_oddsPanel").objectReferenceValue = panel;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("MainScene: ModeButton / OddsButton / RarityOddsPanel 배치 완료.");
    }

    static TMP_FontAsset LoadFont()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        if (font == null) throw new System.InvalidOperationException("Paperlogy-9Black SDF 폰트를 찾지 못했습니다.");
        return font;
    }

    static Button CloneButton(Button source, string name, int stepDown, string label)
    {
        Transform parent = source.transform.parent;
        Transform old = parent.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);

        Button clone = Object.Instantiate(source, parent);
        clone.name = name;
        clone.onClick = new Button.ButtonClickedEvent(); // START의 영구 리스너 제거

        RectTransform src = source.transform as RectTransform;
        RectTransform rect = clone.transform as RectTransform;
        rect.anchoredPosition = src.anchoredPosition - new Vector2(0f, (src.sizeDelta.y + 24f) * stepDown);

        TextMeshProUGUI text = clone.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null) text.text = label;
        return clone;
    }

    static Button BuildOddsButton(Transform canvas, TMP_FontAsset font)
        => BuildCornerButton(canvas, font, "OddsButton", new Vector2(16f, 110f), "확률 정보");

    static Button BuildCornerButton(Transform canvas, TMP_FontAsset font, string name, Vector2 position, string label)
    {
        Transform old = canvas.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);

        return BuildBoxButton(canvas, name, font,
            anchor: new Vector2(0f, 0f), position: position, size: new Vector2(128f, 48f),
            background: new Color(0.16f, 0.24f, 0.38f, 1f), label: label, fontMax: 20f);
    }

    static RarityOddsPanel BuildOddsPanel(Transform canvas, TMP_FontAsset font)
    {
        Transform old = canvas.Find("RarityOddsPanel");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        GameObject go = new GameObject("RarityOddsPanel", typeof(RectTransform));
        go.transform.SetParent(canvas, false);
        RarityOddsPanel panel = go.AddComponent<RarityOddsPanel>();
        panel.Build();

        foreach (TextMeshProUGUI text in go.GetComponentsInChildren<TextMeshProUGUI>(true)) text.font = font;
        foreach (Transform child in go.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = UiLayer;
        go.SetActive(false);
        EditorUtility.SetDirty(panel);
        return panel;
    }

    static Button BuildBoxButton(Transform parent, string name, TMP_FontAsset font,
        Vector2 anchor, Vector2 position, Vector2 size, Color background, string label, float fontMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.layer = UiLayer;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = background;
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        GameObject labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.layer = UiLayer;
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = labelGo.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10f;
        text.fontSizeMax = fontMax;
        text.text = label;
        return button;
    }
}
#endif
