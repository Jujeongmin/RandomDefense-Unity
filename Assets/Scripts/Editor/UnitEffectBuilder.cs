using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class UnitEffectBuilder
{
    const string OutputDir = "Assets/GData/Prefabs/Effect";
    const string CfxrDir = "Assets/Down/JMO Assets/Cartoon FX Remaster/CFXR Prefabs";
    const string GapDir = "Assets/GabrielAguiarProductions/FreeQuickEffectsVol1/Prefabs";
    const string LegacyWizardEffectPath = OutputDir + "/WAttact_0.prefab";

    // 발사체: (출력 이름, 원본 팩 프리팹, 스케일, 속도) — 숫자만 고치고 메뉴를 다시 실행하면 반영
    // 등급 0~3: 기본 / 4(신화), 5(태초): 상위 이펙트
    static readonly (string name, string source, float scale, float speed)[] Projectiles =
    {
        ("Attack_Wizard",          $"{GapDir}/vfx_Projectile_01.prefab",                                 0.25f, 15f),
        ("Attack_Archer",          $"{GapDir}/vfx_Projectile_02.prefab",                                 0.25f, 22f),
        ("Attack_Warrior",         $"{CfxrDir}/Sword Trails/Plain/CFXR4 Sword Hit PLAIN (Cross).prefab", 0.3f,  15f),
        ("Attack_Wizard_Mythic",   $"{CfxrDir}/Impacts/CFXR Impact Glowing HDR (Blue).prefab",           0.22f, 15f),
        ("Attack_Wizard_Eternal",  $"{GapDir}/vfx_Implosion_01.prefab",                                  0.1f,  15f),
        ("Attack_Archer_Mythic",   $"{CfxrDir}/Electric/CFXR3 Hit Electric C (Air).prefab",              0.22f, 22f),
        ("Attack_Archer_Eternal",  $"{GapDir}/vfx_Hyperdrive_01.prefab",                                 0.1f,  24f),
        ("Attack_Warrior_Mythic",  $"{CfxrDir}/Sword Trails/Fire/CFXR4 Sword Hit FIRE (Cross).prefab",   0.25f, 15f),
        ("Attack_Warrior_Eternal", $"{CfxrDir}/Sword Trails/Ice/CFXR4 Sword Hit ICE (Cross).prefab",     0.28f, 15f),
    };

    // 버스트: 태초 유닛 공격 시 몹 경로 전체에 순차 폭발 (제자리 재생 후 풀 반환)
    // alpha: 파티클 시작색 알파 배율 (반투명 연출)
    static readonly (string name, string source, float scale, float duration, float alpha)[] Bursts =
    {
        ("Attack_EternalPathBurst", $"{CfxrDir}/Explosions/CFXR Explosion 1.prefab", 0.35f, 1.2f, 0.5f),
    };

    // 이전 버전 스크립트가 만들었을 수 있는 프리팹 (있으면 삭제)
    static readonly string[] ObsoleteEffects =
    {
        "Attack_Orc", "Attack_Undead", "Attack_Troll",
        "Attack_BossOrc", "Attack_BossTroll", "Attack_BossUndead",
    };

    [MenuItem("Tools/Random Defense/Build Unit Attack Effects")]
    public static void Build()
    {
        var built = new Dictionary<string, GameObject>();
        foreach ((string name, string source, float scale, float speed) in Projectiles)
        {
            GameObject prefab = BuildWrapper(name, source, scale, projectileSpeed: speed);
            if (prefab != null) built[name] = prefab;
        }
        foreach ((string name, string source, float scale, float duration, float alpha) in Bursts)
        {
            GameObject prefab = BuildWrapper(name, source, scale, burstDuration: duration, alphaMultiplier: alpha);
            if (prefab != null) built[name] = prefab;
        }

        AssignToEntityData(built);
        CleanupObsolete();
        AssetDatabase.SaveAssets();
        Debug.Log($"유닛 공격 이펙트 빌드 완료: {built.Count}/{Projectiles.Length + Bursts.Length}");
    }

    static GameObject BuildWrapper(string name, string sourcePath, float scale, float? projectileSpeed = null, float? burstDuration = null, float? alphaMultiplier = null)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (source == null)
        {
            Debug.LogError($"원본 프리팹을 찾을 수 없음: {sourcePath}");
            return null;
        }

        var root = new GameObject(name);
        try
        {
            var child = (GameObject)PrefabUtility.InstantiatePrefab(source);
            child.transform.SetParent(root.transform, false);
            child.transform.localPosition = Vector3.zero;
            root.transform.localScale = Vector3.one * scale;

            if (projectileSpeed.HasValue)
            {
                var projectile = root.AddComponent<ProjectileEffect>();
                var so = new SerializedObject(projectile);
                so.FindProperty("m_speed").floatValue = projectileSpeed.Value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            if (burstDuration.HasValue)
            {
                var burst = root.AddComponent<PooledBurstEffect>();
                var so = new SerializedObject(burst);
                so.FindProperty("m_duration").floatValue = burstDuration.Value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (ParticleSystem ps in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = ps.main;
                // 버스트는 PooledBurstEffect.Play()가 delay 후 직접 재생하므로 자동 재생 금지
                main.playOnAwake = projectileSpeed.HasValue;
                main.stopAction = ParticleSystemStopAction.None;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                if (alphaMultiplier.HasValue)
                    main.startColor = MultiplyAlpha(main.startColor, alphaMultiplier.Value);

                // 스모그(연기) 서브 파티클만 크기·방출량 축소
                if (burstDuration.HasValue && ps.name.IndexOf("Smoke", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    main.startSizeMultiplier *= 0.5f;
                    ParticleSystem.EmissionModule emission = ps.emission;
                    emission.rateOverTimeMultiplier *= 0.5f;
                    emission.rateOverDistanceMultiplier *= 0.5f;
                    for (int i = 0; i < emission.burstCount; i++)
                    {
                        ParticleSystem.Burst b = emission.GetBurst(i);
                        b.count = new ParticleSystem.MinMaxCurve(
                            b.count.constantMin * 0.5f, b.count.constantMax * 0.5f);
                        emission.SetBurst(i, b);
                    }
                }
            }

            // CFXR_Effect: 자동 파괴 방지(풀 재사용) + 카메라 셰이크 비활성화
            foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null || mb.GetType().Name != "CFXR_Effect") continue;
                var so = new SerializedObject(mb);
                SerializedProperty clear = so.FindProperty("clearBehavior");
                if (clear != null) clear.enumValueIndex = 0;
                SerializedProperty shake = so.FindProperty("cameraShake.enabled");
                if (shake != null) shake.boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // 클릭(레이캐스트)에 걸리지 않도록: 콜라이더 비활성화 + Ignore Raycast 레이어
            foreach (Collider c in root.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            foreach (Collider2D c in root.GetComponentsInChildren<Collider2D>(true)) c.enabled = false;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = 2; // Ignore Raycast

            // 유닛/몹 스프라이트(Order 0)보다 항상 앞에, 몹 체력바(200)보다는 뒤에 그려지도록
            // 내부 렌더러 간 상대 순서는 유지하되 최소 100 보장
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
                r.sortingOrder = Mathf.Max(r.sortingOrder + 100, 100);

            return PrefabUtility.SaveAsPrefabAsset(root, $"{OutputDir}/{name}.prefab");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    static void AssignToEntityData(Dictionary<string, GameObject> built)
    {
        var legacyEffect = AssetDatabase.LoadAssetAtPath<GameObject>(LegacyWizardEffectPath);
        built.TryGetValue("Attack_EternalPathBurst", out GameObject pathBurst);

        string[] guids = AssetDatabase.FindAssets("t:EntityData", new[] { "Assets/GData/Data" });
        int assigned = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<EntityData>(path);
            if (data == null) continue;

            GameObject effect = ResolveEffect(data.name, built, legacyEffect);
            if (effect == null) continue;

            // 태초(_5) 유닛만 경로 폭발 이펙트, 나머지는 null 유지
            bool isEternalUnit = pathBurst != null && !data.name.StartsWith("Mob_") && !data.name.StartsWith("Boss_") && data.name.EndsWith("_5");

            var so = new SerializedObject(data);
            SerializedProperty effectProp = so.FindProperty("m_effect");
            SerializedProperty pathProp = so.FindProperty("m_pathEffect");
            GameObject wantedPath = isEternalUnit ? pathBurst : null;
            if (effectProp.objectReferenceValue != effect || pathProp.objectReferenceValue != wantedPath)
            {
                effectProp.objectReferenceValue = effect;
                pathProp.objectReferenceValue = wantedPath;
                so.ApplyModifiedPropertiesWithoutUndo();
                assigned++;
            }
        }
        Debug.Log($"EntityData {assigned}개 갱신");
    }

    /// <summary>
    /// 시작색의 알파에 배율을 적용 (색상/그라디언트 등 모든 MinMaxGradient 모드 지원)
    /// </summary>
    static ParticleSystem.MinMaxGradient MultiplyAlpha(ParticleSystem.MinMaxGradient value, float multiplier)
    {
        switch (value.mode)
        {
            case ParticleSystemGradientMode.Color:
                value.color = ScaleAlpha(value.color, multiplier);
                break;
            case ParticleSystemGradientMode.TwoColors:
                value.colorMin = ScaleAlpha(value.colorMin, multiplier);
                value.colorMax = ScaleAlpha(value.colorMax, multiplier);
                break;
            case ParticleSystemGradientMode.Gradient:
            case ParticleSystemGradientMode.RandomColor:
                value.gradient = ScaleAlpha(value.gradient, multiplier);
                break;
            case ParticleSystemGradientMode.TwoGradients:
                value.gradientMin = ScaleAlpha(value.gradientMin, multiplier);
                value.gradientMax = ScaleAlpha(value.gradientMax, multiplier);
                break;
        }
        return value;
    }

    static Color ScaleAlpha(Color color, float multiplier)
    {
        color.a *= multiplier;
        return color;
    }

    static Gradient ScaleAlpha(Gradient gradient, float multiplier)
    {
        if (gradient == null) return null;
        GradientAlphaKey[] keys = gradient.alphaKeys;
        for (int i = 0; i < keys.Length; i++) keys[i].alpha *= multiplier;
        var result = new Gradient { mode = gradient.mode };
        result.SetKeys(gradient.colorKeys, keys);
        return result;
    }

    static GameObject ResolveEffect(string assetName, Dictionary<string, GameObject> built, GameObject legacyEffect)
    {
        // 몹/보스는 공격하지 않으므로 기존 WAttact_0 유지
        if (assetName.StartsWith("Mob_") || assetName.StartsWith("Boss_")) return legacyEffect;

        string cls = assetName.StartsWith("Wizard_") ? "Wizard"
            : assetName.StartsWith("Archer_") ? "Archer"
            : assetName.StartsWith("Warrior_") ? "Warrior"
            : null;
        if (cls == null) return null;

        string key = assetName.EndsWith("_5") ? $"Attack_{cls}_Eternal"
            : assetName.EndsWith("_4") ? $"Attack_{cls}_Mythic"
            : $"Attack_{cls}";
        return built.TryGetValue(key, out GameObject effect) ? effect : null;
    }

    static void CleanupObsolete()
    {
        foreach (string name in ObsoleteEffects)
        {
            string path = $"{OutputDir}/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"불필요한 이펙트 삭제: {path}");
            }
        }
    }
}
