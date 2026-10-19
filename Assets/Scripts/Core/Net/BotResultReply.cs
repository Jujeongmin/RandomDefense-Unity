namespace DefenseBattle.Core
{
    /// <summary>봇전 결과 반영 (원본 pvpBotResult)</summary>
    public sealed class BotResultReply
    {
        public int Delta;
        public int Trophies;
        public PvpRewardResult Reward;
        public EconState Econ;
    }
}
