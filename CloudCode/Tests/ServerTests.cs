using System.Collections.Generic;
using System.Threading.Tasks;
using DefenseBattle.Core;
using NUnit.Framework;

namespace DefenseBattle.Server.Tests
{
    /// <summary>서버 로직 테스트 (원본 server/test/server.test.ts 의 경우들 + UGS 판 매칭·이탈 판정)</summary>
    public class EconServerTests
    {
        [Test]
        public async Task NewAccount_GetsFreshEconAndIsSaved()
        {
            var rig = new Rig();
            EconReply r = await rig.Server.EconSync("p1");
            Assert.AreEqual(0, r.Econ.Crystals);
            Assert.IsTrue(r.Econ.Owns(Job.Archer));
            Assert.AreEqual(ChestKind.Wood, r.Econ.Chests[0].Kind);
            Assert.AreEqual(1, rig.Store.Writes);
        }

        [Test]
        public async Task Accounts_DoNotMix()
        {
            var rig = new Rig();
            await rig.Server.RedeemPurchase("p1", "crystal_550", "ok:A1", "sig", 6500, "KRW");
            Assert.AreEqual(550, (await rig.Server.EconSync("p1")).Econ.Crystals);
            Assert.AreEqual(0, (await rig.Server.EconSync("p2")).Econ.Crystals);
        }

        [Test]
        public async Task Gacha_NeedsCrystals_ServerDeducts()
        {
            var rig = new Rig();
            EconReply<Reveal> fail = await rig.Server.Gacha("p1", 1, false);
            Assert.IsNull(fail.Result);
            await rig.Server.RedeemPurchase("p1", "crystal_100", "ok:B1", "sig", 1200, "KRW");
            EconReply<Reveal> ok = await rig.Server.Gacha("p1", 1, false);
            Assert.IsNotNull(ok.Result);
            Assert.AreEqual(0, ok.Econ.Crystals);
            EconReply<bool> up = await rig.Server.UpgradeUnit("p1", Job.Archer);
            Assert.IsFalse(up.Result); // 보석·카드 부족
        }

        [Test]
        public async Task AdTicket_ThenGachaWithTicket()
        {
            var rig = new Rig();
            EconReply<int> ad = await rig.Server.AdReward("p1", false);
            Assert.AreEqual(1, ad.Result);
            Assert.AreEqual(1, ad.Econ.Item(ItemKey.GachaTicket));
            EconReply<Reveal> g = await rig.Server.Gacha("p1", 1, true);
            Assert.IsNotNull(g.Result);
            Assert.AreEqual(0, g.Econ.Item(ItemKey.GachaTicket));
            Assert.AreEqual(0, g.Econ.Crystals); // 보석은 그대로
        }

        [Test]
        public async Task Purchase_GrantedOncePerOrder_RecoversAfterFailure()
        {
            var rig = new Rig();
            PurchaseReply a = await rig.Server.RedeemPurchase("p1", "crystal_1200", "ok:O1", "sig", 12000, "KRW");
            Assert.IsTrue(a.Granted);
            Assert.AreEqual(1200, a.Econ.Crystals);
            PurchaseReply again = await rig.Server.RedeemPurchase("p1", "crystal_1200", "dup:O1", "sig", 12000, "KRW");
            Assert.IsFalse(again.Granted);
            Assert.IsTrue(again.AlreadyGranted);
            Assert.AreEqual(1200, again.Econ.Crystals);
            // 스토어는 '이미 검증됨' 인데 우리 지급 기록이 없다 (지난번 지급 도중 실패) → 지금 지급
            PurchaseReply recover = await rig.Server.RedeemPurchase("p1", "crystal_100", "dup:O2", "sig", 1200, "KRW");
            Assert.IsTrue(recover.Granted);
            Assert.AreEqual(1300, recover.Econ.Crystals);
            PurchaseReply bad = await rig.Server.RedeemPurchase("p1", "crystal_100", "forged", "sig", 1200, "KRW");
            Assert.IsFalse(bad.Granted);
            PurchaseReply unknown = await rig.Server.RedeemPurchase("p1", "nope", "ok:O3", "sig", 1200, "KRW");
            Assert.IsFalse(unknown.Granted);
        }

