using System;
using System.Collections.Generic;

namespace DefenseBattle.Core
{
    /// <summary>
    /// 게임 엔진 - 맵/경로/웨이브/전투/경제 로직 (engine.ts Game). 렌더링·입력은 다루지 않는다.
    /// Update(dt) 를 불러 진행하고, 화면은 공개 상태(Mobs, Units, Effects …)를 읽어 그린다.
    /// </summary>
    public sealed partial class Game
    {
        /// <summary>이펙트 상한 (성능)</summary>
        const int MaxEffects = 240;
        /// <summary>상대가 던진 몹이 떨어지는 시간(게임 초) - 착지 지점을 미리 보여 대비할 틈을 준다</summary>
        public const double DropWarn = 1.6;
        /// <summary>수호 주술사 보호 반경 (px)</summary>
        const double GuardRadius = 130;

        public readonly GameOptions Opts;

        public int Gold;
        public int Wave;
        public readonly List<Mob> Mobs = new List<Mob>();
        public readonly List<Unit> Units = new List<Unit>();
        public readonly List<Effect> Effects = new List<Effect>();
        /// <summary>직업별 판 안 강화 레벨 (없으면 1)</summary>
        public readonly Dictionary<Job, int> Levels = new Dictionary<Job, int>();
        public Zone[] Zones;
        public int Kills;
        public int SummonCount;
        public int Sells;
        /// <summary>배속 (1x/2x/3x 버튼)</summary>
        public double Speed = 1;
        public bool Paused;
        public bool Over;
        public bool Cleared;
        public bool Revived;
        public Race CurrentRace = Race.Troll;
        public WaveState WaveState = WaveState.Rest;
        public double RestTimer = 1.0;
        public double SpawnTimer;
        public int SpawnedCount;
        public int SpawnGoal;
        public bool IsBossWave;
        public double BossTimer;
        public readonly List<LogEntry> Log = new List<LogEntry>();
        /// <summary>신화·태초 배너 (없으면 null)</summary>
        public LogEntry MythicBanner;
        /// <summary>화면 흔들림 세기 (렌더러가 감쇠 표시)</summary>
        public double Shake;
        /// <summary>게임 경과 시간 (배속 반영)</summary>
        public double Time;
        /// <summary>PvP: 보낸 공격 수</summary>
        public int SentCount;
        /// <summary>필드 몹 최대치 (별 판정)</summary>
        public int PeakMobs;
        /// <summary>화염술사 불 장판</summary>
        public readonly List<Burn> Burns = new List<Burn>();

        // ---- 이벤트 (원본 on/emit) ----
        public event Action<int> WaveStarted;
        public event Action RosterChanged;
        /// <summary>업적 조건 도달 (Track 일 때만). 처음인지는 메타 쪽이 판단한다</summary>
        public event Action<string> AchievementReached;
        /// <summary>퀘스트 진행 (Track 일 때만). BestWave 는 도달 웨이브를 값으로 준다</summary>
        public event Action<QuestStat, int> Progress;
        public event Action<GameOverData> GameEnded;
        /// <summary>유닛이 공격 발동</summary>
        public event Action<Job> Shoot;
        /// <summary>몹 처치 (보스면 true)</summary>
        public event Action<bool> Kill;
        public event Action BossSpawned;
        /// <summary>합성 성공 (결과 유닛)</summary>
        public event Action<Unit> Merged;
        /// <summary>PvP: 내가 상대에게 몹을 보냄</summary>
        public event Action<AttackPayload> AttackSent;
        /// <summary>PvP: 상대가 보낸 몹 도착</summary>
        public event Action<AttackPayload> AttackIncoming;

        IRandom m_waveRng;
        IRandom m_rand;
        int m_nextId = 1;
        /// <summary>지금 공격 중인 유닛 (처치 보상·부활 차단 판정용)</summary>
        Unit m_hitter;
        /// <summary>프레임마다 다시 계산하는 오라: 천사 공격력 배수, 북치기 위치</summary>
        double m_angelMult = 1;
        readonly List<Unit> m_drummers = new List<Unit>();

        public Game(GameOptions opts = null)
        {
            Opts = opts ?? new GameOptions();
            if (Opts.Seed == 0) Opts.Seed = (uint)new Random().Next(1, int.MaxValue);
            Reset();
        }

        public bool IsPvp => Opts.Mode == GameMode.Pvp;

