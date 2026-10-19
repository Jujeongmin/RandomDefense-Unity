using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DefenseBattle.Core;

namespace DefenseBattle.Server
{
    /// <summary>
    /// 디펜스 배틀 서버 로직 (원본 server/src/server.ts). UGS 와 무관한 순수 로직이라 메모리 저장소로 테스트한다.
    /// 경제(보석·카드·상자·결제·대전 보상)는 서버 권위: 바꾸는 길은 이 클래스의 함수뿐이고, 규칙은 Core 의 EconRules (클라이언트와 같은 코드).
    /// 각 클라이언트가 자기 전장을 시뮬레이션하고, 서버는 매칭·결과 판정·보상만 한다 (같은 시드 = 같은 웨이브).
    /// </summary>
    public sealed partial class GameServer
    {
        readonly IServerStore m_store;
        readonly IClock m_clock;
        readonly INotifier m_notifier;
        readonly IRankBoard m_rank;
        readonly IReceiptVerifier m_receipts;
        readonly Func<IRandom> m_newRandom;

        public GameServer(IServerStore store, IClock clock, INotifier notifier, IRankBoard rank, IReceiptVerifier receipts, Func<IRandom> newRandom = null)
        {
            m_store = store;
            m_clock = clock;
            m_notifier = notifier;
            m_rank = rank;
            m_receipts = receipts;
            m_newRandom = newRandom ?? (() => new SystemRandom());
        }

        long Now => m_clock.NowMs;

        EconContext Ctx() => new EconContext(Now, m_newRandom());

        /// <summary>짧은 무작위 id (기록 대조용)</summary>
        string NewId()
        {
            IRandom r = m_newRandom();
            return Now.ToString("x") + "_" + ((long)(r.Next() * 0xFFFFFFFFL)).ToString("x8");
        }

        // ---------------- 경제 ----------------
        EconRecord NewEconRecord() => new EconRecord { Data = EconState.Fresh(Ctx().SeasonId) };

        /// <summary>플레이어 경제를 잠금 안에서 바꾸고 저장 → 바뀐 상태(복사본)와 결과</summary>
        Task<EconReply<T>> WithEcon<T>(string playerId, Func<EconState, T> fn)
        {
            return m_store.MutatePlayer<EconRecord, EconReply<T>>(playerId, Keys.Econ, NewEconRecord, rec =>
            {
                if (rec.Data == null) rec.Data = EconState.Fresh(Ctx().SeasonId);
                T result = fn(rec.Data);
                return new EconReply<T> { Econ = Json.Clone(rec.Data), Result = result };
            });
        }

        public async Task<EconState> LoadEcon(string playerId)
        {
            EconRecord rec = await m_store.ReadPlayer<EconRecord>(playerId, Keys.Econ);
            return rec?.Data ?? EconState.Fresh(Ctx().SeasonId);
        }

        /// <summary>경제 상태 받기 (처음이면 새 계정 상태를 만들어 저장)</summary>
        public async Task<EconReply> EconSync(string playerId)
        {
            EconReply<bool> r = await WithEcon(playerId, _ => true);
            return new EconReply { Econ = r.Econ };
        }

        public Task<EconReply<bool>> SetDecks(string playerId, List<Job> deck, List<AttackKey> attackDeck)
            => WithEcon(playerId, e => EconRules.SetDecks(e, deck, attackDeck));

        public Task<EconReply<bool>> StartUnlock(string playerId, int slot)
            => WithEcon(playerId, e => EconRules.StartUnlock(e, slot, Ctx()));

        public Task<EconReply<Reveal>> OpenSlot(string playerId, int slot, bool pay)
            => WithEcon(playerId, e => EconRules.OpenSlot(e, slot, pay, Ctx()));

        public Task<EconReply<Reveal>> Gacha(string playerId, int count, bool ticket)
            => WithEcon(playerId, e => EconRules.Gacha(e, count, Ctx(), ticket));

        public Task<EconReply<bool>> UpgradeUnit(string playerId, Job job)
            => WithEcon(playerId, e => EconRules.UpgradeUnit(e, job));

        public Task<EconReply<bool>> BuyUnit(string playerId, Job job)
            => WithEcon(playerId, e => EconRules.BuyUnit(e, job));

        public Task<EconReply<bool>> Research(string playerId, ResearchKey key)
            => WithEcon(playerId, e => EconRules.DoResearch(e, key));

        /// <summary>골드 부스터 1개 사용 (뽑기권은 Gacha 로만 쓴다)</summary>
        public Task<EconReply<bool>> UseGoldBooster(string playerId)
            => WithEcon(playerId, e => EconRules.UseItem(e, ItemKey.GoldBooster));

        public Task<EconReply<RoadClaimResult>> ClaimRoad(string playerId)
            => WithEcon(playerId, e => EconRules.ClaimRoad(e, Ctx()));

        public Task<EconReply<int>> AdReward(string playerId, bool doubleReward)
            => WithEcon(playerId, e => EconRules.AdReward(e, doubleReward, Ctx()));

        public Task<EconReply<int>> ClaimStarReward(string playerId, int chapter, int goal)
            => WithEcon(playerId, e => EconRules.ClaimStarReward(e, chapter, goal));

        public Task<EconReply<int>> ClaimQuest(string playerId, string key)
            => WithEcon(playerId, e => EconRules.ClaimQuest(e, key, Ctx()));

        public Task<EconReply<int>> ClaimAchievement(string playerId, string key)
            => WithEcon(playerId, e => EconRules.ClaimAchievement(e, key));

        public Task<EconReply<bool>> ClaimSeason(string playerId, int tier, bool premium)
            => WithEcon(playerId, e => EconRules.ClaimSeason(e, tier, premium, Ctx()));

        public Task<EconReply<int>> ClaimMonthly(string playerId)
            => WithEcon(playerId, e => EconRules.ClaimMonthly(e, Ctx()));
    }
}
