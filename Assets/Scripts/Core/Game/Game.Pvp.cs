using System;
using System.Collections.Generic;

namespace DefenseBattle.Core
{
    // PvP: 몹 보내기·받기, 상대 화면용 스냅샷, 사거리 빈틈 (engine.ts sendAttack ~ snapshot, pathGaps, logFx)
    public sealed partial class Game
    {
        readonly List<PendingDrop> m_pending = new List<PendingDrop>();
        readonly Dictionary<int, double> m_attackReadyAt = new Dictionary<int, double>();
        /// <summary>상대 화면에 다시 그릴 이펙트 (스냅샷마다 비운다)</summary>
        readonly List<int[]> m_fxLog = new List<int[]>();
        /// <summary>지난 스냅샷 뒤 공격한 유닛 id</summary>
        readonly HashSet<int> m_shotUnits = new HashSet<int>();

        int[] m_attackCards = new int[0];

        /// <summary>내 공격 덱의 카드 번호들 (Attacks.All 번호). 판 시작(Reset) 때 한 번 만든다</summary>
        public IReadOnlyList<int> AttackCards => m_attackCards;

        void RebuildAttackCards()
        {
            m_attackCards = new int[Opts.AttackDeck.Count];
            for (int i = 0; i < m_attackCards.Length; i++) m_attackCards[i] = (int)Opts.AttackDeck[i];
        }

        bool HasAttackCard(int card) => Array.IndexOf(m_attackCards, card) >= 0;

        public int AttackCost(int card) => Attacks.Get(card).Cost;

        /// <summary>남은 쿨타임(초, 게임 시간) - 마왕처럼 쿨타임이 있는 카드</summary>
        public double AttackCooldown(int card)
            => Math.Max(0, (m_attackReadyAt.TryGetValue(card, out double t) ? t : 0) - Time);

        /// <summary>떨어지는 중인 몹 (화면 경고용)</summary>
        public IReadOnlyList<PendingDrop> Pending => m_pending;

        /// <summary>몹 던지기 (덱에 있는 카드만). pos = 상대 길 위치(둘레 비율 0~1)</summary>
        public bool SendAttack(int card, double pos = 0)
        {
            if (!IsPvp || Over || !HasAttackCard(card)) return false;
            AttackDef def = Attacks.Get(card);
            if (Gold < def.Cost || AttackCooldown(card) > 0) return false;
            Gold -= def.Cost;
            m_attackReadyAt[card] = Time + def.Cooldown;
            SentCount += 1;
            var payload = new AttackPayload { Card = card, Wave = Math.Max(1, Wave), Pos = pos };
            PushLog(Loc.Tl("{0} 던짐!", Loc.Tl(def.Kr)));
            AttackSent?.Invoke(payload);
            return true;
        }

        public void ReceiveAttack(AttackPayload p)
        {
            if (Over) return;
            int card = p.Card >= 0 && p.Card < Attacks.All.Length ? p.Card : 0;
            AttackDef def = Attacks.Get(card);
            double hp = BaseHp(p.Wave) * def.HpMult;
            double pos = double.IsNaN(p.Pos) || double.IsInfinity(p.Pos) ? 0 : p.Pos;
            Vec2 at = Board.PathAt(pos);
            for (int i = 0; i < def.Count; i++)
            {
                double delay = DropWarn + i * 0.25;
                m_pending.Add(new PendingDrop { Card = card, Hp = hp, Delay = delay, Pos = pos });
                Effect warn = FxAt(EffectType.Drop, at.X + (m_rand.Next() - 0.5) * 36, at.Y + (m_rand.Next() - 0.5) * 36, delay);
                warn.Dur = delay;
                warn.Card = card;
                warn.Seed = m_rand.Next();
                AddEffectAlways(warn); // 착지 경고는 이펙트 상한과 상관없이
            }
            PushLog(Loc.Tl("{0} 상대의 {1}!", Loc.Icon("warning"), Loc.Tl(def.Kr)));
            if (def.Cost >= 80) Shake = Math.Max(Shake, 6);
            AttackIncoming?.Invoke(p);
        }

