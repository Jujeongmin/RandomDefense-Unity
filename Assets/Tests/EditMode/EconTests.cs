using NUnit.Framework;

namespace DefenseBattle.Core.Tests
{
    /// <summary>경제 규칙 (econ/rules.ts 와 같은 결과)</summary>
    public class EconTests
    {
        // 2026-10-19 00:00 UTC
        const long Now = 1792368000000L;

        static EconContext Ctx(long now = Now, int seed = 1) => new EconContext(now, new SystemRandom(seed));

        static EconState Fresh() => EconState.Fresh(Ctx().SeasonId);

        [Test]
        public void Fresh_HasStartersAndWoodChest()
        {
            EconState e = Fresh();
            Assert.AreEqual(3, e.Deck.Count);
            Assert.IsTrue(e.Owns(Job.Archer));
            Assert.IsFalse(e.Owns(Job.Ranger));
            Assert.AreEqual(ChestKind.Wood, e.Chests[0].Kind);
            Assert.IsNull(e.Chests[1]);
            Assert.AreEqual("S2026-10", e.Season.Id);
        }

        [Test]
        public void Cards_UnlockThenLevel()
        {
            EconState e = Fresh();
            Assert.IsTrue(EconRules.AddCards(e, Job.Ranger, 3)); // 1장 해금 + 2장
            Assert.AreEqual(1, e.LevelOf(Job.Ranger));
            Assert.AreEqual(2, e.CardCount(Job.Ranger));
            CollectionAssert.Contains(e.Deck, Job.Ranger); // 빈 덱 칸에 자동으로
            Assert.IsFalse(EconRules.UpgradeUnit(e, Job.Ranger)); // 보석 부족
            e.Crystals = 40;
            Assert.IsTrue(EconRules.UpgradeUnit(e, Job.Ranger)); // 희귀 Lv1→2: 카드 1장, 보석 40
            Assert.AreEqual(2, e.LevelOf(Job.Ranger));
            Assert.AreEqual(1, e.CardCount(Job.Ranger));
            Assert.AreEqual(0, e.Crystals);
        }

        [Test]
        public void BuyUnit_LegendaryNotForSale()
        {
            EconState e = Fresh();
            e.Crystals = 5000;
            Assert.IsTrue(EconRules.BuyUnit(e, Job.Spearman));
            Assert.AreEqual(4800, e.Crystals);
            Assert.IsFalse(EconRules.BuyUnit(e, Job.Spearman)); // 이미 있음
            Assert.IsFalse(EconRules.BuyUnit(e, Job.Angel));    // 전설은 상자·뽑기로만
        }

        [Test]
        public void Research_CostAndCap()
        {
            EconState e = Fresh();
            e.Crystals = 100000;
            for (int i = 0; i < 10; i++) Assert.IsTrue(EconRules.DoResearch(e, ResearchKey.Rare));
            Assert.IsFalse(EconRules.DoResearch(e, ResearchKey.Rare)); // 최대 10
            Assert.AreEqual(100000 - (5 + 10 + 15 + 20 + 25 + 30 + 35 + 40 + 45 + 50), e.Crystals);
        }

        [Test]
        public void Chests_OneUnlockAtATime_PayToOpen()
        {
            EconState e = Fresh();
            EconContext ctx = Ctx();
            Assert.IsTrue(EconRules.StartUnlock(e, 0, ctx));
            Assert.IsNotNull(EconRules.GiveWinChest(e)); // 칸 1 에 나무
            Assert.IsFalse(EconRules.StartUnlock(e, 1, ctx)); // 이미 하나 여는 중
            Assert.IsNull(EconRules.OpenSlot(e, 0, false, ctx)); // 아직 30분 안 됨
            Reveal r = EconRules.OpenSlot(e, 0, false, Ctx(Now + 30 * 60 * 1000));
            Assert.IsNotNull(r);
            Assert.IsNull(e.Chests[0]);
            int cards = 0;
            foreach (CardDrop d in r.Drops) cards += d.N;
            Assert.AreEqual(Chests.Get(ChestKind.Wood).Cards, cards); // 브론즈 아레나
            Assert.GreaterOrEqual(r.Gems, 8);
            // 보석으로 바로: 남은 30분 = 10 보석
            e.Crystals = 10;
            Reveal paid = EconRules.OpenSlot(e, 1, true, ctx);
            Assert.IsNotNull(paid);
            Assert.AreEqual(paid.Gems, e.Crystals); // 10 을 내고 상자에서 나온 보석만 남음
        }