        /// <summary>이 판의 총 웨이브 수 (PvP 는 무한 → int.MaxValue)</summary>
        public int TotalWaves => IsPvp ? int.MaxValue : Opts.WaveCount > 0 ? Opts.WaveCount : Waves.Total;

        /// <summary>연구 레벨 (연구 미적용 모드에서는 0)</summary>
        public int Res(ResearchKey key)
        {
            if (!Opts.UseResearch || Opts.Research == null) return 0;
            return Opts.Research.TryGetValue(key, out int v) ? v : 0;
        }

        /// <summary>판 처음 상태로</summary>
        public void Reset()
        {
            m_waveRng = new Mulberry32(Opts.Seed);
            m_rand = Opts.RandomSeed != 0 ? new SystemRandom(Opts.RandomSeed) : new SystemRandom();
            Gold = JsMath.RoundInt((Economy.StartGold + Res(ResearchKey.StartGold) * Research.Per(ResearchKey.StartGold)) * Opts.StartGoldMult);
            Wave = 0;
            ReleaseAllMobs();
            Units.Clear();
            ReleaseAllEffects();
            Levels.Clear();
            Zones = Board.MakeZones();
            Kills = 0;
            SummonCount = 0;
            Sells = 0;
            Speed = 1;
            Paused = false;
            Over = false;
            Cleared = false;
            Revived = false;
            CurrentRace = Races.All[0];
            WaveState = WaveState.Rest;
            RestTimer = 1.0;
            SpawnTimer = 0;
            SpawnedCount = 0;
            SpawnGoal = 0;
            IsBossWave = false;
            BossTimer = 0;
            Log.Clear();
            MythicBanner = null;
            Shake = 0;
            Time = 0;
            SentCount = 0;
            PeakMobs = 0;
            m_pending.Clear();
            ReleaseAllBurns();
            m_attackReadyAt.Clear();
            m_scripted.Clear();
            m_fxLog.Clear();
            m_shotUnits.Clear();
            RebuildAttackCards();
        }

        // ---------------- 웨이브 ----------------
        public void StartNextWave()
        {
            Wave += 1;
            if (Wave > TotalWaves) { Win(); return; } // PvP 는 무한 웨이브
            Race[] pool = Opts.Races != null && Opts.Races.Length > 0 ? Opts.Races : Races.All;
            CurrentRace = pool[SystemRandom.Index(m_waveRng, pool.Length)];
            // 10웨이브마다 + 스테이지 마지막 웨이브는 보스
            IsBossWave = Wave % Waves.BossEvery == 0 || Wave == TotalWaves;
            SpawnedCount = 0;
            SpawnTimer = 0;
            WaveState = WaveState.Spawning;
            SpawnGoal = IsBossWave ? 1 : Waves.MobsPerWave;
            if (IsBossWave) BossTimer = Waves.BossTimeLimit;
            if (Opts.Track && !IsPvp) Progress?.Invoke(QuestStat.BestWave, Wave);
            WaveStarted?.Invoke(Wave);
        }

        public double BaseHp() => BaseHp(Wave);

        public double BaseHp(int wave)
        {
            bool pvp = IsPvp;
            double growth = Opts.StageId != null ? Waves.StageHpGrowth : pvp ? Waves.PvpHpGrowth : Waves.HpGrowthPerWave;
            return Waves.HpBase * Opts.HpMult * (pvp ? PvpConfig.HpMult : 1) * Math.Pow(1 + growth, Math.Max(0, wave - 1));
        }

        public double WaveHp()
        {
            double b = BaseHp();
            return IsBossWave ? b * Waves.BossHpMult : b;
        }

        // ---------------- 메인 업데이트 ----------------
        /// <summary>dtReal = 실제 경과 초 × Waves.PlaySpeed (원본과 같이 엔진 밖에서 곱한다). 배속(Speed)은 여기서 곱한다</summary>
        public void Update(double dtReal)
        {
            if (Over || Paused) return;
            ReleaseGraveyard(); // 지난 프레임에 죽은 몹을 풀로
            double dt = dtReal * Speed;
            Time += dt;
            if (Shake > 0) Shake = Math.Max(0, Shake - dtReal * 30);

            for (int i = Log.Count - 1; i >= 0; i--)
            {
                Log[i].Ttl -= dtReal;
                if (Log[i].Ttl <= 0) Log.RemoveAt(i); // 최대 6줄
            }
            if (MythicBanner != null)
            {
                MythicBanner.Ttl -= dtReal;
                if (MythicBanner.Ttl <= 0) MythicBanner = null;
            }

            UpdateWaveState(dt);
            UpdatePending(dt);
            UpdateMobs(dt);
            UpdateUnitMovement(dt);
            UpdateCombat(dt);
            UpdateDots(dt);
            UpdateEffects(dt);

            if (Mobs.Count > PeakMobs) PeakMobs = Mobs.Count;
            if (Mobs.Count >= Waves.GameOverMobCount) GameOver(Loc.Tl("몹이 100마리를 넘었습니다"));
        }

