using System.Collections.Generic;

namespace DefenseBattle.Core
{
    /// <summary>
    /// PvP 몹 카드 16종과 추천 덱 (config.ts ATTACKS ~ ATTACK_DECK_SIZE).
    /// 카드마다 잘 막는 유닛(Counters)이 달라 상대 덱을 보고 고르는 판단이 생긴다.
    /// </summary>
    public static class Attacks
    {
        /// <summary>저항(Resist) 유닛의 피해 배수 (-60%)</summary>
        public const double ResistMult = 0.4;

        static AttackDef A(AttackKey key, string id, string kr, string desc, Race race, int variant, int cost, int count,
            double hp, double speed, double size, double cooldown, params Job[] counters)
        {
            return new AttackDef
            {
                Key = key, Id = id, Kr = kr, Desc = desc, Race = race, Variant = variant, Cost = cost, Count = count,
                HpMult = hp, Speed = speed, Size = size, Cooldown = cooldown, Counters = counters,
            };
        }

        /// <summary>config.ts ATTACKS - 순서가 AttackKey enum (= 카드 번호)과 같다</summary>
        public static readonly AttackDef[] All = Build();

        static AttackDef[] Build()
        {
            var list = new List<AttackDef>
            {
                A(AttackKey.Swarm, "swarm", "고블린 무리", "약한 몹 8마리", Race.Orc, 0, 40, 8, 0.55, 1.1, 0.85, 0,
                    Job.Wizard, Job.Bomber),
                A(AttackKey.Rush, "rush", "질주 엘프", "아주 빠른 몹 4마리", Race.Troll, 1, 50, 4, 0.6, 1.9, 0.85, 0,
                    Job.Frost, Job.Warrior),
                A(AttackKey.Giant, "giant", "거인", "느리고 단단한 1마리", Race.Troll, 0, 80, 1, 7, 0.55, 1.7, 0,
                    Job.Assassin, Job.Paladin),
                A(AttackKey.Ghost, "ghost", "유령", "물리 공격에 강함", Race.Undead, 1, 60, 3, 1.05, 1, 1, 0,
                    Job.Wizard, Job.Frost),
                A(AttackKey.Shield, "shield", "방패병", "화살에 강함", Race.Troll, 2, 65, 3, 1.9, 0.85, 1.05, 0,
                    Job.Wizard, Job.Bomber, Job.Warrior),
                A(AttackKey.Priest, "priest", "어둠 신관", "주변 몹을 회복", Race.Orc, 3, 90, 1, 3.5, 0.9, 1.1, 0,
                    Job.Assassin, Job.Ranger),
                A(AttackKey.Egg, "egg", "분열 알", "죽으면 3마리로", Race.Undead, 3, 70, 2, 2.2, 0.9, 1.15, 0,
                    Job.Bomber, Job.Wizard),
                A(AttackKey.Wraith, "wraith", "망령", "한 번 되살아남", Race.Undead, 2, 60, 5, 0.5, 1.05, 0.95, 0,
                    Job.Bomber, Job.Archer),
                A(AttackKey.King, "king", "고블린 킹", "졸개를 계속 부름", Race.Orc, 4, 120, 1, 7, 0.7, 1.45, 0,
                    Job.Ranger, Job.Paladin),
                A(AttackKey.Demon, "demon", "마왕", "아주 강함 · 긴 쿨타임", Race.Orc, 5, 140, 1, 26, 0.55, 1.9, 30,
                    Job.Paladin, Job.Assassin),
                A(AttackKey.Thief, "thief", "그림자 도적", "잠깐씩 숨어 노릴 수 없음", Race.Orc, 8, 60, 4, 0.9, 1.15, 0.95, 0,
                    Job.Wizard, Job.Bomber, Job.Pyro),
                A(AttackKey.Golem, "golem", "돌 골렘", "마법에 강하고 단단함", Race.Troll, 3, 85, 1, 6, 0.55, 1.6, 0,
                    Job.Knight, Job.Spearman, Job.Assassin),
                A(AttackKey.Berserk, "berserk", "광폭 오크", "체력 절반 아래면 질주", Race.Orc, 7, 70, 3, 1.6, 0.85, 1.05, 0,
                    Job.Frost, Job.Chrono, Job.Assassin),
                A(AttackKey.Witch, "witch", "점멸 마녀", "3초마다 앞으로 순간이동", Race.Undead, 0, 65, 3, 1.1, 0.9, 1, 0,
                    Job.Frost, Job.Chrono, Job.Storm),
                A(AttackKey.Shaman, "shaman", "수호 주술사", "주변 몹 받는 피해 -35%", Race.Orc, 6, 95, 1, 3.2, 0.9, 1.1, 0,
                    Job.Ranger, Job.Crossbow, Job.Assassin),
                A(AttackKey.Slime, "slime", "번식 유령", "5초 뒤 한 마리씩 불어남", Race.Undead, 4, 75, 2, 1.5, 0.9, 1.05, 0,
                    Job.Ranger, Job.Cannoneer, Job.Reaper),
            };
            AttackDef[] a = list.ToArray();
            a[(int)AttackKey.Giant].Gold = 12;
            a[(int)AttackKey.Ghost].Resist = new[] { Job.Warrior, Job.Archer, Job.Ranger, Job.Assassin };
            a[(int)AttackKey.Shield].Resist = new[] { Job.Archer, Job.Ranger };
            a[(int)AttackKey.Priest].Heal = 0.06;
            a[(int)AttackKey.Priest].Gold = 8;
            a[(int)AttackKey.Egg].Split = 3;
            a[(int)AttackKey.Wraith].Revive = true;
            a[(int)AttackKey.King].Spawn = 3;
            a[(int)AttackKey.King].Gold = 16;
            a[(int)AttackKey.Demon].Gold = 30;
            a[(int)AttackKey.Thief].Stealth = 1.8;
            a[(int)AttackKey.Golem].Resist = new[] { Job.Wizard, Job.Storm, Job.Bomber, Job.Pyro, Job.Archmage, Job.Stormking };
            a[(int)AttackKey.Golem].Gold = 12;
            a[(int)AttackKey.Berserk].Enrage = 1.9;
            a[(int)AttackKey.Witch].Blink = 110;
            a[(int)AttackKey.Shaman].Guard = 0.35;
            a[(int)AttackKey.Shaman].Gold = 8;
            a[(int)AttackKey.Slime].Breed = true;
            return a;
        }

