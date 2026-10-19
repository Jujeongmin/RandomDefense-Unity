namespace DefenseBattle.Core
{
    /// <summary>
    /// 결제 상품 (config.ts ProductDef). Category: crystal / package / booster / pass / offer / unit / chest / gacha / retired.
    /// offer = 팝업으로 뜨는 24시간 한정 상품 (뜬 동안만 상점에도 보임), unit = 유닛 패키지, retired = 판매 중단(상점에 안 보임)
    /// </summary>
    public sealed class ProductDef
    {
        /// <summary>스토어 productId (원본 VX Shop id 그대로)</summary>
        public string Id;
        /// <summary>이름 (한국어 원문 키)</summary>
        public string Kr;
        /// <summary>설명 (한국어 원문 키). {0} 이 있으면 DescArg 를 넣는다</summary>
        public string Desc;
        public object DescArg;
        /// <summary>원본 기본 가격 (100 VX = 1달러). 스토어 등록 가격이 있으면 그 값을 표시</summary>
        public int Vx;
        public string Category;
        /// <summary>계정당 1회 한정</summary>
        public bool OneTime;
        /// <summary>배지 문구 (한국어 원문 키, 없으면 null)</summary>
        public string Badge;
        public string Icon;
        public ProductGrant Grant;
    }
}
