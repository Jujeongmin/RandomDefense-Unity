namespace DefenseBattle.Core
{
    /// <summary>PvP 몹 카드 정의 (config.ts AttackDef). 0 인 능력 값은 그 능력이 없다는 뜻</summary>
    public sealed class AttackDef
    {
        public AttackKey Key;
        /// <summary>원본 문자열 id ('swarm' …)</summary>
        public string Id;
        /// <summary>이름 (한국어 원문 키)</summary>
        public string Kr;
        /// <summary>카드 한 줄 설명 (한국어 원문 키)</summary>
        public string Desc;
        /// <summary>그림·상성 종족</summary>
        public Race Race;
        /// <summary>종족 안 그림 번호</summary>
        public int Variant;
        public int Cost;
        public int Count;
        /// <summary>보낸 쪽 웨이브 기본 체력 대비</summary>
        public double HpMult;
        /// <summary>이동 속도 배율</summary>
        public double Speed;
        /// <summary>그리기 크기 배율</summary>
        public double Size;
        /// <summary>쿨타임 (게임 시간 초)</summary>
        public double Cooldown;
        /// <summary>이 카드를 잘 막는 유닛 (봇 판단·카드 설명)</summary>
        public Job[] Counters;
        /// <summary>이 유닛들의 피해 -60% (없으면 빈 배열)</summary>
        public Job[] Resist = new Job[0];
        /// <summary>초당 주변 몹 회복 (최대 체력 비율)</summary>
        public double Heal;
        /// <summary>죽으면 작은 몹 n 마리로 갈라짐</summary>
        public int Split;
        /// <summary>한 번 되살아남 (체력 50%)</summary>
        public bool Revive;
        /// <summary>4초마다 졸개 n 마리 소환</summary>
        public int Spawn;
        /// <summary>4초마다 n초 동안 숨음 (직접 노릴 수 없음 - 범위 공격엔 맞음)</summary>
        public double Stealth;
        /// <summary>체력 50% 아래면 이동 속도 배율</summary>
        public double Enrage;
        /// <summary>3초마다 길을 따라 n px 순간이동</summary>
        public double Blink;
        /// <summary>주변(130px) 다른 몹이 받는 피해 감소 비율</summary>
        public double Guard;
        /// <summary>5초 뒤 한 번 복제 (복제는 2대까지)</summary>
        public bool Breed;
        /// <summary>처치 골드 (없으면 기본 Economy.KillGold)</summary>
        public int? Gold;

        public bool Resists(Job job) => System.Array.IndexOf(Resist, job) >= 0;
    }
}
