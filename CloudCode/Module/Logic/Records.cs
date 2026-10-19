using System.Collections.Generic;
using DefenseBattle.Core;

namespace DefenseBattle.Server
{
    /// <summary>플레이어 서버 전용 데이터 키 / 공용 데이터 키</summary>
    public static class Keys
    {
        public const string Econ = "econ";
        public const string Run = "run";
        public const string Bot = "pvpBot";
        public const string Profile = "profile";
        /// <summary>내가 기다리는 동안 다른 사람이 나를 매칭했을 때 (푸시를 못 받았을 때 poll 로 가져감)</summary>
        public const string PendingMatch = "pvpMatch";
        public const string Queue = "pvpQueue";
        public static string Match(string id) => "match_" + id;
    }

    /// <summary>경제 저장본 (형식이 바뀌면 V 를 올리고 옮기는 코드를 둔다)</summary>
    public sealed class EconRecord
    {
        public const int Version = 1;
        public int V = Version;
        public EconState Data;
    }

    /// <summary>판 시작 기록 (원본 run)</summary>
    public sealed class RunRecord
    {
        public string Id;
        public bool Endless;
        public string StageId;
        public long T;
    }

    /// <summary>봇전 시작 기록</summary>
    public sealed class BotRecord
    {
        public string Id;
        public long T;
    }

    public sealed class ProfileRecord
    {
        public string Nick;
    }

    public sealed class QueueEntry
    {
        public string PlayerId;
        public string Nick;
        public int Trophies;
        /// <summary>마지막 하트비트 (ms)</summary>
        public long T;
        /// <summary>처음 대기열에 들어온 시각 - 오래 기다릴수록 허용 트로피 폭이 넓어진다</summary>
        public long Since;
    }

    public sealed class MatchPlayer
    {
        public string PlayerId;
        public string Nick;
        public int Trophies;
        public bool Ready;
        /// <summary>마지막으로 서버를 부른 시각 (이탈 판정)</summary>
        public long LastSeen;
    }

    /// <summary>경기 기록 (원본 룸 상태)</summary>
    public sealed class MatchRecord
    {
        public string MatchId;
        public uint Seed;
        public long T;
        /// <summary>[0] = 호스트 (Relay 방을 만든다)</summary>
        public List<MatchPlayer> Players = new List<MatchPlayer>();
        public string Status = MatchState.Waiting;
        public long StartAt;
        public string RelayJoinCode;
        public PvpMatchResult Result;

        public MatchPlayer Find(string playerId) => Players.Find(p => p.PlayerId == playerId);
        public MatchPlayer Other(string playerId) => Players.Find(p => p.PlayerId != playerId);
    }
}
