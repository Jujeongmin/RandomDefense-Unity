namespace DefenseBattle.Core
{
    /// <summary>PvP 수치 (config.ts PVP)</summary>
    public static class PvpConfig
    {
        /// <summary>PvP 몹 시작 체력 배율 - Waves.PvpHpGrowth 와 함께 한 판 ~4분 (tools/pvpsim.ts)</summary>
        public const double HpMult = 3;
        /// <summary>상태 보고 주기(초) - 상대 전장을 거의 실시간으로 (스냅샷 최대 ~2.5KB × 4번/초)</summary>
        public const double ReportInterval = 0.25;
        /// <summary>매칭 대기 최대(초) → 초과 시 AI 대전 제안</summary>
        public const double MatchTimeout = 25;
        public const int TrophyWin = 30;
        public const int TrophyLoss = 20;
        /// <summary>보석</summary>
        public const int RewardWin = 40;
        public const int RewardLoss = 10;
        public const int SeasonXpWin = 100;
        public const int SeasonXpLoss = 40;
    }
}
