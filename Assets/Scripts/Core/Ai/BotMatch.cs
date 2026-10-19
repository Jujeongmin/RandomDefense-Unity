using System;
using System.Collections.Generic;

namespace DefenseBattle.Core
{
    /// <summary>
    /// 봇 대전 상대 (pvp.ts BotSession 의 진행 부분). 봇 전장을 헤드리스로 돌리고, 판단·스냅샷을 실시간 기준으로 낸다.
    /// 화면 쪽은 매 프레임 Tick(실제 경과 초)만 부르면 된다. 서버 기록(봇전 시작/결과)은 바깥에서 한다.
    /// </summary>
    public sealed class BotMatch
    {
        /// <summary>봇 전장 (상대 미니맵은 Snapshot 이벤트로 받은 값을 그린다)</summary>
        public readonly Game Game;
        public readonly BotPlayer Ai;
        public readonly double Skill;

        /// <summary>봇이 플레이어에게 몹을 던짐 → 플레이어 Game.ReceiveAttack 에 넘긴다</summary>
        public event Action<AttackPayload> AttackSent;
        /// <summary>봇 전장 스냅샷 (PvpConfig.ReportInterval 마다) → 상대 미니맵</summary>
        public event Action<BoardSnapshot> Snapshot;
        /// <summary>봇 전장이 무너짐 = 플레이어 승리</summary>
        public event Action BotDefeated;

        readonly double m_think;
        readonly double m_openDelay;
        double m_sinceStart;
        double m_thinkAcc;
        double m_snapAcc;
        bool m_done;
        /// <summary>플레이어 전장 최근 스냅샷 (봇이 약점을 노릴 때)</summary>
        BoardSnapshot m_opp;
        // 스냅샷 객체 세 개를 돌려 쓴다 (받는 쪽이 보간용으로 '직전·최신' 두 개를 들고 있어도 덮어쓰지 않게)
        readonly BoardSnapshot[] m_snaps = { new BoardSnapshot(), new BoardSnapshot(), new BoardSnapshot() };
        int m_snapIndex;

        /// <param name="seed">플레이어와 같은 웨이브 시드</param>
        /// <param name="skill">BotProfile.Skill</param>
        /// <param name="trophies">봇 트로피 (덱·연구 수준)</param>
        /// <param name="research">BotProfile.Research 결과</param>
        /// <param name="unitLevel">BotProfile.Level 결과</param>
        public BotMatch(uint seed, double skill, int trophies, Dictionary<ResearchKey, int> research, int unitLevel, IRandom rand = null)
        {
            IRandom r = rand ?? new SystemRandom();
            Skill = skill;
            List<Job> deck = BotProfile.Deck(trophies, r);
            var levels = new Dictionary<Job, int>();
            foreach (Job j in deck) levels[j] = unitLevel;
            Game = new Game(new GameOptions
            {
                Mode = GameMode.Pvp, Seed = seed, UseResearch = true, Research = research, Track = false,
                Deck = deck, AtkMult = BotProfile.AtkMult(skill), AttackDeck = BotProfile.AttackDeck(trophies, r), UnitLevels = levels,
                RandomSeed = (int)(r.Next() * int.MaxValue) + 1,
            });
            Ai = new BotPlayer(Game, skill, r);
            m_think = BotPlayer.ThinkInterval(skill);
            m_openDelay = 1.5 + r.Next() * 1.5; // 시작 직후 잠깐은 손을 대지 않는다 (사람이 상황을 파악하는 시간)
            Game.AttackSent += p => AttackSent?.Invoke(p);
            Game.GameEnded += _ =>
            {
                if (m_done) return;
                m_done = true;
                BotDefeated?.Invoke();
            };
        }

        /// <summary>플레이어 전장 스냅샷을 봇에게 알려 준다 (원본 sendSnapshot)</summary>
        public void SetOpponent(BoardSnapshot playerSnapshot) => m_opp = playerSnapshot;

        /// <summary>플레이어가 던진 몹 (원본 sendAttack)</summary>
        public void ReceiveAttack(AttackPayload p) => Game.ReceiveAttack(p);

        /// <summary>플레이어가 지거나 나감 → 봇 진행을 멈춘다</summary>
        public void Stop() => m_done = true;

        public bool Done => m_done;

        /// <summary>시작 시각 이후 매 프레임. realDt = 실제 경과 초 (끊김 대비 0.2초로 자른다)</summary>
        public void Tick(double realDt)
        {
            if (m_done) return;
            double real = Math.Min(0.2, realDt);
            m_sinceStart += real;
            double dt = real * Waves.PlaySpeed;
            // 0.05초 단위로 나눠 진행 (긴 프레임에도 판정이 건너뛰지 않게)
            for (double left = dt; left > 0 && !m_done; left -= 0.05) Game.Update(Math.Min(left, 0.05));
            if (m_done) return;
            // 판단·스냅샷 모두 실시간 기준 (사람과 같은 손 속도)
            m_thinkAcc += real;
            m_snapAcc += real;
            if (m_thinkAcc >= m_think && m_sinceStart > m_openDelay)
            {
                m_thinkAcc = 0;
                Ai.Step(m_opp);
            }
            if (m_snapAcc >= PvpConfig.ReportInterval)
            {
                m_snapAcc = 0;
                m_snapIndex = (m_snapIndex + 1) % m_snaps.Length;
                Snapshot?.Invoke(Game.Snapshot(m_snaps[m_snapIndex]));
            }
        }

        static readonly string[] s_nickHead = { "용사", "전사", "궁수", "기사", "검객", "사냥꾼", "마법사", "도적" };
        static readonly string[] s_nickTags =
        {
            "달빛궁수", "합성장인", "철벽수비", "몹사냥꾼", "번개마법사", "고블린킬러", "새벽용사", "빙결여왕",
            "랜덤의신", "초보탈출", "성기사짱", "타워장인", "MoonArcher", "TowerKing", "NovaKnight", "LuckyMerge",
        };
        static readonly HashSet<string> s_untranslated = new HashSet<string> { "MoonArcher", "TowerKing", "NovaKnight", "LuckyMerge" };

        /// <summary>실제 유저처럼 보이는 봇 닉네임 (pvp.ts botNick)</summary>
        public static string Nick(IRandom r)
        {
            if (r.Next() < 0.55) return Loc.Tl(s_nickHead[SystemRandom.Index(r, s_nickHead.Length)]) + (1000 + SystemRandom.Index(r, 9000));
            string tag = s_nickTags[SystemRandom.Index(r, s_nickTags.Length)];
            if (!s_untranslated.Contains(tag)) tag = Loc.Tl(tag);
            return r.Next() < 0.4 ? tag + SystemRandom.Index(r, 99) : tag;
        }

        /// <summary>봇 트로피: 내 트로피 ±40</summary>
        public static int OpponentTrophies(int myTrophies, IRandom r) => Math.Max(0, myTrophies + JsMath.RoundInt((r.Next() - 0.5) * 80));
    }
}