        public static AttackDef Get(AttackKey key) => All[(int)key];
        public static AttackDef Get(int card) => All[card];

        static readonly Dictionary<string, AttackKey> s_byId = BuildIds();
        static Dictionary<string, AttackKey> BuildIds()
        {
            var d = new Dictionary<string, AttackKey>();
            foreach (AttackDef a in All) d[a.Id] = a.Key;
            return d;
        }

        /// <summary>원본 문자열 id → AttackKey (없으면 false)</summary>
        public static bool TryParse(string id, out AttackKey key) => s_byId.TryGetValue(id ?? "", out key);

        /// <summary>config.ts STARTER_ATTACKS</summary>
        public static readonly AttackKey[] Starter = { AttackKey.Swarm, AttackKey.Rush, AttackKey.Giant, AttackKey.Egg };
        public const int DeckSize = 4;

        public static readonly DeckPreset<Job>[] UnitPresets =
        {
            new DeckPreset<Job>("기본기", "처음 시작하기 좋은 골고루 덱", Job.Archer, Job.Wizard, Job.Warrior, Job.Ranger),
            new DeckPreset<Job>("물량 정리", "몰려오는 몹을 범위 공격으로 쓸어 담아요 · 고블린 무리·분열 알·도적", Job.Wizard, Job.Bomber, Job.Pyro, Job.Frost),
            new DeckPreset<Job>("거대 몹 사냥", "거인·골렘·마왕처럼 큰 몹을 빨리 녹여요", Job.Paladin, Job.Knight, Job.Assassin, Job.Ranger),
            new DeckPreset<Job>("발 묶기", "빠른 몹·점멸 마녀·광폭 오크를 붙잡아 둬요", Job.Frost, Job.Warrior, Job.Chrono, Job.Storm),
            new DeckPreset<Job>("저격 처형", "신관·주술사 같은 핵심 몹을 먼저 끊어요", Job.Ranger, Job.Crossbow, Job.Assassin, Job.Hunter),
            new DeckPreset<Job>("전설 조합", "전설 유닛만으로 끝까지 버텨요", Job.Archmage, Job.Angel, Job.Dragoon, Job.Stormking),
        };

        public static readonly DeckPreset<AttackKey>[] AttackPresets =
        {
            new DeckPreset<AttackKey>("물량 공세", "몹 수로 밀어붙여요 · 범위 공격이 약한 상대에게", AttackKey.Swarm, AttackKey.Egg, AttackKey.Slime, AttackKey.King),
            new DeckPreset<AttackKey>("철벽 행진", "단단한 몹을 회복시키며 버텨요 · 한 방이 약한 상대에게", AttackKey.Giant, AttackKey.Golem, AttackKey.Shield, AttackKey.Priest),
            new DeckPreset<AttackKey>("기습", "빠르고 잡기 까다로운 몹 · 둔화가 없는 상대에게", AttackKey.Rush, AttackKey.Thief, AttackKey.Witch, AttackKey.Berserk),
            new DeckPreset<AttackKey>("마왕 강림", "주술사 보호막 아래 마왕을 보내요", AttackKey.Demon, AttackKey.Shaman, AttackKey.Priest, AttackKey.Wraith),
        };
    }
}