        [Test]
        public async Task ChestProduct_OpensImmediately()
        {
            var rig = new Rig();
            PurchaseReply r = await rig.Server.RedeemPurchase("p1", "chest_legend", "ok:C1", "sig", 12000, "KRW");
            Assert.AreEqual(1, r.Reveals.Count);
            bool legendary = false;
            foreach (CardDrop d in r.Reveals[0].Drops) legendary |= Units.Get(d.Job).Rarity == Rarity.Legendary;
            Assert.IsTrue(legendary);
        }

        [Test]
        public async Task StageResult_NeedsRecordAndTime_OnlyOnce()
        {
            var rig = new Rig();
            Assert.IsNull(await rig.Server.RunEnd("p1", "nope", true, 3, 10)); // 기록 없음
            RunTicket t = await rig.Server.RunStart("p1", false, "1-3");
            rig.Clock.Advance(5_000);
            Assert.IsNull(await rig.Server.RunEnd("p1", t.Id, true, 3, 10)); // 10웨이브를 5초에 클리어 = 거부
            RunTicket t2 = await rig.Server.RunStart("p1", false, "1-3");
            rig.Clock.Advance(10 * GameServer.MsPerWave);
            EconReply<RunEndResult> ok = await rig.Server.RunEnd("p1", t2.Id, true, 3, 10);
            Assert.IsNotNull(ok);
            Assert.IsTrue(ok.Result.Stage.FirstClear);
            Assert.AreEqual(Job.Ranger, ok.Result.Stage.Unlocked);
            Assert.IsNull(await rig.Server.RunEnd("p1", t2.Id, true, 3, 10)); // 같은 기록 두 번 = 거부
            Assert.Throws<System.ArgumentException>(() => rig.Server.RunStart("p1", false, "9-9").GetAwaiter().GetResult());
        }

        [Test]
        public async Task EndlessResult_WaveCappedByTime()
        {
            var rig = new Rig();
            RunTicket t = await rig.Server.RunStart("p1", true, null);
            rig.Clock.Advance(4 * GameServer.MsPerWave);
            EconReply<RunEndResult> r = await rig.Server.RunEnd("p1", t.Id, false, 0, 50); // 50웨이브라고 해도 4웨이브 분량 시간
            Assert.AreEqual(Campaign.CrystalReward(4), r.Result.Crystals);
        }

        [Test]
        public async Task SetDecks_OnlyOwnedUnitsAndUnlockedCards()
        {
            var rig = new Rig();
            var cards = new List<AttackKey>(Attacks.Starter);
            Assert.IsTrue((await rig.Server.SetDecks("p1", new List<Job> { Job.Wizard, Job.Archer }, cards)).Result);
            Assert.IsFalse((await rig.Server.SetDecks("p1", new List<Job> { Job.Angel }, cards)).Result); // 없는 유닛
            Assert.IsFalse((await rig.Server.SetDecks("p1", new List<Job> { Job.Archer, Job.Archer }, cards)).Result); // 중복
            var locked = new List<AttackKey> { AttackKey.Demon, AttackKey.Swarm, AttackKey.Rush, AttackKey.Giant };
            Assert.IsFalse((await rig.Server.SetDecks("p1", new List<Job> { Job.Archer }, locked)).Result); // 마왕은 1200 트로피
            EconReply e = await rig.Server.EconSync("p1");
            CollectionAssert.AreEqual(new[] { Job.Wizard, Job.Archer }, e.Econ.Deck);
        }

        [Test]
        public async Task WriteConflict_IsRetried()
        {
            var rig = new Rig();
            await rig.Server.EconSync("p1");
            rig.Store.ConflictsToInject = 2;
            EconReply<int> r = await rig.Server.AdReward("p1", false);
            Assert.AreEqual(1, r.Result);
            Assert.AreEqual(1, (await rig.Server.EconSync("p1")).Econ.Item(ItemKey.GachaTicket)); // 한 번만 지급
        }
    }