        void UpdateWaveState(double dt)
        {
            if (WaveState == WaveState.Rest)
            {
                RestTimer -= dt;
                if (RestTimer <= 0) StartNextWave();
                return;
            }
            if (WaveState == WaveState.Spawning)
            {
                SpawnTimer -= dt;
                if (SpawnTimer <= 0 && SpawnedCount < SpawnGoal)
                {
                    SpawnMob();
                    SpawnedCount += 1;
                    SpawnTimer = Waves.SpawnInterval;
                }
                if (SpawnedCount >= SpawnGoal)
                {
                    if (IsBossWave)
                    {
                        WaveState = WaveState.WaitingBoss;
                    }
                    else
                    {
                        WaveState = WaveState.Rest;
                        RestTimer = Waves.RestBetween;
                    }
                }
                return;
            }
            if (WaveState == WaveState.WaitingBoss)
            {
                BossTimer -= dt;
                bool bossAlive = false;
                for (int i = 0; i < Mobs.Count && !bossAlive; i++) bossAlive = Mobs[i].Boss;
                if (!bossAlive)
                {
                    if (Opts.StageId == null) TryAchieve("clear" + Wave);
                    if (Wave >= TotalWaves) { Win(); return; }
                    WaveState = WaveState.Rest;
                    RestTimer = Waves.RestBetween;
                }
                else if (BossTimer <= 0)
                {
                    GameOver(Loc.Tl("보스 제한시간 초과"));
                }
            }
        }

        public void PushLog(string text)
        {
            Log.Insert(0, new LogEntry { Text = text, Ttl = 10 });
            if (Log.Count > 6) Log.RemoveAt(Log.Count - 1);
        }

        void TryAchieve(string key)
        {
            if (!Opts.Track) return;
            AchievementReached?.Invoke(key);
        }

        void ReportProgress(QuestStat stat)
        {
            if (Opts.Track) Progress?.Invoke(stat, 1);
        }

        // ---------------- 종료 ----------------
        public void GameOver(string reason)
        {
            if (Over) return;
            Over = true;
            Cleared = false;
            Finish(reason);
        }

        public void Win()
        {
            if (Over) return;
            Over = true;
            Cleared = true;
            if (Opts.StageId == null) TryAchieve("clear50");
            Finish(Opts.StageId != null ? Loc.Tl("스테이지 {0} 클리어!", Opts.StageId) : Loc.Tl("50 웨이브 클리어!"));
        }

        /// <summary>부활 (보상형 광고): 일반 몹 정리 + 보스 시간 연장. 판당 1회</summary>
        public bool CanRevive() => Over && !Cleared && !IsPvp && !Revived;

        public bool Revive()
        {
            if (!CanRevive()) return false;
            Revived = true;
            Over = false;
            foreach (Mob m in Mobs)
            {
                if (m.Boss) continue;
                Effect pop = FxAt(EffectType.Pop, m.X, m.Y, 0.45);
                pop.Seed = m_rand.Next() * 6.28;
                AddEffect(pop);
                m.Dead = true;
            }
            CompactMobs();
            if (WaveState == WaveState.WaitingBoss) BossTimer = Math.Max(BossTimer, 45);
            Shake = 12;
            PushLog(Loc.Tl("부활! 필드가 정리되었습니다"));
            return true;
        }

        /// <summary>PvP 결과 확정 (상대 패배/이탈 시 승리, 서버 판정 등)</summary>
        public void PvpEnd(bool won, string reason)
        {
            if (Over) return;
            Over = true;
            Cleared = won;
            GameEnded?.Invoke(new GameOverData { Wave = Wave, Reason = reason, Cleared = won, IsPvp = true, PvpWon = won, PeakMobs = PeakMobs });
        }

        void Finish(string reason)
        {
            // PvP 에서 내 필드가 무너지면 패배
            GameEnded?.Invoke(new GameOverData { Wave = Wave, Reason = reason, Cleared = Cleared, IsPvp = IsPvp, PvpWon = false, PeakMobs = PeakMobs });
        }
    }
}
