using System;
using System.Collections.Generic;
using System.Linq;
using DefenseBattle.Core;

// ============================================================
//  헤드리스 밸런스 시뮬레이터 (원본 tools/simulate.ts 이식)
//  실행: dotnet run -c Release --project CoreTests/Sim -- [판수]
//        dotnet run -c Release --project CoreTests/Sim -- stages [판수] [스테이지 접두사]
//  원본과 같은 자동 플레이어로 게임오버까지 돌려 도달 웨이브·클리어율을 잰다.
// ============================================================
static class Program
{
    static readonly Random s_seeds = new Random(20261019);

    static Dictionary<ResearchKey, int> Profile(int atk, int startGold, int goldGain, int rare, int boss)
        => new Dictionary<ResearchKey, int>
        {
            { ResearchKey.Atk, atk }, { ResearchKey.StartGold, startGold }, { ResearchKey.GoldGain, goldGain },
            { ResearchKey.Rare, rare }, { ResearchKey.Boss, boss },
        };

    // 유닛 수가 많은 직업부터 강화 (강화는 직업 단위)
    static bool TryUpgrade(Game game)
    {
        foreach (JobStack s in game.GetJobStacks().OrderByDescending(s => s.Count))
        {
            if (game.Gold >= game.UpgradeCost(s.Job)) return game.Upgrade(s.Job);
        }
        return false;
    }

    // 합성 가능한 조합이 있으면 합성 (낮은 등급부터)
    static bool TryMerge(Game game)
    {
        List<KeyValuePair<Job, GradeKey>> m = game.Mergeable().OrderBy(x => (int)x.Value).ToList();
        return m.Count > 0 && game.Merge(m[0].Key, m[0].Value) != null;
    }

    static void AutoPlay(Game game, int armyTarget)
    {
        const int Summon = 20;
        if (TryMerge(game)) return;
        if (game.Units.Count < armyTarget)
        {
            if (game.Gold >= Summon) game.Summon();
            return;
        }
        // 군대 확보됨 → 강화 우선, 실패 시 소환
        if (!TryUpgrade(game))
        {
            if (game.Gold >= Summon) game.Summon();
        }
    }

    static Game RunGame(GameOptions opts, int armyTarget = 18)
    {
        opts.Track = false;
        opts.RandomSeed = s_seeds.Next(1, int.MaxValue);
        opts.Seed = (uint)s_seeds.Next(1, int.MaxValue);
        var game = new Game(opts);
        game.Speed = 1;
        const double dt = 1 / 20.0;
        const double actionEvery = 0.2;
        double sinceAction = 0, t = 0;
        const double maxT = 90 * 60; // 안전장치(게임시간 90분)
        while (!game.Over && t < maxT)
        {
            game.Update(dt);
            t += dt;
            sinceAction += dt;
            if (sinceAction >= actionEvery)
            {
                sinceAction = 0;
                AutoPlay(game, armyTarget);
            }
        }
        return game;
    }

    static void Sweep(int runs)
    {
        var profiles = new (string, Dictionary<ResearchKey, int>)[]
        {
            ("none  (연구 0)", Profile(0, 0, 0, 0, 0)),
            ("quarter", Profile(5, 5, 5, 2, 5)),
            ("half", Profile(10, 10, 10, 5, 10)),
            ("three-q", Profile(15, 15, 15, 7, 15)),
            ("full  (연구 MAX)", Profile(20, 20, 20, 10, 20)),
        };
        Console.WriteLine($"\n=== 밸런스 시뮬 (각 {runs}회 평균, armyTarget=18) ===");
        Console.WriteLine("profile".PadRight(20) + " avgWave  min  max  clear%");
        foreach ((string name, Dictionary<ResearchKey, int> prof) in profiles)
        {
            var waves = new List<int>();
            for (int i = 0; i < runs; i++) waves.Add(RunGame(new GameOptions { Research = prof }).Wave);
            double avg = Math.Round(waves.Average() * 10) / 10;
            int clear = (int)Math.Round(waves.Count(w => w >= 50) * 100.0 / waves.Count);
            Console.WriteLine(name.PadRight(20) + $" {avg,6} {waves.Min(),4} {waves.Max(),4} {clear,6}%");
        }
    }

