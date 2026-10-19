namespace DefenseBattle.Core
{
    /// <summary>등급 6단계와 사거리 (config.ts GRADES ~ GLOBAL_RANGE_FROM_INDEX, MERGE_COUNT)</summary>
    public static class Grades
    {
        static Grade G(GradeKey key, string kr, double min, double max, double rate, int sell, uint color)
            => new Grade { Key = key, Kr = kr, Index = (int)key, AtkMin = min, AtkMax = max, SummonRate = rate, SellGold = sell, Color = color };

        // 공격력은 등급마다 3배: 같은 등급 3마리 합성 = 합친 공격력 그대로 + 사거리·스킬 보너스.
        public static readonly Grade[] All =
        {
            G(GradeKey.Common,    "일반", 10, 15,     50,  10,   0xb8b8b8),
            G(GradeKey.Rare,      "고급", 30, 45,     33,  25,   0x4caf50),
            G(GradeKey.Elite,     "정예", 90, 135,    10,  60,   0x2196f3),
            G(GradeKey.Legendary, "전설", 270, 405,   6.5, 150,  0x9c27b0),
            G(GradeKey.Mythic,    "신화", 810, 1215,  0.4, 400,  0xff9800),
            G(GradeKey.Eternal,   "태초", 2430, 3645, 0.1, 1000, 0xf44336),
        };

        public static Grade Get(GradeKey key) => All[(int)key];
        public static int Count => All.Length;

        // 등급별 사거리(픽셀, 전장 720). 유닛은 자기 구역 앞 변과 양쪽 모서리 근처만 지킨다 →
        // 어느 직업을 어느 구역에 둘지, 상대가 던지는 쪽에 맞춰 구역을 바꾸는 게 전략이 된다. 태초만 맵 전체.
        public static readonly double[] Range = { 220, 240, 260, 290, 320, double.PositiveInfinity };
        /// <summary>이 등급부터 맵 전체 사거리 (태초)</summary>
        public const int GlobalRangeFromIndex = 5;

        /// <summary>같은 직업·등급 3개 → 상위 등급 1개</summary>
        public const int MergeCount = 3;
    }
}
