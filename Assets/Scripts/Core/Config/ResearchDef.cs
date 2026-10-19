namespace DefenseBattle.Core
{
    /// <summary>연구 항목 (types.ts ResearchDef)</summary>
    public sealed class ResearchDef
    {
        public ResearchKey Key;
        /// <summary>원본 문자열 id ('atk' …)</summary>
        public string Id;
        /// <summary>이름 (한국어 원문 키)</summary>
        public string Kr;
        public int Max;
        /// <summary>설명 (한국어 원문 키)</summary>
        public string Desc;
        /// <summary>레벨당 효과 크기</summary>
        public double Per;
    }
}
