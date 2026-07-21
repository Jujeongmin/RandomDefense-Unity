#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// 안드로이드 적응형 아이콘(Adaptive Icon)의 레이어가 비어 있던 문제를 고칩니다.
/// 레이어가 비면 빌드 시 Unity 기본 템플릿(유니티 로고)이 대신 들어가서
/// 홈 화면 아이콘에 유니티 로고가 겹쳐 보였습니다.
/// 배경 = 네이비 단색, 전경 = 캐릭터+루비 아이콘으로 채웁니다.
/// 일회성 실행 후 지워도 됩니다.
/// </summary>
public static class FixAdaptiveIcon
{
    const string ForegroundPath = "Assets/GData/Image/Store/app-icon-512-v2.png";
    const string BackgroundPath = "Assets/GData/Image/UI/adaptive-icon-background.png";

    [MenuItem("Tools/Random Defense/Fix Adaptive Icon Background")]
    public static void Fix()
    {
        EnsureSpriteImport(BackgroundPath);

        Texture2D foreground = AssetDatabase.LoadAssetAtPath<Texture2D>(ForegroundPath);
        Texture2D background = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
        if (foreground == null) throw new System.InvalidOperationException($"전경 텍스처를 찾지 못했습니다: {ForegroundPath}");
        if (background == null) throw new System.InvalidOperationException($"배경 텍스처를 찾지 못했습니다: {BackgroundPath}");

        // 적응형: 레이어 0 = 배경, 레이어 1 = 전경
        PlatformIcon[] adaptive = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive);
        foreach (PlatformIcon icon in adaptive)
            icon.SetTextures(background, foreground);
        PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive, adaptive);

        // 레거시/라운드도 모든 슬롯을 아이콘 아트로 채운다 (구형 기기·일부 런처 대응)
        FillSingleLayer(AndroidPlatformIconKind.Legacy, foreground);
        FillSingleLayer(AndroidPlatformIconKind.Round, foreground);

        AssetDatabase.SaveAssets();
        Debug.Log($"[FixAdaptiveIcon] 적응형 {adaptive.Length}개 슬롯에 배경+전경 레이어를 채웠습니다.");
    }

    static void FillSingleLayer(PlatformIconKind kind, Texture2D texture)
    {
        PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
        foreach (PlatformIcon icon in icons)
            icon.SetTextures(texture);
        PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
    }

    static void EnsureSpriteImport(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new System.InvalidOperationException($"텍스처를 찾지 못했습니다: {path}");

        bool alreadyCorrect = importer.textureType == TextureImporterType.Sprite
            && importer.spriteImportMode == SpriteImportMode.Single
            && !importer.mipmapEnabled;
        if (alreadyCorrect) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }
}
#endif
