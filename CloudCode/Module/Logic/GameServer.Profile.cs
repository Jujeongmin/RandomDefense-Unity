using System.Threading.Tasks;
using DefenseBattle.Core;

namespace DefenseBattle.Server
{
    // 닉네임·랭킹 (원본 setNick / pvpLeaderboard)
    public sealed partial class GameServer
    {
        /// <summary>닉네임 바꾸기 (비속어·빈 이름은 기본 이름). 대전 기록이 있으면 랭킹 이름도 바로 바꾼다</summary>
        public async Task<string> SetNick(string playerId, string nick)
        {
            string clean = NickFilter.Clean(nick);
            await m_store.MutatePlayer<ProfileRecord, bool>(playerId, Keys.Profile, () => new ProfileRecord(), p =>
            {
                p.Nick = clean;
                return true;
            });
            EconState e = await LoadEcon(playerId);
            if (e.Pvp.Wins + e.Pvp.Losses > 0) await m_rank.Submit(playerId, e.Pvp.Trophies, e.Pvp.Wins, clean);
            return clean;
        }

        public async Task<LeaderboardReply> Leaderboard(string playerId)
        {
            EconState e = await LoadEcon(playerId);
            return new LeaderboardReply { Top = await m_rank.Top(50), Me = e.Pvp };
        }
    }
}
