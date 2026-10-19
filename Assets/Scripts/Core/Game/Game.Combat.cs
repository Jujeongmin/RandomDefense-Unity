using System;
using System.Collections.Generic;

namespace DefenseBattle.Core
{
    // 전투: 사거리·피해 계산·공격·스킬 32종·처치 (engine.ts unitRange ~ killMob)
    public sealed partial class Game
    {
        public double UnitRange(Unit u) => UnitRange(u.GradeIndex, u.Job);

        public double UnitRange(int gradeIndex, Job job)
        {
            return gradeIndex >= Grades.GlobalRangeFromIndex ? double.PositiveInfinity : Grades.Range[gradeIndex] * Core.Units.Get(job).RangeMult;
        }

        /// <summary>컬렉션(영구) 레벨</summary>
        public int CollectionLevel(Job job) => Opts.UnitLevels != null && Opts.UnitLevels.TryGetValue(job, out int lv) && lv > 0 ? lv : 1;

        public double UnitDamage(Unit u, Mob mob)
        {
            int level = LevelOf(u.Job);
            double dmg = u.BaseAtk * (1 + (level - 1) * Economy.UpgradePerLevel);
            UnitDef ud = Core.Units.Get(u.Job);
            dmg *= ud.AtkMult * Core.Units.RarityAtk[(int)ud.Rarity] * (1 + (CollectionLevel(u.Job) - 1) * Core.Units.LevelAtk); // 희귀도 보너스
            dmg *= Core.Units.DmgMult(u.Job, mob.Race);
            AttackDef def = mob.IsCard ? Attacks.Get(mob.Card) : null;
            if (def != null && def.Resists(u.Job)) dmg *= Attacks.ResistMult; // 유령(물리)·방패병(화살)
            if (mob.MarkUntil > 0 && mob.MarkUntil > Time) dmg *= Skills.Hunter.MarkMult;
            if (u.GradeIndex >= Skills.FromIndex)
            {
                if (u.Job == Job.Knight && (mob.Kind == MobKind.Tank || (def != null && IsArmored(def.Key))))
                {
                    dmg *= Skills.Val(u.GradeIndex, Skills.Knight.ArmoredMult, Skills.Knight.ArmoredMultHigh);
                }
                else if (u.Job == Job.Houndmaster && mob.SpeedMult > 1.2)
                {
                    dmg *= Skills.Val(u.GradeIndex, Skills.Houndmaster.FastMult, Skills.Houndmaster.FastMultHigh);
                }
                else if (u.Job == Job.Berserker)
                {
                    dmg *= 1 + Math.Min(Skills.Berserker.Cap, Mobs.Count * Skills.Berserker.PerMob);
                }
            }
            dmg *= m_angelMult;
            // 성기사: 보스와 큰 몹 카드(거인·킹·마왕) 특화
            if (u.Job == Job.Paladin && u.GradeIndex >= Skills.FromIndex && (mob.Boss || (def != null && def.HpMult >= 5)))
            {
                double m = Skills.Val(u.GradeIndex, Skills.Paladin.BossMult, Skills.Paladin.BossMultHigh);
                dmg *= 1 + (m - 1) * Core.Units.PerkMult(CollectionLevel(u.Job));
            }
            dmg *= 1 + Res(ResearchKey.Atk) * Research.Per(ResearchKey.Atk);
            dmg *= Opts.AtkMult;
            if (mob.Boss) dmg *= 1 + Res(ResearchKey.Boss) * Research.Per(ResearchKey.Boss);
            return dmg;
        }

        /// <summary>기사 특화 대상: 방패병·거인·킹·마왕·골렘</summary>
        static bool IsArmored(AttackKey k)
            => k == AttackKey.Shield || k == AttackKey.Giant || k == AttackKey.King || k == AttackKey.Demon || k == AttackKey.Golem;

