using NUnit.Framework;

namespace DefenseBattle.Core.Tests
{
    /// <summary>전투 규칙: 사거리, 피해 배율, 대표 스킬, 몹 카드 능력</summary>
    public class CombatTests
    {
        static Game Setup(Job job, GradeKey grade, out Unit u)
        {
            Game g = TestUtil.NewGame(GameMode.Solo, job);
            TestUtil.HoldWaves(g);
            u = TestUtil.Place(g, job, grade);
            u.X = 360;
            u.Y = 200;
            return g;
        }

        [Test]
        public void UnitRange_ByGradeAndJob()
        {
            Game g = TestUtil.NewGame();
            Assert.AreEqual(220, g.UnitRange(0, Job.Archer));
            Assert.AreEqual(220 * 1.3, g.UnitRange(0, Job.Ranger), 1e-9);
            Assert.AreEqual(320, g.UnitRange(4, Job.Archer));
            Assert.IsTrue(double.IsPositiveInfinity(g.UnitRange(5, Job.Archer)));
        }

        [Test]
        public void Attack_HitsNearestInRange()
        {
            Game g = Setup(Job.Warrior, GradeKey.Common, out Unit u);
            Mob far = g.DebugSpawn(360, 600, 1e6);   // 400px - 사거리 밖
            Mob near = g.DebugSpawn(360, 300, 1e6);  // 100px
            g.Update(0.01);
            Assert.AreEqual(1e6, far.Hp);
            Assert.Less(near.Hp, 1e6);
            Assert.Greater(near.Slow, 0); // 전사 둔화
            Assert.AreEqual(0, u.Face);   // 아래쪽 몹을 바라봄
        }

        [Test]
        public void Damage_ResistAndMark()
        {
            Game g = Setup(Job.Archer, GradeKey.Common, out Unit u);
            Mob plain = g.DebugSpawn(360, 300, 1e6);
            Mob ghost = g.DebugSpawn(360, 300, 1e6, (int)AttackKey.Ghost);
            double p = g.UnitDamage(u, plain);
            Assert.AreEqual(p * Attacks.ResistMult, g.UnitDamage(u, ghost), 1e-9); // 유령: 궁수 피해 -60%
            plain.MarkUntil = g.Time + 5;
            Assert.AreEqual(p * Skills.Hunter.MarkMult, g.UnitDamage(u, plain), 1e-9);
        }

        [Test]
        public void Wizard_SplashHitsNeighbours()
        {
            Game g = Setup(Job.Wizard, GradeKey.Common, out _);
            Mob target = g.DebugSpawn(360, 300, 1e6);
            Mob side = g.DebugSpawn(400, 300, 1e6);  // 40px - 폭발 반경 62 안
            Mob away = g.DebugSpawn(360, 400, 1e6);  // 대상에서 100px - 밖
            g.Update(0.01);
            Assert.Less(target.Hp, 1e6);
            Assert.Less(side.Hp, 1e6);
            Assert.AreEqual(1e6, away.Hp);
        }

        [Test]
        public void Assassin_ExecutesLowHp()
        {
            Game g = Setup(Job.Assassin, GradeKey.Common, out _);
            Mob m = g.DebugSpawn(360, 300, 1e6);
            m.Hp = m.MaxHp * 0.1; // 12% 이하
            g.Update(0.01);
            Assert.AreEqual(0, g.Mobs.Count);
            Assert.AreEqual(1, g.Kills);
        }

        [Test]
        public void Kill_GivesGold()
        {
            Game g = Setup(Job.Warrior, GradeKey.Eternal, out _);
            g.DebugSpawn(360, 300, 1);
            int gold = g.Gold;
            g.Update(0.01);
            Assert.AreEqual(gold + Economy.KillGold, g.Gold);
        }

        [Test]
        public void Wraith_RevivesOnce_ReaperBlocks()
        {
            Game g = Setup(Job.Warrior, GradeKey.Eternal, out _);
            Mob w = g.DebugSpawn(360, 300, 1, (int)AttackKey.Wraith);
            g.Update(0.01);
            Assert.IsFalse(w.Dead); // 한 번 되살아남
            Assert.AreEqual(Economy.KillGold, w.Gold);

            Game g2 = Setup(Job.Reaper, GradeKey.Eternal, out _);
            Mob w2 = g2.DebugSpawn(360, 300, 1, (int)AttackKey.Wraith);
            g2.Update(0.01);
            Assert.IsTrue(w2.Dead); // 사신에게 처치되면 부활 못 함
        }

        [Test]
        public void Egg_SplitsIntoThree()
        {
            Game g = Setup(Job.Warrior, GradeKey.Eternal, out _);
            g.DebugSpawn(360, 300, 1, (int)AttackKey.Egg);
            g.Update(0.01);
            Assert.AreEqual(3, g.Mobs.Count);
            foreach (Mob m in g.Mobs) Assert.AreEqual(1, m.Gold); // 분열된 몹은 다시 안 갈라짐
        }

        [Test]
        public void Thief_HiddenCannotBeTargeted()
        {
            Game g = Setup(Job.Warrior, GradeKey.Common, out _);
            Mob t = g.DebugSpawn(360, 300, 1e6, (int)AttackKey.Thief);
            t.Age = 3.0; // 4초 주기 중 2.2초 뒤부터 1.8초 숨음
            g.Update(0.01);
            Assert.IsTrue(t.Hidden);
            Assert.AreEqual(1e6, t.Hp);
        }

        [Test]
        public void Shaman_ReducesDamageToOthers()
        {
            Game g = Setup(Job.Warrior, GradeKey.Common, out Unit u);
            u.BaseAtk = 100;
            Mob m = g.DebugSpawn(360, 300, 1e6);
            g.DebugSpawn(360, 350, 1e9, (int)AttackKey.Shaman);
            double expect = g.UnitDamage(u, m);
            g.Update(0.01);
            // 치명타가 아니면 1 - 0.35 배
            double dealt = 1e6 - m.Hp;
            Assert.IsTrue(System.Math.Abs(dealt - expect * 0.65) < 1e-6 || System.Math.Abs(dealt - expect * 0.65 * Skills.CritMult) < 1e-6, "dealt=" + dealt);
        }

        [Test]
        public void Poison_StacksUpToThree()
        {
            Game g = Setup(Job.Poisoner, GradeKey.Common, out Unit u);
            Mob m = g.DebugSpawn(360, 300, 1e9);
            for (int i = 0; i < 5; i++)
            {
                u.Cooldown = 0;
                g.Update(0.01);
            }
            Assert.AreEqual(Skills.Poisoner.MaxStack, m.Poison.Stacks);
            Assert.Greater(m.Poison.Until, g.Time);
        }

        [Test]
        public void Angel_AuraRaisesDamage()
        {
            Game g = Setup(Job.Archer, GradeKey.Common, out Unit u);
            Mob m = g.DebugSpawn(360, 300, 1e9);
            double before = g.UnitDamage(u, m);
            TestUtil.Place(g, Job.Angel, GradeKey.Common);
            g.Update(0.01); // 오라는 전투 단계에서 다시 계산
            Assert.AreEqual(before * (1 + Skills.Angel.Aura), g.UnitDamage(u, m), 1e-6);
        }
    }
}
