using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 런타임에 생성한 TMP 텍스트에 한글 폰트(Paperlogy)를 적용하기 위한 헬퍼.
/// 폰트 에셋이 Resources에 없으므로, 씬에 이미 있는 TMP 텍스트에서 폰트를 빌려옵니다.
/// (씬의 기존 텍스트는 한글이 렌더링되는 Paperlogy 폰트를 사용 중이므로 안전합니다.)
/// </summary>
public static class GameFont
{
    static TMP_FontAsset s_font;

    public static TMP_FontAsset Korean
    {
        get
        {
            if (s_font != null) return s_font;

            TMP_Text[] texts = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);

            TMP_FontAsset fallback = null;
            foreach (TMP_Text text in texts)
            {
                if (text == null || text.font == null) continue;
                if (fallback == null) fallback = text.font;
                if (text.font.name.IndexOf("Paperlogy", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    s_font = text.font;
                    return s_font;
                }
            }

            s_font = fallback;
            return s_font;
        }
    }

    public static void Apply(TMP_Text text)
    {
        if (text == null) return;
        TMP_FontAsset font = Korean;
        if (font != null) text.font = font;
    }
}
