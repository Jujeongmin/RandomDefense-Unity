namespace DefenseBattle.Core
{
    /// <summary>웨이브·몹 수치 (config.ts WAVE, MOB_KINDS, PLAY_SPEED)</summary>
    public static class Waves
    {
        public const int Total = 50;
        public const int MobsPerWave = 40;
        public const double HpBase = 100;
        /// <summary>무한 모드 웨이브당 HP 성장. 합성 공격력 3배 이후 연구 0 평균 ~38웨이브, 연구 MAX ~50웨이브</summary>
        public const double HpGrowthPerWave = 0.175;
        /// <summary>캠페인 스테이지 웨이브당 HP 성장 (스테이지 난이도는 Campaign.StageHpBase/Step 으로 맞춘다)</summary>
        public const double StageHpGrowth = 0.115;
        /// <summary>PvP 웨이브당 HP 성장 - 빠르게 올려 한 판을 4분 안팎으로 (tools/pvpsim.ts)</summary>
        public const double PvpHpGrowth = 0.25;
        public const double MobSpeed = 1.2;
        public const double CornerPause = 0.25;
        public const double RestBetween = 5.0;
        public const double SpawnInterval = 0.6;
        public const int GameOverMobCount = 100;
        public const int BossEvery = 10;
        public const double BossTimeLimit = 120;
        public const double BossHpMult = 25;
        public const double BossSizeMult = 2;

        // ---------- 몹 종류 (웨이브 구성 다양화) ----------
        // fast: 빠르고 약함 / tank: 느리고 단단함. 등장 시작 웨이브와 간격(n번째 몹마다)
        public static class Fast { public const double HpMult = 0.6, SpeedMult = 1.6, SizeMult = 0.85; public const int FromWave = 4, Every = 5; }
        public static class Tank { public const double HpMult = 2.6, SpeedMult = 0.72, SizeMult = 1.25; public const int FromWave = 11, Every = 9; }

        /// <summary>
        /// 실시간 1초 = 게임 시간 PlaySpeed 초 (메인 루프 / AI 연습전 루프에서 곱한다).
        /// 배속 버튼(1x/2x/3x)은 이 위에 곱해진다. 엔진(Game.Update)에는 곱하지 않는다 - 시뮬레이터 결과가 바뀌지 않게.
        /// </summary>
        public const double PlaySpeed = 2;
    }
}
