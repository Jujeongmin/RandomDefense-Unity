using System;
using System.Collections.Generic;

namespace DefenseBattle.Core
{
    /// <summary>유닛 32종 표와 유닛 관련 수치 (config.ts UNITS ~ perkMult, DMG_MULT)</summary>
    public static class Units
    {
        static UnitDef U(Job job, string id, string kr, BaseJob b, string sheet, double cd, double range, double atk,
            string role, Rarity rarity, uint tint, Race? fav = null, string stage = null, int? crystals = null)
        {
            return new UnitDef
            {
                Job = job, Id = id, Kr = kr, Base = b, Sheet = sheet, Cooldown = cd, RangeMult = range, AtkMult = atk,
                Role = role, Rarity = rarity, Tint = tint, Fav = fav, UnlockStage = stage, UnlockCrystals = crystals,
            };
        }

        // 등급은 '얻기 어려움'이다 (같은 레벨이면 비슷한 전력). 상자·뽑기에서 나오는 확률과 레벨업 카드 수가 다르다.
        /// <summary>config.ts UNITS - 순서가 Job enum 과 같다</summary>
        public static readonly UnitDef[] All =
        {
            // ---- 일반 ----
            U(Job.Archer,   "archer",   "궁수",     BaseJob.Archer,  "Archer",   1.0,  1.0,  1.0,  "다중 사격", Rarity.Common, 0x3fae4a, Race.Orc),
            U(Job.Wizard,   "wizard",   "마법사",   BaseJob.Wizard,  "Wizard",   1.0,  1.0,  1.0,  "범위 폭발", Rarity.Common, 0x8e5bd8, Race.Undead),
            U(Job.Warrior,  "warrior",  "전사",     BaseJob.Warrior, "Warrior",  1.0,  1.0,  1.0,  "둔화", Rarity.Common, 0xc9a13b, Race.Troll),
            U(Job.Spearman, "spearman", "창병",     BaseJob.Warrior, "Spearman", 1.05, 1.05, 0.95, "관통", Rarity.Common, 0xb8864b, Race.Troll, null, 200),
            U(Job.Slinger,  "slinger",  "투석병",   BaseJob.Archer,  "Slinger",  1.1,  1.0,  0.95, "넉백", Rarity.Common, 0x9a8a6a, Race.Orc, null, 200),
            U(Job.Monk,     "monk",     "수도승",   BaseJob.Warrior, "Monk",     0.6,  0.9,  0.55, "연타", Rarity.Common, 0xe0a050, Race.Troll, null, 200),
            U(Job.Hunter,   "hunter",   "사냥꾼",   BaseJob.Archer,  "Hunter",   1.0,  1.1,  0.9,  "표식", Rarity.Common, 0x6a9a3a, Race.Orc, null, 200),
            U(Job.Acolyte,  "acolyte",  "견습 사제", BaseJob.Wizard, "Acolyte",  1.0,  1.0,  0.9,  "언데드 특화", Rarity.Common, 0xf0e0a0, null, null, 200),
            // ---- 희귀 ----
            U(Job.Ranger,   "ranger",   "레인저",   BaseJob.Archer,  "Ranger",   1.25, 1.3,  1.15, "저격·치명타", Rarity.Rare, 0x3fa7c9, Race.Troll, "1-3", 300),
            U(Job.Frost,    "frost",    "빙결술사", BaseJob.Wizard,  "Frost",    1.1,  1.0,  0.7,  "범위 빙결", Rarity.Rare, 0x7fe0ff, null, "1-6", 500),
            U(Job.Poisoner, "poisoner", "독술사",   BaseJob.Wizard,  "Poisoner", 1.1,  1.0,  0.7,  "중독", Rarity.Rare, 0x7ac943, Race.Troll, null, 500),
            U(Job.Storm,    "storm",    "번개술사", BaseJob.Wizard,  "Storm",    1.2,  1.0,  0.85, "연쇄 번개", Rarity.Rare, 0x7fb0ff, Race.Orc, null, 500),
            U(Job.Crossbow, "crossbow", "석궁병",   BaseJob.Archer,  "Crossbow", 1.3,  1.15, 1.1,  "일직선 관통", Rarity.Rare, 0x8a6a4a, Race.Troll, null, 500),
            U(Job.Knight,   "knight",   "기사",     BaseJob.Warrior, "Knight",   1.1,  0.95, 1.0,  "단단한 몹 특화", Rarity.Rare, 0xa0b0c0, Race.Orc, null, 500),
            U(Job.Berserker, "berserker", "광전사", BaseJob.Warrior, "Berserker", 0.9, 0.9,  0.85, "몹 많을수록 강함", Rarity.Rare, 0xd04a3a, Race.Troll, null, 500),
            U(Job.Merchant, "merchant", "상인",     BaseJob.Archer,  "Merchant", 1.0,  1.0,  0.65, "처치 골드 +", Rarity.Rare, 0xffcf3f, null, null, 500),
            U(Job.Drummer,  "drummer",  "북치기",   BaseJob.Warrior, "Drummer",  1.1,  0.95, 0.6,  "아군 공속 +", Rarity.Rare, 0xe07a3a, null, null, 500),
            U(Job.Houndmaster, "houndmaster", "조련사", BaseJob.Archer, "Houndmaster", 0.9, 1.0, 0.85, "빠른 몹 특화", Rarity.Rare, 0x9a6a3a, Race.Orc, null, 500),
            // ---- 영웅 ----
            U(Job.Paladin,  "paladin",  "성기사",   BaseJob.Warrior, "Paladin",  1.1,  1.0,  1.0,  "보스 특화", Rarity.Epic, 0xffd54f, Race.Undead, "1-10", 800),
            U(Job.Bomber,   "bomber",   "폭격수",   BaseJob.Wizard,  "Bomber",   1.7,  1.05, 1.25, "대형 폭발", Rarity.Epic, 0xff7a3d, Race.Orc, "2-5", 1200),
            U(Job.Assassin, "assassin", "암살자",   BaseJob.Warrior, "Assassin", 0.7,  0.9,  0.8,  "처형", Rarity.Epic, 0xa35bd8, Race.Orc, "3-1", 1800),
            U(Job.Pyro,     "pyro",     "화염술사", BaseJob.Wizard,  "Pyro",     1.3,  1.0,  0.85, "불 장판", Rarity.Epic, 0xff5a2a, Race.Troll, null, 1500),
            U(Job.Reaper,   "reaper",   "사신",     BaseJob.Warrior, "Reaper",   1.0,  1.0,  0.95, "처치 시 강화", Rarity.Epic, 0x5a4a7a, Race.Undead, null, 1500),
            U(Job.Cleric,   "cleric",   "성직자",   BaseJob.Wizard,  "Cleric",   1.0,  1.0,  0.85, "회복·부활 차단", Rarity.Epic, 0xfff0c0, Race.Undead, null, 1500),
            U(Job.Cannoneer, "cannoneer", "대포병", BaseJob.Wizard,  "Cannoneer", 2.2, 1.1,  1.9,  "강타·기절", Rarity.Epic, 0x6a6a6a, Race.Troll, null, 1500),
            U(Job.Chrono,   "chrono",   "시간술사", BaseJob.Wizard,  "Chrono",   1.6,  1.0,  1.2,  "범위 정지", Rarity.Epic, 0xc0a0ff, null, null, 1500),
            // ---- 전설 (상자·뽑기로만) ----
            U(Job.Dragoon,  "dragoon",  "용기사",   BaseJob.Warrior, "Dragoon",  1.4,  1.1,  1.1,  "화염 숨결", Rarity.Legendary, 0xff4a2a, Race.Troll),
            U(Job.Archmage, "archmage", "대마법사", BaseJob.Wizard,  "Archmage", 2.0,  1.1,  1.3,  "운석", Rarity.Legendary, 0xff7ad8, Race.Undead),
            U(Job.Shadow,   "shadow",   "그림자 군주", BaseJob.Warrior, "Shadow", 0.9, 0.95, 0.9,  "분신", Rarity.Legendary, 0x3a2a5a, Race.Orc),
            U(Job.Angel,    "angel",    "천사",     BaseJob.Wizard,  "Angel",    1.0,  1.05, 0.85, "아군 공격력 +", Rarity.Legendary, 0xfff8d0, Race.Undead),
            U(Job.Stormking, "stormking", "폭풍왕", BaseJob.Wizard,  "Stormking", 1.5, 1.0,  1.0,  "맵 전체 번개", Rarity.Legendary, 0x4fa0ff, Race.Orc),
            U(Job.Alchemist, "alchemist", "연금술사", BaseJob.Wizard, "Alchemist", 1.0, 1.0, 0.8,  "합성 보너스", Rarity.Legendary, 0x4adfa0),
        };

