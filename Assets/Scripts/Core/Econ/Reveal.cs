using System.Collections.Generic;

namespace DefenseBattle.Core
{
    /// <summary>상자·뽑기 열기 결과 (연출용). Chest = 어떤 상자 (뽑기면 null)</summary>
    public sealed class Reveal
    {
        /// <summary>제목 (이미 번역된 문장)</summary>
        public string Title;
        public List<CardDrop> Drops = new List<CardDrop>();
        public int Gems;
        public ChestKind? Chest;
        public bool IsGacha;
    }
}