        void UpdateCombat(double dt)
        {
            // 오라: 천사(공격력)·북치기(공속)
            double angel = 0;
            m_drummers.Clear();
            foreach (Unit u in Units)
            {
                if (u.GradeIndex < Skills.FromIndex) continue;
                if (u.Job == Job.Angel) angel += Skills.Val(u.GradeIndex, Skills.Angel.Aura, Skills.Angel.AuraHigh);
                else if (u.Job == Job.Drummer) m_drummers.Add(u);
            }
            m_angelMult = 1 + Math.Min(Skills.Angel.Cap, angel);
            foreach (Unit u in Units)
            {
                if (u.Flash > 0) u.Flash -= dt;
                u.Cooldown -= dt;
                if (u.Cooldown > 0) continue;
                double range = UnitRange(u);
                // 가장 가까운 몹 찾기 - 매 프레임 유닛 × 몹 만큼 도는 가장 무거운 곳이라 제곱 거리로 비교 (sqrt 는 찾은 뒤 한 번)
                double rangeSq = range * range;
                Mob best = null;
                double bestSq = double.PositiveInfinity;
                for (int i = 0; i < Mobs.Count; i++)
                {
                    Mob m = Mobs[i];
                    if (m.Dead || m.Hidden) continue; // 숨은 도적은 직접 못 노림 (범위 공격엔 맞음)
                    double dSq = JsMath.DistSq(m.X, m.Y, u.X, u.Y);
                    if (dSq <= rangeSq && dSq < bestSq) { bestSq = dSq; best = m; }
                }
                if (best == null) { u.Face = 0; continue; } // 노릴 몹이 없으면 정면
                double bestD = Math.Sqrt(bestSq);
                // 공격하는 몹 쪽을 바라본다 (스프라이트 행: 0=하 1=좌 2=우 3=상)
                double fx = best.X - u.X, fy = best.Y - u.Y;
                u.Face = Math.Abs(fx) > Math.Abs(fy) ? (fx > 0 ? 2 : 1) : (fy > 0 ? 0 : 3);
                bool crit = m_rand.Next() < CritChance(u);
                double dmg = UnitDamage(u, best) * (crit ? Skills.CritMult : 1);
                if (u.Job == Job.Reaper && u.Souls > 0) { dmg *= Skills.Reaper.SoulMult; u.Souls -= 1; } // 영혼 소모
                u.Cooldown = Core.Units.Get(u.Job).Cooldown * HasteFor(u);
                m_hitter = u;
                u.Flash = 0.1;
                if (IsPvp) m_shotUnits.Add(u.Id);
                Shoot?.Invoke(u.Job);
                // 투사체: 거리에 비례한 비행 시간 (시각 전용)
                double dur = Math.Min(0.22, 0.06 + bestD / 2600);
                AddEffect(FxProj(u, best.X, best.Y, dur));
                Hit(best, dmg, crit);

                // 직업 스킬 (일반 등급부터, 등급별 3단계)
                if (u.GradeIndex >= Skills.FromIndex) ApplySkill(u, best, range);
                m_hitter = null;

                // 태초 등급: 몹 이동 경로(사각 링) 전체에 촘촘히 폭발 이펙트 (시각 전용 - 데미지는 단일 대상 그대로)
                if (u.GradeIndex == (int)GradeKey.Eternal && Effects.Count < 160)
                {
                    Vec2[] c = Board.Corners;
                    for (int seg = 0; seg < c.Length; seg++)
                    {
                        Vec2 a = c[seg], b = c[(seg + 1) % c.Length];
                        int count = Math.Max(1, JsMath.RoundInt(JsMath.Hypot(b.X - a.X, b.Y - a.Y) / 48)); // ~48px 간격
                        for (int k = 0; k < count; k++)
                        {
                            double t = Math.Min(1, Math.Max(0, (k + 0.5) / count + (m_rand.Next() - 0.5) * 0.05));
                            Vec2 p = Board.PathPoint(seg, t);
                            Effect burst = FxAt(EffectType.Burst, p.X, p.Y, 0.4);
                            burst.Seed = m_rand.Next() * Math.PI * 2;
                            AddEffect(burst);
                        }
                    }
                    Shake = Math.Max(Shake, 2.5);
                }
            }
            CompactMobs();
        }