        public static UnitDef Get(Job job) => All[(int)job];

        static readonly Dictionary<string, Job> s_byId = BuildIds();
        static Dictionary<string, Job> BuildIds()
        {
            var d = new Dictionary<string, Job>();
            foreach (UnitDef u in All) d[u.Id] = u.Job;
            return d;
        }

        /// <summary>원본 문자열 id → Job (없으면 false)</summary>
        public static bool TryParse(string id, out Job job) => s_byId.TryGetValue(id ?? "", out job);

        /// <summary>config.ts STARTER_UNITS</summary>
        public static readonly Job[] Starter = { Job.Archer, Job.Wizard, Job.Warrior };
        /// <summary>config.ts DECK_SIZE (= 배치 구역 수)</summary>
        public const int DeckSize = 4;

        public static readonly string[] RarityKr = { "일반", "희귀", "영웅", "전설" };
        public static readonly uint[] RarityColor = { 0x9aa0a8, 0x4f9dff, 0xb06bff, 0xffb93f };

        // 유닛 영구 레벨 (컬렉션): 레벨당 공격력 +8%, Lv5/Lv10 에 스킬 강화(퍽)
        public const int MaxLevel = 10;                  // config.ts UNIT_MAX_LEVEL
        public const double LevelAtk = 0.08;             // config.ts UNIT_LEVEL_ATK
        /// <summary>희귀도별 기본 공격력 배수 (config.ts RARITY_ATK)</summary>
        public static readonly double[] RarityAtk = { 1, 1.1, 1.25, 1.4 };
        public static readonly int[] PerkLevels = { 5, 10 }; // config.ts UNIT_PERK_LEVELS

