using System;
using System.Collections.Generic;

namespace DefenseBattle.Core
{
    /// <summary>
    /// 경제 규칙 (econ/rules.ts) - 보석·아이템·카드·유닛 레벨·연구·상자·보상.
    /// 온라인: 서버(UGS Cloud Code)가 같은 규칙(원본 rules.ts)으로 계산하고 결과만 내려준다 (기기는 못 바꿈).
    /// 오프라인·테스트: 기기가 이 규칙을 자기 상태에 적용한다. 서버 결과와 같아야 하므로 원본과 한 줄씩 대응시킨다.
    /// </summary>
    public static class EconRules
    {
        const long Day = 24L * 60 * 60 * 1000;
        const long Hour = 60L * 60 * 1000;

        /// <summary>광고 뽑기권: 하루 3회, 1장씩</summary>
        public const int AdTicketsPerDay = 3;
        public const int AdTicketAmount = 1;

        static int ArenaIdx(EconState e) => Arenas.IndexOf(e.Pvp.Trophies);

        // ---------------- 유닛·카드 ----------------
        public static void UnlockUnit(EconState e, Job job)
        {
            if (e.Owns(job)) return;
            e.UnitLevels[job] = 1;
            if (e.Deck.Count < Units.DeckSize && !e.Deck.Contains(job)) e.Deck.Add(job);
        }

        /// <summary>카드 받기: 없는 유닛이면 1장으로 해금, 나머지는 레벨업 재료. 새로 해금되면 true</summary>
        public static bool AddCards(EconState e, Job job, int n)
        {
            if (n <= 0) return false;
            bool fresh = false;
            if (!e.Owns(job)) { UnlockUnit(e, job); n -= 1; fresh = true; }
            e.Cards[job] = e.CardCount(job) + n;
            return fresh;
        }

        public static void AddItem(EconState e, ItemKey key, int n) => e.Items[key] = Math.Max(0, e.Item(key) + n);

        public static bool UpgradeUnit(EconState e, Job job)
        {
            int lv = e.LevelOf(job);
            int need = Units.CardsNeeded(Units.Get(job).Rarity, lv);
            int cost = Units.UpgradeCost(lv);
            if (lv <= 0 || lv >= Units.MaxLevel || e.CardCount(job) < need || e.Crystals < cost) return false;
            e.Crystals -= cost;
            e.Cards[job] = e.CardCount(job) - need;
            e.UnitLevels[job] = lv + 1;
            return true;
        }

        /// <summary>보석으로 바로 해금</summary>
        public static bool BuyUnit(EconState e, Job job)
        {
            UnitDef def = Units.Get(job);
            if (e.Owns(job) || !def.UnlockCrystals.HasValue || e.Crystals < def.UnlockCrystals.Value) return false;
            e.Crystals -= def.UnlockCrystals.Value;
            UnlockUnit(e, job);
            return true;
        }

        public static bool DoResearch(EconState e, ResearchKey key)
        {
            ResearchDef r = Research.Get(key);
            int lv = e.ResearchLevel(key);
            if (lv >= r.Max || e.Crystals < Research.Cost(lv)) return false;
            e.Crystals -= Research.Cost(lv);
            e.Research[key] = lv + 1;
            return true;
        }

        /// <summary>아이템 1개 사용 (골드 부스터)</summary>
        public static bool UseItem(EconState e, ItemKey key)
        {
            if (e.Item(key) <= 0) return false;
            e.Items[key] = e.Item(key) - 1;
            return true;
        }

        // ---------------- 상자·뽑기 ----------------
        static List<Job> Pool(Rarity r)
        {
            var list = new List<Job>();
            foreach (UnitDef u in Units.All) if (u.Rarity == r) list.Add(u.Job);
            return list;
        }

        /// <summary>n 장을 그 등급 유닛 중 최대 k 종에 고르게 나눈다</summary>
        static void Split(Rarity r, int n, int k, Dictionary<Job, int> output, List<Job> order, IRandom rng)
        {
            if (n <= 0) return;
            List<Job> jobs = Pool(r);
            BotProfile.Shuffle(jobs, rng);
            int take = Math.Max(1, Math.Min(k, n));
            for (int i = 0; i < n; i++)
            {
                Job j = jobs[i % take];
                if (!output.ContainsKey(j)) { output[j] = 0; order.Add(j); }
                output[j] += 1;
            }
        }

