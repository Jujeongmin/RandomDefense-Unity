using NUnit.Framework;

namespace DefenseBattle.Core.Tests
{
    /// <summary>판 안 규칙: 소환·합성·강화·판매·구역·웨이브·패배·부활</summary>
    public class GameTests
    {
        [Test]
        public void StartGold_IncludesResearchAndBooster()
        {
            var g = new Game(new GameOptions { Seed = 1, RandomSeed = 1, Track = false, StartGoldMult = 2 });
            Assert.AreEqual(400, g.Gold);
            var r = new GameOptions { Seed = 1, RandomSeed = 1, Track = false };
            r.Research[ResearchKey.StartGold] = 5;
            Assert.AreEqual(250, new Game(r).Gold);
        }

        [Test]
        public void SummonCost_SmallDeckPenalty()
        {
            var o = new GameOptions { Seed = 1, RandomSeed = 1, Track = false, DeckMin = 4 };
            o.Deck.Clear();
            o.Deck.Add(Job.Archer);
            Assert.AreEqual(50, new Game(o).SummonCost); // 빈 칸 3 → 20 × (1 + 1.5)
            o.Deck.Add(Job.Archer); // 같은 유닛은 한 칸
            Assert.AreEqual(50, new Game(o).SummonCost);
            Assert.AreEqual(20, TestUtil.NewGame().SummonCost); // DeckMin 0 = 페널티 없음
        }

        [Test]
        public void Summon_SpendsGoldAndClaimsZoneInOrder()
        {
            Game g = TestUtil.NewGame();
            Unit a = TestUtil.Place(g, Job.Archer, GradeKey.Common);
            Unit w = TestUtil.Place(g, Job.Wizard, GradeKey.Common);
            Unit r = TestUtil.Place(g, Job.Warrior, GradeKey.Common);
            // 빈 구역은 상(0) → 좌(3) → 하(2) → 우(1) 순
            Assert.AreEqual(0, a.ZoneId);
            Assert.AreEqual(3, w.ZoneId);
            Assert.AreEqual(2, r.ZoneId);
            Assert.AreEqual(Job.Archer, g.Zones[0].Job);
            Unit a2 = TestUtil.Place(g, Job.Archer, GradeKey.Rare);
            Assert.AreEqual(0, a2.ZoneId); // 같은 직업은 같은 구역
            Assert.AreEqual(4, g.SummonCount);
        }

        [Test]
        public void Merge_ThreeSameMakesNextGrade()
        {
            Game g = TestUtil.NewGame();
            for (int i = 0; i < 3; i++) TestUtil.Place(g, Job.Archer, GradeKey.Common);
            Assert.IsTrue(g.CanMerge(Job.Archer, GradeKey.Common));
            Assert.AreEqual(1, g.Mergeable().Count);
            Unit u = g.Merge(Job.Archer, GradeKey.Common);
            Assert.IsNotNull(u);
            Assert.AreEqual(GradeKey.Rare, u.Grade);
            Assert.AreEqual(1, g.Units.Count);
            Assert.IsFalse(g.CanMerge(Job.Archer, GradeKey.Rare));
        }

        [Test]
        public void Merge_EternalCannotMerge()
        {
            Game g = TestUtil.NewGame();
            for (int i = 0; i < 3; i++) TestUtil.Place(g, Job.Wizard, GradeKey.Eternal);
            Assert.IsFalse(g.CanMerge(Job.Wizard, GradeKey.Eternal));
            Assert.IsNull(g.Merge(Job.Wizard, GradeKey.Eternal));
        }

        [Test]
        public void Upgrade_IsPerJobAndCostsMore()
        {
            Game g = TestUtil.NewGame();
            TestUtil.Place(g, Job.Archer, GradeKey.Common);
            g.Gold = 1000;
            Assert.AreEqual(20, g.UpgradeCost(Job.Archer));
            Assert.IsTrue(g.Upgrade(Job.Archer));
            Assert.AreEqual(40, g.UpgradeCost(Job.Archer));
            Assert.AreEqual(980, g.Gold);
            Assert.IsFalse(g.Upgrade(Job.Wizard)); // 판에 없는 직업
        }

        [Test]
        public void Sell_FreesZoneWhenEmpty()
        {
            Game g = TestUtil.NewGame();
            TestUtil.Place(g, Job.Archer, GradeKey.Elite);
            int gold = g.Gold;
            Assert.AreEqual(1, g.SellMany(Job.Archer, GradeKey.Elite, int.MaxValue));
            Assert.AreEqual(gold + 60, g.Gold);
            Assert.IsNull(g.Zones[0].Job);
            Assert.AreEqual(1, g.Sells);
        }

        [Test]
        public void SwapZones_MovesUnitsAndJobs()
        {
            Game g = TestUtil.NewGame();
            TestUtil.HoldWaves(g);
            Unit a = TestUtil.Place(g, Job.Archer, GradeKey.Common); // 0
            Unit w = TestUtil.Place(g, Job.Wizard, GradeKey.Common); // 3
            g.SwapZones(0, 3);
            Assert.AreEqual(3, a.ZoneId);
            Assert.AreEqual(0, w.ZoneId);
            Assert.AreEqual(Job.Wizard, g.Zones[0].Job);
            Assert.IsTrue(a.Moving);
            for (int i = 0; i < 20; i++) g.Update(0.05);
            Assert.IsFalse(a.Moving);
        }

