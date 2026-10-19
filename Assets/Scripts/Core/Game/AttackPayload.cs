namespace DefenseBattle.Core
{
    /// <summary>PvP: 상대에게 보내는 몹 카드 (types.ts AttackPayload)</summary>
    public struct AttackPayload
    {
        /// <summary>몹 카드 번호 (Attacks.All 번호)</summary>
        public int Card;
        /// <summary>보낸 쪽 웨이브 (체력 기준)</summary>
        public int Wave;
        /// <summary>던진 위치: 길(사각 링) 둘레 비율 0~1 (출발점=0, 반시계)</summary>
        public double Pos;
    }
}