        static Reveal RollAndGrantChest(EconState e, ChestKind kind, EconContext ctx)
        {
            ChestDef d = Chests.Get(kind);
            int a = ArenaIdx(e);
            int total = d.Cards + d.PerArena * a;
            int epic = (int)Math.Floor(d.Epic) + (ctx.Rng.Next() < d.Epic % 1 ? 1 : 0);
            int legend = ctx.Rng.Next() < d.Legendary ? 1 : 0;
            int rare = Math.Min(total - epic - legend, JsMath.RoundInt(d.Rare * (1 + a * 0.25)));
            int common = Math.Max(0, total - rare - epic - legend);
            var cards = new Dictionary<Job, int>();
            var order = new List<Job>();
            Split(Rarity.Common, common, 3, cards, order, ctx.Rng);
            Split(Rarity.Rare, rare, 2, cards, order, ctx.Rng);
            Split(Rarity.Epic, epic, epic, cards, order, ctx.Rng);
            Split(Rarity.Legendary, legend, 1, cards, order, ctx.Rng);
            int gems = d.GemsMin + SystemRandom.Index(ctx.Rng, d.GemsMax - d.GemsMin + 1);
            return new Reveal { Title = Loc.Tl(d.Kr), Drops = GrantDrops(e, cards, order, gems), Gems = gems, Chest = kind };
        }

        /// <summary>카드·보석 지급. 결과는 전설 → 영웅 → 희귀 → 일반 순 (같은 등급은 나온 순서)</summary>
        static List<CardDrop> GrantDrops(EconState e, Dictionary<Job, int> cards, List<Job> order, int gems)
        {
            e.Crystals += gems;
            var drops = new List<CardDrop>();
            foreach (Job j in order) drops.Add(new CardDrop { Job = j, N = cards[j], Fresh = AddCards(e, j, cards[j]) });
            var sorted = new List<CardDrop>(drops.Count);
            for (int r = (int)Rarity.Legendary; r >= 0; r--)
            {
                foreach (CardDrop c in drops) if ((int)Units.Get(c.Job).Rarity == r) sorted.Add(c);
            }
            return sorted;
        }

        public static Reveal OpenChestNow(EconState e, ChestKind kind, EconContext ctx) => RollAndGrantChest(e, kind, ctx);

        /// <summary>대전 승리 상자 → 빈 칸 (가득이면 null)</summary>
        public static ChestKind? GiveWinChest(EconState e)
        {
            int i = Array.FindIndex(e.Chests, c => c == null);
            if (i < 0) return null;
            ChestKind kind = Chests.Cycle[e.ChestCycle % Chests.Cycle.Length];
            e.ChestCycle += 1;
            e.Chests[i] = new ChestSlot { Kind = kind };
            return kind;
        }

        /// <summary>상자 열기 시작 (한 번에 하나만)</summary>
        public static bool StartUnlock(EconState e, int i, EconContext ctx)
        {
            if (i < 0 || i >= e.Chests.Length) return false;
            ChestSlot c = e.Chests[i];
            if (c == null || c.OpenAt.HasValue || Array.Exists(e.Chests, x => x != null && x.OpenAt.HasValue)) return false;
            c.OpenAt = ctx.Now + (long)(Chests.Get(c.Kind).Hours * Hour);
            return true;
        }

        /// <summary>남은 시간(ms) - 열기 전이면 전체 시간</summary>
        public static long ChestLeftMs(ChestSlot c, long now)
            => c.OpenAt.HasValue ? c.OpenAt.Value - now : (long)(Chests.Get(c.Kind).Hours * Hour);

