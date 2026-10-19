namespace DefenseBattle.Core
{
    /// <summary>캠페인 스테이지 (config.ts StageDef)</summary>
    public sealed class StageDef
    {
        /// <summary>"1-3"</summary>
        public string Id;
        public int Chapter;
        /// <summary>전체 순번 (0~)</summary>
        public int Index;
        public int Waves;
        public double HpMult;
        /// <summary>등장 종족 제한 (초반은 1종 → 상성 학습). null = 전 종족</summary>
        public Race[] Races;
        /// <summary>첫 클리어 보석</summary>
        public int FirstReward;
        public int ReplayReward;
        /// <summary>첫 클리어 시 해금 유닛 (없으면 null)</summary>
        public Job? Unlocks;
    }
}
