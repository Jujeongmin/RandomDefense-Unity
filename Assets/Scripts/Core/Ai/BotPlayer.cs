using System;
using System.Collections.Generic;

namespace DefenseBattle.Core
{
    /// <summary>
    /// AI 플레이어 (ai.ts) - PvP 연습 상대(봇)의 자동 플레이 정책. 게임 하나에 하나씩 만든다.
    /// skill(0~1): 높을수록 판단이 빠르고 합성/강화/몹 보내기를 잘 활용.
    /// </summary>
    public sealed class BotPlayer
    {
        readonly Game m_game;
        readonly IRandom m_rand;
        public readonly double Skill;

        /// <summary>던지기 계획: 카드를 정하면 골드를 모아 던진다</summary>
        bool m_hasPlan;
        int m_planCard;
        double m_planPos;
        /// <summary>다음 던지기 시각 (게임 시간, 처음엔 아직 안 정함)</summary>
        double? m_nextThrow;
        double m_nextZoneCheck;

        /// <summary>구역 정리 평가용 길 표본 48점 (고정)</summary>
        static readonly Vec2[] s_samples = BuildSamples();
        static Vec2[] BuildSamples()
        {
            var pts = new Vec2[48];
            for (int i = 0; i < pts.Length; i++) pts[i] = Board.PathAt(i / 48.0);
            return pts;
        }

        // 재사용 버퍼
        readonly double[] m_weight = new double[48];
        readonly int[] m_place = new int[4];
        readonly HashSet<Job> m_oppJobs = new HashSet<Job>();

        public BotPlayer(Game game, double skill, IRandom rand = null)
        {
            m_game = game;
            Skill = skill;
            m_rand = rand ?? new SystemRandom();
        }

        /// <summary>한 번 판단하는 간격(실시간 초) - 사람 손 속도 (실력 0.3 ≈ 0.6초, 0.75 ≈ 0.44초)</summary>
        public static double ThinkInterval(double skill) => 0.7 - skill * 0.35;

        /// <summary>던지기 간격(실시간 초) - 실력이 높을수록 자주 (0.2 ≈ 38초, 0.5 ≈ 29초, 0.75 ≈ 21초)</summary>
        public static double ThrowInterval(double skill) => 44 - skill * 30;

        /// <summary>상대 스냅샷에서 구역(0=상 1=우 2=하 3=좌)별 전력 (등급이 높을수록 크게)</summary>
        static void ZonePower(BoardSnapshot opp, double[] power)
        {
            Array.Clear(power, 0, power.Length);
            double c = Board.MapSize / 2;
            for (int i = 0; i + 3 < opp.Units.Count; i += 4)
            {
                double dx = BoardSnapshot.Dequantize(opp.Units[i + 2]) - c, dy = BoardSnapshot.Dequantize(opp.Units[i + 3]) - c;
                int zone = Math.Abs(dy) > Math.Abs(dx) ? (dy < 0 ? 0 : 2) : (dx > 0 ? 1 : 3);
                power[zone] += Math.Pow(3, opp.Units[i + 1]);
            }
        }

        readonly double[] m_power = new double[4];

        /// <summary>던질 카드와 위치 고르기: 상대 덱에 카운터 유닛이 없는 카드 우선, 가장 약한 구역 앞 길 시작점. 없으면 false</summary>
        public bool PickThrow(BoardSnapshot opp, bool ignoreGold, out int card, out double pos)
        {
            card = -1;
            pos = 0;
            m_oppJobs.Clear();
            if (opp != null)
            {
                for (int i = 0; i + 3 < opp.Units.Count; i += 4)
                {
                    int j = opp.Units[i];
                    if (j >= 0 && j < Units.All.Length) m_oppJobs.Add((Job)j);
                }
            }
            double bestScore = double.NegativeInfinity;
            foreach (int c in m_game.AttackCards)
            {
                if (!(ignoreGold || m_game.Gold >= m_game.AttackCost(c)) || m_game.AttackCooldown(c) > 0) continue;
                AttackDef d = Attacks.Get(c);
                int open = 0;
                foreach (Job j in d.Counters) if (!m_oppJobs.Contains(j)) open++;
                int resisted = 0;
                foreach (Job j in d.Resist) if (m_oppJobs.Contains(j)) resisted++;
                double score = open / (double)d.Counters.Length * 2 + resisted * 0.3 + d.Cost / 200.0 + m_rand.Next() * (1.2 - Skill);
                if (score > bestScore) { bestScore = score; card = c; }
            }
            if (card < 0) return false;
            pos = m_rand.Next();
            if (opp != null && m_rand.Next() < 0.3 + Skill * 0.6)
            {
                ZonePower(opp, m_power);
                int seg = 0;
                for (int s = 1; s < 4; s++) if (m_power[Board.SegZone[s]] < m_power[Board.SegZone[seg]]) seg = s;
                pos = (seg + 0.05) / 4;
            }
            return true;
        }

