using System;

namespace DefenseBattle.Core
{
    /// <summary>경제 규칙 실행 환경: 지금 시각(ms, UTC)과 난수</summary>
    public sealed class EconContext
    {
        public long Now;
        public IRandom Rng;

        public EconContext(long now, IRandom rng)
        {
            Now = now;
            Rng = rng;
        }

        /// <summary>지금 시각으로 (기기 시계 - 온라인이면 서버가 자기 시계로 다시 계산한다)</summary>
        public static EconContext Current() => new EconContext(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), new SystemRandom());

        /// <summary>UTC 날짜 "yyyy-MM-dd" (rules.ts todayOf)</summary>
        public string Today => DateTimeOffset.FromUnixTimeMilliseconds(Now).UtcDateTime.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>시즌 id "S2026-10" 형식 (rules.ts seasonOf)</summary>
        public string SeasonId => "S" + DateTimeOffset.FromUnixTimeMilliseconds(Now).UtcDateTime.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
    }
}