        /// <summary>다 된 상자 열기, 또는 pay=true 면 보석으로 바로</summary>
        public static Reveal OpenSlot(EconState e, int i, bool pay, EconContext ctx)
        {
            if (i < 0 || i >= e.Chests.Length) return null;
            ChestSlot c = e.Chests[i];
            if (c == null) return null;
            long left = ChestLeftMs(c, ctx.Now);
            if (left > 0)
            {
                if (!pay) return null;
                int cost = Chests.OpenCost(left);
                if (e.Crystals < cost) return null;
                e.Crystals -= cost;
            }
            e.Chests[i] = null;
            return OpenChestNow(e, c.Kind, ctx);
        }

        /// <summary>유닛 뽑기: 보석으로, 또는 ticket=true 면 뽑기권 n장으로 (10회도 영웅+ 보장 그대로)</summary>
        public static Reveal Gacha(EconState e, int n, EconContext ctx, bool ticket = false)
        {
            n = n == 10 ? 10 : 1;
            if (ticket)
            {
                if (e.Item(ItemKey.GachaTicket) < n) return null;
                e.Items[ItemKey.GachaTicket] = e.Item(ItemKey.GachaTicket) - n;
            }
            else
            {
                int cost = n == 1 ? Chests.GachaOne : Chests.GachaTen;
                if (e.Crystals < cost) return null;
                e.Crystals -= cost;
            }
            var rolls = new Rarity[n];
            for (int i = 0; i < n; i++) rolls[i] = RollGachaRarity(ctx.Rng);
            if (n == 10 && Array.TrueForAll(rolls, r => r != Rarity.Epic && r != Rarity.Legendary)) rolls[n - 1] = Rarity.Epic;
            var cards = new Dictionary<Job, int>();
            var order = new List<Job>();
            foreach (Rarity r in rolls)
            {
                List<Job> p = Pool(r);
                Job j = p[SystemRandom.Index(ctx.Rng, p.Count)];
                if (!cards.ContainsKey(j)) { cards[j] = 0; order.Add(j); }
                cards[j] += Chests.GachaCount[(int)r];
            }
            return new Reveal { Title = Loc.Tl("유닛 뽑기 {0}회", n), Drops = GrantDrops(e, cards, order, 0), Gems = 0, IsGacha = true };
        }

        static Rarity RollGachaRarity(IRandom rng)
        {
            double r = rng.Next();
            for (int k = (int)Rarity.Legendary; k >= 0; k--)
            {
                if (r < Chests.GachaRates[k]) return (Rarity)k;
                r -= Chests.GachaRates[k];
            }
            return Rarity.Common;
        }

        // ---------------- 보상 ----------------
        /// <summary>광고 '보상 2배' 대상 (직전 판 보상, 10분 안 1회)</summary>
        static void SetLastReward(EconState e, int amount, EconContext ctx)
        {
            e.LastReward = amount > 0 ? new LastReward { Amount = amount, At = ctx.Now, Used = false } : null;
        }

        public static void AddSeasonXp(EconState e, int xp, EconContext ctx)
        {
            string id = ctx.SeasonId;
            if (e.Season.Id != id) e.Season = SeasonState.Fresh(id);
            e.Season.Xp += xp;
        }

        /// <summary>트로피 로드: 도달한 보상 받기 (트로피는 서버 기록)</summary>
        public static RoadClaimResult ClaimRoad(EconState e, EconContext ctx)
        {
            var res = new RoadClaimResult();
            foreach (RoadStep s in TrophyRoad.Steps)
            {
                if (e.Pvp.Trophies < s.At || e.RoadClaimed.Contains(s.At)) continue;
                e.RoadClaimed.Add(s.At);
                if (s.Crystals > 0) e.Crystals += s.Crystals;
                if (s.Unit.HasValue) UnlockUnit(e, s.Unit.Value);
                if (s.Chest.HasValue) res.Reveals.Add(OpenChestNow(e, s.Chest.Value, ctx));
                res.Steps.Add(s);
            }
            return res;
        }