        void UpdatePending(double dt)
        {
            if (m_pending.Count == 0) return;
            // 다 떨어진 몹을 순서대로 길에 내려놓고 목록에서 뺀다 (할당 없이 제자리 정리)
            int firstCard = -1;
            int w = 0;
            for (int r = 0; r < m_pending.Count; r++)
            {
                PendingDrop p = m_pending[r];
                p.Delay -= dt;
                if (p.Delay > 0) { m_pending[w++] = p; continue; }
                if (firstCard < 0) firstCard = p.Card;
                AddMob(MobKind.Card, p.Hp, false, p.Pos, p.Card);
            }
            if (firstCard < 0) return;
            m_pending.RemoveRange(w, m_pending.Count - w);
            Shake = Math.Max(Shake, Attacks.Get(firstCard).Cost >= 80 ? 7 : 3); // 착지 쿵
        }

        /// <summary>
        /// PvP 상대 미니맵/상태 보고용 요약. 공격 이펙트·공격한 유닛 기록은 여기서 비운다.
        /// reuse 를 주면 그 객체의 리스트를 비우고 다시 채운다 (0.25초마다 부르므로 할당을 줄이려면 재사용).
        /// </summary>
        public BoardSnapshot Snapshot(BoardSnapshot reuse = null)
        {
            BoardSnapshot s = reuse ?? new BoardSnapshot();
            s.Mobs.Clear(); s.Units.Clear(); s.Uc.Clear(); s.Mh.Clear(); s.Ms.Clear(); s.Gp.Clear(); s.Fx.Clear(); s.Deck.Clear(); s.Atk.Clear();
            int mobN = Math.Min(120, Mobs.Count);
            for (int i = 0; i < mobN; i++)
            {
                Mob m = Mobs[i];
                int kind = m.Boss ? BoardSnapshot.KindBoss : m.Kind == MobKind.Card ? BoardSnapshot.KindSent : BoardSnapshot.KindNormal;
                s.Mobs.Add(m.Id);
                s.Mobs.Add(BoardSnapshot.Quantize(m.X));
                s.Mobs.Add(BoardSnapshot.Quantize(m.Y));
                s.Mobs.Add((int)m.Race | (kind << 2) | (m.Dir << 4) | (m.Variant << 6));
                s.Mobs.Add(JsMath.RoundInt(m.Size));
                s.Mh.Add(Math.Max(0, Math.Min(100, (int)Math.Ceiling(m.Hp / m.MaxHp * 100))));
                s.Ms.Add((m.Slow > 0 ? BoardSnapshot.StatusSlow : 0)
                    | (m.Pause > 0 ? BoardSnapshot.StatusStop : 0)
                    | (m.Poison.Until > Time ? BoardSnapshot.StatusPoison : 0)
                    | (m.MarkUntil > 0 && m.MarkUntil > Time ? BoardSnapshot.StatusMark : 0)
                    | (m.Enraged ? BoardSnapshot.StatusEnraged : 0)
                    | (m.Ghost ? BoardSnapshot.StatusGhost : 0)
                    | (m.Hidden ? BoardSnapshot.StatusHidden : 0));
            }
            int gapWords = (BoardSnapshot.GapSamples + BoardSnapshot.GapBits - 1) / BoardSnapshot.GapBits;
            for (int i = 0; i < gapWords; i++) s.Gp.Add(0);
            foreach (int i in PathGaps(m_gapBuffer)) s.Gp[i / BoardSnapshot.GapBits] |= 1 << (i % BoardSnapshot.GapBits);
            int unitN = Math.Min(80, Units.Count);
            for (int i = 0; i < unitN; i++)
            {
                Unit u = Units[i];
                s.Units.Add((int)u.Job);
                s.Units.Add(u.GradeIndex);
                s.Units.Add(BoardSnapshot.Quantize(u.X));
                s.Units.Add(BoardSnapshot.Quantize(u.Y));
                s.Uc.Add(u.Face | (m_shotUnits.Contains(u.Id) ? 4 : 0));
            }
            m_shotUnits.Clear();
            s.Fx.AddRange(m_fxLog);
            m_fxLog.Clear();
            Mob boss = null;
            foreach (Mob m in Mobs) if (m.Boss) { boss = m; break; }
            s.Wave = Wave;
            s.MobCount = Mobs.Count;
            s.UnitCount = Units.Count;
            int top = -1;
            foreach (Unit u in Units) top = Math.Max(top, u.GradeIndex);
            s.TopGrade = top;
            s.Gold = Gold;
            s.BossHp = boss != null ? boss.Hp / boss.MaxHp : -1;
            foreach (Job j in Opts.Deck) s.Deck.Add((int)j);
            s.Atk.AddRange(AttackCards);
            s.T = JsMath.Round(Time * 100) / 100;
            return s;
        }

