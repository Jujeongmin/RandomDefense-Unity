namespace DefenseBattle.Core
{
    /// <summary>판 종료 정보 (types.ts GameOverData)</summary>
    public sealed class GameOverData
    {
        public int Wave;
        /// <summary>이유 (이미 번역된 문장)</summary>
        public string Reason;
        public bool Cleared;
        /// <summary>PvP 판이면 true</summary>
        public bool IsPvp;
        /// <summary>PvP 승리 여부</summary>
        public bool PvpWon;
        /// <summary>판 도중 필드 몹 최대치 (별 판정)</summary>
        public int PeakMobs;
    }
}
