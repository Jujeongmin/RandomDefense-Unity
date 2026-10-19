using NUnit.Framework;

namespace DefenseBattle.Core.Tests
{
    /// <summary>PvP: 몹 던지기·받기, 스냅샷, 봇 대전</summary>
    public class PvpTests
    {
        [Test]
        public void SendAttack_OnlyDeckCardsAndGold()
        {
            Game g = TestUtil.NewGame(GameMode.Pvp);
            AttackPayload? sent = null;
            g.AttackSent += p => sent = p;
            g.Gold = 1000;
            Assert.IsFalse(g.SendAttack((int)AttackKey.Demon)); // 덱에 없음
            Assert.IsTrue(g.SendAttack((int)AttackKey.Swarm, 0.25));
            Assert.AreEqual(1000 - 40, g.Gold);
            Assert.IsTrue(sent.HasValue);
            Assert.AreEqual(0.25, sent.Value.Pos);
            g.Gold = 10;
            Assert.IsFalse(g.SendAttack((int)AttackKey.Giant)); // 골드 부족

            Game solo = TestUtil.NewGame(GameMode.Solo);
            solo.Gold = 1000;
            Assert.IsFalse(solo.SendAttack((int)AttackKey.Swarm)); // 일반 모드는 못 던짐
        }

        [Test]
        public void DemonCard_HasCooldown()
        {
            var o = new GameOptions { Mode = GameMode.Pvp, Seed = 1, RandomSeed = 1, Track = false };
            o.AttackDeck.Clear();
            o.AttackDeck.Add(AttackKey.Demon);
            var g = new Game(o);
            g.Gold = 1000;
            Assert.IsTrue(g.SendAttack((int)AttackKey.Demon));
            Assert.AreEqual(30, g.AttackCooldown((int)AttackKey.Demon), 1e-9);
            Assert.IsFalse(g.SendAttack((int)AttackKey.Demon));
        }

        [Test]
        public void ReceiveAttack_DropsAfterWarning()
        {
            Game g = TestUtil.NewGame(GameMode.Pvp);
            TestUtil.HoldWaves(g);
            g.ReceiveAttack(new AttackPayload { Card = (int)AttackKey.Swarm, Wave = 3, Pos = 0.5 });
            Assert.AreEqual(8, g.Pending.Count);
            Assert.AreEqual(0, g.Mobs.Count);
            for (int i = 0; i < 31; i++) g.Update(0.05); // 1.55초 - 아직
            Assert.AreEqual(0, g.Mobs.Count);
            for (int i = 0; i < 60; i++) g.Update(0.05);
            Assert.AreEqual(8, g.Mobs.Count);
            Assert.AreEqual(0, g.Pending.Count);
            double hp = g.BaseHp(3) * Attacks.Get(AttackKey.Swarm).HpMult;
            Assert.AreEqual(hp, g.Mobs[0].MaxHp, 1e-9);
            Assert.AreEqual(MobKind.Card, g.Mobs[0].Kind);
        }

        [Test]
        public void Snapshot_QuantizesAndClearsFx()
        {
            Game g = TestUtil.NewGame(GameMode.Pvp, Job.Archer);
            TestUtil.HoldWaves(g);
            TestUtil.Place(g, Job.Archer, GradeKey.Rare);
            g.DebugSpawn(720, 0, 100);
            BoardSnapshot s = g.Snapshot();
            Assert.AreEqual(5, s.Mobs.Count);
            Assert.AreEqual(255, s.Mobs[1]);
            Assert.AreEqual(0, s.Mobs[2]);
            Assert.AreEqual(4, s.Units.Count);
            Assert.AreEqual((int)Job.Archer, s.Units[0]);
            Assert.AreEqual(1, s.TopGrade);
            Assert.AreEqual(8, s.Gp.Count); // 160 표본 / 20 비트
            Assert.Greater(s.Fx.Count, 0);  // 소환 이펙트
            BoardSnapshot again = g.Snapshot(s); // 재사용
            Assert.AreSame(s, again);
            Assert.AreEqual(0, again.Fx.Count);
            Assert.AreEqual(5, again.Mobs.Count);
        }

        [Test]
        public void PvpHp_StartsTripleAndGrowsFaster()
        {
            Game pvp = TestUtil.NewGame(GameMode.Pvp);
            Game solo = TestUtil.NewGame(GameMode.Solo);
            Assert.AreEqual(300, pvp.BaseHp(1), 1e-9);
            Assert.AreEqual(100, solo.BaseHp(1), 1e-9);
            Assert.AreEqual(300 * 1.25, pvp.BaseHp(2), 1e-9);
            Assert.AreEqual(int.MaxValue, pvp.TotalWaves);
        }

        [Test]
        public void BotMatch_PlaysAndThrows()
        {
            var bot = new BotMatch(seed: 42, skill: 0.6, trophies: 500, research: BotProfile.Research(500, 10), unitLevel: 3, rand: new SystemRandom(9));
            int throws = 0, snaps = 0;
            bot.AttackSent += _ => throws++;
            bot.Snapshot += _ => snaps++;
            for (int i = 0; i < 60 * 60 * 4 && !bot.Done; i++) bot.Tick(1 / 60.0); // 실시간 4분
            Assert.Greater(snaps, 100);
            Assert.Greater(throws, 0);
            Assert.Greater(bot.Game.Units.Count, 10);
        }

        [Test]
        public void BotProfile_Rules()
        {
            Assert.AreEqual(0.05, BotProfile.Skill(1000, 1, 1, 0, 0)); // 첫 3판
            Assert.AreEqual(0.1 + 300 / 1500.0, BotProfile.Skill(300, 5, 5, 0, 0), 1e-12);
            Assert.AreEqual(0.85, BotProfile.Skill(5000, 5, 5, 4, 0));
            Assert.AreEqual(0, BotProfile.Research(1500, 2)[ResearchKey.Atk]);
            Assert.AreEqual(20, BotProfile.Research(1500, 3)[ResearchKey.Atk]);
            Assert.AreEqual(40, BotProfile.TrophyDelta(true, 5, 100));
            Assert.AreEqual(-7, BotProfile.TrophyDelta(false, 0, 7));
            var r = new SystemRandom(1);
            Assert.AreEqual(4, BotProfile.Deck(0, r).Count);
            foreach (Job j in BotProfile.Deck(0, r)) Assert.AreEqual(Rarity.Common, Units.Get(j).Rarity);
            foreach (AttackKey k in BotProfile.AttackDeck(0, r)) CollectionAssert.Contains(Attacks.Starter, k);
        }
    }
}