        /// <summary>길 한 바퀴 표본 좌표 (고정이라 한 번만 계산)</summary>
        static readonly Vec2[] s_gapPoints = BuildGapPoints();
        static Vec2[] BuildGapPoints()
        {
            var pts = new Vec2[BoardSnapshot.GapSamples];
            for (int i = 0; i < pts.Length; i++) pts[i] = Board.PathAt(i / (double)BoardSnapshot.GapSamples);
            return pts;
        }

        readonly List<int> m_gapBuffer = new List<int>(BoardSnapshot.GapSamples);
        double[] m_rangeSq = new double[32];

        /// <summary>어떤 유닛 사거리에도 들지 않는 길 표본 번호 (화면의 빨간 구멍). 유닛이 없으면 빈 목록</summary>
        public List<int> PathGaps() => new List<int>(PathGaps(m_gapBuffer));

        /// <summary>PathGaps 를 output 에 채운다 (매 프레임 그리는 쪽은 자기 리스트를 넘겨 할당 없이)</summary>
        public List<int> PathGaps(List<int> output)
        {
            output.Clear();
            int n = Units.Count;
            if (n == 0) return output;
            if (m_rangeSq.Length < n) m_rangeSq = new double[Math.Max(n, m_rangeSq.Length * 2)];
            for (int k = 0; k < n; k++)
            {
                double r = UnitRange(Units[k]);
                m_rangeSq[k] = r * r; // 제곱 거리로 비교 (sqrt 없음)
            }
            for (int i = 0; i < s_gapPoints.Length; i++)
            {
                Vec2 p = s_gapPoints[i];
                bool covered = false;
                for (int k = 0; k < n && !covered; k++)
                {
                    double dx = Units[k].X - p.X, dy = Units[k].Y - p.Y;
                    covered = dx * dx + dy * dy <= m_rangeSq[k];
                }
                if (!covered) output.Add(i);
            }
            return output;
        }

        /// <summary>
        /// 상대 화면에 다시 그릴 이펙트 (양자화 좌표).
        /// [0 투사체, x1,y1,x2,y2, 직업, 등급] [1 폭발, x,y, 반지름, 색] [2 처치, x,y, 보스] [3 베기, x,y, 각도 0~255]
        /// [4 직선, x1,y1,x2,y2, 굵기, 색] [5 합성, x,y, 색] [6 소환, x,y, 색, 큼] [7 태초 타격, x,y]
        /// </summary>
        void LogFx(Effect e)
        {
            int[] f;
            switch (e.Type)
            {
                case EffectType.Proj: f = new[] { 0, Q(e.X1), Q(e.Y1), Q(e.X2), Q(e.Y2), (int)e.Job, e.Grade }; break;
                case EffectType.Splash: f = new[] { 1, Q(e.X), Q(e.Y), JsMath.RoundInt(e.R), (int)e.Color }; break;
                case EffectType.Pop: f = new[] { 2, Q(e.X), Q(e.Y), e.Boss ? 1 : 0 }; break;
                case EffectType.Slash: f = new[] { 3, Q(e.X), Q(e.Y), JsMath.RoundInt(e.Seed % Math.PI / Math.PI * 255) }; break;
                case EffectType.Beam: f = new[] { 4, Q(e.X1), Q(e.Y1), Q(e.X2), Q(e.Y2), JsMath.RoundInt(e.W), (int)e.Color }; break;
                case EffectType.Merge: f = new[] { 5, Q(e.X), Q(e.Y), (int)e.Color }; break;
                case EffectType.Summon: f = new[] { 6, Q(e.X), Q(e.Y), (int)e.Color, e.Big ? 1 : 0 }; break;
                case EffectType.Burst: f = new[] { 7, Q(e.X), Q(e.Y) }; break;
                default: return;
            }
            m_fxLog.Add(f);
            if (m_fxLog.Count > 60) m_fxLog.RemoveAt(0); // 오래된 것부터 버린다 (스냅샷 크기 제한)
        }

        static int Q(double v) => BoardSnapshot.Quantize(v);
    }
}
