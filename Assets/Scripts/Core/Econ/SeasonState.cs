using System.Collections.Generic;

namespace DefenseBattle.Core
{
    /// <summary>PvP 시즌 패스 진행 (월 단위 시즌 id "S2026-10")</summary>
    public sealed class SeasonState
    {
        public string Id = "";
        public int Xp;
        public bool Premium;
        public List<int> ClaimedFree = new List<int>();
        public List<int> ClaimedPremium = new List<int>();

        public static SeasonState Fresh(string id) => new SeasonState { Id = id };
    }
}
