using System.Collections.Generic;
using System.Threading.Tasks;
using DefenseBattle.Core;
using DefenseBattle.Server;

namespace DefenseBattle.Server.Tests
{
    /// <summary>Cloud Save 흉내: 값마다 버전(잠금)을 두고, 잠금이 다르면 충돌. ConflictsToInject 만큼 일부러 충돌</summary>
    sealed class MemoryStore : IServerStore
    {
        readonly Dictionary<string, (string json, int ver)> m_data = new Dictionary<string, (string, int)>();
        public int ConflictsToInject;
        public int Writes;

        Task<StoredItem> Get(string k)
            => Task.FromResult(m_data.TryGetValue(k, out var v) ? new StoredItem { Json = v.json, WriteLock = v.ver.ToString() } : null);

        Task<string> Set(string k, string json, string writeLock)
        {
            if (ConflictsToInject > 0) { ConflictsToInject--; throw new WriteConflictException(k); }
            if (m_data.TryGetValue(k, out var cur) && writeLock != null && writeLock != cur.ver.ToString()) throw new WriteConflictException(k);
            int ver = (m_data.TryGetValue(k, out cur) ? cur.ver : 0) + 1;
            m_data[k] = (json, ver);
            Writes++;
            return Task.FromResult(ver.ToString());
        }

        public Task<StoredItem> GetPlayer(string playerId, string key) => Get("p:" + playerId + ":" + key);
        public Task<string> SetPlayer(string playerId, string key, string json, string writeLock) => Set("p:" + playerId + ":" + key, json, writeLock);
        public Task<StoredItem> GetShared(string key) => Get("s:" + key);
        public Task<string> SetShared(string key, string json, string writeLock) => Set("s:" + key, json, writeLock);
    }

    sealed class FakeClock : IClock
    {
        public long NowMs { get; set; } = 1792368000000L; // 2026-10-19 00:00 UTC
        public void Advance(long ms) => NowMs += ms;
    }

    sealed class RecordingNotifier : INotifier
    {
        public readonly List<(string player, string type, object payload)> Sent = new List<(string, string, object)>();

        public Task Send(string playerId, string type, object payload)
        {
            Sent.Add((playerId, type, payload));
            return Task.CompletedTask;
        }
    }

    sealed class MemoryRankBoard : IRankBoard
    {
        public readonly Dictionary<string, RankEntry> Rows = new Dictionary<string, RankEntry>();

        public Task Submit(string playerId, int trophies, int wins, string nick)
        {
            Rows[playerId] = new RankEntry { PlayerId = playerId, Trophies = trophies, Wins = wins, Nick = nick };
            return Task.CompletedTask;
        }

        public Task<List<RankEntry>> Top(int count)
        {
            var list = new List<RankEntry>(Rows.Values);
            list.Sort((a, b) => b.Trophies.CompareTo(a.Trophies));
            return Task.FromResult(list.GetRange(0, System.Math.Min(count, list.Count)));
        }
    }

    /// <summary>영수증 흉내: purchaseData 가 "ok:주문번호" 면 통과, "dup:주문번호" 면 이미 검증됨</summary>
    sealed class FakeReceipts : IReceiptVerifier
    {
        public Task<ReceiptCheck> Verify(string playerId, string productId, string purchaseData, string signature, int localCost, string localCurrency)
        {
            if (purchaseData != null && purchaseData.StartsWith("ok:")) return Task.FromResult(new ReceiptCheck { Valid = true, OrderId = purchaseData.Substring(3) });
            if (purchaseData != null && purchaseData.StartsWith("dup:")) return Task.FromResult(new ReceiptCheck { AlreadyRedeemed = true, OrderId = purchaseData.Substring(4) });
            return Task.FromResult(new ReceiptCheck { Valid = false });
        }
    }

    /// <summary>테스트마다 새 서버</summary>
    sealed class Rig
    {
        public readonly MemoryStore Store = new MemoryStore();
        public readonly FakeClock Clock = new FakeClock();
        public readonly RecordingNotifier Push = new RecordingNotifier();
        public readonly MemoryRankBoard Rank = new MemoryRankBoard();
        public readonly GameServer Server;
        int m_seed = 1;

        public Rig()
        {
            Server = new GameServer(Store, Clock, Push, Rank, new FakeReceipts(), () => new SystemRandom(m_seed++));
        }
    }
}
