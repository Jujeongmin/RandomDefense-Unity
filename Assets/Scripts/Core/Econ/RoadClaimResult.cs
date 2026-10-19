using System.Collections.Generic;

namespace DefenseBattle.Core
{
    /// <summary>트로피 로드 보상 받기 결과 (받은 칸 + 상자 연출)</summary>
    public sealed class RoadClaimResult
    {
        public List<RoadStep> Steps = new List<RoadStep>();
        public List<Reveal> Reveals = new List<Reveal>();
    }
}
