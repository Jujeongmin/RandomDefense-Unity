namespace DefenseBattle.Core
{
    /// <summary>판 안 등급 정의 (types.ts Grade)</summary>
    public sealed class Grade
    {
        public GradeKey Key;
        /// <summary>이름 (한국어 원문 키)</summary>
        public string Kr;
        /// <summary>등급 번호 = 스프라이트 번호 (sprites/{시트}_{번호}.png)</summary>
        public int Index;
        public double AtkMin;
        public double AtkMax;
        /// <summary>소환 확률(%)</summary>
        public double SummonRate;
        public int SellGold;
        /// <summary>0xRRGGBB</summary>
        public uint Color;
    }
}
