using System.Collections.Generic;

namespace DefenseBattle.Core
{
    /// <summary>
    /// 상점 상품 (config.ts PRODUCTS ~ ITEM_KR).
    /// 보석 상품은 비쌀수록 보너스(110~150%), 패키지는 효율을 크게 줘서(300%) 첫 결제와 월정액을 유도.
    /// </summary>
    public static class Products
    {
        public const int MonthlyDailyCrystals = 40;
        /// <summary>효율 기준: 보석 1개 = 12원 (보석 100 = ₩1,200 상품이 100%). 원본은 1 VX = 보석 1</summary>
        public const double KrwPerGem = 12;
        /// <summary>아이템의 보석 환산 가치 (뽑기권 = 유닛 뽑기 1회)</summary>
        public static int ItemGemValue(ItemKey k) => k == ItemKey.GachaTicket ? 100 : 50;

        static ProductDef P(string id, string kr, string icon, string cat, int krw, string desc, ProductGrant g, bool oneTime = false, string badge = null, object descArg = null)
            => new ProductDef { Id = id, Kr = kr, Icon = icon, Category = cat, Krw = krw, Desc = desc, DescArg = descArg, Grant = g, OneTime = oneTime, Badge = badge };

        static ProductGrant Gems(int n) => new ProductGrant { Crystals = n };

        static ProductGrant Grant(int crystals = 0, int tickets = 0, int boosters = 0)
        {
            var g = new ProductGrant { Crystals = crystals };
            if (tickets > 0) g.Items[ItemKey.GachaTicket] = tickets;
            if (boosters > 0) g.Items[ItemKey.GoldBooster] = boosters;
            return g;
        }

        static ProductGrant UnitPack(Job job, int crystals)
        {
            var g = new ProductGrant { Crystals = crystals };
            g.Units[job] = 5;
            return g;
        }

        // 원본과 다른 점 (사용자 결정 2026-10-19): 원화 가격, 시즌 패스 ₩5,900, 한정 상품 보상을 효율 약 200% 로 낮춤,
        // 뽑기권 묶음은 팔지 않음 (보석으로 뽑으면 되므로)
        public static readonly ProductDef[] All =
        {
            // 패키지
            P("starter_pack", "스타터 패키지", "chest", "package", 1200, "보석 150 + 뽑기권 1 + 골드 부스터 1", Grant(150, 1, 1), true, "1회 한정"),
            P("monthly_pass", "월정액 (30일)", "calendar", "package", 6500, "즉시 보석 300 + 30일간 매일 보석 {0}",
                new ProductGrant { Crystals = 300, MonthlyDays = 30 }, false, "추천", MonthlyDailyCrystals),
            P("ad_remove", "광고 제거 패키지", "bolt", "package", 6500, "광고 시청 없이 바로 보상 + 3배속 해금 + 보석 500",
                new ProductGrant { AdRemove = true, Crystals = 500 }, true, "1회 한정"),
            // 시즌 패스
            P("season_premium", "PvP 시즌 패스", "trophy", "pass", 5900, "이번 시즌 프리미엄 보상 트랙 해금 (지난 단계 보상도 수령 가능)",
                new ProductGrant { SeasonPremium = true }),
            // 부스터 (일반 모드 전용 - PvP 에서는 사용 불가)
            P("gold_booster_pack", "골드 부스터 5개", "bag", "booster", 2500, "사용한 판의 시작 골드 2배 (일반 모드)", Grant(0, 0, 5)),
            // 팝업 한정 패키지 (24시간) - 패배 연속 / 새 아레나 / 새 챕터 때 뜬다 (offers.ts). 효율 약 200%
            P("offer_comeback", "다시 일어서기 패키지", "chest", "offer", 2500, "보석 220 + 뽑기권 1 + 골드 부스터 2", Grant(220, 1, 2), false, "24시간 한정"),
            P("offer_arena", "승급 축하 패키지", "crown", "offer", 6500, "보석 700 + 뽑기권 4", Grant(700, 4), false, "24시간 한정"),
            P("offer_chapter", "새 챕터 패키지", "map", "offer", 6500, "보석 600 + 뽑기권 3 + 골드 부스터 4", Grant(600, 3, 4), false, "24시간 한정"),
            // 유닛 패키지 - 바로 해금 + Lv5 (계정당 1회)
            P("unit_ranger", "레인저 패키지", "helmet", "unit", 3900, "레인저 즉시 해금 + Lv5 + 보석 200", UnitPack(Job.Ranger, 200), true),
            P("unit_frost", "빙결술사 패키지", "helmet", "unit", 3900, "빙결술사 즉시 해금 + Lv5", UnitPack(Job.Frost, 0), true),
            P("unit_paladin", "성기사 패키지", "helmet", "unit", 6500, "성기사 즉시 해금 + Lv5 + 보석 300", UnitPack(Job.Paladin, 300), true),
            P("unit_bomber", "폭격수 패키지", "helmet", "unit", 6500, "폭격수 즉시 해금 + Lv5", UnitPack(Job.Bomber, 0), true),
            P("unit_assassin", "암살자 패키지", "helmet", "unit", 8900, "암살자 즉시 해금 + Lv5", UnitPack(Job.Assassin, 0), true),
            // 상자 (사면 바로 열림)
            P("chest_gold", "금 상자", "chest", "chest", 3900, "카드 40장+ · 영웅 1장", new ProductGrant { Chest = ChestKind.Gold }),
            P("chest_magic", "마법 상자", "chest", "chest", 8900, "카드 90장+ · 영웅 4장", new ProductGrant { Chest = ChestKind.Magic }),
            P("chest_legend", "전설 상자", "crown", "chest", 12000, "전설 카드 1장 보장", new ProductGrant { Chest = ChestKind.Legend }),
            // 보석
            P("crystal_100", "보석 100", "gem", "crystal", 1200, "", Gems(100)),
            P("crystal_550", "보석 550", "gem", "crystal", 6500, "", Gems(550)),
            P("crystal_1200", "보석 1,200", "gem", "crystal", 12000, "", Gems(1200)),
            P("crystal_2600", "보석 2,600", "gem", "crystal", 25000, "", Gems(2600)),
            P("crystal_7000", "보석 7,000", "gem", "crystal", 65000, "", Gems(7000)),
            P("crystal_15000", "보석 15,000", "gem", "crystal", 119000, "", Gems(15000)),
        };