        /// <summary>유닛 레벨업 보석 비용 (config.ts unitUpgradeCost): Lv1→2: 40 … Lv9→10: 360</summary>
        public static int UpgradeCost(int level) => 40 + (level - 1) * 40;

        /// <summary>Lv n → n+1 에 필요한 카드 (일반 기준, 인덱스 0 = Lv1→2) - config.ts CARD_NEED</summary>
        public static readonly int[] CardNeed = { 2, 4, 10, 20, 50, 100, 200, 400, 800 };
        /// <summary>등급별 나눗수 - 희귀할수록 적게 필요 (config.ts RARITY_DIV)</summary>
        public static readonly int[] RarityDiv = { 1, 2, 5, 10 };

        public static int CardsNeeded(Rarity rarity, int level)
        {
            int i = Math.Max(0, Math.Min(CardNeed.Length - 1, level - 1));
            return (int)Math.Ceiling(CardNeed[i] / (double)RarityDiv[(int)rarity]);
        }

        /// <summary>퍽 개수에 따른 스킬 위력 배수 (config.ts perkMult)</summary>
        public static double PerkMult(int level)
        {
            int n = 0;
            foreach (int l in PerkLevels) if (level >= l) n++;
            return 1 + n * 0.3;
        }

        // ---------- 직업 x 종족 데미지 배율 (config.ts DMG_MULT) ----------
        // 상성: 유리 1.0 / 불리 0.9. 차이를 작게 둬서 상성을 몰라도 크게 손해 보지 않게 한다.
        static readonly double[,] s_dmgMult = BuildDmgMult();
        static double[,] BuildDmgMult()
        {
            var m = new double[All.Length, 3];
            foreach (UnitDef u in All)
                for (int r = 0; r < 3; r++) m[(int)u.Job, r] = u.Fav.HasValue && (int)u.Fav.Value == r ? 1.0 : 0.9;
            // 언데드 특화: 견습 사제·성직자는 언데드에 큰 피해
            m[(int)Job.Acolyte, (int)Race.Undead] = 1.4;
            m[(int)Job.Cleric, (int)Race.Undead] = 1.25;
            return m;
        }

        public static double DmgMult(Job job, Race race) => s_dmgMult[(int)job, (int)race];
    }
}
