namespace DefenseBattle.Core
{
    /// <summary>상자 칸 하나 (types.ts MetaData.chests 항목). OpenAt = 열리는 시각(ms, 열기 시작 전엔 null)</summary>
    public sealed class ChestSlot
    {
        public ChestKind Kind;
        public long? OpenAt;
    }
}