        /// <summary>주변 북치기 → 공격 간격 배수 (가장 센 북 하나)</summary>
        double HasteFor(Unit u)
        {
            double best = 0;
            foreach (Unit d in m_drummers)
            {
                if (d == u || !JsMath.Within(d.X, d.Y, u.X, u.Y, Skills.Drummer.Radius)) continue;
                best = Math.Max(best, Skills.Val(d.GradeIndex, Skills.Drummer.Haste, Skills.Drummer.HasteHigh));
            }
            return 1 - best;
        }

        /// <summary>단일 대상 피해 처리 + 데미지 숫자</summary>
        void Hit(Mob m, double dmg, bool crit)
        {
            if (m.Dead) return;
            // 수호 주술사 근처의 다른 몹은 피해 감소 (가장 먼저 찾은 하나)
            foreach (Mob g in m_guards)
            {
                if (g != m && !g.Dead && JsMath.Within(g.X, g.Y, m.X, m.Y, GuardRadius)) { dmg *= 1 - Attacks.Get(g.Card).Guard; break; }
            }
            m.Hp -= dmg;
            m.HitFlash = 0.12;
            // 데미지 숫자 (너무 많으면 생략해 성능/난잡함 방지)
            if (Effects.Count < 90 || crit)
            {
                Effect num = FxAt(EffectType.Dmg, m.X + (m_rand.Next() - 0.5) * 14, m.Y - m.Size / 2, 0.7);
                num.Value = JsMath.Round(dmg);
                num.Crit = crit;
                AddEffect(num);
            }
            if (m.Hp <= 0) KillMob(m);
        }

        double CritChance(Unit u)
        {
            if (u.Job == Job.Ranger && u.GradeIndex >= Skills.FromIndex)
            {
                double c = Skills.Val(u.GradeIndex, Skills.Ranger.CritChance, Skills.Ranger.CritChanceHigh);
                return Math.Min(0.8, c * Core.Units.PerkMult(CollectionLevel(u.Job)));
            }
            return Skills.CritChance;
        }

        /// <summary>범위 피해 (대상 제외)</summary>
        void SplashAround(Unit u, Mob target, double r, double ratio)
        {
            for (int i = 0; i < Mobs.Count; i++)
            {
                Mob m = Mobs[i];
                if (m == target || m.Dead) continue;
                if (JsMath.Within(m.X, m.Y, target.X, target.Y, r)) Hit(m, UnitDamage(u, m) * ratio, false);
            }
        }

        void SlowMob(Mob m, double pct, double dur)
        {
            if (m.Dead) return;
            m.SlowPct = m.Slow > 0 ? Math.Max(m.SlowPct, pct) : pct;
            m.Slow = Math.Max(m.Slow, dur);
        }

        /// <summary>
        /// 유닛 → 대상 방향 직선에서 대상 뒤 length 안, 폭 width 안의 몹을 가까운 순으로 m_candidates 에 채운다.
        /// 삽입 정렬 = 안정 정렬 (원본 Array.sort 와 같은 순서), 대상 수가 적어 충분히 빠르다.
        /// </summary>
        List<Mob> LineTargets(Unit u, Mob target, double length, double width)
        {
            m_candidates.Clear();
            m_candidateKeys.Clear();
            double d = JsMath.Hypot(target.X - u.X, target.Y - u.Y);
            if (d == 0) d = 1;
            double dx = (target.X - u.X) / d, dy = (target.Y - u.Y) / d;
            foreach (Mob m in Mobs)
            {
                if (m == target || m.Dead) continue;
                double rx = m.X - u.X, ry = m.Y - u.Y;
                double t = rx * dx + ry * dy;
                if (t < d - 10 || t > d + length) continue;
                if (Math.Abs(rx * dy - ry * dx) <= width) InsertSorted(m, t);
            }
            return m_candidates;
        }

