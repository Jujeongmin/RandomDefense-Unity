namespace DefenseBattle.Core
{
    /// <summary>
    /// 판 위의 몹 한 마리 (types.ts Mob). 엔진이 풀에서 재사용한다 -
    /// 화면 쪽은 객체 참조가 아니라 Id 로 스프라이트를 짝지을 것.
    /// </summary>
    public sealed class Mob
    {
        public int Id;
        public Race Race;
        /// <summary>종족 안의 그림 변형</summary>
        public int Variant;
        public MobKind Kind;
        /// <summary>몹 카드 번호 (Attacks.All 번호) - 특수 능력. 웨이브 몹이면 -1</summary>
        public int Card = -1;
        /// <summary>회복·소환 주기 타이머</summary>
        public double Tick;
        /// <summary>나온 뒤 지난 시간 (은신·점멸·번식 주기)</summary>
        public double Age;
        /// <summary>그림자 도적: 숨은 중 (직접 노릴 수 없음)</summary>
        public bool Hidden;
        /// <summary>광폭 오크: 질주 중</summary>
        public bool Enraged;
        /// <summary>점멸 마녀: 다음 순간이동까지 (처음엔 null → 3초)</summary>
        public double? BlinkT;
        /// <summary>번식 유령: 몇 번째 복제인지 (0 = 원본)</summary>
        public int Gen;
        /// <summary>번식 유령: 이미 복제함</summary>
        public bool Bred;
        /// <summary>반투명 (유령)</summary>
        public bool Ghost;
        /// <summary>처치 골드 덮어쓰기 (분열·졸개·되살아난 망령). null = 기본</summary>
        public int? Gold;
        /// <summary>사냥꾼 표식 (받는 피해 +) 끝나는 게임 시간</summary>
        public double MarkUntil;
        /// <summary>중독 상태 (항상 있음 - Until 이 지금보다 크면 중독 중). 몹마다 한 번만 만든다</summary>
        public readonly Poison Poison = new Poison();
        /// <summary>성직자 정화 (회복·부활 막음) 끝나는 게임 시간</summary>
        public double PurifyUntil;
        /// <summary>사신에게 처치됨 → 부활 못 함</summary>
        public bool NoRevive;
        public double X;
        public double Y;
        /// <summary>다음 목표 모서리 번호</summary>
        public int Seg;
        public double Pause;
        public double Hp;
        public double MaxHp;
        public bool Boss;
        /// <summary>그리기 크기 (판정에는 쓰지 않음)</summary>
        public double Size;
        public bool Dead;
        public double HitFlash;
        /// <summary>종류별 이동 속도 배율</summary>
        public double SpeedMult;
        /// <summary>둔화 남은 시간(초)</summary>
        public double Slow;
        /// <summary>둔화 비율 (0~1)</summary>
        public double SlowPct;
        /// <summary>스프라이트 방향 행: 0=하 1=좌 2=우 3=상</summary>
        public int Dir;

        public bool IsCard => Card >= 0;

        /// <summary>풀에서 꺼낸 뒤 상태 초기화 (Id·위치·체력 등은 엔진이 바로 채운다)</summary>
        public void ResetState()
        {
            Card = -1;
            Tick = Age = 0;
            Hidden = Enraged = Bred = Ghost = NoRevive = Boss = Dead = false;
            BlinkT = null;
            Gen = 0;
            Gold = null;
            MarkUntil = PurifyUntil = 0;
            Poison.Dps = Poison.Until = 0;
            Poison.Stacks = 0;
            X = Y = Pause = Hp = MaxHp = Size = HitFlash = Slow = SlowPct = 0;
            SpeedMult = 1;
            Seg = Dir = Variant = 0;
            Kind = MobKind.Normal;
        }
    }
}
