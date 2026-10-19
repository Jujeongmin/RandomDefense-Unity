using System;
using System.Collections.Generic;

namespace DefenseBattle.Core
{
    // 유닛: 소환·합성·강화·판매·구역 (engine.ts summon ~ updateUnitMovement, getStacks, getJobStacks)
    public sealed partial class Game
    {
        /// <summary>다음 소환들의 결과를 정해 둔다 (튜토리얼: 같은 유닛 3장으로 합성을 바로 배우게)</summary>
        readonly Queue<KeyValuePair<Job, GradeKey>> m_scripted = new Queue<KeyValuePair<Job, GradeKey>>();

        public void ScriptSummons(IEnumerable<KeyValuePair<Job, GradeKey>> list)
        {
            foreach (KeyValuePair<Job, GradeKey> s in list) m_scripted.Enqueue(s);
        }

        /// <summary>소환 비용: 덱 빈 칸 1개당 Economy.DeckGapPenalty 배씩 비싸진다 (넣을 수 있는데 안 넣은 칸만)</summary>
        public int SummonCost
        {
            get
            {
                // 덱의 서로 다른 유닛 수 (Job 이 32종이라 비트마스크로 셈 - LINQ Distinct 할당 없음)
                uint seen = 0;
                int distinct = 0;
                foreach (Job j in Opts.Deck)
                {
                    uint bit = 1u << (int)j;
                    if ((seen & bit) == 0) { seen |= bit; distinct++; }
                }
                int gap = Math.Max(0, Opts.DeckMin - distinct);
                return JsMath.RoundInt(Economy.SummonCost * (1 + gap * Economy.DeckGapPenalty));
            }
        }

        public Unit Summon()
        {
            int cost = SummonCost;
            if (Gold < cost || Over) return null;
            Gold -= cost;
            SummonCount += 1;
            ReportProgress(QuestStat.Summons);

            if (m_scripted.Count > 0)
            {
                KeyValuePair<Job, GradeKey> forced = m_scripted.Dequeue();
                return CreateUnit(forced.Key, forced.Value, false);
            }
            double rareBonus = Res(ResearchKey.Rare) * Research.Per(ResearchKey.Rare);
            GradeKey gradeKey = global::DefenseBattle.Core.Summon.RollGrade(m_rand, rareBonus);
            Job job = global::DefenseBattle.Core.Summon.RollJob(m_rand, Opts.Deck);
            return CreateUnit(job, gradeKey, false);
        }

        // ---------------- 합성 ----------------
        // 같은 직업·등급 3개 → 같은 직업 상위 등급 1개 (태초는 합성 불가)
        public bool CanMerge(Job job, GradeKey gradeKey)
            => (int)gradeKey < Grades.Count - 1 && CountOf(job, gradeKey) >= Grades.MergeCount;

        public Unit Merge(Job job, GradeKey gradeKey)
        {
            if (Over || !CanMerge(job, gradeKey)) return null;
            int removed = 0;
            int zoneId = 0;
            for (int i = Units.Count - 1; i >= 0 && removed < Grades.MergeCount; i--)
            {
                Unit u = Units[i];
                if (u.Job == job && u.Grade == gradeKey)
                {
                    zoneId = u.ZoneId;
                    Units.RemoveAt(i);
                    removed += 1;
                }
            }
            Grade next = Grades.All[(int)gradeKey + 1];
            // 연금술사: 확률로 한 등급 더 (여럿이면 합산)
            double alch = 0;
            foreach (Unit a in Units)
            {
                if (a.Job == Job.Alchemist && a.GradeIndex >= Skills.FromIndex) alch += Skills.Val(a.GradeIndex, Skills.Alchemist.Chance, Skills.Alchemist.ChanceHigh);
            }
            Grade skip = next.Index + 1 < Grades.Count ? Grades.All[next.Index + 1] : null;
            if (skip != null && m_rand.Next() < Math.Min(Skills.Alchemist.Cap, alch))
            {
                next = skip;
                PushLog(Loc.Tl("연금술! {0} 등급", Loc.Tl(skip.Kr)));
            }
            Unit unit = CreateUnit(job, next.Key, true);
            Zone z = zoneId < Zones.Length ? Zones[zoneId] : null;
            Effect fx = FxAt(EffectType.Merge, unit.X, unit.Y, 0.7);
            fx.Color = next.Color;
            AddEffectAlways(fx); // 합성 연출은 이펙트 수 제한 없이
            if (IsPvp) LogFx(fx);
            if (z != null && !Units.Exists(u => u.ZoneId == z.Id)) z.Job = null;
            Merged?.Invoke(unit);
            return unit;
        }

