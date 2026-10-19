namespace DefenseBattle.Core
{
    /// <summary>스테이지·무한 모드 결과 보상. 스테이지면 Stage, 무한 모드면 Crystals 만</summary>
    public sealed class RunEndResult
    {
        public StageEndResult Stage;
        public int Crystals;
    }
}
