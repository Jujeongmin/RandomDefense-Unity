namespace DefenseBattle.Core
{
    /// <summary>트로피 로드 한 칸 (config.ts RoadStep)</summary>
    public sealed class RoadStep
    {
        public int At;
        public int Crystals;
        public Job? Unit;
        public AttackKey? Attack;
        public ChestKind? Chest;
    }
}
