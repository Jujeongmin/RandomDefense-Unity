using System.Collections.Generic;

namespace DefenseBattle.Core
{
    /// <summary>실시간 대전 결과 (원본 finishMatch 결과). Reason: "defeat"(필드가 무너짐) / "leave"(나감·연결 끊김)</summary>
    public sealed class PvpMatchResult
    {
        public string MatchId;
        public string Winner;
        public string Loser;
        public string Reason;
        /// <summary>플레이어 id → 트로피 변화</summary>
        public Dictionary<string, int> Delta = new Dictionary<string, int>();
        /// <summary>플레이어 id → 바뀐 트로피</summary>
        public Dictionary<string, int> Trophies = new Dictionary<string, int>();
        /// <summary>플레이어 id → 받은 보상 (자기 것만 보면 된다)</summary>
        public Dictionary<string, PvpRewardResult> Rewards = new Dictionary<string, PvpRewardResult>();
    }
}
