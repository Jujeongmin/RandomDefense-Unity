namespace DefenseBattle.Core
{
    /// <summary>
    /// 시각 전용 이펙트 (types.ts Effect). 종류마다 쓰는 필드가 다르다:
    /// Proj(X1..Y2, Job, Grade, Dur) Pop(X, Y, Boss, Seed) Dmg(X, Y, Value, Crit) Gold(X, Y, Value)
    /// Splash(X, Y, R, Color) Slash(X, Y, Seed) Merge(X, Y, Color) Summon(X, Y, Color, Big) Burst(X, Y, Seed)
    /// Drop(X, Y, Dur, Card, Seed) Beam(X1..Y2, Dur, Color, W)
    /// 엔진이 풀에서 재사용한다 - 화면 쪽은 프레임을 넘겨 참조를 들고 있지 말 것 (매 프레임 Game.Effects 를 다시 읽는다).
    /// </summary>
    public sealed class Effect
    {
        public EffectType Type;
        public double X;
        public double Y;
        public double X1;
        public double Y1;
        public double X2;
        public double Y2;
        /// <summary>남은 시간(초)</summary>
        public double Ttl;
        /// <summary>전체 시간(초)</summary>
        public double Dur;
        public Job Job;
        public int Grade;
        public double Value;
        public bool Crit;
        public bool Boss;
        public bool Big;
        public double R;
        /// <summary>0xRRGGBB</summary>
        public uint Color;
        public double Seed;
        public int Card;
        /// <summary>선 굵기 (Beam)</summary>
        public double W;

        /// <summary>풀에서 꺼낸 뒤 모든 필드를 기본값으로 (이전 사용 흔적 제거)</summary>
        public void Clear()
        {
            Type = EffectType.Proj;
            X = Y = X1 = Y1 = X2 = Y2 = Ttl = Dur = Value = R = Seed = W = 0;
            Job = Job.Archer;
            Grade = 0;
            Crit = Boss = Big = false;
            Color = 0;
            Card = 0;
        }
    }
}
