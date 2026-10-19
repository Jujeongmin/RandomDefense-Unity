using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// 디펜스 배틀 에셋 임포트 설정 자동화 (Assets/Art/DefenseBattle, Assets/Audio/DefenseBattle).
/// 픽셀아트는 번지지 않게(Point, 무압축, 밉맵 없음), 유닛·몹 시트(96×128 = 32px 3열×4행)는 12장으로 자르고,
/// UI 프레임은 원본 CSS border-image 값으로 9-slice 테두리를 넣는다. 새 파일을 넣거나 Reimport 하면 다시 적용된다.
/// </summary>
public class DefenseBattleArtImporter : AssetPostprocessor
{
    const string ArtRoot = "Assets/Art/DefenseBattle/";
    const string AudioRoot = "Assets/Audio/DefenseBattle/";

    /// <summary>캐릭터 시트 한 칸 (px) - RPG Maker 형식</summary>
    public const int Cell = 32;
    /// <summary>스프라이트·타일 PPU: 32px = 월드 1 단위</summary>
    public const int WorldPpu = 32;

    /// <summary>
    /// 9-slice 테두리 (왼, 아래, 오른, 위 px) - 원본 style.css 의 border-image slice 값.
    /// btn_* / paper* = 44, ribbon_* = 20 128 8 128 (위 오른 아래 왼), wood = 96, wood_tight = 53 52 71 52
    /// </summary>
    static Vector4 BorderFor(string fileName)
    {
        if (fileName.StartsWith("btn_") || fileName.StartsWith("paper")) return new Vector4(44, 44, 44, 44);
        if (fileName.StartsWith("ribbon_")) return new Vector4(128, 8, 128, 20);
        if (fileName == "wood") return new Vector4(96, 96, 96, 96);
        if (fileName == "wood_tight") return new Vector4(52, 71, 52, 53);
        return Vector4.zero;
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(ArtRoot)) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;

        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        bool isSheet = assetPath.StartsWith(ArtRoot + "Sprites/");
        bool isTile = assetPath.StartsWith(ArtRoot + "Tiles/");
        importer.spritePixelsPerUnit = isSheet || isTile ? WorldPpu : 100;

        if (isSheet)
        {
            importer.spriteImportMode = SpriteImportMode.Multiple;
            SliceSheet(importer, fileName);
            return;
        }
        importer.spriteImportMode = SpriteImportMode.Single;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; // 9-slice·픽셀 정렬에 맞게
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        importer.SetTextureSettings(settings);
        if (assetPath.StartsWith(ArtRoot + "UI/Frame/")) importer.spriteBorder = BorderFor(fileName);
    }

    /// <summary>
    /// 시트를 32px 칸으로 자른다. 이름 = {파일}_{방향}_{프레임}, 방향 0=하 1=좌 2=우 3=상 (위에서부터 행), 프레임 0~2 (1 = 서 있기)
    /// </summary>
    void SliceSheet(TextureImporter importer, string fileName)
    {
        if (!ReadPngSize(assetPath, out int width, out int height)) return;
        int cols = width / Cell, rows = height / Cell;
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider dp = factory.GetSpriteEditorDataProviderFromObject(importer);
        dp.InitSpriteEditorDataProvider();

        // 이미 자른 적이 있으면 같은 이름의 id 를 그대로 써서 씬·프리팹 참조가 끊기지 않게
        var oldIds = new Dictionary<string, GUID>();
        foreach (SpriteRect r in dp.GetSpriteRects()) oldIds[r.name] = r.spriteID;

        var rects = new List<SpriteRect>();
        var pairs = new List<SpriteNameFileIdPair>();
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                string name = fileName + "_" + row + "_" + col;
                GUID id = oldIds.TryGetValue(name, out GUID g) ? g : GUID.Generate();
                rects.Add(new SpriteRect
                {
                    name = name,
                    spriteID = id,
                    rect = new Rect(col * Cell, height - (row + 1) * Cell, Cell, Cell), // Unity 좌표는 아래가 0
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                });
                pairs.Add(new SpriteNameFileIdPair(name, id));
            }
        }
        dp.SetSpriteRects(rects.ToArray());
        dp.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
        dp.Apply();
    }

    /// <summary>PNG 헤더(IHDR)에서 크기 읽기 - 임포트 전이라 텍스처를 아직 못 읽는다</summary>
    static bool ReadPngSize(string path, out int width, out int height)
    {
        width = height = 0;
        using (FileStream fs = File.OpenRead(path))
        {
            var header = new byte[24];
            if (fs.Read(header, 0, 24) < 24) return false;
            width = header[16] << 24 | header[17] << 16 | header[18] << 8 | header[19];
            height = header[20] << 24 | header[21] << 16 | header[22] << 8 | header[23];
        }
        return width > 0 && height > 0;
    }

    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(AudioRoot)) return;
        var importer = (AudioImporter)assetImporter;
        bool bgm = assetPath.StartsWith(AudioRoot + "Bgm/");
        AudioImporterSampleSettings s = importer.defaultSampleSettings;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        // BGM 은 길어서 스트리밍 (메모리), 효과음은 짧고 자주 나서 메모리에 풀어 둔다 (재생 지연·CPU)
        s.loadType = bgm ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        s.quality = bgm ? 0.5f : 0.7f;
        importer.defaultSampleSettings = s;
        importer.forceToMono = !bgm;
        importer.loadInBackground = bgm;
    }
}
