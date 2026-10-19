using System.Collections.Generic;

namespace DefenseBattle.Core
{
    /// <summary>
    /// 경제 상태 = 저장본 중 서버가 권위를 갖는 부분 (rules.ts Econ). 온라인에선 서버(UGS Cloud Code)가 계산하고
    /// 기기는 받은 값을 읽기만 한다. 오프라인·테스트에선 EconRules 가 이 객체에 직접 적용한다.
    /// </summary>
    public sealed class EconState
    {
        public int Crystals;
        public Dictionary<ItemKey, int> Items = new Dictionary<ItemKey, int> { { ItemKey.GachaTicket, 0 }, { ItemKey.GoldBooster, 0 } };
        /// <summary>레벨업용으로 모은 카드 (해금에 쓴 1장 제외)</summary>
        public Dictionary<Job, int> Cards = new Dictionary<Job, int>();
        /// <summary>해금한 유닛과 영구 레벨 (없으면 미해금)</summary>
        public Dictionary<Job, int> UnitLevels = new Dictionary<Job, int>();
        public Dictionary<ResearchKey, int> Research = new Dictionary<ResearchKey, int>();
        /// <summary>상자 칸 4개 (null = 빈 칸)</summary>
        public ChestSlot[] Chests = new ChestSlot[4];
        /// <summary>다음 승리 상자 순서 번호 (Chests.Cycle)</summary>
        public int ChestCycle;
        /// <summary>받은 트로피 로드 보상 (트로피 값)</summary>
        public List<int> RoadClaimed = new List<int>();
        /// <summary>캠페인 스테이지별 별 개수 ("1-3": 2)</summary>
        public Dictionary<string, int> Stages = new Dictionary<string, int>();
        /// <summary>한 번만 받는 것들 (1회 상품 id, "stars:1:0", "quest:2026-10-19:summon30" …)</summary>
        public HashSet<string> OneTime = new HashSet<string>();
        /// <summary>날짜별 횟수 ("adTicket:2026-10-19" → 3)</summary>
        public Dictionary<string, int> Counters = new Dictionary<string, int>();
        /// <summary>광고 2배 대상 직전 판 보상 (없으면 null)</summary>
        public LastReward LastReward;
        public MonthlyState Monthly = new MonthlyState();
        public SeasonState Season = new SeasonState();
        public HashSet<string> Achievements = new HashSet<string>();
        public bool AdRemoved;
        /// <summary>출전 덱 (해금 시 빈 칸이 있으면 자동으로 들어간다). 원본은 기기 저장본에 있었지만 해금 여부를 서버가 확인하도록 여기로</summary>
        public List<Job> Deck = new List<Job>();
        /// <summary>PvP 몹 카드 덱 4장</summary>
        public List<AttackKey> AttackDeck = new List<AttackKey>(Attacks.Starter);
        public PvpStats Pvp = new PvpStats();
        /// <summary>이미 처리한 결제·대전 id (같은 것을 두 번 지급하지 않게, 최근 MaxRecentIds 개)</summary>
        public List<string> RecentIds = new List<string>();

        public const int MaxRecentIds = 500;

        /// <summary>새 계정 (meta.ts freshMeta 의 경제 부분): 기본 3종 해금, 나무 상자 1개</summary>
        public static EconState Fresh(string seasonId)
        {
            var e = new EconState { Season = SeasonState.Fresh(seasonId) };
            foreach (Job j in Units.Starter)
            {
                e.UnitLevels[j] = 1;
                e.Deck.Add(j);
            }
            foreach (ResearchDef r in Core.Research.All) e.Research[r.Key] = 0;
            e.Chests[0] = new ChestSlot { Kind = ChestKind.Wood };
            return e;
        }

        public int Item(ItemKey k) => Items.TryGetValue(k, out int n) ? n : 0;
        public int CardCount(Job j) => Cards.TryGetValue(j, out int n) ? n : 0;
        /// <summary>유닛 레벨 (미해금 0)</summary>
        public int LevelOf(Job j) => UnitLevels.TryGetValue(j, out int n) ? n : 0;
        public bool Owns(Job j) => LevelOf(j) > 0;
        public int ResearchLevel(ResearchKey k) => Research.TryGetValue(k, out int n) ? n : 0;
        public int StarsOf(string stageId) => Stages.TryGetValue(stageId, out int n) ? n : 0;

        /// <summary>이 id 를 처음 처리하면 true (기록해 둔다), 이미 처리했으면 false</summary>
        public bool MarkProcessed(string id)
        {
            if (string.IsNullOrEmpty(id) || RecentIds.Contains(id)) return false;
            RecentIds.Add(id);
            if (RecentIds.Count > MaxRecentIds) RecentIds.RemoveRange(0, RecentIds.Count - MaxRecentIds);
            return true;
        }
    }
}
