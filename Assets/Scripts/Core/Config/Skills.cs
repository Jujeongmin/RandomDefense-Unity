using System;

namespace DefenseBattle.Core
{
    /// <summary>
    /// 직업 스킬 수치 (config.ts SKILLS, SKILL_KR, SKILL_DESC, skillTier, skillVal).
    /// 일반 등급부터 발동, 1단계 일반·고급 = 기본값, 2단계 정예 = High, 3단계 전설 이상 = High 에서 같은 폭만큼 한 번 더.
    /// </summary>
    public static class Skills
    {
        public const int FromIndex = 0;

        public static int Tier(int gradeIndex) => gradeIndex >= 3 ? 3 : gradeIndex >= 2 ? 2 : 1;

        /// <summary>등급별 스킬 수치. 둘 다 정수면 정수로, cap 이 있으면 넘지 않게</summary>
        public static double Val(int gradeIndex, double b, double high, double cap = double.PositiveInfinity)
        {
            int t = Tier(gradeIndex);
            if (t == 1) return b;
            if (t == 2) return high;
            double v = high + (high - b);
            return Math.Min(cap, JsMath.IsInteger(b) && JsMath.IsInteger(high) ? JsMath.Round(v) : v);
        }

        public static class Archer { public const double ExtraTargets = 1, ExtraTargetsHigh = 2, Ratio = 0.5; }
        public static class Wizard { public const double Radius = 62, RadiusHigh = 86, Ratio = 0.35; }
        public static class Warrior { public const double SlowPct = 0.25, SlowPctHigh = 0.38, Dur = 1.1; }
        public static class Ranger { public const double CritChance = 0.3, CritChanceHigh = 0.45; }
        public static class Frost { public const double Radius = 70, RadiusHigh = 95, SlowPct = 0.3, SlowPctHigh = 0.45, Dur = 1.4; }
        public static class Paladin { public const double BossMult = 1.6, BossMultHigh = 2.0; }
        public static class Bomber { public const double Radius = 95, RadiusHigh = 125, Ratio = 0.6; }
        public static class Assassin { public const double Execute = 0.12, ExecuteHigh = 0.2, Cap = 0.35; }
        public static class Spearman { public const double Pierce = 1, PierceHigh = 2, Ratio = 0.6; }
        public static class Slinger { public const double Push = 26, PushHigh = 40; }
        public static class Monk { public const double Hits = 2, HitsHigh = 3, Ratio = 0.45; }
        public static class Hunter { public const double MarkDur = 3, MarkDurHigh = 4.5, MarkMult = 1.2; }
        public static class Acolyte { public const double Radius = 60, RadiusHigh = 80, Ratio = 0.4; }
        public static class Poisoner { public const double Dps = 0.35, DpsHigh = 0.5, Dur = 4; public const int MaxStack = 3; }
        public static class Storm { public const double Bounces = 2, BouncesHigh = 3, Range = 110, Ratio = 0.6; }
        public static class Crossbow { public const double Length = 260, Width = 26, Ratio = 0.7, RatioHigh = 0.9; }
        public static class Knight { public const double ArmoredMult = 1.6, ArmoredMultHigh = 2.0; }
        public static class Berserker { public const double PerMob = 0.012, Cap = 0.8; }
        public static class Merchant { public const double Gold = 1, GoldHigh = 2; }
        public static class Drummer { public const double Radius = 170, Haste = 0.15, HasteHigh = 0.22; }
        public static class Houndmaster { public const double FastMult = 1.7, FastMultHigh = 2.1; }
        public static class Pyro { public const double Radius = 55, RadiusHigh = 75, Dps = 0.4, Dur = 2.5; }
        public static class Reaper { public const double SoulMult = 2.0; public const int MaxSouls = 3; }
        public static class Cleric { public const double PurifyDur = 3, PurifyDurHigh = 5; }
        public static class Cannoneer { public const double Stun = 0.5, StunHigh = 0.8, Radius = 45, Ratio = 0.5; }
        public static class Chrono { public const double Radius = 85, RadiusHigh = 110, Stop = 0.7, StopHigh = 1.0; }
        public static class Dragoon { public const double Length = 240, Width = 34, Ratio = 0.6, RatioHigh = 0.85; }
        public static class Archmage { public const double Radius = 115, RadiusHigh = 150, Ratio = 0.8; }
        public static class Shadow { public const double CloneRatio = 0.6, CloneRatioHigh = 0.9; }
        public static class Angel { public const double Aura = 0.08, AuraHigh = 0.12, Cap = 0.3; }
        public static class Stormking { public const double Strikes = 3, StrikesHigh = 5, Ratio = 0.45; }
        public static class Alchemist { public const double Chance = 0.06, ChanceHigh = 0.1, Cap = 0.3; }

