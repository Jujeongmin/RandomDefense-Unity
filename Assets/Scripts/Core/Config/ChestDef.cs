namespace DefenseBattle.Core
{
    /// <summary>상자 (config.ts ChestDef)</summary>
    public sealed class ChestDef
    {
        public ChestKind Kind;
        /// <summary>원본 문자열 id ('wood' …)</summary>
        public string Id;
        /// <summary>이름 (한국어 원문 키)</summary>
        public string Kr;
        /// <summary>열리는 데 걸리는 시간</summary>
        public double Hours;
        /// <summary>카드 장수 (아레나마다 + PerArena)</summary>
        public int Cards;
        public int PerArena;
        public int GemsMin;
        public int GemsMax;
        /// <summary>희귀 카드 장수 (Cards 중)</summary>
        public int Rare;
        /// <summary>영웅 카드 기대 장수 (소수 = 확률)</summary>
        public double Epic;
        /// <summary>전설 카드 확률</summary>
        public double Legendary;
        public uint Color;
    }
}
