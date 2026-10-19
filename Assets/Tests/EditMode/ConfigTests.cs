using NUnit.Framework;

namespace DefenseBattle.Core.Tests
{
    /// <summary>데이터 표·RNG·맵 기하가 원본(TS)과 같은지. 기대값은 원본 코드를 실행해서 뽑은 값</summary>
    public class ConfigTests
    {
        [Test]
        public void Mulberry32_MatchesOriginalBitForBit()
        {
            var r = new Mulberry32(12345);
            double[] expected = { 0.9797282677609473, 0.3067522644996643, 0.484205421525985, 0.817934412509203, 0.5094283693470061 };
            foreach (double e in expected) Assert.AreEqual(e, r.Next(), 0.0);

            var big = new Mulberry32(2147483647);
            double[] expectedBig = { 0.4290980885270983, 0.12713524978607893, 0.3852774982806295 };
            foreach (double e in expectedBig) Assert.AreEqual(e, big.Next(), 0.0);
        }

        [Test]
        public void WaveRaces_SameSeedSameSequenceAsOriginal()
        {
            // 원본: makeRng(777) 로 웨이브마다 RACES[floor(rng*3)]
            int[] expected = { 2, 0, 0, 0, 1, 1, 0, 2, 1, 2, 2, 2, 0, 2, 1, 2, 1, 0, 2, 2 };
            var g = new Game(new GameOptions { Seed = 777, RandomSeed = 1, Track = false });
            foreach (int race in expected)
            {
                g.StartNextWave();
                Assert.AreEqual((Race)race, g.CurrentRace);
            }
        }

        [Test]
        public void StageHpCurve_MatchesOriginal()
        {
            double[] expected =
            {
                0.55, 0.63, 0.71, 0.79, 0.87, 0.95, 1.03, 1.11, 1.19, 1.27, 1.4, 1.54, 1.68, 1.82, 1.96,
                2.1, 2.24, 2.38, 2.52, 2.66, 2.6, 2.76, 2.92, 3.08, 3.24, 3.4, 3.56, 3.72, 3.88, 4.04,
            };
            Assert.AreEqual(30, Campaign.Stages.Length);
            for (int i = 0; i < expected.Length; i++) Assert.AreEqual(expected[i], Campaign.Stages[i].HpMult, 1e-12);
            Assert.AreEqual(Job.Ranger, Campaign.Get("1-3").Unlocks);
            Assert.AreEqual(Job.Assassin, Campaign.Get("3-1").Unlocks);
            Assert.AreEqual(new[] { Race.Orc }, Campaign.Get("1-1").Races);
            Assert.IsNull(Campaign.Get("1-4").Races);
        }

        [Test]
        public void SkillVal_ThreeTiers()
        {
            Assert.AreEqual(1, Skills.Val(0, 1, 2));
            Assert.AreEqual(2, Skills.Val(2, 1, 2));
            Assert.AreEqual(3, Skills.Val(3, 1, 2));
            Assert.AreEqual(0.51, Skills.Val(4, 0.25, 0.38), 1e-12);
            Assert.AreEqual(86, Skills.Val(2, 62, 86));
            Assert.AreEqual(0.6000000000000001, Skills.Val(5, 0.3, 0.45), 0.0);
        }

        [Test]
        public void PathGeometry_MatchesOriginal()
        {
            Vec2 a = Board.PathAt(0.3, out int next);
            Assert.AreEqual(186.0000000000001, a.X, 1e-9);
            Assert.AreEqual(650, a.Y, 1e-9);
            Assert.AreEqual(2, next);
            Vec2 b = Board.PathAt(-0.1, out int next2);
            Assert.AreEqual(301.99999999999994, b.X, 1e-9);
            Assert.AreEqual(70, b.Y, 1e-9);
            Assert.AreEqual(0, next2);
            Assert.AreEqual(0.39224137931034486, Board.PathPosNear(400, 600), 1e-12);
        }

        [Test]
        public void Tables_AreInEnumOrder()
        {
            Assert.AreEqual(32, Units.All.Length);
            for (int i = 0; i < Units.All.Length; i++) Assert.AreEqual((Job)i, Units.All[i].Job);
            Assert.AreEqual(16, Attacks.All.Length);
            for (int i = 0; i < Attacks.All.Length; i++) Assert.AreEqual((AttackKey)i, Attacks.All[i].Key);
            Assert.AreEqual(32, Skills.Kr.Length);
            Assert.AreEqual(32, Skills.Desc.Length);
            Assert.IsTrue(Units.TryParse("houndmaster", out Job h));
            Assert.AreEqual(Job.Houndmaster, h);
        }

        [Test]
        public void UnitCardsAndPerks()
        {
            Assert.AreEqual(2, Units.CardsNeeded(Rarity.Common, 1));
            Assert.AreEqual(80, Units.CardsNeeded(Rarity.Legendary, 9)); // 800 / 10
            Assert.AreEqual(1, Units.CardsNeeded(Rarity.Epic, 1));       // ceil(2 / 5)
            Assert.AreEqual(1.0, Units.PerkMult(4));
            Assert.AreEqual(1.3, Units.PerkMult(5), 1e-12);
            Assert.AreEqual(1.6, Units.PerkMult(10), 1e-12);
            Assert.AreEqual(1.4, Units.DmgMult(Job.Acolyte, Race.Undead));
            Assert.AreEqual(1.0, Units.DmgMult(Job.Archer, Race.Orc));
            Assert.AreEqual(0.9, Units.DmgMult(Job.Archer, Race.Troll));
        }

        [Test]
        public void SummonRates_RareResearchMovesToLegendaryPlus()
        {
            double[] r = Summon.Rates(5);
            Assert.AreEqual(6.5 + 3.5, r[3], 1e-12);
            Assert.AreEqual(0.4 + 1.0, r[4], 1e-12);
            Assert.AreEqual(0.1 + 0.5, r[5], 1e-12);
        }

        [Test]
        public void Products_KrwPricingDecisions()
        {
            // 사용자 결정 (2026-10-19): 한정 상품 효율 약 200%, 시즌 패스 ₩5,900, 뽑기권 묶음은 안 판다
            foreach (string id in new[] { "offer_comeback", "offer_arena", "offer_chapter" })
            {
                int eff = Products.Efficiency(Products.Get(id));
                Assert.That(eff, Is.InRange(195, 210), id + " " + eff + "%");
            }
            Assert.AreEqual(5900, Products.Get("season_premium").Krw);
            Assert.IsNull(Products.Get("summon_ticket_pack"));
            Assert.IsNull(Products.Get("mythic_ticket"));
            Assert.AreEqual(100, Products.Efficiency(Products.Get("crystal_100")));
            Assert.AreEqual(300, Products.Efficiency(Products.Get("starter_pack")));
            foreach (ProductDef p in Products.All) Assert.Greater(p.Krw, 0, p.Id);
        }

        [Test]
        public void TextFormatsLikeJavaScript()
        {
            Assert.AreEqual("6.5% 궁수 전설등급 소환", Loc.Tl("{0}% {1} {2}등급 소환", 6.5, "궁수", "전설"));
            Assert.AreEqual("영웅", Loc.Tl("영웅@@button"));
        }
    }
}
