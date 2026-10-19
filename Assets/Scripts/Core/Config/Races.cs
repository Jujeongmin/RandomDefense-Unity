namespace DefenseBattle.Core
{
    /// <summary>몹 종족 (config.ts RACES, RACE_KR, RACE_SPRITE, MOB_VARIANTS)</summary>
    public static class Races
    {
        public static readonly Race[] All = { Race.Troll, Race.Orc, Race.Undead };
        public static readonly string[] Kr = { "트롤", "오크", "언데드" };
        /// <summary>몹 그림 시트 접두사 (sprites/{접두사}_{변형}.png)</summary>
        public static readonly string[] Sprite = { "Mob_Troll", "Mob_Orc", "Mob_Undead" };
        /// <summary>종족별 웨이브 몹 그림 변형 개수 - 몹 종류마다 다른 그림을 쓴다</summary>
        public static readonly int[] Variants = { 3, 6, 4 };
    }
}
