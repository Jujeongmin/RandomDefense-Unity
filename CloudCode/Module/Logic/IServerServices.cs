using System.Collections.Generic;
using System.Threading.Tasks;
using DefenseBattle.Core;

namespace DefenseBattle.Server
{
    /// <summary>지금 시각 (ms, UTC)</summary>
    public interface IClock
    {
        long NowMs { get; }
    }

    /// <summary>결제 영수증 검증 (UGS Economy 의 Google Play 검증 자리)</summary>
    public interface IReceiptVerifier
    {
        Task<ReceiptCheck> Verify(string playerId, string productId, string purchaseData, string signature, int localCost, string localCurrency);
    }

    /// <summary>영수증 검증 결과. OrderId 는 같은 결제를 두 번 지급하지 않게 쓰는 id</summary>
    public sealed class ReceiptCheck
    {
        public bool Valid;
        /// <summary>이 플레이어가 이미 검증받은 영수증 (서버가 지급 도중 실패했을 수 있어 지급 기록을 다시 본다)</summary>
        public bool AlreadyRedeemed;
        public string OrderId;
    }

    /// <summary>플레이어에게 알림 보내기 (UGS Cloud Code 푸시 자리)</summary>
    public interface INotifier
    {
        Task Send(string playerId, string type, object payload);
    }

    /// <summary>트로피 랭킹 (UGS Leaderboards 자리)</summary>
    public interface IRankBoard
    {
        Task Submit(string playerId, int trophies, int wins, string nick);
        Task<List<RankEntry>> Top(int count);
    }
}
