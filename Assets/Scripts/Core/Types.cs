// ============================================================
//  공용 열거형 (원본 game/src/types.ts, config.ts 의 문자열 유니온)
//  enum 순서 = 원본 배열 순서. PvP 스냅샷·카드 번호가 이 순서를 쓰므로 바꾸지 않는다.
// ============================================================
namespace DefenseBattle.Core
{
    /// <summary>유닛 종류 32종. 순서 = config.ts UNITS (= JOBS 번호)</summary>
    public enum Job
    {
        Archer, Wizard, Warrior, Spearman, Slinger, Monk, Hunter, Acolyte,
        Ranger, Frost, Poisoner, Storm, Crossbow, Knight, Berserker, Merchant, Drummer, Houndmaster,
        Paladin, Bomber, Assassin, Pyro, Reaper, Cleric, Cannoneer, Chrono,
        Dragoon, Archmage, Shadow, Angel, Stormking, Alchemist,
    }

    /// <summary>유닛 등급 (얻기 어려움 - 상자·뽑기 확률, 레벨업에 필요한 카드 수)</summary>
    public enum Rarity { Common, Rare, Epic, Legendary }

    /// <summary>투사체 형태 (archer=화살, wizard=마법구, warrior=근접)</summary>
    public enum BaseJob { Archer, Wizard, Warrior }

    /// <summary>몹 종족. 순서 = config.ts RACES (스냅샷 code 의 종족 번호)</summary>
    public enum Race { Troll, Orc, Undead }

    /// <summary>판 안 등급 6단계. 값 = 등급 번호 (GRADE_INDEX)</summary>
    public enum GradeKey { Common, Rare, Elite, Legendary, Mythic, Eternal }

    public enum ResearchKey { Atk, StartGold, GoldGain, Rare, Boss }

    public enum QuestStat { Summons, Kills, BestWave, Sells }

    /// <summary>normal / fast=빠름·약함 / tank=느림·단단함 / card=PvP 상대가 던진 몹 카드</summary>
    public enum MobKind { Normal, Fast, Tank, Card }

    public enum WaveState { Spawning, WaitingBoss, Rest }

    public enum GameMode { Solo, Pvp }

    public enum ItemKey { GachaTicket, GoldBooster }

    public enum ChestKind { Wood, Silver, Gold, Magic, Legend }

    /// <summary>몹 카드 16종. 순서 = config.ts ATTACKS (= 카드 번호)</summary>
    public enum AttackKey
    {
        Swarm, Rush, Giant, Ghost, Shield, Priest, Egg, Wraith, King, Demon,
        Thief, Golem, Berserk, Witch, Shaman, Slime,
    }

    /// <summary>시각 전용 이펙트 종류 (데미지 판정은 엔진에서 즉시 처리)</summary>
    public enum EffectType { Proj, Pop, Dmg, Gold, Splash, Slash, Merge, Summon, Burst, Drop, Beam }
}
