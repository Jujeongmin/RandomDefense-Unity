namespace DefenseBattle.Core
{
    /// <summary>유닛 배치 구역 (types.ts Zone). 0=상 1=우 2=하 3=좌</summary>
    public sealed class Zone
    {
        public int Id;
        public double Cx;
        public double Cy;
        /// <summary>이 구역을 차지한 직업 (없으면 null)</summary>
        public Job? Job;
        /// <summary>삼각형 꼭짓점 3개 (랜덤 배치용)</summary>
        public Vec2[] Tri;
    }
}