        /// <summary>합성 가능한 (직업, 등급) 목록</summary>
        public List<KeyValuePair<Job, GradeKey>> Mergeable()
        {
            var output = new List<KeyValuePair<Job, GradeKey>>();
            foreach (UnitStack s in GetStacks()) if (CanMerge(s.Job, s.Grade)) output.Add(new KeyValuePair<Job, GradeKey>(s.Job, s.Grade));
            return output;
        }

        // 새 직업 구역 배정: 몹 진행(반시계: 좌상→좌하→하단→우측)에 맞춰 상단(0) → 좌측(3) → 하단(2) → 우측(1) 순으로 빈 구역을 채운다
        static readonly int[] s_zonePriority = { 0, 3, 2, 1 };

        /// <summary>소환 확정 로직 (직업/등급이 정해진 뒤 공통으로 처리)</summary>
        Unit CreateUnit(Job job, GradeKey gradeKey, bool merged)
        {
            Grade grade = Grades.Get(gradeKey);
            int gIdx = grade.Index;
            double baseAtk = grade.AtkMin + m_rand.Next() * (grade.AtkMax - grade.AtkMin);

            Zone zone = Array.Find(Zones, z => z.Job == job);
            if (zone == null)
            {
                foreach (int i in s_zonePriority)
                {
                    if (Zones[i].Job == null) { zone = Zones[i]; break; }
                }
            }
            if (zone == null) zone = Zones[0];
            zone.Job = job;

            if (!Levels.ContainsKey(job)) Levels[job] = 1; // 강화는 직업 단위

            var unit = new Unit
            {
                Id = m_nextId++, Job = job, Grade = gradeKey, GradeIndex = gIdx,
                BaseAtk = JsMath.Round(baseAtk), ZoneId = zone.Id,
                X = zone.Cx, Y = zone.Cy,
            };
            Units.Add(unit);
            PlaceInZone(unit, zone); // 구역 안 랜덤 배치 (겹침 허용)

            string jobKr = Loc.Tl(Core.Units.Get(job).Kr);
            string gradeKr = Loc.Tl(grade.Kr);
            if (gIdx >= (int)GradeKey.Mythic)
            {
                string verb = merged ? Loc.Tl("합성") : Loc.Tl("소환");
                MythicBanner = new LogEntry { Text = Loc.Tl("{0} {1} 등급 {2}!", jobKr, gradeKr, verb), Ttl = 4 };
                Shake = Math.Max(Shake, 5);
            }
            else
            {
                PushLog(merged
                    ? Loc.Tl("{0} {1} {2}등급 합성", Loc.Icon("hammer"), jobKr, gradeKr)
                    : Loc.Tl("{0}% {1} {2}등급 소환", grade.SummonRate, jobKr, gradeKr));
            }
            Effect sfx = FxAt(EffectType.Summon, unit.X, unit.Y, 0.5);
            sfx.Color = grade.Color;
            sfx.Big = gIdx >= (int)GradeKey.Legendary;
            AddEffect(sfx);

            if (gradeKey == GradeKey.Legendary) TryAchieve("firstLegendary");
            if (gradeKey == GradeKey.Mythic) TryAchieve("firstMythic");
            if (gradeKey == GradeKey.Eternal) TryAchieve("firstEternal");

            RosterChanged?.Invoke();
            return unit;
        }

