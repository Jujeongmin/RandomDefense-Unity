namespace DefenseBattle.Core
{
    /// <summary>
    /// 시드 RNG (config.ts makeRng, mulberry32). PvP 두 사람이 같은 시드로 같은 웨이브 구성을 본다.
    /// 원본과 비트 단위로 같은 값을 내야 한다 (uint 연산 = JS 의 &gt;&gt;&gt; 0 / Math.imul 결과).
    /// </summary>
    public sealed class Mulberry32 : IRandom
    {
        uint m_state;

        public Mulberry32(uint seed) { m_state = seed; }

        public double Next()
        {
            unchecked
            {
                m_state += 0x6d2b79f5;
                uint t = m_state;
                t = (t ^ (t >> 15)) * (t | 1);
                t ^= t + (t ^ (t >> 7)) * (t | 61);
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }
    }
}
