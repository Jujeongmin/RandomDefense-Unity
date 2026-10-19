using System.Collections.Generic;

namespace DefenseBattle.Core
{
    /// <summary>소환 확률 도우미 (config.ts rollGrade, rollJob)</summary>
    public static class Summon
    {
        /// <summary>등급별 소환 확률(%) - 희귀 소환 연구 보너스가 전설 이상으로 간다</summary>
        public static double[] Rates(double rareBonus = 0)
        {
            var t = new double[Grades.Count];
            for (int i = 0; i < t.Length; i++) t[i] = Grades.All[i].SummonRate;
            if (rareBonus > 0)
            {
                t[3] += rareBonus * 0.7;
                t[4] += rareBonus * 0.2;
                t[5] += rareBonus * 0.1;
            }
            return t;
        }

        public static GradeKey RollGrade(IRandom rand, double rareBonus = 0)
        {
            double[] table = Rates(rareBonus);
            double total = 0;
            foreach (double r in table) total += r;
            double roll = rand.Next() * total;
            for (int i = 0; i < table.Length; i++)
            {
                if (roll < table[i]) return (GradeKey)i;
                roll -= table[i];
            }
            return GradeKey.Common;
        }

        public static Job RollJob(IRandom rand, IReadOnlyList<Job> deck)
        {
            if (deck == null || deck.Count == 0) deck = Units.Starter;
            return deck[SystemRandom.Index(rand, deck.Count)];
        }
    }
}
