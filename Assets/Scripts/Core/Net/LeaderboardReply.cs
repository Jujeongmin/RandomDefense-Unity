using System.Collections.Generic;

namespace DefenseBattle.Core
{
    /// <summary>랭킹 상위 50 + 내 전적</summary>
    public sealed class LeaderboardReply
    {
        public List<RankEntry> Top = new List<RankEntry>();
        public PvpStats Me;
    }
}
