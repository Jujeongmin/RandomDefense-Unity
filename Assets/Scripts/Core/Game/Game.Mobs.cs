using System;
using System.Collections.Generic;

namespace DefenseBattle.Core
{
    // 몹: 생성·이동·몹 카드 능력·지속 피해 (engine.ts spawnMob ~ cardAbility, updateMobs, updateDots)
    public sealed partial class Game
    {
        /// <summary>이번 프레임 살아 있는 수호 주술사 (Hit 에서 피해 감소)</summary>
        readonly List<Mob> m_guards = new List<Mob>();

        /// <summary>웨이브 안에서 index 번째 몹의 종류 (결정적 → PvP 양쪽 동일)</summary>
        MobKind KindFor(int index)
        {
            int n = index + 1;
            if (Wave >= Waves.Tank.FromWave && n % Waves.Tank.Every == 0) return MobKind.Tank;
            if (Wave >= Waves.Fast.FromWave && n % Waves.Fast.Every == 0) return MobKind.Fast;
            return MobKind.Normal;
        }

        void SpawnMob()
        {
            if (IsBossWave)
            {
                AddMob(MobKind.Normal, WaveHp(), true);
                Shake = Math.Max(Shake, 10);
                BossSpawned?.Invoke();
                return;
            }
            MobKind kind = KindFor(SpawnedCount);
            double hpMult = kind == MobKind.Fast ? Waves.Fast.HpMult : kind == MobKind.Tank ? Waves.Tank.HpMult : 1;
            AddMob(kind, WaveHp() * hpMult, false);
        }

        Mob AddMob(MobKind kind, double hp, bool boss, double pos = 0, int card = -1)
        {
            double size = 60; // 그리기 크기 (판정에는 쓰지 않음) - 작은 화면에서도 알아보게 44 → 60
            double speedMult = 1;
            AttackDef def = card >= 0 ? Attacks.Get(card) : null;
            if (boss) size = 44 * Waves.BossSizeMult;
            else if (def != null)
            {
                size *= def.Size;
                speedMult = def.Speed;
            }
            else if (kind == MobKind.Fast)
            {
                size *= Waves.Fast.SizeMult;
                speedMult = Waves.Fast.SpeedMult;
            }
            else if (kind == MobKind.Tank)
            {
                size *= Waves.Tank.SizeMult;
                speedMult = Waves.Tank.SpeedMult;
            }
            Vec2 start = Board.PathAt(pos, out int next);
            // 몹 카드는 카드마다 정해진 그림·종족, 웨이브 몹은 이번 웨이브 종족
            Race race = def != null ? def.Race : CurrentRace;
            int n = Races.Variants[(int)race];
            // 그림 변형: 보스는 가장 강해 보이는 마지막, 탱커는 그 앞, 빠른 몹은 1번, 보통 몹은 앞쪽 두 가지를 섞는다
            int variant = def != null ? def.Variant
                : boss ? n - 1
                : kind == MobKind.Tank ? Math.Max(0, n - 2)
                : kind == MobKind.Fast ? Math.Min(1, n - 1)
                : SystemRandom.Index(m_rand, Math.Min(2, n));
            Mob m = NewMob(); // 풀에서 (ResetState 로 나머지 필드는 기본값)
            m.Id = m_nextId++;
            m.Race = race;
            m.Variant = variant;
            m.Kind = kind;
            m.Card = card;
            m.Ghost = def != null && def.Key == AttackKey.Ghost;
            m.X = start.X;
            m.Y = start.Y;
            m.Seg = next;
            m.Hp = hp;
            m.MaxHp = hp;
            m.Boss = boss;
            m.Size = size;
            m.SpeedMult = speedMult;
            Mobs.Add(m);
            return m;
        }

        /// <summary>테스트·디버그용: 원하는 자리에 몹 하나를 바로 세운다</summary>
        internal Mob DebugSpawn(double x, double y, double hp, int card = -1, bool boss = false, MobKind kind = MobKind.Normal)
        {
            Mob m = AddMob(card >= 0 ? MobKind.Card : kind, hp, boss, 0, card);
            m.X = x;
            m.Y = y;
            return m;
        }

