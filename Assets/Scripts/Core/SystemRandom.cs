using System;

namespace DefenseBattle.Core
{
    /// <summary>System.Random 감싸기 (원본 Math.random 자리). 시드를 주면 테스트에서 결과가 고정된다.</summary>
    public sealed class SystemRandom : IRandom
    {
        readonly Random m_random;

        public SystemRandom() { m_random = new Random(); }
        public SystemRandom(int seed) { m_random = new Random(seed); }

        public double Next() => m_random.NextDouble();

        /// <summary>0 이상 max 미만 정수 (원본 Math.floor(Math.random() * max))</summary>
        public static int Index(IRandom r, int max) => (int)Math.Floor(r.Next() * max);
    }
}