        /// <summary>대전 결과 보상 (트로피·승패는 서버 판정 뒤)</summary>
        public static PvpRewardResult PvpReward(EconState e, bool won, EconContext ctx)
        {
            int crystals = won ? PvpConfig.RewardWin : PvpConfig.RewardLoss;
            e.Crystals += crystals;
            AddSeasonXp(e, won ? PvpConfig.SeasonXpWin : PvpConfig.SeasonXpLoss, ctx);
            SetLastReward(e, crystals, ctx);
            ChestKind? chest = won ? GiveWinChest(e) : null;
            RoadClaimResult road = ClaimRoad(e, ctx);
            return new PvpRewardResult { Crystals = crystals, Chest = chest, Road = road.Steps, Reveals = road.Reveals };
        }

        /// <summary>스테이지 끝 (온라인은 서버가 시작 기록·최소 시간을 확인한 뒤 부른다)</summary>
        public static StageEndResult StageEnd(EconState e, string id, bool cleared, int stars, int wave, EconContext ctx)
        {
            StageDef st = Campaign.Get(id);
            if (st == null) return new StageEndResult();
            int prev = e.StarsOf(id);
            if (!cleared)
            {
                int c = Math.Max(0, Math.Min(st.Waves, wave)) >> 1;
                e.Crystals += c;
                SetLastReward(e, c, ctx);
                return new StageEndResult { Crystals = c };
            }
            stars = Math.Max(1, Math.Min(3, stars));
            bool firstClear = prev == 0;
            int crystals = firstClear ? st.FirstReward : st.ReplayReward;
            e.Stages[id] = Math.Max(prev, stars);
            e.Crystals += crystals;
            AddSeasonXp(e, 30, ctx);
            SetLastReward(e, crystals, ctx);
            Job? unlocked = null;
            if (firstClear && st.Unlocks.HasValue && !e.Owns(st.Unlocks.Value))
            {
                unlocked = st.Unlocks;
                UnlockUnit(e, st.Unlocks.Value);
            }
            return new StageEndResult { Stars = stars, FirstClear = firstClear, NewBest = stars > prev, Crystals = crystals, Unlocked = unlocked };
        }

        public static int EndlessEnd(EconState e, int wave, EconContext ctx)
        {
            int c = Campaign.CrystalReward(Math.Max(0, Math.Min(50, wave)));
            e.Crystals += c;
            SetLastReward(e, c, ctx);
            return c;
        }

        /// <summary>광고 보상: double = 직전 판 보상 한 번 더, 아니면 뽑기권 (하루 횟수 제한). 받은 양 (못 받으면 0)</summary>
        public static int AdReward(EconState e, bool doubleReward, EconContext ctx)
        {
            if (doubleReward)
            {
                LastReward l = e.LastReward;
                if (l == null || l.Used || ctx.Now - l.At > 10 * 60 * 1000) return 0;
                l.Used = true;
                e.Crystals += l.Amount;
                return l.Amount;
            }
            string k = "adTicket:" + ctx.Today;
            int used = e.Counters.TryGetValue(k, out int n) ? n : 0;
            if (used >= AdTicketsPerDay) return 0;
            e.Counters[k] = used + 1;
            AddItem(e, ItemKey.GachaTicket, AdTicketAmount);
            return AdTicketAmount;
        }

        /// <summary>오늘 남은 광고 뽑기권 횟수</summary>
        public static int AdTicketsLeft(EconState e, EconContext ctx)
            => AdTicketsPerDay - (e.Counters.TryGetValue("adTicket:" + ctx.Today, out int n) ? n : 0);

        public static int ChapterStars(EconState e, int chapter)
        {
            int stars = 0;
            foreach (KeyValuePair<string, int> kv in e.Stages)
            {
                StageDef st = Campaign.Get(kv.Key);
                if (st != null && st.Chapter == chapter) stars += kv.Value;
            }
            return stars;
        }

        public static int ClaimStarReward(EconState e, int chapter, int goalIdx)
        {
            string key = "stars:" + chapter + ":" + goalIdx;
            if (e.OneTime.Contains(key) || goalIdx < 0 || goalIdx >= Campaign.ChapterStarGoals.Length) return 0;
            if (ChapterStars(e, chapter) < Campaign.ChapterStarGoals[goalIdx]) return 0;
            int r = Campaign.ChapterStarReward(goalIdx);
            e.OneTime.Add(key);
            e.Crystals += r;
            return r;
        }

