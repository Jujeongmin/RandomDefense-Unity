namespace DefenseBattle.Core
{
    /// <summary>PvP 시즌 패스 20단계 (config.ts SEASON_XP_PER_TIER, SEASON_TIERS)</summary>
    public static class Season
    {
        public const int XpPerTier = 200;
        public const int TierCount = 20;

        /// <summary>tier = 1..20</summary>
        public static SeasonReward Free(int tier)
            => tier % 5 == 0 ? new SeasonReward { GachaTickets = 1 } : new SeasonReward { Crystals = 20 + tier * 2 };

        /// <summary>tier = 1..20</summary>
        public static SeasonReward Premium(int tier)
        {
            if (tier % 10 == 0) return new SeasonReward { GachaTickets = 6, Crystals = 200 };
            if (tier % 5 == 0) return new SeasonReward { GoldBoosters = 2, GachaTickets = 1 };
            return new SeasonReward { Crystals = 60 + tier * 5 };
        }
    }
}