        /// <summary>다른 몹 자리에서 새 몹 (분열·졸개)</summary>
        Mob SpawnFrom(Mob src, int card, double hp, int gold, double sizeMult = 1)
        {
            Mob m = AddMob(MobKind.Card, hp, false, 0, card);
            m.X = src.X + (m_rand.Next() - 0.5) * 30;
            m.Y = src.Y + (m_rand.Next() - 0.5) * 30;
            m.Seg = src.Seg;
            m.Size *= sizeMult;
            m.Gold = gold;
            return m;
        }

        /// <summary>길을 따라 dist 만큼 앞으로 (점멸 마녀)</summary>
        void Advance(Mob m, double dist)
        {
            Vec2[] c = Board.Corners;
            for (int i = 0; i < 8 && dist > 0; i++)
            {
                Vec2 t = c[m.Seg];
                double d = JsMath.Hypot(t.X - m.X, t.Y - m.Y);
                if (d > dist)
                {
                    m.X += (t.X - m.X) / d * dist;
                    m.Y += (t.Y - m.Y) / d * dist;
                    return;
                }
                m.X = t.X;
                m.Y = t.Y;
                dist -= d;
                m.Seg = (m.Seg + 1) % c.Length;
            }
        }

        /// <summary>길을 거슬러 dist 만큼 뒤로 (지난 모서리를 넘지 않음)</summary>
        void PushBack(Mob m, double dist)
        {
            if (m.Dead || m.Boss) return;
            Vec2[] c = Board.Corners;
            Vec2 prev = c[(m.Seg - 1 + c.Length) % c.Length];
            double d = JsMath.Hypot(prev.X - m.X, prev.Y - m.Y);
            double k = d > 0 ? Math.Min(1, dist / d) : 0;
            m.X += (prev.X - m.X) * k;
            m.Y += (prev.Y - m.Y) * k;
            m.Pause = Math.Max(m.Pause, 0.15);
        }

        void UpdateMobs(double dt)
        {
            double speedPx = 50 * (Waves.MobSpeed / 1.2);
            m_guards.Clear();
            foreach (Mob g in Mobs) if (g.IsCard && !g.Dead && Attacks.Get(g.Card).Guard > 0) m_guards.Add(g);
            // 능력으로 새로 생긴 몹은 다음 프레임부터 움직인다 → 지금 목록을 재사용 버퍼에 복사해 돈다
            m_mobBuffer.Clear();
            m_mobBuffer.AddRange(Mobs);
            for (int mi = 0; mi < m_mobBuffer.Count; mi++)
            {
                Mob m = m_mobBuffer[mi];
                if (m.HitFlash > 0) m.HitFlash -= dt;
                if (m.Slow > 0) m.Slow -= dt;
                if (m.IsCard && !m.Dead) CardAbility(m, dt);
                if (m.Pause > 0) { m.Pause -= dt; continue; }
                Vec2 target = Board.Corners[m.Seg];
                double dx = target.X - m.X, dy = target.Y - m.Y;
                double dist = JsMath.Hypot(dx, dy);
                // 이동 방향으로 스프라이트 방향 결정 (0=하 1=좌 2=우 3=상)
                if (Math.Abs(dx) > Math.Abs(dy)) m.Dir = dx > 0 ? 2 : 1;
                else m.Dir = dy > 0 ? 0 : 3;
                double rage = m.Enraged ? Attacks.Get(m.Card).Enrage : 1;
                double step = speedPx * m.SpeedMult * rage * (m.Slow > 0 ? 1 - m.SlowPct : 1) * dt;
                if (dist <= step)
                {
                    m.X = target.X;
                    m.Y = target.Y;
                    m.Seg = (m.Seg + 1) % Board.Corners.Length;
                    m.Pause = Waves.CornerPause;
                }
                else
                {
                    m.X += dx / dist * step;
                    m.Y += dy / dist * step;
                }
            }
        }