        /// <summary>
        /// 구역 정리 - 사거리가 짧아 배치가 중요하다. 가끔(사람처럼) 길 전체를 가장 고르게 덮는 구역 교환 하나를 한다.
        /// 상대가 던진 몹이 떨어지는 자리(경고 고리)는 더 중요하게 본다. 교환했으면 true
        /// </summary>
        bool TidyZones()
        {
            Game game = m_game;
            if (game.Time < m_nextZoneCheck || game.Units.Count < 6) return false;
            m_nextZoneCheck = game.Time + (16 - Skill * 10) * Waves.PlaySpeed;
            if (m_rand.Next() > 0.25 + Skill * 0.65) return false;
            for (int i = 0; i < s_samples.Length; i++)
            {
                m_weight[i] = 1;
                foreach (Effect e in game.Effects)
                {
                    if (e.Type == EffectType.Drop && JsMath.DistSq(e.X, e.Y, s_samples[i].X, s_samples[i].Y) < 160 * 160) { m_weight[i] = 3; break; }
                }
            }
            for (int i = 0; i < 4; i++) m_place[i] = i;
            double baseScore = PlacementScore(m_place);
            int bestA = -1, bestB = -1;
            double bestS = baseScore * 1.06; // 6% 이상 나아질 때만
            for (int a = 0; a < 4; a++)
            {
                for (int b = a + 1; b < 4; b++)
                {
                    for (int i = 0; i < 4; i++) m_place[i] = i;
                    m_place[a] = b;
                    m_place[b] = a;
                    double s = PlacementScore(m_place);
                    if (s > bestS) { bestS = s; bestA = a; bestB = b; }
                }
            }
            if (bestA < 0) return false;
            game.SwapZones(bestA, bestB);
            return true;
        }

        /// <summary>place: 구역 번호 → 실제로 서게 될 구역 번호. 빈 곳(0)이 크게 깎인다</summary>
        double PlacementScore(int[] place)
        {
            Game game = m_game;
            double s = 0;
            for (int i = 0; i < s_samples.Length; i++)
            {
                Vec2 p = s_samples[i];
                double power = 0;
                foreach (Unit u in game.Units)
                {
                    Zone z = game.Zones[place[u.ZoneId]];
                    if (JsMath.Within(z.Cx, z.Cy, p.X, p.Y, game.UnitRange(u))) power += Math.Pow(3, u.GradeIndex);
                }
                s += m_weight[i] * Math.Log(1 + power);
            }
            return s;
        }

        /// <summary>한 번 판단하고 행동한다 (aiStep). opp = 상대 최근 스냅샷 (없으면 null)</summary>
        public void Step(BoardSnapshot opp = null)
        {
            Game game = m_game;
            if (game.Over) return;
            // 실수: 낮은 실력일수록 행동을 자주 건너뜀
            if (m_rand.Next() > 0.35 + Skill * 0.6) return;

            // 0) 구역 정리 (가끔)
            if (TidyZones()) return;
            // 1) 합성 (낮은 등급부터)
            if (m_rand.Next() < 0.3 + Skill * 0.6)
            {
                List<KeyValuePair<Job, GradeKey>> list = game.Mergeable();
                int pick = -1;
                for (int i = 0; i < list.Count; i++) if (pick < 0 || list[i].Value < list[pick].Value) pick = i;
                if (pick >= 0 && game.Merge(list[pick].Key, list[pick].Value) != null) return;
            }
            // 2) 몹 던지기: 때가 되면 카드를 정하고 골드를 모은다. 내 필드가 위험하면 계획을 접고 방어에 쓴다
            if (game.IsPvp && game.Units.Count >= 10)
            {
                if (m_nextThrow == null) m_nextThrow = game.Time + (8 + m_rand.Next() * 10) * Waves.PlaySpeed;
                if (!m_hasPlan && game.Time >= m_nextThrow.Value && game.Mobs.Count < 40)
                {
                    m_hasPlan = PickThrow(opp, true, out m_planCard, out m_planPos);
                }
                if (m_hasPlan)
                {
                    if (game.Mobs.Count > 50) m_hasPlan = false;
                    else if (game.Gold >= game.AttackCost(m_planCard))
                    {
                        if (game.SendAttack(m_planCard, m_planPos))
                        {
                            m_hasPlan = false;
                            m_nextThrow = game.Time + ThrowInterval(Skill) * (0.7 + m_rand.Next() * 0.6) * Waves.PlaySpeed;
                        }
                        return;
                    }
                    else return; // 골드 모으는 중 (합성만 한다)
                }
            }
            // 3) 군대 확보 → 강화
            int target = 16 + JsMath.RoundInt(Skill * 10);
            if (game.Units.Count < target)
            {
                if (game.Gold >= game.SummonCost) game.Summon();
                return;
            }
            List<JobStack> jobs = game.GetJobStacks();
            // 유닛 수가 많은 직업부터 (같으면 직업 id 순 - 안정 정렬)
            for (int i = 1; i < jobs.Count; i++)
            {
                JobStack x = jobs[i];
                int k = i - 1;
                while (k >= 0 && jobs[k].Count < x.Count) { jobs[k + 1] = jobs[k]; k--; }
                jobs[k + 1] = x;
            }
            foreach (JobStack s in jobs)
            {
                if (game.Gold >= game.UpgradeCost(s.Job)) { game.Upgrade(s.Job); return; }
            }
            if (game.Gold >= game.SummonCost) game.Summon();
        }
    }
}