        static readonly Dictionary<string, ProductDef> s_byId = BuildIndex();
        static Dictionary<string, ProductDef> BuildIndex()
        {
            var d = new Dictionary<string, ProductDef>();
            foreach (ProductDef p in All) d[p.Id] = p;
            return d;
        }

        /// <summary>id 로 상품 찾기 (없으면 null)</summary>
        public static ProductDef Get(string id) => id != null && s_byId.TryGetValue(id, out ProductDef p) ? p : null;

        /// <summary>팝업 한정 상품: 어떤 순간에 뜨는지 → 상품 id (comeback / arena / chapter)</summary>
        public static readonly Dictionary<string, string> OfferProduct = new Dictionary<string, string>
        {
            { "comeback", "offer_comeback" }, { "arena", "offer_arena" }, { "chapter", "offer_chapter" },
        };
        public const int OfferHours = 24;

        /// <summary>유닛 → 그 유닛 패키지 id</summary>
        public static readonly Dictionary<Job, string> UnitPacks = new Dictionary<Job, string>
        {
            { Job.Ranger, "unit_ranger" }, { Job.Frost, "unit_frost" }, { Job.Paladin, "unit_paladin" },
            { Job.Bomber, "unit_bomber" }, { Job.Assassin, "unit_assassin" },
        };

        /// <summary>상품의 보석 환산 가치 (광고 제거·시즌 패스처럼 환산이 안 되는 혜택은 빼고)</summary>
        public static int GemValue(ProductDef p)
        {
            ProductGrant g = p.Grant;
            int v = g.Crystals;
            foreach (KeyValuePair<ItemKey, int> kv in g.Items) v += kv.Value * ItemGemValue(kv.Key);
            // 유닛: 보석 해금 가격 + 그 레벨까지 강화 비용
            foreach (KeyValuePair<Job, int> kv in g.Units)
            {
                v += global::DefenseBattle.Core.Units.Get(kv.Key).UnlockCrystals ?? 0;
                for (int l = 1; l < kv.Value; l++) v += global::DefenseBattle.Core.Units.UpgradeCost(l);
            }
            if (g.MonthlyDays > 0) v += g.MonthlyDays * MonthlyDailyCrystals;
            return v;
        }

        /// <summary>효율(%) - 기본 보석 상품(₩1,200 = 보석 100) 대비. price = 원화 가격 (스토어 가격을 알면 그 값). 표시할 가치가 없으면 0</summary>
        public static int Efficiency(ProductDef p, int price)
        {
            if (price <= 0 || p.Grant.AdRemove || p.Grant.SeasonPremium) return 0;
            return JsMath.RoundInt(GemValue(p) / (price / KrwPerGem) * 100);
        }

        public static int Efficiency(ProductDef p) => Efficiency(p, p.Krw);

        /// <summary>아이템 이름·아이콘·설명 (config.ts ITEM_KR)</summary>
        public static string ItemKr(ItemKey k) => k == ItemKey.GachaTicket ? "뽑기권" : "골드 부스터";
        public static string ItemIcon(ItemKey k) => k == ItemKey.GachaTicket ? "ticket" : "bag";
        public static string ItemDesc(ItemKey k) => k == ItemKey.GachaTicket
            ? "상점에서 유닛 뽑기 1회 (보석 대신)"
            : "켜고 시작하면 그 판 시작 골드 2배 (일반 모드)";
    }
}
