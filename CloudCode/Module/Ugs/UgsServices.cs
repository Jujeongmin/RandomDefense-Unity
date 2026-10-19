using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DefenseBattle.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.Economy.Model;
using Unity.Services.Leaderboards.Model;

namespace DefenseBattle.Server.Ugs
{
    public sealed class SystemClock : IClock
    {
        public long NowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>INotifier → Cloud Code 푸시 메시지 (기기는 SubscribeToPlayerMessagesAsync 로 받는다)</summary>
    public sealed class UgsNotifier : INotifier
    {
        readonly IPushClient m_push;
        readonly IExecutionContext m_ctx;

        public UgsNotifier(IPushClient push, IExecutionContext ctx)
        {
            m_push = push;
            m_ctx = ctx;
        }

        public async Task Send(string playerId, string type, object payload)
        {
            try
            {
                await m_push.SendPlayerMessageAsync(m_ctx, JsonConvert.SerializeObject(payload, Json.Settings), type, playerId);
            }
            catch (ApiException)
            {
                // 알림은 덤 - 기기가 poll 로도 확인하므로 실패해도 진행
            }
        }
    }

    /// <summary>IRankBoard → UGS Leaderboards "trophies" (대시보드에서 '마지막 점수 유지'로 만든다 - 트로피는 내려가기도 하므로)</summary>
    public sealed class UgsRankBoard : IRankBoard
    {
        public const string LeaderboardId = "trophies";

        readonly IGameApiClient m_api;
        readonly IExecutionContext m_ctx;

        public UgsRankBoard(IGameApiClient api, IExecutionContext ctx)
        {
            m_api = api;
            m_ctx = ctx;
        }

        public async Task Submit(string playerId, int trophies, int wins, string nick)
        {
            var meta = new Dictionary<string, object> { { "nick", nick }, { "wins", wins } };
            await m_api.Leaderboards.AddLeaderboardPlayerScoreAsync(m_ctx, m_ctx.ServiceToken, Guid.Parse(m_ctx.ProjectId), LeaderboardId, playerId,
                new AddLeaderboardScore(trophies, meta));
        }

        public async Task<List<RankEntry>> Top(int count)
        {
            var res = await m_api.Leaderboards.GetLeaderboardScoresAsync(m_ctx, m_ctx.ServiceToken, Guid.Parse(m_ctx.ProjectId), LeaderboardId, true, 0, count);
            var list = new List<RankEntry>();
            foreach (LeaderboardEntry e in res.Data.Results)
            {
                JObject meta = e.Metadata == null ? null : e.Metadata as JObject ?? JObject.Parse(e.Metadata.ToString());
                list.Add(new RankEntry
                {
                    PlayerId = e.PlayerId,
                    Nick = (string)meta?["nick"] ?? Loc.Tl(NickFilter.DefaultNick),
                    Wins = (int?)meta?["wins"] ?? 0,
                    Trophies = (int)e.Score,
                    Rank = e.Rank + 1,
                });
            }
            return list;
        }
    }

    /// <summary>
    /// IReceiptVerifier → UGS Economy 의 Google Play 영수증 검증. Economy 대시보드에 상품마다
    /// '실제 결제(Real money purchase)' 항목을 만들어 둬야 한다: Economy id = productId 대문자, Google Play 상품 id = productId.
    /// Economy 가 서명 확인·같은 영수증 재사용·다른 플레이어 영수증·상품 불일치를 걸러 준다. 실제 지급은 GameServer 가 한다.
    /// </summary>
    public sealed class UgsReceiptVerifier : IReceiptVerifier
    {
        readonly IGameApiClient m_api;
        readonly IExecutionContext m_ctx;

        public UgsReceiptVerifier(IGameApiClient api, IExecutionContext ctx)
        {
            m_api = api;
            m_ctx = ctx;
        }

        public static string EconomyId(string productId) => productId.ToUpperInvariant();

        public async Task<ReceiptCheck> Verify(string playerId, string productId, string purchaseData, string signature, int localCost, string localCurrency)
        {
            string orderId;
            try
            {
                JObject data = JObject.Parse(purchaseData);
                if ((string)data["productId"] != productId) return new ReceiptCheck { Valid = false };
                orderId = (string)data["orderId"] ?? (string)data["purchaseToken"];
            }
            catch (JsonException)
            {
                return new ReceiptCheck { Valid = false };
            }
            var req = new PlayerPurchaseGoogleplaystoreRequest(EconomyId(productId), purchaseData, signature, localCost, localCurrency ?? "KRW");
            try
            {
                var res = await m_api.EconomyPurchases.RedeemGooglePlayPurchaseAsync(m_ctx, m_ctx.ServiceToken, m_ctx.ProjectId, playerId, req);
                string status = res.Data?.Verification?.Status.ToString();
                bool ok = status == "VALID" || status == "VALIDNOTREDEEMED";
                return new ReceiptCheck { Valid = ok, OrderId = orderId };
            }
            catch (ApiException e) when (e.Response != null && (int)e.Response.StatusCode == 422)
            {
                // 검증 실패 이유: data.verification.status
                string status = null;
                try { status = (string)JObject.Parse(e.Response.RawContent ?? "{}").SelectToken("data.verification.status"); }
                catch (JsonException) { }
                return new ReceiptCheck { Valid = false, AlreadyRedeemed = status == "INVALID_ALREADY_REDEEMED" || status == "INVALIDALREADYREDEEMED", OrderId = orderId };
            }
        }
    }
}
