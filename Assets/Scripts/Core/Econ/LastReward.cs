namespace DefenseBattle.Core
{
    /// <summary>광고 '보상 2배' 대상 - 직전 판 보상 (10분 안 1회)</summary>
    public sealed class LastReward
    {
        public int Amount;
        /// <summary>받은 시각 (ms)</summary>
        public long At;
        public bool Used;
    }
}
