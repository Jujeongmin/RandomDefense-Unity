namespace DefenseBattle.Core
{
    /// <summary>매칭 대기 상태: "waiting" / "idle"(대기열에 없음 - 다시 등록) / "matched"(Match 있음)</summary>
    public sealed class QueueStatus
    {
        public const string Waiting = "waiting";
        public const string Idle = "idle";
        public const string Matched = "matched";

        public string Status;
        public MatchInfo Match;
    }
}
