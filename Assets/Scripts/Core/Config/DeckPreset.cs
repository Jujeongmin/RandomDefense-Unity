namespace DefenseBattle.Core
{
    /// <summary>추천 덱 (config.ts DeckPreset) - 없는 카드가 있으면 잠금으로 보인다</summary>
    public sealed class DeckPreset<TCard>
    {
        /// <summary>이름 (한국어 원문 키)</summary>
        public string Name;
        /// <summary>설명 (한국어 원문 키)</summary>
        public string Desc;
        public TCard[] Cards;

        public DeckPreset(string name, string desc, params TCard[] cards)
        {
            Name = name;
            Desc = desc;
            Cards = cards;
        }
    }
}
