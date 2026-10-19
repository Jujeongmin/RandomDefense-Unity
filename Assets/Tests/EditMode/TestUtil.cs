using System.Collections.Generic;

namespace DefenseBattle.Core.Tests
{
    /// <summary>테스트용 게임 만들기 - 시드를 고정해 결과가 매번 같게</summary>
    static class TestUtil
    {
        public static Game NewGame(GameMode mode = GameMode.Solo, params Job[] deck)
        {
            var o = new GameOptions
            {
                Mode = mode, Seed = 777, RandomSeed = 4242, Track = false,
                Deck = deck.Length > 0 ? new List<Job>(deck) : new List<Job> { Job.Archer, Job.Wizard, Job.Warrior },
            };
            return new Game(o);
        }

        /// <summary>정해진 유닛을 소환 (골드를 채워서)</summary>
        public static Unit Place(Game g, Job job, GradeKey grade)
        {
            g.Gold += g.SummonCost;
            g.ScriptSummons(new[] { new KeyValuePair<Job, GradeKey>(job, grade) });
            return g.Summon();
        }

        /// <summary>웨이브가 시작되지 않게 멈춰 둔다 (전투 규칙만 시험)</summary>
        public static void HoldWaves(Game g) => g.RestTimer = 1e9;
    }
}
