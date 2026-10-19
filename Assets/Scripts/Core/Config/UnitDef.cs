namespace DefenseBattle.Core
{
    /// <summary>유닛(직업) 정의 (config.ts UnitDef)</summary>
    public sealed class UnitDef
    {
        public Job Job;
        /// <summary>원본 문자열 id ('archer' …) - 저장 데이터·서버와 주고받는 키</summary>
        public string Id;
        /// <summary>이름 (한국어 원문 키)</summary>
        public string Kr;
        /// <summary>투사체 모양</summary>
        public BaseJob Base;
        /// <summary>스프라이트 시트 이름 (sprites/{Sheet}_{등급}.png)</summary>
        public string Sheet;
        /// <summary>공격 간격(초)</summary>
        public double Cooldown;
        /// <summary>사거리 배수</summary>
        public double RangeMult;
        public double AtkMult;
        /// <summary>역할 한 줄 (한국어 원문 키)</summary>
        public string Role;
        public Rarity Rarity;
        /// <summary>구역·투사체 색 0xRRGGBB</summary>
        public uint Tint;
        /// <summary>상성 유리 종족 (1.0, 나머지 0.9). 없으면 모두 0.9</summary>
        public Race? Fav;
        /// <summary>해금 캠페인 스테이지 ("1-3"), 없으면 null</summary>
        public string UnlockStage;
        /// <summary>보석으로 바로 해금하는 가격 (전설은 상자·뽑기로만 → null)</summary>
        public int? UnlockCrystals;
    }
}
