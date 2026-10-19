namespace DefenseBattle.Core
{
    /// <summary>
    /// 서버(UGS Cloud Code 모듈) 이름과 함수 이름. 클라이언트는 CallModuleEndpointAsync(Module, 함수, 인자)로 부른다.
    /// 인자 이름은 서버 함수의 매개변수 이름과 같아야 한다 (각 상수 옆 주석).
    /// </summary>
    public static class CloudApi
    {
        public const string Module = "DefenseBattleCloud";

        // ---- 경제 ----
        public const string EconSync = "EconSync";                 // () → EconReply
        public const string SetDecks = "SetDecks";                 // (deck: Job[], attackDeck: AttackKey[]) → EconReply<bool>
        public const string StartUnlock = "StartUnlock";           // (slot) → EconReply<bool>
        public const string OpenSlot = "OpenSlot";                 // (slot, pay) → EconReply<Reveal>
        public const string Gacha = "Gacha";                       // (count, ticket) → EconReply<Reveal>
        public const string UpgradeUnit = "UpgradeUnit";           // (job) → EconReply<bool>
        public const string BuyUnit = "BuyUnit";                   // (job) → EconReply<bool>
        public const string Research = "Research";                 // (key) → EconReply<bool>
        public const string UseGoldBooster = "UseGoldBooster";     // () → EconReply<bool>
        public const string ClaimRoad = "ClaimRoad";               // () → EconReply<RoadClaimResult>
        public const string AdReward = "AdReward";                 // (doubleReward) → EconReply<int>
        public const string ClaimStarReward = "ClaimStarReward";   // (chapter, goal) → EconReply<int>
        public const string ClaimQuest = "ClaimQuest";             // (key) → EconReply<int>
        public const string ClaimAchievement = "ClaimAchievement"; // (key) → EconReply<int>
        public const string ClaimSeason = "ClaimSeason";           // (tier, premium) → EconReply<bool>
        public const string ClaimMonthly = "ClaimMonthly";         // () → EconReply<int>

        // ---- 판 결과 ----
        public const string RunStart = "RunStart";                 // (endless, stageId) → RunTicket
        public const string RunEnd = "RunEnd";                     // (id, cleared, stars, wave) → EconReply<RunEndResult> (null = 보상 없음)

        // ---- 결제 ----
        public const string RedeemPurchase = "RedeemPurchase";     // (productId, purchaseData, signature, localCost, localCurrency) → PurchaseReply

        // ---- 대전 ----
        public const string PvpQueue = "PvpQueue";                 // () → QueueStatus
        public const string PvpPoll = "PvpPoll";                   // () → QueueStatus
        public const string PvpCancel = "PvpCancel";               // () → bool
        public const string PvpReady = "PvpReady";                 // (matchId, relayJoinCode) → MatchState
        public const string PvpPing = "PvpPing";                   // (matchId) → MatchState (경기 중 몇 초마다 - 이탈 판정용)
        public const string PvpDefeat = "PvpDefeat";               // (matchId) → PvpMatchResult
        public const string PvpLeave = "PvpLeave";                 // (matchId) → PvpMatchResult
        public const string PvpClaimAway = "PvpClaimAway";         // (matchId) → PvpMatchResult (상대가 오래 응답 없으면 승리, 아니면 null)
        public const string PvpBotStart = "PvpBotStart";           // () → RunTicket
        public const string PvpBotResult = "PvpBotResult";         // (id, won) → BotResultReply (null = 무시됨)

        // ---- 닉네임·랭킹 ----
        public const string SetNick = "SetNick";                   // (nick) → string (실제로 저장된 이름)
        public const string Leaderboard = "Leaderboard";           // () → LeaderboardReply

        // ---- 서버가 보내는 알림 (Cloud Code 푸시 메시지 type) ----
        public const string PushMatch = "pvpMatch";                // MatchInfo
        public const string PushMatchState = "pvpState";           // MatchState
        public const string PushResult = "pvpResult";              // PvpMatchResult
    }
}
