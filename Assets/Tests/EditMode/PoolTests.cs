using System;
using NUnit.Framework;

namespace DefenseBattle.Core.Tests
{
    /// <summary>오브젝트 풀링: 재사용이 되는지, 재사용된 객체가 이전 상태를 끌고 오지 않는지</summary>
    public class PoolTests
    {
        [Test]
        public void ObjectPool_ReusesAndCaps()
        {
            int created = 0;
            var pool = new ObjectPool<object>(() => { created++; return new object(); }, prewarm: 2, maxSize: 3);
            Assert.AreEqual(2, created);
            object a = pool.Get(), b = pool.Get(), c = pool.Get();
            Assert.AreEqual(3, created); // 미리 만든 2개 다 쓰고 1개 새로
            pool.Release(a); pool.Release(b); pool.Release(c); pool.Release(new object());
            Assert.AreEqual(3, pool.FreeCount); // 상한 3
            Assert.AreSame(c, pool.Get());       // 스택: 마지막에 돌려준 것부터
        }

        [Test]
        public void ReusedMob_StartsClean()
        {
            Game g = TestUtil.NewGame();
            TestUtil.HoldWaves(g);
            Mob m = g.DebugSpawn(100, 100, 50, (int)AttackKey.Wraith);
            int oldId = m.Id;
            m.MarkUntil = 99;
            m.Poison.Stacks = 3;
            m.Poison.Until = 99;
            m.Slow = 5;
            m.Gold = 7;
            m.Dead = true;
            g.Update(0.01); // 무덤으로
            g.Update(0.01); // 다음 프레임 시작에 풀로
            Mob n = g.DebugSpawn(200, 200, 80);
            Assert.AreSame(m, n); // 같은 객체 재사용
            Assert.AreEqual(-1, n.Card);
            Assert.AreEqual(0, n.MarkUntil);
            Assert.AreEqual(0, n.Poison.Stacks);
            Assert.AreEqual(0, n.Poison.Until);
            Assert.AreEqual(0, n.Slow);
            Assert.IsNull(n.Gold);
            Assert.IsFalse(n.Dead);
            Assert.AreEqual(80, n.MaxHp);
            Assert.AreNotEqual(oldId, n.Id); // 새 Id
        }

        [Test]
        public void Effects_AreRecycled_NoGrowthOverLongGame()
        {
            var o = new GameOptions { Seed = 11, RandomSeed = 22, Track = false };
            var g = new Game(o);
            // 판을 오래 돌려도 엔진이 쥔 이펙트 수는 상한 근처에서 멈춘다 (풀이 계속 돌아감)
            for (int i = 0; i < 20000 && !g.Over; i++)
            {
                g.Update(0.05);
                if (i % 4 == 0 && g.Gold >= g.SummonCost) g.Summon();
                foreach (var mg in g.Mergeable()) { g.Merge(mg.Key, mg.Value); break; }
                Assert.LessOrEqual(g.Effects.Count, 400);
            }
        }

        [Test]
        public void Update_SteadyStateAllocatesAlmostNothing()
        {
#if UNITY_EDITOR
            // GC.GetAllocatedBytesForCurrentThread 는 Unity(Mono)에서 정확하지 않다 - 이 측정은 CoreTests(.NET)에서 한다.
            // Unity 에서는 Profiler 의 GC Alloc 열로 확인.
            Assert.Ignore("CoreTests (.NET) 에서 측정");
#endif
            var o = new GameOptions { Seed = 5, RandomSeed = 6, Track = false };
            o.Research[ResearchKey.Atk] = 20;
            var g = new Game(o);
            // 데워 두기 (풀·버퍼가 필요한 크기까지 자라게)
            for (int i = 0; i < 4000 && !g.Over; i++)
            {
                g.Update(0.05);
                if (i % 4 == 0 && g.Gold >= g.SummonCost) g.Summon();
            }
            if (g.Over) Assert.Inconclusive("판이 너무 일찍 끝남");
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 2000 && !g.Over; i++) g.Update(0.05);
            long perFrame = (GC.GetAllocatedBytesForCurrentThread() - before) / 2000;
            // 웨이브 시작·로그 문자열 정도만 남는다 (프레임당 평균 64바이트 미만)
            Assert.Less(perFrame, 64, "frame alloc=" + perFrame);
        }
    }
}
