using System.Collections.Generic;

namespace DefenseBattle.Core
{
    /// <summary>상품 지급 내용 (config.ts ProductGrant)</summary>
    public sealed class ProductGrant
    {
        public int Crystals;
        public Dictionary<ItemKey, int> Items = new Dictionary<ItemKey, int>();
        public bool AdRemove;
        /// <summary>월정액 기간(일)</summary>
        public int MonthlyDays;
        /// <summary>현재 시즌 패스 프리미엄</summary>
        public bool SeasonPremium;
        /// <summary>유닛 즉시 해금 + 이 레벨까지 (이미 더 높으면 그대로)</summary>
        public Dictionary<Job, int> Units = new Dictionary<Job, int>();
        /// <summary>상자 바로 열기</summary>
        public ChestKind? Chest;
    }
}
