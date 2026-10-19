namespace DefenseBattle.Core
{
    /// <summary>
    /// 결제 상품 (config.ts ProductDef). Category: crystal / package / booster / pass / offer / unit / chest.
    /// offer = 팝업으로 뜨는 24시간 한정 상품 (뜬 동안만 상점에도 보임), unit = 유닛 패키지
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
        /// <summary>
        /// 기본 가격 (원, 부가세 포함 - Google Play 등록 가격). 화면에는 스토어가 알려 주는 현지 가격을 우선 표시하고,
        /// 이 값은 효율(%) 계산과 스토어 연결 전 표시용. 원본 VX 가격을 국내 가격대로 옮긴 값 (docs/ugs-setup.md 6번)
        /// </summary>
        public int Krw;
        public string Category;
        /// <summary>계정당 1회 한정</summary>
        public bool OneTime;
        /// <summary>배지 문구 (한국어 원문 키, 없으면 null)</summary>
        public string Badge;
        public string Icon;
        public ProductGrant Grant;
    }
}
