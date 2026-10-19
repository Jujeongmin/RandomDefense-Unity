namespace DefenseBattle.Core
{
    /// <summary>
    /// 매칭 결과 (원본 MatchInfo). Host = 이 기기가 Relay 방을 만드는 쪽인지 (먼저 기다리던 쪽).
    /// 두 사람은 같은 Seed 로 같은 웨이브를 막는다.
    /// </summary>
    public sealed class MatchInfo
    {
        public string MatchId;
        public uint Seed;
        /// <summary>매칭된 시각 (ms)</summary>
        public long T;
        public bool Host;
        public OpponentInfo Opponent;
    }
}