    // 캠페인 스테이지별 클리어율/별 (연구 진척 × 유닛 레벨 프로필)
    // 덱: 해당 스테이지 시점에 해금된 유닛 중 4종 (원본과 같이 UNITS 순서의 마지막 4개)
    static void StageSweep(int runs, string filter)
    {
        var profiles = new (string, Dictionary<ResearchKey, int>, int)[]
        {
            ("none", Profile(0, 0, 0, 0, 0), 1),
            ("quarter", Profile(5, 5, 5, 2, 5), 3),
            ("half", Profile(10, 10, 10, 5, 10), 5),
            ("3/4", Profile(15, 15, 15, 7, 15), 7),
        };
        List<string> order = Campaign.Stages.Select(s => s.Id).ToList();
        Console.WriteLine($"\n=== 스테이지 클리어율% / 평균별 (각 {runs}회) ===");
        Console.WriteLine("stage  waves hpX   " + string.Concat(profiles.Select(p => p.Item1.PadLeft(11))));
        foreach (StageDef st in Campaign.Stages)
        {
            if (filter != null && !st.Id.StartsWith(filter, StringComparison.Ordinal)) continue;
            List<Job> unlocked = Units.All
                .Where(u => u.UnlockStage == null || order.IndexOf(u.UnlockStage) < order.IndexOf(st.Id))
                .Select(u => u.Job).ToList();
            List<Job> deck = unlocked.Skip(Math.Max(0, unlocked.Count - 4)).ToList();
            var cells = new List<string>();
            foreach ((string _, Dictionary<ResearchKey, int> prof, int lv) in profiles)
            {
                int clears = 0, stars = 0;
                for (int i = 0; i < runs; i++)
                {
                    GameOptions o = GameOptions.ForStage(st);
                    o.Research = prof;
                    o.Deck = new List<Job>(deck);
                    o.UnitLevels = deck.ToDictionary(j => j, j => lv);
                    Game g = RunGame(o);
                    if (g.Cleared) { clears++; stars += Campaign.StarsFor(g.PeakMobs); }
                }
                string star = clears > 0 ? (stars / (double)clears).ToString("0.0") : "-";
                cells.Add($"{(int)Math.Round(clears * 100.0 / runs)}%/{star}".PadLeft(11));
            }
            Console.WriteLine(st.Id.PadRight(6) + " " + st.Waves.ToString().PadLeft(5) + " " + st.HpMult.ToString().PadRight(5) + " " + string.Concat(cells));
        }
    }

    // 시드를 고정한 판들의 결과 요약 - 최적화 전후로 로직이 그대로인지 비교한다 (같은 시드 = 같은 결과)
    static void Fingerprint(int runs)
    {
        long hash = 17;
        for (int i = 0; i < runs; i++)
        {
            StageDef st = Campaign.Stages[i % Campaign.Stages.Length];
            GameOptions o = i % 3 == 0 ? new GameOptions() : GameOptions.ForStage(st);
            o.Track = false;
            o.Seed = (uint)(1000 + i);
            o.RandomSeed = 5000 + i;
            o.Deck = i % 2 == 0
                ? new List<Job> { Job.Archer, Job.Wizard, Job.Warrior, Job.Ranger }
                : Units.All.Skip((i * 4) % 28).Take(4).Select(u => u.Job).ToList();
            o.Research = Profile(i % 21, i % 21, i % 21, i % 11, i % 21);
            o.UnitLevels = o.Deck.ToDictionary(j => j, j => 1 + i % 10);
            var game = new Game(o);
            double sinceAction = 0, t = 0;
            while (!game.Over && t < 90 * 60)
            {
                game.Update(0.05);
                t += 0.05;
                sinceAction += 0.05;
                if (sinceAction >= 0.2) { sinceAction = 0; AutoPlay(game, 18); }
            }
            string line = $"{i,3} wave={game.Wave} kills={game.Kills} gold={game.Gold} summons={game.SummonCount} units={game.Units.Count} mobs={game.Mobs.Count} peak={game.PeakMobs} t={game.Time:F2}";
            Console.WriteLine(line);
            foreach (char c in line) hash = hash * 31 + c;
        }
        Console.WriteLine("hash=" + hash);
    }

    // 성능 측정: 판 전체에서 Update 에 쓴 시간과 GC 할당량 (자동 플레이어 동작은 빼고 엔진만)
    static void Bench(int runs, int research)
    {
        long updates = 0, bytes = 0;
        double ms = 0;
        int peakMobs = 0;
        var sw = new System.Diagnostics.Stopwatch();
        for (int i = 0; i < runs; i++)
        {
            var o = new GameOptions { Track = false, Seed = (uint)(77 + i), RandomSeed = 99 + i, Research = Profile(research, research, research, research / 2, research),
                Deck = new List<Job> { Job.Archer, Job.Wizard, Job.Storm, Job.Pyro } };
            var game = new Game(o);
            double sinceAction = 0;
            while (!game.Over)
            {
                long b0 = GC.GetAllocatedBytesForCurrentThread();
                sw.Restart();
                game.Update(0.05);
                sw.Stop();
                bytes += GC.GetAllocatedBytesForCurrentThread() - b0;
                ms += sw.Elapsed.TotalMilliseconds;
                updates++;
                peakMobs = Math.Max(peakMobs, game.Mobs.Count);
                sinceAction += 0.05;
                if (sinceAction >= 0.2) { sinceAction = 0; AutoPlay(game, 18); }
            }
        }
        Console.WriteLine($"updates={updates} avg={ms / updates * 1000:F1}us/frame alloc={bytes / (double)updates:F1}B/frame total={bytes / 1024.0:F0}KB peakMobs={peakMobs}");
    }

