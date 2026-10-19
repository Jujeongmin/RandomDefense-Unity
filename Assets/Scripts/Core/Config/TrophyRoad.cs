namespace DefenseBattle.Core
{
    /// <summary>
    /// 트로피 로드 (config.ts TROPHY_ROAD) - 트로피가 오르면 보석·유닛·몹 카드·상자.
    /// 몹 카드는 트로피가 오를 때마다 하나씩 (기본 4장 + 로드 12장 = 16장). tools/progsim.ts 로 검증.
    /// </summary>
    public static class TrophyRoad
    {
        static RoadStep Gems(int at, int n) => new RoadStep { At = at, Crystals = n };
        static RoadStep Unit(int at, Job j) => new RoadStep { At = at, Unit = j };
        static RoadStep Card(int at, AttackKey k) => new RoadStep { At = at, Attack = k };
        static RoadStep Box(int at, ChestKind c) => new RoadStep { At = at, Chest = c };

        public static readonly RoadStep[] Steps =
        {
            Gems(15, 50),
            Unit(30, Job.Ranger),
            Card(60, AttackKey.Ghost),
            Gems(90, 80),
            Unit(120, Job.Frost),
            Gems(150, 100),
            Card(180, AttackKey.Shield),
            Gems(220, 120),
            Card(240, AttackKey.Berserk),
            Box(260, ChestKind.Silver),
            Unit(300, Job.Paladin),
            Gems(350, 160),
            Card(400, AttackKey.Wraith),
            Gems(450, 180),
            Card(470, AttackKey.Thief),
            Box(500, ChestKind.Gold),
            Unit(550, Job.Bomber),
            Gems(600, 220),
            Card(650, AttackKey.Priest),
            Card(690, AttackKey.Witch),
            Gems(720, 250),
            Box(800, ChestKind.Magic),
            Unit(850, Job.Assassin),
            Card(900, AttackKey.Golem),
            Card(950, AttackKey.King),
            Gems(1000, 400),
            Card(1050, AttackKey.Shaman),
            Gems(1100, 450),
            Card(1200, AttackKey.Demon),
            Card(1300, AttackKey.Slime),
            Box(1350, ChestKind.Legend),
            Gems(1500, 600),
            Gems(1700, 700),
            Box(2000, ChestKind.Legend),
        };

        /// <summary>이 유닛이 로드에서 열리는 트로피 (없으면 null)</summary>
        public static int? UnitAt(Job job)
        {
            foreach (RoadStep s in Steps) if (s.Unit == job) return s.At;
            return null;
        }

        /// <summary>이 몹 카드가 로드에서 열리는 트로피 (없으면 null)</summary>
        public static int? AttackAt(AttackKey key)
        {
            foreach (RoadStep s in Steps) if (s.Attack == key) return s.At;
            return null;
        }
    }
}