        /// <summary>m_candidates 를 key 오름차순으로 유지하며 넣기 (같은 key 는 먼저 온 것이 앞 - 안정)</summary>
        void InsertSorted(Mob m, double key)
        {
            int i = m_candidateKeys.Count;
            while (i > 0 && m_candidateKeys[i - 1] > key) i--;
            m_candidates.Insert(i, m);
            m_candidateKeys.Insert(i, key);
        }

        Effect SlashFx(Mob at, double ttl) => FxSlash(at, ttl);

        Effect SplashFx(Mob at, double ttl, double r, uint color) => FxSplash(at.X, at.Y, ttl, r, color);

        void ApplySkill(Unit u, Mob target, double range)
        {
            double perk = Core.Units.PerkMult(CollectionLevel(u.Job)); // 컬렉션 Lv5/10 퍽
            int g = u.GradeIndex;
            switch (u.Job)
            {
                case Job.Archer:
                {
                    int n = (int)Skills.Val(g, Skills.Archer.ExtraTargets, Skills.Archer.ExtraTargetsHigh) + (perk > 1.5 ? 1 : 0);
                    // 사거리 안 다른 몹 중 대상에 가까운 n명 (정렬 없이 앞에서부터 n개만 유지)
                    m_candidates.Clear();
                    m_candidateKeys.Clear();
                    foreach (Mob m in Mobs)
                    {
                        if (m == target || m.Dead || m.Hidden || !JsMath.Within(m.X, m.Y, u.X, u.Y, range)) continue;
                        double key = JsMath.DistSq(m.X, m.Y, target.X, target.Y); // 제곱 거리도 순서는 같다
                        if (m_candidates.Count >= n && key >= m_candidateKeys[n - 1]) continue;
                        InsertSorted(m, key);
                        if (m_candidates.Count > n) { m_candidates.RemoveAt(n); m_candidateKeys.RemoveAt(n); }
                    }
                    m_mobBuffer.Clear();
                    m_mobBuffer.AddRange(m_candidates); // Hit 이 분열로 목록을 바꿀 수 있어 따로 복사
                    foreach (Mob m in m_mobBuffer)
                    {
                        AddEffect(FxProj(u, m.X, m.Y, 0.12));
                        Hit(m, UnitDamage(u, m) * Skills.Archer.Ratio * perk, false);
                    }
                    break;
                }
                case Job.Wizard:
                {
                    double r = Skills.Val(g, Skills.Wizard.Radius, Skills.Wizard.RadiusHigh);
                    AddEffect(SplashFx(target, 0.35, r, 0xb48cff));
                    SplashAround(u, target, r, Skills.Wizard.Ratio * perk);
                    break;
                }
                case Job.Bomber:
                {
                    double r = Skills.Val(g, Skills.Bomber.Radius, Skills.Bomber.RadiusHigh);
                    AddEffect(SplashFx(target, 0.45, r, 0xff7a3d));
                    SplashAround(u, target, r, Skills.Bomber.Ratio * perk);
                    Shake = Math.Max(Shake, 1.5);
                    break;
                }
                case Job.Frost:
                {
                    double r = Skills.Val(g, Skills.Frost.Radius, Skills.Frost.RadiusHigh);
                    double pct = Math.Min(0.7, Skills.Val(g, Skills.Frost.SlowPct, Skills.Frost.SlowPctHigh) * (1 + (perk - 1) * 0.5));
                    AddEffect(SplashFx(target, 0.4, r, 0x7fe0ff));
                    for (int i = 0; i < Mobs.Count; i++)
                    {
                        Mob m = Mobs[i];
                        if (JsMath.Within(m.X, m.Y, target.X, target.Y, r)) SlowMob(m, pct, Skills.Frost.Dur);
                    }
                    break;
                }
                case Job.Warrior:
                {
                    double pct = Math.Min(0.7, Skills.Val(g, Skills.Warrior.SlowPct, Skills.Warrior.SlowPctHigh) * (1 + (perk - 1) * 0.5));
                    SlowMob(target, pct, Skills.Warrior.Dur);
                    AddEffect(SlashFx(target, 0.25));
                    AddEffect(SplashFx(target, 0.35, 28, 0x8fd8ff)); // 둔화 충격 (파랑)
                    break;
                }
                case Job.Assassin:
                {
                    double th = Math.Min(Skills.Assassin.Cap, Skills.Val(g, Skills.Assassin.Execute, Skills.Assassin.ExecuteHigh) * perk);
                    if (!target.Dead && !target.Boss && target.Hp / target.MaxHp <= th)
                    {
                        AddEffect(SlashFx(target, 0.3));
                        Hit(target, target.Hp + 1, true);
                    }
                    break;
                }
                case Job.Spearman:
                {
                    int n = (int)Skills.Val(g, Skills.Spearman.Pierce, Skills.Spearman.PierceHigh);
                    List<Mob> line = LineTargets(u, target, 140, 26);
                    m_mobBuffer.Clear();
                    for (int i = 0; i < line.Count && i < n; i++) m_mobBuffer.Add(line[i]);
                    AddEffect(FxBeam(u.X, u.Y, target.X + (target.X - u.X) * 0.3, target.Y + (target.Y - u.Y) * 0.3, 0.15, 0xe8d0a0, 5));
                    foreach (Mob m in m_mobBuffer) Hit(m, UnitDamage(u, m) * Skills.Spearman.Ratio * perk, false);
                    break;
                }
                case Job.Crossbow:
                case Job.Dragoon:
                {
                    bool dragoon = u.Job == Job.Dragoon;
                    double length = dragoon ? Skills.Dragoon.Length : Skills.Crossbow.Length;
                    double width = dragoon ? Skills.Dragoon.Width : Skills.Crossbow.Width;
                    double ratio = (dragoon
                        ? Skills.Val(g, Skills.Dragoon.Ratio, Skills.Dragoon.RatioHigh)
                        : Skills.Val(g, Skills.Crossbow.Ratio, Skills.Crossbow.RatioHigh)) * perk;
                    m_mobBuffer.Clear();
                    m_mobBuffer.AddRange(LineTargets(u, target, length, width));
                    double d = JsMath.Hypot(target.X - u.X, target.Y - u.Y);
                    if (d == 0) d = 1;
                    double ex = u.X + (target.X - u.X) / d * (d + length), ey = u.Y + (target.Y - u.Y) / d * (d + length);
                    AddEffect(FxBeam(u.X, u.Y, ex, ey, 0.25, dragoon ? 0xff6a2au : 0xd8c8a0u, dragoon ? 22 : 4));
                    foreach (Mob m in m_mobBuffer) Hit(m, UnitDamage(u, m) * ratio, false);
                    break;
                }
                case Job.Slinger:
                    PushBack(target, Skills.Val(g, Skills.Slinger.Push, Skills.Slinger.PushHigh) * perk);
                    break;
                case Job.Monk:
                {
                    int n = (int)Skills.Val(g, Skills.Monk.Hits, Skills.Monk.HitsHigh) - 1;
                    for (int i = 0; i < n && !target.Dead; i++) Hit(target, UnitDamage(u, target) * Skills.Monk.Ratio * perk, false);
                    AddEffect(SlashFx(target, 0.2));
                    break;
                }
                case Job.Hunter:
                    target.MarkUntil = Time + Skills.Val(g, Skills.Hunter.MarkDur, Skills.Hunter.MarkDurHigh) * perk;
                    break;
                case Job.Acolyte:
                {
                    double r = Skills.Val(g, Skills.Acolyte.Radius, Skills.Acolyte.RadiusHigh);
                    AddEffect(SplashFx(target, 0.35, r, 0xfff2a8));
                    for (int i = 0; i < Mobs.Count; i++)
                    {
                        Mob m = Mobs[i];
                        if (m == target || m.Dead || m.Race != Race.Undead || !JsMath.Within(m.X, m.Y, target.X, target.Y, r)) continue;
                        Hit(m, UnitDamage(u, m) * Skills.Acolyte.Ratio * perk, false);
                    }
                    break;
                }
                case Job.Cleric:
                    target.PurifyUntil = Time + Skills.Val(g, Skills.Cleric.PurifyDur, Skills.Cleric.PurifyDurHigh) * perk;
                    AddEffect(SplashFx(target, 0.35, 34, 0xfff0c0));
                    break;
                case Job.Poisoner:
                {
                    double dps = UnitDamage(u, target) * Skills.Val(g, Skills.Poisoner.Dps, Skills.Poisoner.DpsHigh) * perk;
                    Poison p = target.Poison; // 몹마다 하나 - 새로 만들지 않고 값만 바꾼다
                    bool active = p.Until > Time;
                    p.Stacks = Math.Min(Skills.Poisoner.MaxStack, (active ? p.Stacks : 0) + 1);
                    p.Dps = Math.Max(dps, active ? p.Dps : 0);
                    p.Until = Time + Skills.Poisoner.Dur;
                    AddEffect(SplashFx(target, 0.3, 26, 0x7ac943));
                    break;
                }
                case Job.Storm:
                {
                    Mob from = target;
                    HashSet<Mob> done = m_stormDone;
                    done.Clear();
                    done.Add(target);
                    int n = (int)Skills.Val(g, Skills.Storm.Bounces, Skills.Storm.BouncesHigh);
                    for (int i = 0; i < n; i++)
                    {
                        Mob next = null;
                        double nd = Skills.Storm.Range * Skills.Storm.Range;
                        foreach (Mob m in Mobs)
                        {
                            if (m.Dead || done.Contains(m)) continue;
                            double dd = JsMath.DistSq(m.X, m.Y, from.X, from.Y);
                            if (dd < nd) { nd = dd; next = m; }
                        }
                        if (next == null) break;
                        done.Add(next);
                        AddEffect(FxBeam(from.X, from.Y, next.X, next.Y, 0.2, 0xa8d0ff, 3));
                        Hit(next, UnitDamage(u, next) * Skills.Storm.Ratio * perk, false);
                        from = next;
                    }
                    break;
                }
                case Job.Pyro:
                {
                    double r = Skills.Val(g, Skills.Pyro.Radius, Skills.Pyro.RadiusHigh);
                    if (Burns.Count < 24) AddBurn(target.X, target.Y, r, UnitDamage(u, target) * Skills.Pyro.Dps * perk, Time + Skills.Pyro.Dur);
                    break;
                }
                case Job.Cannoneer:
                    target.Pause = Math.Max(target.Pause, Skills.Val(g, Skills.Cannoneer.Stun, Skills.Cannoneer.StunHigh) * perk);
                    AddEffect(SplashFx(target, 0.4, Skills.Cannoneer.Radius, 0xc8c8c8));
                    SplashAround(u, target, Skills.Cannoneer.Radius, Skills.Cannoneer.Ratio * perk);
                    Shake = Math.Max(Shake, 2);
                    break;
                case Job.Chrono:
                {
                    double r = Skills.Val(g, Skills.Chrono.Radius, Skills.Chrono.RadiusHigh);
                    double stop = Skills.Val(g, Skills.Chrono.Stop, Skills.Chrono.StopHigh) * perk;
                    AddEffect(SplashFx(target, 0.5, r, 0xc0a0ff));
                    foreach (Mob m in Mobs) if (!m.Boss && JsMath.Within(m.X, m.Y, target.X, target.Y, r)) m.Pause = Math.Max(m.Pause, stop);
                    break;
                }
                case Job.Archmage:
                {
                    double r = Skills.Val(g, Skills.Archmage.Radius, Skills.Archmage.RadiusHigh);
                    AddEffect(SplashFx(target, 0.5, r, 0xff7ad8));
                    SplashAround(u, target, r, Skills.Archmage.Ratio * perk);
                    Shake = Math.Max(Shake, 2.5);
                    break;
                }
                case Job.Shadow:
                {
                    Mob other = null;
                    foreach (Mob m in Mobs)
                    {
                        if (m != target && !m.Dead && JsMath.Within(m.X, m.Y, u.X, u.Y, range)) { other = m; break; }
                    }
                    if (other == null && !target.Dead) other = target;
                    if (other != null)
                    {
                        AddEffect(SlashFx(other, 0.25));
                        Hit(other, UnitDamage(u, other) * Skills.Val(g, Skills.Shadow.CloneRatio, Skills.Shadow.CloneRatioHigh) * perk, false);
                    }
                    break;
                }
                case Job.Stormking:
                {
                    int n = (int)Skills.Val(g, Skills.Stormking.Strikes, Skills.Stormking.StrikesHigh);
                    List<Mob> alive = m_mobBuffer;
                    alive.Clear();
                    foreach (Mob m in Mobs) if (!m.Dead) alive.Add(m);
                    for (int i = 0; i < n && alive.Count > 0; i++)
                    {
                        Mob m = alive[SystemRandom.Index(m_rand, alive.Count)];
                        AddEffect(FxBeam(m.X + (m_rand.Next() - 0.5) * 30, m.Y - 140, m.X, m.Y, 0.2, 0xa8d8ff, 4));
                        Hit(m, UnitDamage(u, m) * Skills.Stormking.Ratio * perk, false);
                    }
                    break;
                }
                case Job.Paladin:
                case Job.Ranger:
                    // 레인저(치명타)·성기사(보스 피해)는 CritChance/UnitDamage 에서 처리
                    AddEffect(SlashFx(target, 0.2));
                    break;
            }
        }

