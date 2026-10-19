using System;
using System.Collections.Generic;

namespace DefenseBattle.Core
{
    /// <summary>
    /// 캠페인 (챕터 x 스테이지)와 판 결과 규칙 (config.ts CHAPTERS ~ crystalReward).
    /// 짧은 판(5~12분) 단위 진행. 첫 클리어 보상·유닛 해금·별 3개 도전으로 반복 플레이 유도.
    /// </summary>
    public static class Campaign
    {
        public static readonly ChapterDef[] Chapters =
        {
            new ChapterDef { N = 1, Kr = "초원의 관문", Waves = 10, Desc = "기본기를 익히는 첫 전장" },
            new ChapterDef { N = 2, Kr = "어둠의 숲", Waves = 15, Desc = "빠른 몹과 탱커가 몰려온다" },
            new ChapterDef { N = 3, Kr = "망자의 성채", Waves = 20, Desc = "연구와 유닛 성장이 필요한 최종 전장" },
        };
        public const int StagesPerChapter = 10;
        // 난이도 곡선: 챕터 내에서 점진 상승, 챕터 시작 시 웨이브 수 증가로 한 번 더 상승
        public static readonly double[] StageHpBase = { 0.55, 1.4, 2.6 };
        public static readonly double[] StageHpStep = { 0.08, 0.14, 0.16 };

        public static readonly StageDef[] Stages = BuildStages();
        static readonly Dictionary<string, StageDef> s_byId = BuildIndex();

        static StageDef[] BuildStages()
        {
            var list = new List<StageDef>();
            for (int ci = 0; ci < Chapters.Length; ci++)
            {
                for (int si = 0; si < StagesPerChapter; si++)
                {
                    string id = Chapters[ci].N + "-" + (si + 1);
                    int index = ci * StagesPerChapter + si;
                    Job? unlocks = null;
                    foreach (UnitDef u in Units.All) if (u.UnlockStage == id) { unlocks = u.Job; break; }
                    list.Add(new StageDef
                    {
                        Id = id,
                        Chapter = Chapters[ci].N,
                        Index = index,
                        Waves = Chapters[ci].Waves,
                        HpMult = JsMath.Round((StageHpBase[ci] + si * StageHpStep[ci]) * 100) / 100,
                        // 1-1~1-3 은 종족 1종으로 상성 학습, 이후 전 종족
                        Races = index == 0 ? new[] { Race.Orc } : index == 1 ? new[] { Race.Undead } : index == 2 ? new[] { Race.Troll } : null,
                        FirstReward = 40 + index * 12,
                        ReplayReward = 10 + index * 3,
                        Unlocks = unlocks, // 트로피 로드에서도 열린다 (먼저 닿는 쪽)
                    });
                }
            }
            return list.ToArray();
        }

        static Dictionary<string, StageDef> BuildIndex()
        {
            var d = new Dictionary<string, StageDef>();
            foreach (StageDef s in Stages) d[s.Id] = s;
            return d;
        }

        /// <summary>id 로 스테이지 찾기 (없으면 null)</summary>
        public static StageDef Get(string id) => id != null && s_byId.TryGetValue(id, out StageDef s) ? s : null;

        /// <summary>챕터 별 개수 목표 → 보석 보상</summary>
        public static readonly int[] ChapterStarGoals = { 10, 20, 30 };
        public static int ChapterStarReward(int goalIdx) => new[] { 100, 200, 400 }[goalIdx];

        // 별 기준: 판 도중 필드 몹 최대치 (적을수록 안정적인 방어)
        public const int StarPeakThree = 40;
        public const int StarPeakTwo = 70;
        public static int StarsFor(int peakMobs) => peakMobs <= StarPeakThree ? 3 : peakMobs <= StarPeakTwo ? 2 : 1;

        /// <summary>
        /// 기능 해금 스테이지 - 로비 메뉴는 해금 전엔 보이지 않고, 해금되면 NEW 로 하나씩 등장한다.
        /// 1-1 튜토리얼을 마치면 바로 대전·덱, 이후 캠페인을 하며 연구·퀘스트·무한 모드가 열린다.
        /// </summary>
        public static readonly Dictionary<string, string> FeatureUnlock = new Dictionary<string, string>
        {
            { "units", "1-1" }, { "research", "1-2" }, { "quests", "1-3" }, { "pvp", "1-1" }, { "endless", "1-10" },
        };
        public static readonly Dictionary<string, string> FeatureKr = new Dictionary<string, string>
        {
            { "units", "유닛" }, { "research", "연구" }, { "quests", "퀘스트" }, { "pvp", "PvP 대전" }, { "endless", "무한 모드" },
        };

        /// <summary>무한 모드 종료 보석 (config.ts crystalReward)</summary>
        public static int CrystalReward(int wave)
        {
            int c = wave * 2;
            c += (int)Math.Floor(wave / 10.0) * 10;
            return c;
        }
    }
}