        public int UpgradeCost(Job job)
        {
            int level = Levels.TryGetValue(job, out int lv) ? lv : 1;
            return Economy.UpgradeStep * level;
        }

        /// <summary>직업 강화: 해당 직업의 모든 등급 유닛에 레벨이 적용된다</summary>
        public bool Upgrade(Job job)
        {
            if (!Units.Exists(u => u.Job == job)) return false;
            int cost = UpgradeCost(job);
            if (Gold < cost) return false;
            Gold -= cost;
            Levels[job] = (Levels.TryGetValue(job, out int lv) ? lv : 1) + 1;
            RosterChanged?.Invoke();
            return true;
        }

        public bool SellOne(Job job, GradeKey gradeKey)
        {
            int idx = Units.FindIndex(u => u.Job == job && u.Grade == gradeKey);
            if (idx < 0) return false;
            Gold += Grades.Get(gradeKey).SellGold;
            Unit removed = Units[idx];
            Units.RemoveAt(idx);
            Sells += 1;
            ReportProgress(QuestStat.Sells);
            // 구역이 비면 직업 해제 (남은 유닛은 각자 위치 유지)
            if (!Units.Exists(u => u.ZoneId == removed.ZoneId))
            {
                Zone z = Array.Find(Zones, x => x.Id == removed.ZoneId);
                if (z != null) z.Job = null;
            }
            RosterChanged?.Invoke();
            return true;
        }

        /// <summary>qty 만큼 판매 (전부는 int.MaxValue). 판매한 개수 반환</summary>
        public int SellMany(Job job, GradeKey gradeKey, int qty)
        {
            int sold = 0;
            while (sold < qty && SellOne(job, gradeKey)) sold += 1;
            return sold;
        }

        public int CountOf(Job job, GradeKey gradeKey)
        {
            int n = 0;
            foreach (Unit u in Units) if (u.Job == job && u.Grade == gradeKey) n++;
            return n;
        }

        public void SwapZones(int idA, int idB)
        {
            if (idA < 0 || idB < 0 || idA >= Zones.Length || idB >= Zones.Length) return;
            Zone a = Zones[idA], b = Zones[idB];
            Job? tmp = a.Job;
            a.Job = b.Job;
            b.Job = tmp;
            foreach (Unit u in Units)
            {
                if (u.ZoneId == idA) u.NewZone = idB;
                else if (u.ZoneId == idB) u.NewZone = idA;
            }
            foreach (Unit u in Units)
            {
                if (u.NewZone >= 0) { u.ZoneId = u.NewZone; u.NewZone = -1; }
            }
            // 교환된 두 구역의 유닛을 새 구역 안의 임의 지점까지 걸어가게 한다
            foreach (Unit u in Units)
            {
                if (u.ZoneId == idA) StartWalk(u, RandomPointInZone(a));
                else if (u.ZoneId == idB) StartWalk(u, RandomPointInZone(b));
            }
            RosterChanged?.Invoke();
        }

        /// <summary>구역 삼각형 안의 임의 지점 (유닛끼리 겹쳐도 됨)</summary>
        Vec2 RandomPointInZone(Zone zone)
        {
            double r1 = m_rand.Next(), r2 = m_rand.Next();
            if (r1 + r2 > 1) { r1 = 1 - r1; r2 = 1 - r2; } // 삼각형 균등 샘플링
            Vec2 p0 = zone.Tri[0], p1 = zone.Tri[1], p2 = zone.Tri[2];
            double x = p0.X + r1 * (p1.X - p0.X) + r2 * (p2.X - p0.X);
            double y = p0.Y + r1 * (p1.Y - p0.Y) + r2 * (p2.Y - p0.Y);
            // 가장자리 여백: 무게중심 쪽으로 살짝 당겨 스프라이트가 밖으로 안 나가게
            return new Vec2(zone.Cx + (x - zone.Cx) * 0.82, zone.Cy + (y - zone.Cy) * 0.82);
        }

