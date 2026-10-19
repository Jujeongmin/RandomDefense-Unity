using System;

namespace DefenseBattle.Core
{
    /// <summary>상자·뽑기 (config.ts CHESTS, CHEST_CYCLE, chestOpenCost, GACHA)</summary>
    public static class Chests
    {
        static ChestDef C(ChestKind k, string id, string kr, double hours, int cards, int per, int gMin, int gMax, int rare, double epic, double legend, uint color)
            => new ChestDef { Kind = k, Id = id, Kr = kr, Hours = hours, Cards = cards, PerArena = per, GemsMin = gMin, GemsMax = gMax, Rare = rare, Epic = epic, Legendary = legend, Color = color };

        public static readonly ChestDef[] All =
        {
            C(ChestKind.Wood,   "wood",   "나무 상자", 0.5, 6,  2,  8,   15,  1,  0.05, 0,    0xb8864b),
            C(ChestKind.Silver, "silver", "은 상자",   3,   15, 5,  25,  45,  3,  0.25, 0.01, 0xc8d0d8),
            C(ChestKind.Gold,   "gold",   "금 상자",   8,   40, 10, 70,  110, 8,  1,    0.04, 0xffcf3f),
            C(ChestKind.Magic,  "magic",  "마법 상자", 12,  90, 20, 150, 220, 20, 4,    0.2,  0xb06bff),
            C(ChestKind.Legend, "legend", "전설 상자", 24,  30, 5,  200, 300, 6,  2,    1,    0xffb93f),
        };

        public static ChestDef Get(ChestKind kind) => All[(int)kind];

        public static bool TryParse(string id, out ChestKind kind)
        {
            foreach (ChestDef c in All)
            {
                if (c.Id == id) { kind = c.Kind; return true; }
            }
            kind = default;
            return false;
        }

        /// <summary>대전 승리 시 받는 상자 순서 (반복)</summary>
        public static readonly ChestKind[] Cycle =
        {
            ChestKind.Wood, ChestKind.Wood, ChestKind.Silver, ChestKind.Wood, ChestKind.Silver, ChestKind.Gold,
            ChestKind.Wood, ChestKind.Silver, ChestKind.Silver, ChestKind.Wood, ChestKind.Gold, ChestKind.Magic,
        };

        /// <summary>보석으로 바로 열기: 남은 3분마다 1</summary>
        public static int OpenCost(double msLeft) => Math.Max(1, (int)Math.Ceiling(msLeft / (3 * 60 * 1000.0)));

        // ---------- 뽑기 (보석) ----------
        public const int GachaOne = 100;
        public const int GachaTen = 900;
        /// <summary>뽑기 등급 확률 (Rarity 순서)</summary>
        public static readonly double[] GachaRates = { 0.7, 0.22, 0.07, 0.01 };
        /// <summary>한 번 뽑을 때 나오는 카드 장수 (Rarity 순서)</summary>
        public static readonly int[] GachaCount = { 6, 3, 1, 1 };
    }
}
