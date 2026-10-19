using System.Collections.Generic;

namespace DefenseBattle.Core
{
    /// <summary>대전 결과 보상 (rules.ts pvpReward 반환값)</summary>
    public sealed class PvpRewardResult
    {
        public int Crystals;
        /// <summary>받은 승리 상자 (칸이 가득이면 null)</summary>
        public ChestKind? Chest;
        public List<RoadStep> Road = new List<RoadStep>();
        public List<Reveal> Reveals = new List<Reveal>();
    }
}
