namespace DefenseBattle.Core
{
    /// <summary>서버 경제 함수의 답: 바뀐 뒤의 경제 상태 (기기는 이 값으로 덮어쓴다)</summary>
    public class EconReply
    {
        public EconState Econ;
    }

    /// <summary>서버 경제 함수의 답 + 동작 결과</summary>
    public class EconReply<T> : EconReply
    {
        public T Result;
    }
}
