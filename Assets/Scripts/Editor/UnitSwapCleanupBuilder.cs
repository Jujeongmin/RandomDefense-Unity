using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class UnitSwapCleanupBuilder
{
    [MenuItem("Tools/Random Defense/Remove Unit Swap Hint")]
    public static void RemoveHint()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
        GameObject hint = GameObject.Find("UnitSwapHint");
        if (hint != null) Object.DestroyImmediate(hint);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Unit swap text hint removed. Drag arrow visualization is runtime-driven.");
    }
}
