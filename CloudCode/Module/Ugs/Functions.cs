using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DefenseBattle.Core;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace DefenseBattle.Server.Ugs
{
    /// <summary>
    /// 기기가 부르는 Cloud Code 함수 (이름은 Core.CloudApi). 플레이어는 항상 호출한 사람(context.PlayerId) -
    /// 다른 사람 데이터를 바꾸는 인자는 받지 않는다. 실제 로직은 GameServer.
    /// </summary>
    public sealed class Functions
    {
        readonly IGameApiClient m_api;
        readonly IPushClient m_push;

        public Functions(IGameApiClient api, IServiceProvider services)
        {
            m_api = api;
            // 푸시는 런타임이 주는 경우에만 (없으면 기기의 poll 로 대신한다)
            m_push = services.GetService(typeof(IPushClient)) as IPushClient;
        }

        GameServer Server(IExecutionContext ctx)
        {
            INotifier notifier = m_push != null ? new UgsNotifier(m_push, ctx) : (INotifier)new NoNotifier();
            return new GameServer(new UgsStore(m_api, ctx), new SystemClock(), notifier, new UgsRankBoard(m_api, ctx), new UgsReceiptVerifier(m_api, ctx));
        }

        static string Me(IExecutionContext ctx) => ctx.PlayerId ?? throw new InvalidOperationException("player only");

        sealed class NoNotifier : INotifier
        {
            public Task Send(string playerId, string type, object payload) => Task.CompletedTask;
        }

        // ---------------- 경제 ----------------
        [CloudCodeFunction(CloudApi.EconSync)]
        public Task<EconReply> EconSync(IExecutionContext context) => Server(context).EconSync(Me(context));

        [CloudCodeFunction(CloudApi.SetDecks)]
        public Task<EconReply<bool>> SetDecks(IExecutionContext context, List<Job> deck, List<AttackKey> attackDeck)
            => Server(context).SetDecks(Me(context), deck, attackDeck);

        [CloudCodeFunction(CloudApi.StartUnlock)]
        public Task<EconReply<bool>> StartUnlock(IExecutionContext context, int slot) => Server(context).StartUnlock(Me(context), slot);

        [CloudCodeFunction(CloudApi.OpenSlot)]
        public Task<EconReply<Reveal>> OpenSlot(IExecutionContext context, int slot, bool pay) => Server(context).OpenSlot(Me(context), slot, pay);

        [CloudCodeFunction(CloudApi.Gacha)]
        public Task<EconReply<Reveal>> Gacha(IExecutionContext context, int count, bool ticket) => Server(context).Gacha(Me(context), count, ticket);

        [CloudCodeFunction(CloudApi.UpgradeUnit)]
        public Task<EconReply<bool>> UpgradeUnit(IExecutionContext context, Job job) => Server(context).UpgradeUnit(Me(context), job);

        [CloudCodeFunction(CloudApi.BuyUnit)]
        public Task<EconReply<bool>> BuyUnit(IExecutionContext context, Job job) => Server(context).BuyUnit(Me(context), job);

        [CloudCodeFunction(CloudApi.Research)]
        public Task<EconReply<bool>> Research(IExecutionContext context, ResearchKey key) => Server(context).Research(Me(context), key);

        [CloudCodeFunction(CloudApi.UseGoldBooster)]
        public Task<EconReply<bool>> UseGoldBooster(IExecutionContext context) => Server(context).UseGoldBooster(Me(context));

        [CloudCodeFunction(CloudApi.ClaimRoad)]
        public Task<EconReply<RoadClaimResult>> ClaimRoad(IExecutionContext context) => Server(context).ClaimRoad(Me(context));

        [CloudCodeFunction(CloudApi.AdReward)]
        public Task<EconReply<int>> AdReward(IExecutionContext context, bool doubleReward) => Server(context).AdReward(Me(context), doubleReward);

        [CloudCodeFunction(CloudApi.ClaimStarReward)]
        public Task<EconReply<int>> ClaimStarReward(IExecutionContext context, int chapter, int goal) => Server(context).ClaimStarReward(Me(context), chapter, goal);

        [CloudCodeFunction(CloudApi.ClaimQuest)]
        public Task<EconReply<int>> ClaimQuest(IExecutionContext context, string key) => Server(context).ClaimQuest(Me(context), key);

        [CloudCodeFunction(CloudApi.ClaimAchievement)]
        public Task<EconReply<int>> ClaimAchievement(IExecutionContext context, string key) => Server(context).ClaimAchievement(Me(context), key);

        [CloudCodeFunction(CloudApi.ClaimSeason)]
        public Task<EconReply<bool>> ClaimSeason(IExecutionContext context, int tier, bool premium) => Server(context).ClaimSeason(Me(context), tier, premium);

        [CloudCodeFunction(CloudApi.ClaimMonthly)]
        public Task<EconReply<int>> ClaimMonthly(IExecutionContext context) => Server(context).ClaimMonthly(Me(context));

        // ---------------- 판 결과 ----------------
        [CloudCodeFunction(CloudApi.RunStart)]
        public Task<RunTicket> RunStart(IExecutionContext context, bool endless, string stageId) => Server(context).RunStart(Me(context), endless, stageId);

        [CloudCodeFunction(CloudApi.RunEnd)]
        public Task<EconReply<RunEndResult>> RunEnd(IExecutionContext context, string id, bool cleared, int stars, int wave)
            => Server(context).RunEnd(Me(context), id, cleared, stars, wave);

        // ---------------- 결제 ----------------
        [CloudCodeFunction(CloudApi.RedeemPurchase)]
        public Task<PurchaseReply> RedeemPurchase(IExecutionContext context, string productId, string purchaseData, string signature, int localCost, string localCurrency)
            => Server(context).RedeemPurchase(Me(context), productId, purchaseData, signature, localCost, localCurrency);

        // ---------------- 대전 ----------------
        [CloudCodeFunction(CloudApi.PvpQueue)]
        public Task<QueueStatus> PvpQueue(IExecutionContext context) => Server(context).PvpQueue(Me(context));

        [CloudCodeFunction(CloudApi.PvpPoll)]
        public Task<QueueStatus> PvpPoll(IExecutionContext context) => Server(context).PvpPoll(Me(context));

        [CloudCodeFunction(CloudApi.PvpCancel)]
        public Task<bool> PvpCancel(IExecutionContext context) => Server(context).PvpCancel(Me(context));

        [CloudCodeFunction(CloudApi.PvpReady)]
        public Task<MatchState> PvpReady(IExecutionContext context, string matchId, string relayJoinCode)
            => Server(context).PvpReady(Me(context), matchId, relayJoinCode);

        [CloudCodeFunction(CloudApi.PvpPing)]
        public Task<MatchState> PvpPing(IExecutionContext context, string matchId) => Server(context).PvpPing(Me(context), matchId);

        [CloudCodeFunction(CloudApi.PvpDefeat)]
        public Task<PvpMatchResult> PvpDefeat(IExecutionContext context, string matchId) => Server(context).PvpDefeat(Me(context), matchId);

        [CloudCodeFunction(CloudApi.PvpLeave)]
        public Task<PvpMatchResult> PvpLeave(IExecutionContext context, string matchId) => Server(context).PvpLeave(Me(context), matchId);

        [CloudCodeFunction(CloudApi.PvpClaimAway)]
        public Task<PvpMatchResult> PvpClaimAway(IExecutionContext context, string matchId) => Server(context).PvpClaimAway(Me(context), matchId);

        [CloudCodeFunction(CloudApi.PvpBotStart)]
        public Task<RunTicket> PvpBotStart(IExecutionContext context) => Server(context).PvpBotStart(Me(context));

        [CloudCodeFunction(CloudApi.PvpBotResult)]
        public Task<BotResultReply> PvpBotResult(IExecutionContext context, string id, bool won) => Server(context).PvpBotResult(Me(context), id, won);

        // ---------------- 닉네임·랭킹 ----------------
        [CloudCodeFunction(CloudApi.SetNick)]
        public Task<string> SetNick(IExecutionContext context, string nick) => Server(context).SetNick(Me(context), nick);

        [CloudCodeFunction(CloudApi.Leaderboard)]
        public Task<LeaderboardReply> Leaderboard(IExecutionContext context) => Server(context).Leaderboard(Me(context));
    }
}
