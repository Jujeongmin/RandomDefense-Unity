namespace DefenseBattle.Core
{
    /// <summary>직업 단위 집계 (types.ts JobStack) - 강화가 직업 단위이므로 하단 강화 패널용</summary>
    public sealed class JobStack
    {
        public Job Job;
        public int Count;
        public int Level;
        /// <summary>보유 최고 등급 (대표 스프라이트)</summary>
        public GradeKey TopGrade;
        public int TopGradeIndex;
    }
}