        /// <summary>일일 퀘스트 보상 (진행도는 기기 기록 - 퀘스트마다 하루 한 번만)</summary>
        public static int ClaimQuest(EconState e, string key, EconContext ctx)
        {
            DailyQuestDef q = Quests.GetDaily(key);
            if (q == null) return 0;
            string k = "quest:" + ctx.Today + ":" + key;
            if (e.OneTime.Contains(k)) return 0;
            e.OneTime.Add(k);
            e.Crystals += q.Reward;
            return q.Reward;
        }

        /// <summary>업적 보상 (업적마다 한 번)</summary>
        public static int ClaimAchievement(EconState e, string key)
        {
            AchievementDef a = Quests.GetAchievement(key);
            if (a == null || e.Achievements.Contains(key)) return 0;
            e.Achievements.Add(key);
            e.Crystals += a.Reward;
            return a.Reward;
        }

        public static int SeasonTierOf(EconState e) => Math.Min(Season.TierCount, e.Season.Xp / Season.XpPerTier);

        static void ApplySeasonReward(EconState e, SeasonReward r)
        {
            if (r.Crystals > 0) e.Crystals += r.Crystals;
            if (r.GachaTickets > 0) AddItem(e, ItemKey.GachaTicket, r.GachaTickets);
            if (r.GoldBoosters > 0) AddItem(e, ItemKey.GoldBooster, r.GoldBoosters);
        }

        public static bool ClaimSeason(EconState e, int tier, bool premium, EconContext ctx)
        {
            if (e.Season.Id != ctx.SeasonId || tier < 1 || tier > SeasonTierOf(e)) return false;
            List<int> list = premium ? e.Season.ClaimedPremium : e.Season.ClaimedFree;
            if (list.Contains(tier) || (premium && !e.Season.Premium)) return false;
            ApplySeasonReward(e, premium ? Season.Premium(tier) : Season.Free(tier));
            list.Add(tier);
            return true;
        }

        public static int ClaimMonthly(EconState e, EconContext ctx)
        {
            if (e.Monthly.Expires <= ctx.Now || e.Monthly.LastClaim == ctx.Today) return 0;
            e.Monthly.LastClaim = ctx.Today;
            e.Crystals += Products.MonthlyDailyCrystals;
            return Products.MonthlyDailyCrystals;
        }

        // ---------------- 결제 지급 ----------------
        /// <summary>상품 지급 (서버: 영수증 검증 뒤 / 오프라인: 테스트 결제 뒤). 상자 상품이면 열린 내용 반환</summary>
        public static List<Reveal> GrantProduct(EconState e, string productId, int qty, EconContext ctx)
        {
            var reveals = new List<Reveal>();
            ProductDef def = Products.Get(productId);
            if (def == null) return reveals;
            if (def.OneTime && e.OneTime.Contains(def.Id)) return reveals;
            ProductGrant g = def.Grant;
            qty = Math.Max(1, Math.Min(99, qty));
            if (g.Crystals > 0) e.Crystals += g.Crystals * qty;
            foreach (KeyValuePair<ItemKey, int> kv in g.Items) AddItem(e, kv.Key, kv.Value * qty);
            if (g.AdRemove) e.AdRemoved = true;
            if (g.MonthlyDays > 0) e.Monthly.Expires = Math.Max(ctx.Now, e.Monthly.Expires) + g.MonthlyDays * Day * qty;
            if (g.SeasonPremium)
            {
                if (e.Season.Id != ctx.SeasonId) e.Season = SeasonState.Fresh(ctx.SeasonId);
                e.Season.Premium = true;
            }
            if (g.Chest.HasValue) for (int i = 0; i < qty; i++) reveals.Add(OpenChestNow(e, g.Chest.Value, ctx));
            foreach (KeyValuePair<Job, int> kv in g.Units)
            {
                if (!e.Owns(kv.Key)) UnlockUnit(e, kv.Key);
                e.UnitLevels[kv.Key] = Math.Max(e.LevelOf(kv.Key), kv.Value);
            }
            if (def.OneTime) e.OneTime.Add(def.Id);
            return reveals;
        }
    }
}