        /// <summary>즉시 배치 (소환 시 최초 등장 - 걷는 모습을 보일 필요 없음)</summary>
        void PlaceInZone(Unit u, Zone zone)
        {
            Vec2 p = RandomPointInZone(zone);
            u.X = p.X;
            u.Y = p.Y;
        }

        /// <summary>목표 지점까지 걸어가는 트윈 시작 (구역 교환 시 사용)</summary>
        void StartWalk(Unit u, Vec2 target, double dur = 0.55)
        {
            double dx = target.X - u.X, dy = target.Y - u.Y;
            if (Math.Abs(dx) > Math.Abs(dy)) u.Dir = dx > 0 ? 2 : 1; // 좌/우
            else u.Dir = dy > 0 ? 0 : 3;                              // 하/상
            u.Moving = true;
            u.MoveFrom = new Vec2(u.X, u.Y);
            u.MoveTo = target;
            u.MoveElapsed = 0;
            u.MoveDur = dur;
        }

        void UpdateUnitMovement(double dt)
        {
            foreach (Unit u in Units)
            {
                if (!u.Moving) continue;
                u.MoveElapsed += dt;
                double t = Math.Min(1, u.MoveElapsed / u.MoveDur);
                double e = t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2; // easeInOutQuad
                u.X = u.MoveFrom.X + (u.MoveTo.X - u.MoveFrom.X) * e;
                u.Y = u.MoveFrom.Y + (u.MoveTo.Y - u.MoveFrom.Y) * e;
                if (t >= 1)
                {
                    u.X = u.MoveTo.X;
                    u.Y = u.MoveTo.Y;
                    u.Moving = false;
                }
            }
        }

        // ---------------- UI 조회 헬퍼 ----------------
        /// <summary>직업+등급별 (판매 패널용) - 높은 등급 먼저, 같으면 직업 id 순</summary>
        public List<UnitStack> GetStacks()
        {
            var map = new Dictionary<int, UnitStack>();
            var order = new List<UnitStack>();
            foreach (Unit u in Units)
            {
                int key = (int)u.Job * 16 + u.GradeIndex;
                if (!map.TryGetValue(key, out UnitStack s))
                {
                    s = new UnitStack { Job = u.Job, Grade = u.Grade, GradeIndex = u.GradeIndex, Count = 0, Level = LevelOf(u.Job) };
                    map[key] = s;
                    order.Add(s);
                }
                s.Count += 1;
            }
            order.Sort(s_stackOrder); // (직업, 등급) 쌍이 겹치지 않아 정렬 안정성이 필요 없다
            return order;
        }

        static readonly Comparison<UnitStack> s_stackOrder = (a, b) =>
        {
            int c = b.GradeIndex.CompareTo(a.GradeIndex);
            return c != 0 ? c : string.CompareOrdinal(Core.Units.Get(a.Job).Id, Core.Units.Get(b.Job).Id);
        };

        static readonly Comparison<JobStack> s_jobStackOrder = (a, b) =>
            string.CompareOrdinal(Core.Units.Get(a.Job).Id, Core.Units.Get(b.Job).Id);

        /// <summary>직업별 (하단 강화 패널용) - 직업 id 순</summary>
        public List<JobStack> GetJobStacks()
        {
            var map = new Dictionary<Job, JobStack>();
            var order = new List<JobStack>();
            foreach (Unit u in Units)
            {
                if (!map.TryGetValue(u.Job, out JobStack s))
                {
                    s = new JobStack { Job = u.Job, Count = 0, Level = LevelOf(u.Job), TopGrade = u.Grade, TopGradeIndex = u.GradeIndex };
                    map[u.Job] = s;
                    order.Add(s);
                }
                s.Count += 1;
                if (u.GradeIndex > s.TopGradeIndex) { s.TopGradeIndex = u.GradeIndex; s.TopGrade = u.Grade; }
            }
            order.Sort(s_jobStackOrder);
            return order;
        }

        /// <summary>판 안 강화 레벨 (없으면 1)</summary>
        public int LevelOf(Job job) => Levels.TryGetValue(job, out int lv) ? lv : 1;
    }
}