        public void KillMob(Mob m)
        {
            if (m.Dead) return;
            AttackDef def = m.IsCard ? Attacks.Get(m.Card) : null;
            Unit by = m_hitter;
            bool bySkill = by != null && by.GradeIndex >= Skills.FromIndex;
            if (bySkill && by.Job == Job.Reaper)
            {
                m.NoRevive = true;
                by.Souls = Math.Min(Skills.Reaper.MaxSouls, by.Souls + 1);
            }
            // 망령: 한 번 되살아남 (사신·정화로 막을 수 있음)
            if (def != null && def.Revive && m.Gold == null && !m.NoRevive && !(m.PurifyUntil > Time))
            {
                m.Gold = Economy.KillGold;
                m.Hp = m.MaxHp * 0.5;
                m.Pause = 0.6;
                AddEffect(FxSplash(m.X, m.Y, 0.4, 40, 0xc9a2ff));
                return;
            }
            m.Dead = true; // 목록에서 빼는 건 각 단계 끝에서 한꺼번에
            // 분열 알: 작은 몹으로 갈라짐 (분열된 몹은 다시 갈라지지 않음)
            if (def != null && def.Split > 0 && m.Gold == null)
            {
                for (int i = 0; i < def.Split; i++) SpawnFrom(m, m.Card, m.MaxHp * 0.25, 1, 0.6);
            }
            double goldMult = 1 + Res(ResearchKey.GoldGain) * Research.Per(ResearchKey.GoldGain);
            double gold = m.Boss ? Economy.BossGold : m.Gold ?? def?.Gold ?? Economy.KillGold;
            if (bySkill && by.Job == Job.Merchant) gold += Skills.Val(by.GradeIndex, Skills.Merchant.Gold, Skills.Merchant.GoldHigh);
            int gained = JsMath.RoundInt(gold * goldMult);
            Gold += gained;
            Kills += 1;
            ReportProgress(QuestStat.Kills);
            Effect pop = FxAt(EffectType.Pop, m.X, m.Y, m.Boss ? 0.8 : 0.45);
            pop.Boss = m.Boss;
            pop.Seed = m_rand.Next() * 6.28;
            AddEffect(pop);
            if (m.Boss || Effects.Count < 140)
            {
                Effect coin = FxAt(EffectType.Gold, m.X, m.Y - 10, 0.8);
                coin.Value = gained;
                AddEffect(coin);
            }
            if (m.Boss) Shake = Math.Max(Shake, 14);
            Kill?.Invoke(m.Boss);
        }
    }
}
