using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DefenseBattle.Core;

namespace DefenseBattle.Server
{
    // 실시간 대전 (원본 pvpQueue ~ finishMatch). 실시간 전장 중계는 두 기기가 UGS Relay 로 직접 하고,
    // 서버는 매칭·시작 시각·이탈 판정·결과(트로피·보상)만 맡는다.
    public sealed partial class GameServer
    {
        /// <summary>대기열 하트비트 만료 - 클라이언트가 2초마다 poll</summary>
        public const long QueueTtlMs = 15_000;
        /// <summary>매칭 결과 보관</summary>
        public const long MatchTtlMs = 60_000;
        /// <summary>둘 다 준비되면 이만큼 뒤에 동시 시작</summary>
        public const long StartDelayMs = 3_500;
        /// <summary>대전 중 응답이 끊긴 쪽을 기다려 주는 시간 - 지나면 남은 쪽이 승리를 요청할 수 있다</summary>
        public const long AwayGraceMs = 20_000;
        /// <summary>매칭 뒤 이만큼 안에 둘 다 준비 안 되면 경기 취소</summary>
        public const long ReadyTimeoutMs = 30_000;

        sealed class QueueList
        {
            public List<QueueEntry> Entries = new List<QueueEntry>();
        }

        async Task<string> NickOf(string playerId)
        {
            ProfileRecord p = await m_store.ReadPlayer<ProfileRecord>(playerId, Keys.Profile);
            return p?.Nick ?? Loc.Tl(NickFilter.DefaultNick);
        }

        // ---------------- 매칭 ----------------
        /// <summary>대기열 등록. 트로피가 가장 가까운 상대가 있으면 바로 매칭 (오래 기다린 사람일수록 허용 폭 확대)</summary>
        public async Task<QueueStatus> PvpQueue(string playerId)
        {
            EconState me = await LoadEcon(playerId);
            string nick = await NickOf(playerId);
            await ClearPendingMatch(playerId);
            long now = Now;
            MatchInfo forMe = null, forOpp = null;
            QueueEntry opp = null;
            await m_store.MutateShared<QueueList, bool>(Keys.Queue, () => new QueueList(), q =>
            {
                forMe = forOpp = null;
                opp = null;
                q.Entries.RemoveAll(x => now - x.T >= QueueTtlMs || x.PlayerId == playerId);
                double bestDiff = double.PositiveInfinity;
                foreach (QueueEntry x in q.Entries)
                {
                    double waited = (now - x.Since) / 1000.0;
                    int diff = Math.Abs(x.Trophies - me.Pvp.Trophies);
                    if (diff > 200 + waited * 40) continue;
                    if (diff < bestDiff) { bestDiff = diff; opp = x; }
                }
                if (opp != null)
                {
                    QueueEntry o = opp;
                    q.Entries.RemoveAll(x => x.PlayerId == o.PlayerId);
                    return true;
                }
                q.Entries.Add(new QueueEntry { PlayerId = playerId, Nick = nick, Trophies = me.Pvp.Trophies, T = now, Since = now });
                return true;
            });
            if (opp == null) return new QueueStatus { Status = QueueStatus.Waiting };

            // 매칭: 새로 들어온 쪽(나)이 호스트 - 바로 답을 받으니 Relay 방을 먼저 만든다
            var rec = new MatchRecord
            {
                MatchId = NewId(),
                Seed = (uint)(m_newRandom().Next() * int.MaxValue) + 1,
                T = now,
                Players =
                {
                    new MatchPlayer { PlayerId = playerId, Nick = nick, Trophies = me.Pvp.Trophies, LastSeen = now },
                    new MatchPlayer { PlayerId = opp.PlayerId, Nick = opp.Nick, Trophies = opp.Trophies, LastSeen = now },
                },
            };
            await m_store.SetShared(Keys.Match(rec.MatchId), Json.Write(rec), null);
            forMe = new MatchInfo { MatchId = rec.MatchId, Seed = rec.Seed, T = now, Host = true,
                Opponent = new OpponentInfo { PlayerId = opp.PlayerId, Nick = opp.Nick, Trophies = opp.Trophies } };
            forOpp = new MatchInfo { MatchId = rec.MatchId, Seed = rec.Seed, T = now, Host = false,
                Opponent = new OpponentInfo { PlayerId = playerId, Nick = nick, Trophies = me.Pvp.Trophies } };
            MatchInfo notify = forOpp;
            await m_store.MutatePlayer<PendingMatchRecord, bool>(opp.PlayerId, Keys.PendingMatch, () => new PendingMatchRecord(), p =>
            {
                p.Match = notify;
                return true;
            });
            await m_notifier.Send(opp.PlayerId, CloudApi.PushMatch, forOpp);
            return new QueueStatus { Status = QueueStatus.Matched, Match = forMe };
        }

