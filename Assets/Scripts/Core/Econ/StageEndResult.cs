namespace DefenseBattle.Core
{
    /// <summary>스테이지 끝 결과 (rules.ts stageEnd 반환값)</summary>
    public sealed class StageEndResult
    {
        public int Stars;
        public bool FirstClear;
        public bool NewBest;
        public int Crystals;
        /// <summary>첫 클리어로 해금된 유닛 (없으면 null)</summary>
        public Job? Unlocked;
    }
}
