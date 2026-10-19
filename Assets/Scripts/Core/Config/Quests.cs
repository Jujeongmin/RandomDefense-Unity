namespace DefenseBattle.Core
{
    /// <summary>일일 퀘스트와 업적 (config.ts DAILY_QUESTS, ACHIEVEMENTS)</summary>
    public static class Quests
    {
        public static readonly DailyQuestDef[] Daily =
        {
            new DailyQuestDef { Key = "summon30", Kr = "유닛 30회 소환",      Goal = 30,  Stat = QuestStat.Summons,  Reward = 20 },
            new DailyQuestDef { Key = "kill200",  Kr = "몬스터 200마리 처치", Goal = 200, Stat = QuestStat.Kills,    Reward = 20 },
            new DailyQuestDef { Key = "wave20",   Kr = "20웨이브 도달",       Goal = 20,  Stat = QuestStat.BestWave, Reward = 30 },
            new DailyQuestDef { Key = "sell20",   Kr = "유닛 20회 판매",      Goal = 20,  Stat = QuestStat.Sells,    Reward = 15 },
        };

        public static readonly AchievementDef[] Achievements =
        {
            new AchievementDef { Key = "firstLegendary", Kr = "전설 유닛 최초 획득", Reward = 50 },
            new AchievementDef { Key = "firstMythic",    Kr = "신화 유닛 최초 획득", Reward = 100 },
            new AchievementDef { Key = "firstEternal",   Kr = "태초 유닛 최초 획득", Reward = 300 },
            new AchievementDef { Key = "clear10",        Kr = "10웨이브 클리어",     Reward = 30 },
            new AchievementDef { Key = "clear20",        Kr = "20웨이브 클리어",     Reward = 60 },
            new AchievementDef { Key = "clear30",        Kr = "30웨이브 클리어",     Reward = 100 },
            new AchievementDef { Key = "clear40",        Kr = "40웨이브 클리어",     Reward = 200 },
            new AchievementDef { Key = "clear50",        Kr = "50웨이브 클리어",     Reward = 500 },
        };

        public static DailyQuestDef GetDaily(string key)
        {
            foreach (DailyQuestDef q in Daily) if (q.Key == key) return q;
            return null;
        }

        public static AchievementDef GetAchievement(string key)
        {
            foreach (AchievementDef a in Achievements) if (a.Key == key) return a;
            return null;
        }
    }
}
