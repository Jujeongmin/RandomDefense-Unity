using System;

namespace DefenseBattle.Core
{
    /// <summary>
    /// 원본(JavaScript)과 같은 결과를 내는 수학 도우미.
    /// C# Math.Round 는 기본이 은행가 반올림이라 JS Math.round(0.5 → 1, -0.5 → 0)와 다르다.
    /// </summary>
    public static class JsMath
    {
        /// <summary>JS Math.round: 가장 가까운 정수, .5 는 +무한대 쪽</summary>
        public static double Round(double v) => Math.Floor(v + 0.5);

        /// <summary>JS Math.round 결과를 int 로</summary>
        public static int RoundInt(double v) => (int)Math.Floor(v + 0.5);

        /// <summary>JS Math.hypot (두 값)</summary>
        public static double Hypot(double x, double y) => Math.Sqrt(x * x + y * y);

        /// <summary>두 점 거리의 제곱 (거리 비교만 할 때 sqrt 를 피한다)</summary>
        public static double DistSq(double x1, double y1, double x2, double y2)
        {
            double dx = x1 - x2, dy = y1 - y2;
            return dx * dx + dy * dy;
        }

        /// <summary>두 점이 r 안에 있는지 (sqrt 없이). r 이 무한대면 항상 true</summary>
        public static bool Within(double x1, double y1, double x2, double y2, double r)
            => DistSq(x1, y1, x2, y2) <= r * r;

        /// <summary>JS Number.isInteger</summary>
        public static bool IsInteger(double v) => !double.IsInfinity(v) && Math.Floor(v) == v;
    }
}