        [Test]
        public void PathGaps_WholeRingWithoutCover_NoneWithEternal()
        {
            Game g = TestUtil.NewGame();
            Assert.AreEqual(0, g.PathGaps().Count); // 유닛이 없으면 빈 목록
            TestUtil.Place(g, Job.Archer, GradeKey.Common);
            int gaps = g.PathGaps().Count;
            Assert.Greater(gaps, 0); // 위 구역 하나로는 한 바퀴를 못 덮는다
            Assert.Less(gaps, BoardSnapshot.GapSamples);
            TestUtil.Place(g, Job.Wizard, GradeKey.Eternal); // 태초 = 맵 전체
            Assert.AreEqual(0, g.PathGaps().Count);
        }

        [Test]
        public void Waves_FirstWaveSpawns40_BossEvery10()
        {
            Game g = TestUtil.NewGame();
            for (int i = 0; i < 40; i++) g.Update(0.05); // 휴식 1초 뒤 웨이브 1
            Assert.AreEqual(1, g.Wave);
            Assert.AreEqual(WaveState.Spawning, g.WaveState);
            Assert.AreEqual(40, g.SpawnGoal);
            g.Wave = 9;
            g.StartNextWave();
            Assert.IsTrue(g.IsBossWave);
            Assert.AreEqual(1, g.SpawnGoal);
        }

        [Test]
        public void MobKinds_FastAndTankByIndex()
        {
            Game g = TestUtil.NewGame();
            g.Wave = 11;
            g.StartNextWave(); // 12웨이브: fast(5번째마다), tank(9번째마다)
            for (int i = 0; i < 400 && g.SpawnedCount < 10; i++) g.Update(0.05);
            Assert.AreEqual(MobKind.Fast, g.Mobs[4].Kind);
            Assert.AreEqual(MobKind.Tank, g.Mobs[8].Kind);
            Assert.AreEqual(MobKind.Normal, g.Mobs[0].Kind);
        }

        [Test]
        public void GameOver_At100Mobs_ThenReviveClearsField()
        {
            Game g = TestUtil.NewGame();
            TestUtil.HoldWaves(g);
            GameOverData over = null;
            g.GameEnded += d => over = d;
            for (int i = 0; i < 100; i++) g.DebugSpawn(70, 70, 1e9);
            g.Update(0.01);
            Assert.IsTrue(g.Over);
            Assert.IsNotNull(over);
            Assert.IsFalse(over.Cleared);
            Assert.IsTrue(g.CanRevive());
            Assert.IsTrue(g.Revive());
            Assert.AreEqual(0, g.Mobs.Count);
            Assert.IsFalse(g.CanRevive()); // 판당 1회
        }

        [Test]
        public void StageWin_AfterLastBoss()
        {
            StageDef st = Campaign.Get("1-1");
            GameOptions o = GameOptions.ForStage(st);
            o.Seed = 5;
            o.RandomSeed = 5;
            o.Track = false;
            var g = new Game(o);
            g.Wave = st.Waves - 1;
            g.StartNextWave();
            Assert.IsTrue(g.IsBossWave); // 마지막 웨이브는 보스
            g.Update(0.05); // 보스 등장
            Assert.AreEqual(1, g.Mobs.Count);
            g.Mobs[0].Hp = 0;
            g.KillMob(g.Mobs[0]);
            for (int i = 0; i < 5 && !g.Over; i++) g.Update(0.05);
            Assert.IsTrue(g.Over);
            Assert.IsTrue(g.Cleared);
        }

        [Test]
        public void SameSeeds_SameResult()
        {
            string Run()
            {
                var o = new GameOptions { Seed = 99, RandomSeed = 123, Track = false };
                var g = new Game(o);
                for (int i = 0; i < 3000 && !g.Over; i++)
                {
                    g.Update(0.05);
                    if (i % 4 == 0 && g.Gold >= g.SummonCost) g.Summon();
                    foreach (var m in g.Mergeable()) { g.Merge(m.Key, m.Value); break; }
                }
                return g.Wave + "/" + g.Kills + "/" + g.Gold + "/" + g.Units.Count;
            }
            Assert.AreEqual(Run(), Run());
        }

        [Test]
        public void Track_ReportsProgressAndAchievements()
        {
            var g = new Game(new GameOptions { Seed = 3, RandomSeed = 3, Track = true });
            int summons = 0;
            string ach = null;
            g.Progress += (stat, n) => { if (stat == QuestStat.Summons) summons += n; };
            g.AchievementReached += k => ach = k;
            TestUtil.Place(g, Job.Archer, GradeKey.Legendary);
            Assert.AreEqual(1, summons);
            Assert.AreEqual("firstLegendary", ach);
        }
    }
}
