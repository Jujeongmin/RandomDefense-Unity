namespace DefenseBattle.Core
{
    /// <summary>
    /// 판 안 경제 (config.ts ECONOMY).
    /// 시작 골드/처치 골드는 넉넉하게: 초반 화력이 모자라면 처치→골드→소환이 막혀 운으로 무너진다 (tools/simulate.ts)
    /// </summary>
    public static class Economy
    {
        public const int StartGold = 200;
        public const int SummonCost = 20;
        /// <summary>덱을 덜 채운 만큼 소환 비용 증가 (빈 칸 1개당 +N배). 한 유닛만 넣고 그 유닛만 합성하는 전략 견제 - tools/decksim.ts</summary>
        public const double DeckGapPenalty = 0.5;
        public const int KillGold = 3;
        public const int BossGold = 100;
        public const int UpgradeBase = 20;
        public const int UpgradeStep = 20;
        public const double UpgradePerLevel = 0.10;
    }
}