        [Test]
        public void WinChest_FollowsCycleAndFullSlots()
        {
            EconState e = Fresh();
            Assert.AreEqual(ChestKind.Wood, EconRules.GiveWinChest(e));
            Assert.AreEqual(ChestKind.Wood, EconRules.GiveWinChest(e));
            Assert.AreEqual(ChestKind.Silver, EconRules.GiveWinChest(e));
            Assert.IsNull(EconRules.GiveWinChest(e)); // 4칸 가득
            Assert.AreEqual(3, e.ChestCycle);
        }

        [Test]
        public void Gacha_TenGuaranteesEpicPlus()
        {
            for (int seed = 1; seed <= 30; seed++)
            {
                EconState e = Fresh();
                e.Crystals = 900;
                Reveal r = EconRules.Gacha(e, 10, Ctx(Now, seed));
                Assert.IsNotNull(r);
                Assert.AreEqual(0, e.Crystals);
                bool epic = false;
                foreach (CardDrop d in r.Drops) epic |= Units.Get(d.Job).Rarity >= Rarity.Epic;
                Assert.IsTrue(epic, "seed " + seed);
            }
            EconState t = Fresh();
            Assert.IsNull(EconRules.Gacha(t, 1, Ctx(), ticket: true)); // 뽑기권 없음
        }

        [Test]
        public void StageEnd_FirstClearUnlocksAndRewards()
        {
            EconState e = Fresh();
            StageEndResult r = EconRules.StageEnd(e, "1-3", true, 3, 10, Ctx());
            Assert.IsTrue(r.FirstClear);
            Assert.AreEqual(40 + 2 * 12, r.Crystals);
            Assert.AreEqual(Job.Ranger, r.Unlocked);
            Assert.AreEqual(30, e.Season.Xp);
            StageEndResult again = EconRules.StageEnd(e, "1-3", true, 2, 10, Ctx());
            Assert.IsFalse(again.FirstClear);
            Assert.AreEqual(10 + 2 * 3, again.Crystals);
            Assert.AreEqual(3, e.StarsOf("1-3")); // 최고 기록 유지
            StageEndResult fail = EconRules.StageEnd(e, "1-4", false, 0, 7, Ctx());
            Assert.AreEqual(3, fail.Crystals); // 웨이브 7 >> 1
        }

        [Test]
        public void AdReward_DoubleOnceWithin10Min_TicketsThreePerDay()
        {
            EconState e = Fresh();
            EconRules.EndlessEnd(e, 20, Ctx());
            int c = e.Crystals;
            Assert.AreEqual(Campaign.CrystalReward(20), EconRules.AdReward(e, true, Ctx(Now + 60_000)));
            Assert.AreEqual(0, EconRules.AdReward(e, true, Ctx(Now + 60_000))); // 1회
            Assert.AreEqual(c * 2, e.Crystals);
            for (int i = 0; i < 3; i++) Assert.AreEqual(1, EconRules.AdReward(e, false, Ctx()));
            Assert.AreEqual(0, EconRules.AdReward(e, false, Ctx()));
            Assert.AreEqual(1, EconRules.AdReward(e, false, Ctx(Now + 24L * 3600 * 1000))); // 다음 날
        }

