namespace DefenseBattle.Core
{
    /// <summary>직업+등급별 묶음 (types.ts Stack) - 판매·합성 패널용</summary>
    public sealed class UnitStack
    {
        public Job Job;
        public GradeKey Grade;
        public int GradeIndex;
        public int Count;
        /// <summary>판 안 강화 레벨</summary>
        public int Level;
    }
}
