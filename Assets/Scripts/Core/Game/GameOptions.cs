using System.Collections.Generic;

namespace DefenseBattle.Core
{
    /// <summary>게임 모드/옵션 (types.ts GameOptions) - solo(일반)와 pvp(실시간 1:1)가 같은 엔진을 공유한다</summary>
    public sealed class GameOptions
    {
        public GameMode Mode = GameMode.Solo;
        /// <summary>웨이브 구성(종족) 시드. PvP 에서는 두 플레이어가 같은 시드를 쓴다. 0 이면 무작위</summary>
        public uint Seed;
        /// <summary>그 밖의 무작위(소환·치명타·이펙트) 시드. 0 이면 무작위 (테스트·시뮬에서 고정용)</summary>
        public int RandomSeed;
        /// <summary>연구소 효과 적용 여부</summary>
        public bool UseResearch = true;
        /// <summary>연구 레벨 (원본은 비우면 내 저장본 값 - 여기선 호출하는 쪽이 넣는다). 없는 키는 0</summary>
        public Dictionary<ResearchKey, int> Research = new Dictionary<ResearchKey, int>();
        /// <summary>시작 골드 배수 (골드 부스터)</summary>
        public double StartGoldMult = 1;
        /// <summary>퀘스트/업적/최고기록 이벤트를 낼지 (AI 상대의 헤드리스 게임은 false)</summary>
        public bool Track = true;
        /// <summary>출전 덱 - 소환 시 이 중에서 랜덤</summary>
        public List<Job> Deck = new List<Job>(Units.Starter);
        /// <summary>덱에 넣을 수 있는 최대 유닛 수. 덱이 이보다 적으면 소환이 비싸진다. 0 = 페널티 없음</summary>
        public int DeckMin;
        /// <summary>PvP 공격 덱 (몹 카드 4장)</summary>
        public List<AttackKey> AttackDeck = new List<AttackKey>(Attacks.Starter);
        /// <summary>유닛 영구 레벨 (컬렉션 성장). 없는 키는 1</summary>
        public Dictionary<Job, int> UnitLevels = new Dictionary<Job, int>();
        /// <summary>웨이브 수 (0 = 기본 50, PvP 는 무한)</summary>
        public int WaveCount;
        /// <summary>몹 체력 배수 (스테이지 난이도)</summary>
        public double HpMult = 1;
        /// <summary>유닛 공격력 배수 (봇 핸디캡)</summary>
        public double AtkMult = 1;
        /// <summary>등장 종족 제한 (null 이거나 비면 전체)</summary>
        public Race[] Races;
        /// <summary>캠페인 스테이지 id (null 이면 무한/PvP)</summary>
        public string StageId;

        /// <summary>스테이지 정의로 옵션 채우기 (progress.ts stageOptions 의 판 규칙 부분)</summary>
        public static GameOptions ForStage(StageDef st)
        {
            return new GameOptions { StageId = st.Id, WaveCount = st.Waves, HpMult = st.HpMult, Races = st.Races };
        }
    }
}
