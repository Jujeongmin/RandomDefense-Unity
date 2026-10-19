namespace DefenseBattle.Core
{
    /// <summary>대전 기록 (트로피는 서버 기록)</summary>
    public sealed class PvpStats
    {
        public int Trophies;
        public int Wins;
        public int Losses;
        public int Streak;
        public int LossStreak;
    }
}