        /// <summary>몹 카드 능력: 신관 회복, 킹 졸개 소환, 도적 은신, 오크 광폭, 마녀 점멸, 주술사 보호막 표시, 유령 번식</summary>
        void CardAbility(Mob m, double dt)
        {
            AttackDef def = Attacks.Get(m.Card);
            m.Age += dt;
            if (def.Stealth > 0)
            {
                bool hide = m.Age % 4 >= 4 - def.Stealth;
                if (hide && !m.Hidden) AddEffect(FxSplash(m.X, m.Y, 0.3, 26, 0x6a4c93));
                m.Hidden = hide;
            }
            if (def.Enrage > 0 && !m.Enraged && m.Hp < m.MaxHp * 0.5)
            {
                m.Enraged = true;
                AddEffect(FxSplash(m.X, m.Y, 0.4, 34, 0xff4a3a));
            }
            if (def.Blink > 0)
            {
                m.BlinkT = (m.BlinkT ?? 3) - dt;
                if (m.BlinkT <= 0)
                {
                    m.BlinkT = 3;
                    AddEffect(FxSplash(m.X, m.Y, 0.35, 30, 0xc9a2ff));
                    Advance(m, def.Blink);
                    AddEffect(FxSplash(m.X, m.Y, 0.35, 30, 0xc9a2ff));
                }
            }
            if (def.Breed && !m.Bred && m.Gen < 2 && m.Age >= 5)
            {
                m.Bred = true;
                Mob c = SpawnFrom(m, m.Card, m.MaxHp * 0.7, 1, 0.9);
                c.Gen = m.Gen + 1;
                AddEffect(FxSplash(m.X, m.Y, 0.4, 32, 0xa8e86a));
            }
            if (def.Heal <= 0 && def.Spawn <= 0 && def.Guard <= 0) return;
            m.Tick += dt;
            if (def.Guard > 0 && m.Tick >= 1)
            {
                m.Tick -= 1;
                AddEffect(FxSplash(m.X, m.Y, 0.5, GuardRadius, 0x6fd3ff));
            }
            if (def.Heal > 0 && m.Tick >= 1)
            {
                m.Tick -= 1;
                for (int i = 0; i < Mobs.Count; i++)
                {
                    Mob o = Mobs[i];
                    if (o.Dead || o.Boss || o.Hp >= o.MaxHp || o.PurifyUntil > Time || !JsMath.Within(o.X, o.Y, m.X, m.Y, 130)) continue;
                    o.Hp = Math.Min(o.MaxHp, o.Hp + o.MaxHp * def.Heal);
                }
                AddEffect(FxSplash(m.X, m.Y, 0.4, 130, 0x7ee87e));
            }
            if (def.Spawn > 0 && m.Tick >= 4)
            {
                m.Tick -= 4;
                for (int i = 0; i < def.Spawn; i++) SpawnFrom(m, 0, m.MaxHp * 0.06, 1, 0.8);
            }
        }

        /// <summary>중독·불 장판 지속 피해 (막타 보상 없이 처치)</summary>
        void UpdateDots(double dt)
        {
            for (int i = 0; i < Mobs.Count; i++)
            {
                Mob m = Mobs[i];
                if (m.Poison.Until > Time) Dot(m, m.Poison.Dps * m.Poison.Stacks * dt);
            }
            if (Burns.Count > 0)
            {
                CompactBurns();
                for (int bi = 0; bi < Burns.Count; bi++)
                {
                    Burn b = Burns[bi];
                    for (int i = 0; i < Mobs.Count; i++)
                    {
                        Mob m = Mobs[i];
                        if (JsMath.Within(m.X, m.Y, b.X, b.Y, b.R)) Dot(m, b.Dps * dt);
                    }
                }
            }
            CompactMobs();
        }

        void Dot(Mob m, double dmg)
        {
            if (m.Dead) return;
            m.Hp -= dmg;
            if (m.Hp <= 0) KillMob(m);
        }
    }
}