    public class PvpServerTests
    {
        static async Task<(Rig rig, MatchInfo host, MatchInfo guest)> Matched()
        {
            var rig = new Rig();
            await rig.Server.SetNick("p1", "가나다");
            await rig.Server.SetNick("p2", "라마바");
            QueueStatus w = await rig.Server.PvpQueue("p1");
            Assert.AreEqual(QueueStatus.Waiting, w.Status);
            QueueStatus m = await rig.Server.PvpQueue("p2");
            Assert.AreEqual(QueueStatus.Matched, m.Status);
            QueueStatus polled = await rig.Server.PvpPoll("p1");
            Assert.AreEqual(QueueStatus.Matched, polled.Status);
            return (rig, m.Match, polled.Match);
        }

        static async Task<MatchState> Start(Rig rig, MatchInfo host, MatchInfo guest)
        {
            MatchState s1 = await rig.Server.PvpReady("p1", guest.MatchId, null);
            Assert.AreEqual(MatchState.Waiting, s1.Status);
            MatchState s2 = await rig.Server.PvpReady("p2", host.MatchId, "RELAY1");
            Assert.AreEqual(MatchState.Playing, s2.Status);
            return s2;
        }

        [Test]
        public async Task TwoPlayers_SameMatchAndSeed_NewcomerHosts()
        {
            var (rig, host, guest) = await Matched();
            Assert.AreEqual(host.MatchId, guest.MatchId);
            Assert.AreEqual(host.Seed, guest.Seed);
            Assert.IsTrue(host.Host);
            Assert.IsFalse(guest.Host);
            Assert.AreEqual("가나다", host.Opponent.Nick);
            Assert.AreEqual("라마바", guest.Opponent.Nick);
            Assert.IsTrue(rig.Push.Sent.Exists(s => s.player == "p1" && s.type == CloudApi.PushMatch)); // 기다리던 쪽에 알림
            Assert.AreEqual(QueueStatus.Idle, (await rig.Server.PvpPoll("p1")).Status); // 이미 가져감
        }

        [Test]
        public async Task Cancel_LeavesQueue()
        {
            var rig = new Rig();
            await rig.Server.PvpQueue("p1");
            await rig.Server.PvpCancel("p1");
            Assert.AreEqual(QueueStatus.Waiting, (await rig.Server.PvpQueue("p2")).Status); // p1 이 없어서 대기
        }

        [Test]
        public async Task TrophyGap_WidensWhileWaiting()
        {
            var rig = new Rig();
            // p1 을 600 트로피로 (봇전 20연승)
            for (int i = 0; i < 20; i++)
            {
                RunTicket b = await rig.Server.PvpBotStart("p1");
                await rig.Server.PvpBotResult("p1", b.Id, true);
            }
            Assert.Greater((await rig.Server.EconSync("p1")).Econ.Pvp.Trophies, 500);
            await rig.Server.PvpQueue("p1");
            Assert.AreEqual(QueueStatus.Waiting, (await rig.Server.PvpQueue("p2")).Status); // 0 트로피와는 바로 안 붙음
            await rig.Server.PvpCancel("p2");
            int gap = (await rig.Server.EconSync("p1")).Econ.Pvp.Trophies;
            int seconds = (gap - 200) / 40 + 2; // 허용 폭 = 200 + 기다린 초 × 40
            for (int i = 0; i < seconds; i++)
            {
                rig.Clock.Advance(1_000);
                await rig.Server.PvpPoll("p1"); // 하트비트 (원본은 하트비트마다 기다린 시간이 0 으로 돌아가 폭이 안 넓어졌다)
            }
            Assert.AreEqual(QueueStatus.Matched, (await rig.Server.PvpQueue("p2")).Status); // 오래 기다리면 폭이 넓어짐
        }

