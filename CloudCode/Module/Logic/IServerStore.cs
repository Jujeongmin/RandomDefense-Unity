using System.Threading.Tasks;

namespace DefenseBattle.Server
{
    /// <summary>
    /// 서버 저장소 (UGS Cloud Save 자리). 플레이어 데이터 = 그 플레이어의 서버 전용(Protected) 값,
    /// 공용 데이터 = 게임 전체 값(대기열·경기). 쓰기는 낙관적 잠금: 읽을 때 받은 WriteLock 을 넘겨야 하고,
    /// 그 사이 누가 먼저 썼으면 WriteConflictException.
    /// </summary>
    public interface IServerStore
    {
        /// <summary>없으면 null</summary>
        Task<StoredItem> GetPlayer(string playerId, string key);
        /// <returns>새 WriteLock</returns>
        Task<string> SetPlayer(string playerId, string key, string json, string writeLock);
        Task<StoredItem> GetShared(string key);
        Task<string> SetShared(string key, string json, string writeLock);
    }

    /// <summary>저장된 값 (JSON)과 다음 쓰기에 넘길 잠금</summary>
    public sealed class StoredItem
    {
        public string Json;
        public string WriteLock;
    }

    /// <summary>다른 호출이 먼저 써서 다시 읽고 해야 함</summary>
    public sealed class WriteConflictException : System.Exception
    {
        public WriteConflictException(string key) : base("write conflict: " + key) { }
    }
}
