namespace DefenseBattle.Core
{
    /// <summary>아레나(티어) 한 단계 (config.ts PVP_TIERS 항목)</summary>
    public sealed class PvpTier
    {
        public int Min;
        /// <summary>이름 (한국어 원문 키)</summary>
        public string Kr;
        public uint Color;
    }
}
