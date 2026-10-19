using System;

namespace DefenseBattle.Core
{
    /// <summary>
    /// 맵 기하 (engine.ts MAP, PATH, pathAt, pathPosNear, SEG_ZONE, makeZones).
    /// 몹은 사각 링 길을 좌상(시작) → 좌하 → 우하 → 우상 순서(반시계)로 끝없이 돈다.
    /// </summary>
    public static class Board
    {
        public const double MapSize = 720;
        /// <summary>경로 바깥 여백</summary>
        const double P = 70;
        public const double PathWidth = 44;
        /// <summary>유닛 배치 사각형 여백</summary>
        const double Inner = 118;

        public static readonly Vec2[] Corners =
        {
            new Vec2(P, P),                      // 좌상 (스폰)
            new Vec2(P, MapSize - P),            // 좌하
            new Vec2(MapSize - P, MapSize - P),  // 우하
            new Vec2(MapSize - P, P),            // 우상
        };

        /// <summary>길의 각 변(seg) 앞에 있는 유닛 구역: 0=좌변→좌(3), 1=하변→하(2), 2=우변→우(1), 3=상변→상(0)</summary>
        public static readonly int[] SegZone = { 3, 2, 1, 0 };

        /// <summary>길 둘레 비율(0~1) → 좌표와 다음 목표 모서리. 0=출발점(좌상), 반시계 순서로 한 바퀴</summary>
        public static Vec2 PathAt(double pos, out int next)
        {
            int n = Corners.Length;
            double d = ((pos % 1) + 1) % 1 * n;
            int seg = (int)Math.Floor(d) % n;
            double t = d - Math.Floor(d);
            Vec2 a = Corners[seg], b = Corners[(seg + 1) % n];
            next = (seg + 1) % n;
            return new Vec2(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
        }

        public static Vec2 PathAt(double pos) => PathAt(pos, out _);

        /// <summary>전장 좌표에서 가장 가까운 길 위치(둘레 비율) - 몹을 던진 지점을 길에 붙인다</summary>
        public static double PathPosNear(double x, double y)
        {
            int n = Corners.Length;
            double best = 0, bestD = double.PositiveInfinity;
            for (int i = 0; i < n; i++)
            {
                Vec2 a = Corners[i], b = Corners[(i + 1) % n];
                double vx = b.X - a.X, vy = b.Y - a.Y;
                double t = Math.Max(0, Math.Min(1, ((x - a.X) * vx + (y - a.Y) * vy) / (vx * vx + vy * vy)));
                double dist = JsMath.Hypot(a.X + vx * t - x, a.Y + vy * t - y);
                if (dist < bestD) { bestD = dist; best = (i + t) / n; }
            }
            return best;
        }

        /// <summary>경로 seg 번째 변에서 t(0~1) 위치의 좌표</summary>
        public static Vec2 PathPoint(int seg, double t)
        {
            int n = Corners.Length;
            Vec2 a = Corners[seg % n], b = Corners[(seg + 1) % n];
            return new Vec2(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
        }

        /// <summary>유닛 구역 4개 (상/우/하/좌 삼각형)</summary>
        public static Zone[] MakeZones()
        {
            double a = Inner, b = MapSize - Inner, o = MapSize / 2;
            Vec2 A = new Vec2(a, a), B = new Vec2(b, a), C = new Vec2(b, b), D = new Vec2(a, b);
            Vec2 O = new Vec2(o, o);
            Vec2[][] tris = { new[] { A, B, O }, new[] { B, C, O }, new[] { C, D, O }, new[] { D, A, O } };
            var zones = new Zone[tris.Length];
            for (int i = 0; i < tris.Length; i++)
            {
                Vec2[] t = tris[i];
                zones[i] = new Zone
                {
                    Id = i,
                    Cx = (t[0].X + t[1].X + t[2].X) / 3,
                    Cy = (t[0].Y + t[1].Y + t[2].Y) / 3,
                    Job = null,
                    Tri = t,
                };
            }
            return zones;
        }
    }
}
