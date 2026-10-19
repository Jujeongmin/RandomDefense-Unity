namespace DefenseBattle.Core
{
    /// <summary>일일 퀘스트 (types.ts DailyQuestDef)</summary>
    public sealed class DailyQuestDef
    {
        public string Key;
        /// <summary>한국어 원문 키</summary>
        public string Kr;
        public int Goal;
        public QuestStat Stat;
        public int Reward;
    }
}
