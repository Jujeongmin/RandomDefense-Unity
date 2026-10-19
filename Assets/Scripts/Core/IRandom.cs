namespace DefenseBattle.Core
{
    /// <summary>0 이상 1 미만의 난수를 내는 소스 (원본의 Math.random / makeRng 자리)</summary>
    public interface IRandom
    {
        /// <summary>[0, 1) 난수</summary>
        double Next();
    }
}
