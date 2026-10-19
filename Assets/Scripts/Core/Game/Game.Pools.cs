using System.Collections.Generic;

namespace DefenseBattle.Core
{
    // 풀링·재사용 버퍼 (GC 할당 0 을 목표로).
    // 전투 한 프레임에 몹 100마리 × 유닛 수십 개가 돌고 이펙트가 초당 수백 개 생긴다.
    // 매번 new 하면 모바일에서 GC 가 주기적으로 프레임을 끊으므로 객체·리스트를 모두 재사용한다.
    public sealed partial class Game
    {
        // 몹은 한 판에 동시 최대 100마리(+분열·졸개), 이펙트는 MaxEffects(240) + 떨어지는 몹 경고
        readonly ObjectPool<Mob> m_mobPool = new ObjectPool<Mob>(() => new Mob(), 128, 512);
        readonly ObjectPool<Effect> m_fxPool = new ObjectPool<Effect>(() => new Effect(), 256, 1024);
        readonly ObjectPool<Burn> m_burnPool = new ObjectPool<Burn>(() => new Burn(), 24, 64);

        /// <summary>
        /// 이번 프레임에 죽거나 치워진 몹. 바로 풀에 돌려보내지 않고 다음 Update 시작에 돌려보낸다
        /// (같은 프레임에 아직 참조 중인 곳 - 수호 주술사 목록 등 - 이 있어서)
        /// </summary>
        readonly List<Mob> m_graveyard = new List<Mob>(64);

        // 재사용 버퍼 (프레임마다 Clear 후 사용)
        readonly List<Mob> m_mobBuffer = new List<Mob>(160);
        readonly List<Mob> m_candidates = new List<Mob>(160);
        readonly List<double> m_candidateKeys = new List<double>(160);
        readonly HashSet<Mob> m_stormDone = new HashSet<Mob>();

        Mob NewMob()
        {
            Mob m = m_mobPool.Get();
            m.ResetState();
            return m;
        }

        /// <summary>죽은 몹을 목록에서 빼고 무덤으로 (순서 유지, 할당 없음 - RemoveAll 과 같은 결과)</summary>
        void CompactMobs()
        {
            int w = 0;
            for (int r = 0; r < Mobs.Count; r++)
            {
                Mob m = Mobs[r];
                if (m.Dead) m_graveyard.Add(m);
                else Mobs[w++] = m;
            }
            if (w < Mobs.Count) Mobs.RemoveRange(w, Mobs.Count - w);
        }

        void ReleaseGraveyard()
        {
            for (int i = 0; i < m_graveyard.Count; i++) m_mobPool.Release(m_graveyard[i]);
            m_graveyard.Clear();
        }

        void ReleaseAllMobs()
        {
            foreach (Mob m in Mobs) m_mobPool.Release(m);
            Mobs.Clear();
            ReleaseGraveyard();
        }

        // ---------------- 이펙트 ----------------
        Effect NewFx(EffectType type, double ttl)
        {
            Effect e = m_fxPool.Get();
            e.Clear();
            e.Type = type;
            e.Ttl = ttl;
            return e;
        }

        Effect FxAt(EffectType type, double x, double y, double ttl)
        {
            Effect e = NewFx(type, ttl);
            e.X = x;
            e.Y = y;
            return e;
        }

        Effect FxSplash(double x, double y, double ttl, double r, uint color)
        {
            Effect e = FxAt(EffectType.Splash, x, y, ttl);
            e.R = r;
            e.Color = color;
            return e;
        }

        Effect FxLine(EffectType type, double x1, double y1, double x2, double y2, double dur)
        {
            Effect e = NewFx(type, dur);
            e.X1 = x1;
            e.Y1 = y1;
            e.X2 = x2;
            e.Y2 = y2;
            e.Dur = dur;
            return e;
        }

        Effect FxBeam(double x1, double y1, double x2, double y2, double dur, uint color, double w)
        {
            Effect e = FxLine(EffectType.Beam, x1, y1, x2, y2, dur);
            e.Color = color;
            e.W = w;
            return e;
        }

        Effect FxProj(Unit u, double x2, double y2, double dur)
        {
            Effect e = FxLine(EffectType.Proj, u.X, u.Y - 8, x2, y2, dur);
            e.Job = u.Job;
            e.Grade = u.GradeIndex;
            return e;
        }

        /// <summary>베기 이펙트 (각도용 난수를 하나 쓴다 - 원본과 난수 순서를 맞추려고 생성 시점에 뽑는다)</summary>
        Effect FxSlash(Mob at, double ttl)
        {
            Effect e = FxAt(EffectType.Slash, at.X, at.Y, ttl);
            e.Seed = m_rand.Next() * System.Math.PI;
            return e;
        }

        /// <summary>상한 안이면 목록에 넣는다. 못 넣은 이펙트는 PvP 기록만 남기고 바로 풀로 돌려보낸다</summary>
        void AddEffect(Effect e)
        {
            bool kept = Effects.Count < MaxEffects;
            if (kept) Effects.Add(e);
            if (IsPvp) LogFx(e);
            if (!kept) m_fxPool.Release(e);
        }

        /// <summary>상한과 상관없이 넣는다 (합성 연출·떨어지는 몹 경고)</summary>
        void AddEffectAlways(Effect e)
        {
            Effects.Add(e);
        }

        void UpdateEffects(double dt)
        {
            int w = 0;
            for (int r = 0; r < Effects.Count; r++)
            {
                Effect e = Effects[r];
                e.Ttl -= dt;
                if (e.Ttl > 0) Effects[w++] = e;
                else m_fxPool.Release(e);
            }
            if (w < Effects.Count) Effects.RemoveRange(w, Effects.Count - w);
        }

        void ReleaseAllEffects()
        {
            foreach (Effect e in Effects) m_fxPool.Release(e);
            Effects.Clear();
        }

        // ---------------- 불 장판 ----------------
        void AddBurn(double x, double y, double r, double dps, double until)
        {
            Burn b = m_burnPool.Get();
            b.X = x;
            b.Y = y;
            b.R = r;
            b.Dps = dps;
            b.Until = until;
            Burns.Add(b);
        }

        /// <summary>끝난 장판 정리 (순서 유지)</summary>
        void CompactBurns()
        {
            int w = 0;
            for (int r = 0; r < Burns.Count; r++)
            {
                Burn b = Burns[r];
                if (b.Until > Time) Burns[w++] = b;
                else m_burnPool.Release(b);
            }
            if (w < Burns.Count) Burns.RemoveRange(w, Burns.Count - w);
        }

        void ReleaseAllBurns()
        {
            foreach (Burn b in Burns) m_burnPool.Release(b);
            Burns.Clear();
        }
    }
}