        [Test]
        public async Task Queue_ExpiresWithoutHeartbeat()
        {
            var rig = new Rig();
            await rig.Server.PvpQueue("p1");
            rig.Clock.Advance(GameServer.QueueTtlMs + 1);
            Assert.AreEqual(QueueStatus.Waiting, (await rig.Server.PvpQueue("p2")).Status);
            Assert.AreEqual(QueueStatus.Idle, (await rig.Server.PvpPoll("p1")).Status);
        }

        [Test]
        public async Task ReadyBoth_NeedsRelayCode_ThenStartsTogether()
        {
            var (rig, host, guest) = await Matched();
            MatchState g = await rig.Server.PvpReady("p1", guest.MatchId, "IGNORED"); // 손님 코드는 무시
            Assert.AreEqual(MatchState.Waiting, g.Status);
            Assert.IsNull(g.RelayJoinCode);
            MatchState h = await rig.Server.PvpReady("p2", host.MatchId, "RELAY1");
            Assert.AreEqual(MatchState.Playing, h.Status);
            Assert.AreEqual("RELAY1", h.RelayJoinCode);
            Assert.AreEqual(rig.Clock.NowMs + GameServer.StartDelayMs, h.StartAt);
            Assert.AreEqual(2, rig.Push.Sent.FindAll(s => s.type == CloudApi.PushMatchState).Count);
            Assert.IsNull(await rig.Server.PvpReady("p3", host.MatchId, null)); // 남의 경기
        }

        [Test]
        public async Task Defeat_WinnerGetsTrophiesAndChest_OnlyOnce()
        {
            var (rig, host, guest) = await Matched();
            await Start(rig, host, guest);
            PvpMatchResult r = await rig.Server.PvpDefeat("p1", host.MatchId);
            Assert.AreEqual("p2", r.Winner);
            Assert.AreEqual("defeat", r.Reason);
            Assert.AreEqual(30, r.Delta["p2"]);
            Assert.AreEqual(0, r.Delta["p1"]); // 0 트로피 아래로는 안 내려감
            Assert.AreEqual(ChestKind.Wood, r.Rewards["p2"].Chest);
            EconState p2 = (await rig.Server.EconSync("p2")).Econ;
            Assert.AreEqual(30, p2.Pvp.Trophies);
            Assert.AreEqual(1, p2.Pvp.Wins);
            Assert.AreEqual(PvpConfig.RewardWin + 50, p2.Crystals); // 승리 보석 + 트로피 로드 15
            // 두 번째 패배 보고·상대의 패배 보고 = 같은 결과, 트로피 그대로
            PvpMatchResult again = await rig.Server.PvpDefeat("p2", host.MatchId);
            Assert.AreEqual("p2", again.Winner);
            Assert.AreEqual(30, (await rig.Server.EconSync("p2")).Econ.Pvp.Trophies);
            Assert.AreEqual(30, rig.Rank.Rows["p2"].Trophies);
            Assert.AreEqual("라마바", rig.Rank.Rows["p2"].Nick);
            Assert.AreEqual(2, rig.Push.Sent.FindAll(s => s.type == CloudApi.PushResult).Count);
        }

        [Test]
        public async Task Defeat_BeforeStart_IsIgnored()
        {
            var (rig, host, _) = await Matched();
            Assert.IsNull((await rig.Server.PvpDefeat("p1", host.MatchId))?.Winner);
            Assert.AreEqual(0, (await rig.Server.EconSync("p2")).Econ.Pvp.Wins);
        }

        [Test]
        public async Task Away_ClaimOnlyAfterGrace_PingKeepsAlive()
        {
            var (rig, host, guest) = await Matched();
            await Start(rig, host, guest);
            rig.Clock.Advance(GameServer.StartDelayMs + 10_000);
            Assert.IsNull(await rig.Server.PvpClaimAway("p2", host.MatchId)); // 아직 유예 안
            rig.Clock.Advance(15_000);
            await rig.Server.PvpPing("p1", host.MatchId); // p1 은 살아 있음
            rig.Clock.Advance(15_000);
            Assert.IsNull(await rig.Server.PvpClaimAway("p2", host.MatchId)); // 마지막 응답 15초 전
            rig.Clock.Advance(GameServer.AwayGraceMs);
            PvpMatchResult r = await rig.Server.PvpClaimAway("p2", host.MatchId);
            Assert.AreEqual("p2", r.Winner);
            Assert.AreEqual("leave", r.Reason);
            MatchState back = await rig.Server.PvpPing("p1", host.MatchId); // 늦게 돌아온 쪽은 결과를 본다
            Assert.AreEqual(MatchState.Done, back.Status);
            Assert.AreEqual("p2", back.Result.Winner);
        }

