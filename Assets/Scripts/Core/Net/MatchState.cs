namespace DefenseBattle.Core
{
    /// <summary>
    /// 경기 진행 상태. Status: "waiting"(준비 기다림) / "playing" / "done" / "cancelled".
    /// playing 이 되면 StartAt(서버 시각 ms)에 둘이 같이 시작한다 - 기기 시계 차이는 Now 로 보정 (StartAt - Now 만큼 남음)
    /// </summary>
    public sealed class MatchState
    {
        public const string Waiting = "waiting";
        public const string Playing = "playing";
        public const string Done = "done";
        public const string Cancelled = "cancelled";

        public string MatchId;
        public string Status;
        public long StartAt;
        public long Now;
        public uint Seed;
        /// <summary>호스트가 올린 Relay 참가 코드 (아직 없으면 null)</summary>
        public string RelayJoinCode;
        /// <summary>끝났으면 결과</summary>
        public PvpMatchResult Result;
    }
}