    // PvP: 플레이어(고정 실력, 봇 AI 로 대신) 대 봇 - 원본 tools/pvpsim.ts 1) 트로피 구간별 승률 / 한 판 길이
    static (bool won, double sec) PvpMatch(double playerSkill, double botSkill, int trophies, int levels = 1)
    {
        var rand = new SystemRandom(s_seeds.Next(1, int.MaxValue));
        uint seed = (uint)s_seeds.Next(1, int.MaxValue);
        var deck = new List<Job> { Job.Archer, Job.Wizard, Job.Warrior, Job.Ranger };
        List<Job> bDeck = BotProfile.Deck(trophies, rand);
        var a = new Game(new GameOptions { Mode = GameMode.Pvp, Seed = seed, UseResearch = false, Track = false, Deck = deck,
            UnitLevels = deck.ToDictionary(j => j, j => levels), RandomSeed = s_seeds.Next(1, int.MaxValue) });
        var b = new Game(new GameOptions { Mode = GameMode.Pvp, Seed = seed, UseResearch = false, Track = false, Deck = bDeck,
            AtkMult = BotProfile.AtkMult(botSkill), AttackDeck = BotProfile.AttackDeck(trophies, rand),
            UnitLevels = bDeck.ToDictionary(j => j, j => levels), RandomSeed = s_seeds.Next(1, int.MaxValue) });
        a.AttackSent += p => b.ReceiveAttack(p);
        b.AttackSent += p => a.ReceiveAttack(p);
        var aiA = new BotPlayer(a, playerSkill, rand);
        var aiB = new BotPlayer(b, botSkill, rand);
        BoardSnapshot snapA = null, snapB = null;
        double t = 0, actA = 0, actB = 0, snap = 0;
        while (!a.Over && !b.Over && t < 60 * 30)
        {
            a.Update(0.05); b.Update(0.05); t += 0.05; snap += 0.05;
            if (snap >= PvpConfig.ReportInterval * Waves.PlaySpeed) { snap = 0; snapA = a.Snapshot(); snapB = b.Snapshot(); }
            actA += 0.05; actB += 0.05;
            if (actA >= 0.7 * Waves.PlaySpeed) { actA = 0; aiA.Step(snapB); }
            if (actB >= BotPlayer.ThinkInterval(botSkill) * Waves.PlaySpeed) { actB = 0; aiB.Step(snapA); }
        }
        bool won = b.Over && !a.Over ? true : a.Over && !b.Over ? false : a.Mobs.Count < b.Mobs.Count;
        return (won, t / Waves.PlaySpeed);
    }

    static void PvpSweep(int runs, double player)
    {
        Console.WriteLine($"\n=== 플레이어 실력 {player} vs 봇 (트로피별, 각 {runs}판) ===");
        Console.WriteLine("trophies  botSkill  win%   avgMin");
        foreach (int tr in new[] { 0, 200, 400, 700, 1000, 1500, 2000 })
        {
            double sk = BotProfile.Skill(tr, 10, 10, 0, 0); // 첫 3판 규칙 제외
            int w = 0;
            double sec = 0;
            for (int i = 0; i < runs; i++) { (bool won, double s) = PvpMatch(player, sk, tr); if (won) w++; sec += s; }
            Console.WriteLine($"{tr,8} {sk,9:F2} {(int)Math.Round(w * 100.0 / runs) + "%",6} {sec / runs / 60,8:F1}");
        }
    }

    static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "pvp") { PvpSweep(args.Length > 1 ? int.Parse(args[1]) : 12, args.Length > 2 ? double.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture) : 0.55); return; }
        if (args.Length > 0 && args[0] == "bench") { Bench(args.Length > 1 ? int.Parse(args[1]) : 3, args.Length > 2 ? int.Parse(args[2]) : 20); return; }
        string mode = args.Length > 0 ? args[0] : "sweep";
        if (mode == "fingerprint") { Fingerprint(args.Length > 1 ? int.Parse(args[1]) : 40); return; }
        if (mode == "stages") StageSweep(args.Length > 1 ? int.Parse(args[1]) : 8, args.Length > 2 ? args[2] : null);
        else Sweep(int.TryParse(mode, out int n) ? n : 30);
    }
}