        sealed class PendingMatchRecord
        {
            public MatchInfo Match;
        }

        Task ClearPendingMatch(string playerId)
            => m_store.MutatePlayer<PendingMatchRecord, bool>(playerId, Keys.PendingMatch, () => new PendingMatchRecord(), p =>
            {
                p.Match = null;
                return true;
            });

        /// <summary>대기 중 하트비트 + 매칭 확인 (푸시 유실 대비)</summary>
        public async Task<QueueStatus> PvpPoll(string playerId)
        {
            long now = Now;
            PendingMatchRecord pending = await m_store.ReadPlayer<PendingMatchRecord>(playerId, Keys.PendingMatch);
            if (pending?.Match != null && now - pending.Match.T < MatchTtlMs)
            {
                await ClearPendingMatch(playerId);
                return new QueueStatus { Status = QueueStatus.Matched, Match = pending.Match };
            }
            bool inQueue = await m_store.MutateShared<QueueList, bool>(Keys.Queue, () => new QueueList(), q =>
            {
                QueueEntry mine = q.Entries.Find(x => x.PlayerId == playerId);
                if (mine == null) return false;
                mine.T = now;
                return true;
            });
            return new QueueStatus { Status = inQueue ? QueueStatus.Waiting : QueueStatus.Idle };
        }

        public async Task<bool> PvpCancel(string playerId)
        {
            await m_store.MutateShared<QueueList, bool>(Keys.Queue, () => new QueueList(), q =>
            {
                q.Entries.RemoveAll(x => x.PlayerId == playerId);
                return true;
            });
            await ClearPendingMatch(playerId);
            return true;
        }

        // ---------------- 경기 ----------------
        static MatchState StateOf(MatchRecord r, long now) => new MatchState
        {
            MatchId = r.MatchId, Status = r.Status, StartAt = r.StartAt, Now = now, Seed = r.Seed, RelayJoinCode = r.RelayJoinCode, Result = r.Result,
        };

        /// <summary>
        /// 경기 준비 완료. 호스트는 Relay 참가 코드를 같이 올린다. 둘 다 준비되고 코드가 있으면 StartDelayMs 뒤 동시 시작.
        /// 상대가 시간 안에 준비하지 않으면 취소.
        /// </summary>
        public async Task<MatchState> PvpReady(string playerId, string matchId, string relayJoinCode)
        {
            long now = Now;
            bool started = false;
            MatchRecord rec = await MutateMatch(matchId, r =>
            {
                started = false;
                MatchPlayer me = r.Find(playerId);
                if (me == null || r.Status != MatchState.Waiting) return;
                me.Ready = true;
                me.LastSeen = now;
                if (r.Players[0].PlayerId == playerId && !string.IsNullOrEmpty(relayJoinCode)) r.RelayJoinCode = relayJoinCode;
                if (r.Players.TrueForAll(p => p.Ready) && !string.IsNullOrEmpty(r.RelayJoinCode))
                {
                    r.Status = MatchState.Playing;
                    r.StartAt = now + StartDelayMs;
                    started = true;
                }
                else if (now - r.T > ReadyTimeoutMs) r.Status = MatchState.Cancelled;
            });
            if (rec == null || rec.Find(playerId) == null) return null;
            MatchState st = StateOf(rec, now);
            if (started || rec.Status == MatchState.Cancelled)
            {
                foreach (MatchPlayer p in rec.Players) await m_notifier.Send(p.PlayerId, CloudApi.PushMatchState, st);
            }
            return st;
        }

        /// <summary>경기 중 몇 초마다: 내가 아직 있다고 알리고 상태를 받는다 (이탈 판정·재접속 때 결과 확인)</summary>
        public async Task<MatchState> PvpPing(string playerId, string matchId)
        {
            long now = Now;
            MatchRecord rec = await MutateMatch(matchId, r =>
            {
                MatchPlayer me = r.Find(playerId);
                if (me != null) me.LastSeen = now;
            });
            return rec == null || rec.Find(playerId) == null ? null : StateOf(rec, now);
        }