        /// <summary>공통 치명타</summary>
        public const double CritChance = 0.1;
        public const double CritMult = 2;

        /// <summary>스킬 이름 (한국어 원문 키) - Job 순서</summary>
        public static readonly string[] Kr =
        {
            "다중 사격", "마력 폭발", "둔화 강타", "관통 찌르기", "넉백", "연타", "표식", "성광",
            "저격", "눈보라", "맹독", "연쇄 번개", "관통 화살", "방패 부수기", "광폭화", "전리품", "진군 북소리", "사냥개",
            "심판", "대폭발", "처형", "불바다", "영혼 수확", "정화", "포격", "시간 정지",
            "화염 숨결", "운석", "분신", "축복", "폭풍", "연성",
        };

        /// <summary>스킬 설명 (한국어 원문 키) - Job 순서</summary>
        public static readonly string[] Desc =
        {
            "주변 적 1명(정예 2명·전설 3명)에게 50% 추가 피해",
            "대상 주변 범위에 35% 폭발 피해",
            "대상 이동속도 25%(정예 38%·전설 51%) 감소",
            "대상 뒤 일직선 1명(정예 2명·전설 3명) 관통 60%",
            "맞은 적을 길 뒤로 밀어냄",
            "아주 빠른 약공. 2연타(정예 3연타·전설 4연타)",
            "표식 - 그 적이 받는 모든 피해 +20%",
            "언데드에 강함. 주변 언데드에 성광 피해",
            "긴 사거리. 치명타 확률 30%(정예 45%·전설 60%)",
            "공격력 낮음. 주변 적 모두 30%(정예 45%·전설 60%) 둔화",
            "중독 - 4초간 지속 피해, 3번까지 겹침",
            "번개가 주변 적 2명(정예 3명·전설 4명)에게 튐",
            "화살이 일직선의 적을 모두 관통",
            "방패병·거인·킹·마왕·탱커에게 1.6배(정예 2배·전설 2.4배)",
            "필드 몹이 많을수록 강해짐 (최대 +80%)",
            "공격력 낮음. 막타마다 골드 +1(정예 +2·전설 +3)",
            "공격력 낮음. 주변 아군 공격 속도 +15%(정예 +22%·전설 +29%)",
            "빠른 몹(질주 엘프 등)에게 1.7배(정예 2.1배·전설 2.5배)",
            "보스·정예 몹에게 1.6배(정예 2배·전설 2.4배) 피해",
            "느린 공격. 넓은 범위에 60% 폭발 피해",
            "빠른 공격. 체력 12%(정예 20%·전설 28%) 이하 적 즉시 처치",
            "맞은 자리에 불 장판 - 지나가는 적에게 지속 피해",
            "처치하면 영혼 - 다음 공격 2배, 망령 부활 막음",
            "언데드에 강함. 정화 - 신관 회복·망령 부활 막음",
            "느린 강타. 기절 + 작은 폭발",
            "주변 적의 시간을 잠깐 멈춤",
            "일직선 화염 숨결로 줄지어 오는 적을 태움",
            "느린 공격. 넓은 운석 폭발 80%",
            "분신이 다른 적을 한 번 더 공격",
            "모든 아군 공격력 +8%(정예 +12%·전설 +16%), 여럿이면 합산",
            "공격마다 맵 어디든 번개 3개(정예 5개·전설 7개)",
            "합성할 때 6%(정예 10%·전설 14%) 확률로 한 등급 더",
        };
    }
}
