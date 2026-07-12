#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 상점 상품의 표시용 가격(fallbackPrice) 정의 및 씬 적용.
/// 실제 결제 가격은 구글 플레이에서 받아오고, 스토어 연결 전에는 이 값이 표시됩니다.
/// </summary>
public static class ShopPrices
{
    const string ScenePath = "Assets/Scenes/MainScene.unity";

    // productId → 표시용 가격 (여기만 고치면 됩니다)
    static readonly Dictionary<string, string> Prices = new Dictionary<string, string>
    {
        { "crystal_500", "₩1,100" },
        { "crystal_1200", "₩2,200" },
        { "crystal_3000", "₩4,900" },
        { "crystal_6500", "₩9,900" },
        { "remove_ads", "₩4,400" },
    };

    public static string Get(string productId)
        => productId != null && Prices.TryGetValue(productId, out string price) ? price : string.Empty;

    [MenuItem("Tools/Random Defense/Apply Shop Prices")]
    public static void Apply()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        MainMenuCarouselUI shop = Object.FindAnyObjectByType<MainMenuCarouselUI>(FindObjectsInactive.Include);
        if (shop == null) throw new System.InvalidOperationException("MainScene에서 MainMenuCarouselUI를 찾지 못했습니다.");

        SerializedObject so = new SerializedObject(shop);
        SerializedProperty products = so.FindProperty("m_products");
        if (products == null || !products.isArray) throw new System.InvalidOperationException("m_products 배열을 찾지 못했습니다.");

        int applied = 0;
        for (int i = 0; i < products.arraySize; i++)
        {
            SerializedProperty element = products.GetArrayElementAtIndex(i);
            string id = element.FindPropertyRelative("productId").stringValue;
            SerializedProperty fallback = element.FindPropertyRelative("fallbackPrice");
            string price = Get(id);
            if (fallback != null && !string.IsNullOrEmpty(price))
            {
                fallback.stringValue = price;
                applied++;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(shop);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"상점 표시 가격 {applied}개 적용 완료.");
    }
}
#endif