        /// <summary>내 전장이 무너짐 → 상대 승리</summary>
        public Task<PvpMatchResult> PvpDefeat(string playerId, string matchId) => FinishMatch(matchId, playerId, "defeat");

        /// <summary>경기 중 나가기 → 상대 승리 (준비 전이면 취소)</summary>
        public async Task<PvpMatchResult> PvpLeave(string playerId, string matchId)
        {
            MatchRecord rec = await MutateMatch(matchId, r =>
            {
                if (r.Find(playerId) != null && r.Status == MatchState.Waiting) r.Status = MatchState.Cancelled;
            });
            if (rec == null) return null;
            if (rec.Status == MatchState.Cancelled)
            {
                MatchPlayer other = rec.Other(playerId);
                if (other != null) await m_notifier.Send(other.PlayerId, CloudApi.PushMatchState, StateOf(rec, Now));
                return null;
            }
            return await FinishMatch(matchId, playerId, "leave");
        }

        /// <summary>상대가 AwayGraceMs 넘게 응답이 없다 → 남은 쪽 승리 (서버가 마지막 응답 시각으로 직접 확인)</summary>
        public async Task<PvpMatchResult> PvpClaimAway(string playerId, string matchId)
        {
            long now = Now;
            MatchRecord rec = await MutateMatch(matchId, r =>
            {
                MatchPlayer me = r.Find(playerId);
                if (me != null) me.LastSeen = now;
            });
            if (rec == null || rec.Status != MatchState.Playing || rec.Find(playerId) == null) return null;
            MatchPlayer gone = rec.Other(playerId);
            if (gone == null || now - Math.Max(gone.LastSeen, rec.StartAt) < AwayGraceMs) return null;
            return await FinishMatch(matchId, gone.PlayerId, "leave");
        }

        /// <summary>경기 기록을 잠금 안에서 바꾼다. 없는 경기면 null (아무것도 쓰지 않음)</summary>
        async Task<MatchRecord> MutateMatch(string matchId, Action<MatchRecord> fn)
        {
            if (string.IsNullOrEmpty(matchId)) return null;
            MatchRecordResult r = await m_store.MutateShared<MatchRecord, MatchRecordResult>(Keys.Match(matchId), () => new MatchRecord(), rec =>
            {
                if (rec.MatchId == null) return new MatchRecordResult { Changed = false };
                fn(rec);
                return new MatchRecordResult { Value = rec };
            });
            return r.Value;
        }

        sealed class MatchRecordResult : IMutation
        {
            public bool Changed { get; set; } = true;
            public MatchRecord Value;
        }

        /// <summary>
        /// 승패 확정 (중복 호출 안전). 경기 기록을 먼저 'done' 으로 바꾼 쪽만 트로피·보상을 지급한다.
        /// 각 플레이어 지급은 경기 id 로 한 번만 (도중에 실패해 다시 불려도 두 번 주지 않음).
        /// </summary>
        async Task<PvpMatchResult> FinishMatch(string matchId, string loser, string reason)
        {
            bool mine = false;
            MatchRecord rec = await MutateMatch(matchId, r =>
            {
                mine = false;
                if (r.Status != MatchState.Playing || r.Find(loser) == null) return;
                r.Status = MatchState.Done;
                r.Result = new PvpMatchResult { MatchId = matchId, Loser = loser, Winner = r.Other(loser)?.PlayerId, Reason = reason };
                mine = true;
            });
            if (rec == null) return null;
            if (!mine) return rec.Result;

            PvpMatchResult result = rec.Result;
            foreach (MatchPlayer p in rec.Players)
            {
                bool won = p.PlayerId == result.Winner;
                EconReply<PvpRewardResult> r = await WithEcon(p.PlayerId, e =>
                {
                    if (!e.MarkProcessed("match:" + matchId)) return null;
                    result.Delta[p.PlayerId] = EconRules.ApplyPvpResult(e, won);
                    return EconRules.PvpReward(e, won, Ctx());
                });
                result.Trophies[p.PlayerId] = r.Econ.Pvp.Trophies;
                if (r.Result != null) result.Rewards[p.PlayerId] = r.Result;
                await m_rank.Submit(p.PlayerId, r.Econ.Pvp.Trophies, r.Econ.Pvp.Wins, p.Nick);
            }
            await MutateMatch(matchId, r => r.Result = result);
            foreach (MatchPlayer p in rec.Players) await m_notifier.Send(p.PlayerId, CloudApi.PushResult, result);
            return result;
        }
    }
}
