namespace DefenseBattle.Core
{
    /// <summary>PvP: 상대가 던져 떨어지는 중인 몹 (engine.ts pending)</summary>
    public sealed class PendingDrop
    {
        public int Card;
        public double Hp;
        /// <summary>착지까지 남은 게임 시간</summary>
        public double Delay;
        public double Pos;
    }
}