        [Test]
        public async Task Leave_DuringPlayLoses_BeforeStartCancels()
        {
            var (rig, host, guest) = await Matched();
            Assert.IsNull(await rig.Server.PvpLeave("p1", host.MatchId)); // 준비 전 = 취소
            MatchState s = await rig.Server.PvpPing("p2", host.MatchId);
            Assert.AreEqual(MatchState.Cancelled, s.Status);

            var (rig2, host2, guest2) = await Matched();
            await Start(rig2, host2, guest2);
            PvpMatchResult r = await rig2.Server.PvpLeave("p2", host2.MatchId);
            Assert.AreEqual("p1", r.Winner);
            Assert.AreEqual("leave", r.Reason);
        }

        [Test]
        public async Task BotResult_WrongIdIgnored_RightIdOnce()
        {
            var rig = new Rig();
            Assert.IsNull(await rig.Server.PvpBotResult("p1", "nope", true));
            RunTicket t = await rig.Server.PvpBotStart("p1");
            BotResultReply r = await rig.Server.PvpBotResult("p1", t.Id, true);
            Assert.AreEqual(30, r.Delta);
            Assert.AreEqual(30, r.Trophies);
            Assert.AreEqual(ChestKind.Wood, r.Reward.Chest);
            Assert.IsNull(await rig.Server.PvpBotResult("p1", t.Id, true)); // 한 번만
            RunTicket t2 = await rig.Server.PvpBotStart("p1");
            BotResultReply loss = await rig.Server.PvpBotResult("p1", t2.Id, false);
            Assert.AreEqual(-20, loss.Delta);
            Assert.AreEqual(10, loss.Trophies);
            Assert.AreEqual(1, loss.Econ.Pvp.LossStreak);
        }

        [Test]
        public async Task Nick_BadBecomesDefault_RankUpdatesImmediately()
        {
            var rig = new Rig();
            Assert.AreEqual("용사", await rig.Server.SetNick("p1", "시1발"));
            Assert.AreEqual("용사", await rig.Server.SetNick("p1", "   "));
            Assert.AreEqual("가나다라마바사아자차카타", await rig.Server.SetNick("p1", "가나다라마바사아자차카타파하")); // 12자
            RunTicket t = await rig.Server.PvpBotStart("p1");
            await rig.Server.PvpBotResult("p1", t.Id, true);
            await rig.Server.SetNick("p1", "새이름");
            Assert.AreEqual("새이름", rig.Rank.Rows["p1"].Nick);
            LeaderboardReply lb = await rig.Server.Leaderboard("p1");
            Assert.AreEqual(1, lb.Top.Count);
            Assert.AreEqual(30, lb.Me.Trophies);
        }
    }

    public class NickFilterTests
    {
        [TestCase("fuuuck")]
        [TestCase("f.u.c.k")]
        [TestCase("5h1t")]
        [TestCase("ＦＵＣＫ")]
        [TestCase("시 발")]
        [TestCase("ㅅㅂ")]
        [TestCase("シネ")]
        [TestCase("傻逼")]
        public void Bad(string nick) => Assert.IsTrue(NickFilter.IsBad(nick), nick);

        [TestCase("용사1234")]
        [TestCase("MoonArcher")]
        [TestCase("달빛궁수")]
        [TestCase("합성장인")]
        public void Good(string nick) => Assert.IsFalse(NickFilter.IsBad(nick), nick);
    }
}