        [Test]
        public void PvpReward_ChestAndRoad()
        {
            EconState e = Fresh();
            e.Pvp.Trophies = 35;
            PvpRewardResult r = EconRules.PvpReward(e, true, Ctx());
            Assert.AreEqual(PvpConfig.RewardWin + 50, e.Crystals); // 승리 40 + 로드 15트로피 50
            Assert.AreEqual(ChestKind.Wood, r.Chest);
            Assert.AreEqual(2, r.Road.Count);       // 15(보석), 30(레인저)
            Assert.IsTrue(e.Owns(Job.Ranger));
            Assert.AreEqual(0, EconRules.ClaimRoad(e, Ctx()).Steps.Count); // 이미 받음
        }

        [Test]
        public void GrantProduct_OneTimeAndUnitPack()
        {
            EconState e = Fresh();
            EconRules.GrantProduct(e, "unit_ranger", 1, Ctx());
            Assert.AreEqual(5, e.LevelOf(Job.Ranger));
            Assert.AreEqual(200, e.Crystals);
            EconRules.GrantProduct(e, "unit_ranger", 1, Ctx()); // 1회 한정
            Assert.AreEqual(200, e.Crystals);
            EconRules.GrantProduct(e, "crystal_550", 2, Ctx());
            Assert.AreEqual(200 + 1100, e.Crystals);
            Assert.AreEqual(1, EconRules.GrantProduct(e, "chest_legend", 1, Ctx()).Count);
            EconRules.GrantProduct(e, "monthly_pass", 1, Ctx());
            Assert.AreEqual(Now + 30L * 24 * 3600 * 1000, e.Monthly.Expires);
            Assert.AreEqual(40, EconRules.ClaimMonthly(e, Ctx()));
            Assert.AreEqual(0, EconRules.ClaimMonthly(e, Ctx())); // 하루 한 번
            Assert.AreEqual(0, EconRules.GrantProduct(e, "nope", 1, Ctx()).Count);
        }

        [Test]
        public void Season_ClaimTiers()
        {
            EconState e = Fresh();
            EconRules.AddSeasonXp(e, 450, Ctx());
            Assert.AreEqual(2, EconRules.SeasonTierOf(e));
            Assert.IsTrue(EconRules.ClaimSeason(e, 1, false, Ctx()));
            Assert.IsFalse(EconRules.ClaimSeason(e, 1, false, Ctx()));
            Assert.IsFalse(EconRules.ClaimSeason(e, 3, false, Ctx())); // 아직
            Assert.IsFalse(EconRules.ClaimSeason(e, 2, true, Ctx()));  // 프리미엄 없음
            EconRules.GrantProduct(e, "season_premium", 1, Ctx());
            Assert.IsTrue(EconRules.ClaimSeason(e, 2, true, Ctx()));
            // 다음 달이면 새 시즌
            EconRules.AddSeasonXp(e, 10, Ctx(Now + 31L * 24 * 3600 * 1000));
            Assert.AreEqual("S2026-11", e.Season.Id);
            Assert.AreEqual(10, e.Season.Xp);
        }

        [Test]
        public void Quests_StarsAchievements_Once()
        {
            EconState e = Fresh();
            Assert.AreEqual(20, EconRules.ClaimQuest(e, "summon30", Ctx()));
            Assert.AreEqual(0, EconRules.ClaimQuest(e, "summon30", Ctx()));
            Assert.AreEqual(20, EconRules.ClaimQuest(e, "summon30", Ctx(Now + 24L * 3600 * 1000)));
            Assert.AreEqual(500, EconRules.ClaimAchievement(e, "clear50"));
            Assert.AreEqual(0, EconRules.ClaimAchievement(e, "clear50"));
            Assert.AreEqual(0, EconRules.ClaimStarReward(e, 1, 0));
            for (int i = 1; i <= 4; i++) e.Stages["1-" + i] = 3;
            Assert.AreEqual(100, EconRules.ClaimStarReward(e, 1, 0)); // 12별 ≥ 10
            Assert.AreEqual(0, EconRules.ClaimStarReward(e, 1, 0));
        }
    }
}
