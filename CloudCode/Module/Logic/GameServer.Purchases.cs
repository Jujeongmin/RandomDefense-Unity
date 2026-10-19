using System.Collections.Generic;
using System.Threading.Tasks;
using DefenseBattle.Core;

namespace DefenseBattle.Server
{
    // 결제 지급 (원본 $onItemPurchased) - 스토어 영수증을 검증한 뒤 서버가 지급, 같은 주문은 한 번만
    public sealed partial class GameServer
    {
        public async Task<PurchaseReply> RedeemPurchase(string playerId, string productId, string purchaseData, string signature, int localCost, string localCurrency)
        {
            ProductDef def = Products.Get(productId);
            if (def == null) return new PurchaseReply { Granted = false, Econ = await LoadEcon(playerId) };
            ReceiptCheck check = await m_receipts.Verify(playerId, productId, purchaseData, signature, localCost, localCurrency);
            if (check == null || (!check.Valid && !check.AlreadyRedeemed) || string.IsNullOrEmpty(check.OrderId))
                return new PurchaseReply { Granted = false, Econ = await LoadEcon(playerId) };

            // 검증은 됐는데 지급 기록이 없으면 (지난번에 지급 도중 실패) 지금 지급한다
            EconReply<PurchaseReply> r = await WithEcon(playerId, e =>
            {
                if (!e.MarkProcessed("buy:" + check.OrderId)) return new PurchaseReply { Granted = false, AlreadyGranted = true };
                List<Reveal> reveals = EconRules.GrantProduct(e, productId, 1, Ctx());
                return new PurchaseReply { Granted = true, Reveals = reveals };
            });
            r.Result.Econ = r.Econ;
            return r.Result;
        }
    }
}
