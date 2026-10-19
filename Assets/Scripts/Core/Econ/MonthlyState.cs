namespace DefenseBattle.Core
{
    /// <summary>월정액: 만료 시각(ms)과 마지막 일일 수령 날짜 ("yyyy-MM-dd")</summary>
    public sealed class MonthlyState
    {
        public long Expires;
        public string LastClaim = "";
    }
}
