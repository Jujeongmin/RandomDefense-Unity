namespace DefenseBattle.Core
{
    /// <summary>PvP 트로피 → 아레나 6단계 (config.ts PVP_TIERS, pvpTier)</summary>
    public static class Arenas
    {
        public static readonly PvpTier[] All =
        {
            new PvpTier { Min = 0,   Kr = "브론즈",   Color = 0xcd7f32 },
            new PvpTier { Min = 80,  Kr = "실버",     Color = 0xc0c0c0 },
            new PvpTier { Min = 200, Kr = "골드",     Color = 0xffcf3f },
            new PvpTier { Min = 320, Kr = "플래티넘", Color = 0x6fe0d0 },
            new PvpTier { Min = 500, Kr = "다이아",   Color = 0x6fb6ff },
            new PvpTier { Min = 800, Kr = "마스터",   Color = 0xff6bd5 },
        };

        /// <summary>트로피에 맞는 아레나 번호 (0~5)</summary>
        public static int IndexOf(int trophies)
        {
            int idx = 0;
            for (int i = 0; i < All.Length; i++) if (trophies >= All[i].Min) idx = i;
            return idx;
        }

        public static PvpTier Of(int trophies) => All[IndexOf(trophies)];
    }
}
