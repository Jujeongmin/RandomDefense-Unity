using System.Collections.Generic;

namespace DefenseBattle.Core
{
    /// <summary>결제 지급 결과. Granted=false 면 검증 실패 또는 이미 지급한 영수증 (그래도 구매 확인(consume)은 해도 된다)</summary>
    public sealed class PurchaseReply
    {
        public bool Granted;
        /// <summary>이미 지급된 영수증이었으면 true (중복 호출 - 화면엔 성공으로 보여도 됨)</summary>
        public bool AlreadyGranted;
        public List<Reveal> Reveals = new List<Reveal>();
        public EconState Econ;
    }
}
