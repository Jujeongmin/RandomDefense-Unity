namespace DefenseBattle.Core
{
    /// <summary>판 위의 유닛 한 개 (types.ts Unit)</summary>
    public sealed class Unit
    {
        public int Id;
        public Job Job;
        public GradeKey Grade;
        public int GradeIndex;
        public double BaseAtk;
        public int ZoneId;
        public double X;
        public double Y;
        public double Cooldown;
        public double Flash;
        /// <summary>스프라이트 방향 행 (이동 중일 때만 사용) 0=하 1=좌 2=우 3=상</summary>
        public int Dir;
        /// <summary>서 있을 때 방향 행: 공격한 몹 쪽, 노릴 몹이 없으면 정면(0)</summary>
        public int Face;
        /// <summary>걷기 트윈 중이면 true</summary>
        public bool Moving;
        public Vec2 MoveFrom;
        public Vec2 MoveTo;
        public double MoveElapsed;
        public double MoveDur;
        /// <summary>사신: 모은 영혼 (다음 공격 강화)</summary>
        public int Souls;

        internal int NewZone = -1;
    }
}
