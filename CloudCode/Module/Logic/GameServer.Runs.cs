using System;
using System.Threading.Tasks;
using DefenseBattle.Core;

namespace DefenseBattle.Server
{
    // 스테이지·무한 모드 결과 (원본 runStart / runEnd) - 시작 기록 1개당 결과 1번, 웨이브 수에 맞는 시간이 지나야 보상
    public sealed partial class GameServer
    {
        /// <summary>결과를 받기 위한 최소 진행 시간 (웨이브당, 3배속 기준으로 넉넉히)</summary>
        public const long MsPerWave = 2500;

        sealed class Change<T> : IMutation
        {
            public bool Changed { get; set; } = true;
            public T Value;
        }

        public async Task<RunTicket> RunStart(string playerId, bool endless, string stageId)
        {
            if (!endless && Campaign.Get(stageId) == null) throw new ArgumentException("unknown stage");
            var rec = new RunRecord { Id = NewId(), Endless = endless, StageId = endless ? null : stageId, T = Now };
            await m_store.MutatePlayer<RunRecord, bool>(playerId, Keys.Run, () => new RunRecord(), r =>
            {
                r.Id = rec.Id;
                r.Endless = rec.Endless;
                r.StageId = rec.StageId;
                r.T = rec.T;
                return true;
            });
            return new RunTicket { Id = rec.Id };
        }

        /// <summary>결과 → 보상. 기록이 없거나(다른 id) 클리어가 너무 빠르면 null (보상 없음)</summary>
        public async Task<EconReply<RunEndResult>> RunEnd(string playerId, string id, bool cleared, int stars, int wave)
        {
            // 기록을 꺼내면서 지운다 (같은 기록으로 두 번 받지 못하게)
            Change<RunRecord> taken = await m_store.MutatePlayer<RunRecord, Change<RunRecord>>(playerId, Keys.Run, () => new RunRecord(), r =>
            {
                if (r.Id == null || r.Id != id) return new Change<RunRecord> { Changed = false };
                var copy = new RunRecord { Id = r.Id, Endless = r.Endless, StageId = r.StageId, T = r.T };
                r.Id = null;
                return new Change<RunRecord> { Value = copy };
            });
            RunRecord run = taken.Value;
            if (run == null) return null;
            long elapsed = Now - run.T;
            wave = Math.Max(0, Math.Min(999, wave));
            stars = Math.Max(0, Math.Min(3, stars));
            int maxWave = (int)(elapsed / MsPerWave);
            if (!run.Endless)
            {
                StageDef def = Campaign.Get(run.StageId);
                if (cleared && elapsed < def.Waves * MsPerWave) return null; // 클리어인데 너무 빠름
                int w = Math.Min(wave, maxWave);
                return await WithEcon(playerId, e => new RunEndResult { Stage = EconRules.StageEnd(e, run.StageId, cleared, stars, w, Ctx()) });
            }
            int ew = Math.Min(wave, maxWave);
            return await WithEcon(playerId, e => new RunEndResult { Crystals = EconRules.EndlessEnd(e, ew, Ctx()) });
        }
    }
}
