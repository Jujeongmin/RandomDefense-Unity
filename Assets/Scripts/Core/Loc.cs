using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DefenseBattle.Core
{
    /// <summary>
    /// 화면 문구 (원본 i18n.ts tl()). 한국어 원문이 키이고 {0}·{1} 자리에 인자가 들어간다.
    /// Core 는 번역 표를 모른다 - Unity 쪽 다국어 모듈이 Translate 를 바꿔 끼운다.
    /// </summary>
    public static class Loc
    {
        /// <summary>원문 키 → 현재 언어 문장. 기본값은 한국어 원문 그대로</summary>
        public static Func<string, string> Translate = key => key;

        /// <summary>아이콘 이름 → 문장에 끼울 표시 (기본: TextMeshPro 스프라이트 태그)</summary>
        public static Func<string, string> Icon = name => "<sprite name=\"" + name + "\">";

        static readonly Regex s_slot = new Regex(@"\{(\d+)\}");

        /// <summary>원본 tl(원문, ...인자). 같은 원문이 뜻이 다르면 키 뒤에 @@문맥 (표시 때 떼어 냄)</summary>
        public static string Tl(string key, params object[] args)
        {
            string s = Translate(key);
            int at = s.IndexOf("@@", StringComparison.Ordinal);
            if (at >= 0) s = s.Substring(0, at);
            if (args == null || args.Length == 0) return s;
            return s_slot.Replace(s, m =>
            {
                int i = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                return i < args.Length ? Format(args[i]) : m.Value;
            });
        }

        /// <summary>JS 문자열 변환과 같게 (6.5 → "6.5", 3 → "3")</summary>
        static string Format(object v)
        {
            switch (v)
            {
                case double d: return d.ToString("R", CultureInfo.InvariantCulture);
                case float f: return ((double)f).ToString("R", CultureInfo.InvariantCulture);
                case IFormattable fm: return fm.ToString(null, CultureInfo.InvariantCulture);
                default: return v?.ToString() ?? "";
            }
        }
    }
}
