namespace DefenseBattle.Core
{
    /// <summary>
    /// 연구소 (config.ts RESEARCH, researchCost, RESEARCH_PER).
    /// 연구 성장이 지수 HP 벽을 넘게 해주는 핵심 진행 요소.
    /// </summary>
    public static class Research
    {
        public static readonly ResearchDef[] All =
        {
            new ResearchDef { Key = ResearchKey.Atk,       Id = "atk",       Kr = "공격력 연구",   Max = 20, Desc = "레벨 당 공격력 +4%",          Per = 0.04 },
            new ResearchDef { Key = ResearchKey.StartGold, Id = "startGold", Kr = "시작 골드 연구", Max = 20, Desc = "레벨 당 시작 골드 +10",        Per = 10 },
            new ResearchDef { Key = ResearchKey.GoldGain,  Id = "goldGain",  Kr = "골드 획득 연구", Max = 20, Desc = "몬스터 처치 골드 +4%",         Per = 0.04 },
            new ResearchDef { Key = ResearchKey.Rare,      Id = "rare",      Kr = "희귀 소환 연구", Max = 10, Desc = "전설 이상 등장 확률 소폭 증가", Per = 0.5 },
            new ResearchDef { Key = ResearchKey.Boss,      Id = "boss",      Kr = "보스 피해 연구", Max = 20, Desc = "보스 대상 공격력 +4%",          Per = 0.04 },
        };

        public static ResearchDef Get(ResearchKey key) => All[(int)key];

        /// <summary>레벨당 효과 크기 (엔진이 참조하는 단일 소스)</summary>
        public static double Per(ResearchKey key) => All[(int)key].Per;

        public static int Cost(int level) => 5 + level * 5;

        public static bool TryParse(string id, out ResearchKey key)
        {
            foreach (ResearchDef r in All)
            {
                if (r.Id == id) { key = r.Key; return true; }
            }
            key = default;
            return false;
        }
    }
}
