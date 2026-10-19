namespace DefenseBattle.Core
{
    /// <summary>상자·뽑기에서 나온 카드 (fresh = 이 카드로 새로 해금)</summary>
    public sealed class CardDrop
    {
        public Job Job;
        public int N;
        public bool Fresh;
    }
}
