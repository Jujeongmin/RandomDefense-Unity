using System;
using System.Collections.Generic;

namespace DefenseBattle.Core
{
    /// <summary>
    /// 봇 상대 세팅 (pvp.ts botSkill ~ botAttackDeck, trophyDelta).
    /// 원본은 저장본(meta)을 직접 읽었지만 여기선 필요한 값을 인자로 받는다.
    /// </summary>
    public static class BotProfile
    {
        /// <summary>
        /// 봇 실력: 트로피가 오를수록 강해지되 사람이 이길 만한 수준에서 멈춘다 (300 트로피 ≈ 0.3, 900 ≈ 0.7, 1125+ ≈ 0.85).
        /// 연승하면 조금 세게, 연패하면 확실히 약하게. 첫 3판은 가장 약한 봇 - 처음 대전에서 이기는 경험.
        /// </summary>
        public static double Skill(int trophies, int wins, int losses, int streak, int lossStreak)
        {
            if (wins + losses < 3) return 0.05;
            double b = 0.1 + trophies / 1500.0;
            double adj = Math.Min(4, streak) * 0.05 - Math.Min(3, lossStreak) * 0.1;
            return Math.Max(0.05, Math.Min(0.85, b + adj));
        }

        /// <summary>봇 연구 레벨: 트로피에 맞춰 (1500 트로피에서 최대). 첫 3판은 연구 없음</summary>
        public static Dictionary<ResearchKey, int> Research(int trophies, int gamesPlayed)
        {
            double k = gamesPlayed < 3 ? 0 : Math.Min(1, Math.Max(0, trophies) / 1500.0);
            var d = new Dictionary<ResearchKey, int>();
            foreach (ResearchDef r in Core.Research.All) d[r.Key] = (int)Math.Floor(r.Max * k);
            return d;
        }

        /// <summary>봇 유닛 레벨 = 내 출전 덱의 평균 레벨</summary>
        public static int Level(IReadOnlyList<Job> myDeck, IReadOnlyDictionary<Job, int> myLevels)
        {
            double sum = 0;
            foreach (Job j in myDeck) sum += myLevels != null && myLevels.TryGetValue(j, out int lv) ? lv : 1;
            return Math.Max(1, JsMath.RoundInt(sum / Math.Max(1, myDeck.Count)));
        }

        /// <summary>봇 유닛 공격력 배수: 약한 봇일수록 낮다 (0.05 → 0.62, 0.3 → 0.75, 0.85 → 1.0)</summary>
        public static double AtkMult(double skill) => 0.6 + skill * 0.5;

        /// <summary>봇 덱: 그 트로피의 사람이 가졌을 법한 유닛 (기본·로드 해금 + 등급별 트로피 기준: 희귀 80, 영웅 300, 전설 800)</summary>
        public static List<Job> Deck(int trophies, IRandom rand)
        {
            var pool = new List<Job>();
            foreach (UnitDef u in Units.All)
            {
                bool tierOk = u.Rarity == Rarity.Common || (u.Rarity == Rarity.Rare && trophies >= 80)
                    || (u.Rarity == Rarity.Epic && trophies >= 300) || (u.Rarity == Rarity.Legendary && trophies >= 800);
                int? roadAt = TrophyRoad.UnitAt(u.Job);
                if (Array.IndexOf(Units.Starter, u.Job) >= 0 || (roadAt.HasValue && roadAt.Value <= trophies) || tierOk) pool.Add(u.Job);
            }
            Shuffle(pool, rand);
            return pool.GetRange(0, Math.Min(Units.DeckSize, pool.Count));
        }

        /// <summary>봇 공격 덱: 기본 4장 + 그 트로피에서 열려 있을 몹 카드 중 4장</summary>
        public static List<AttackKey> AttackDeck(int trophies, IRandom rand)
        {
            var pool = new List<AttackKey>();
            foreach (AttackDef a in Attacks.All)
            {
                int? at = TrophyRoad.AttackAt(a.Key);
                if (Array.IndexOf(Attacks.Starter, a.Key) >= 0 || (at.HasValue && at.Value <= trophies)) pool.Add(a.Key);
            }
            Shuffle(pool, rand);
            return pool.GetRange(0, Math.Min(Attacks.DeckSize, pool.Count));
        }

        /// <summary>봇전 트로피 변화 - 서버 finishMatch 와 같은 규칙</summary>
        public static int TrophyDelta(bool won, int streak, int trophies)
            => won ? PvpConfig.TrophyWin + Math.Min(10, streak * 2) : -Math.Min(PvpConfig.TrophyLoss, trophies);

        /// <summary>목록을 무작위로 섞는다</summary>
        public static void Shuffle<T>(IList<T> list, IRandom rand)
        {
            // Fisher-Yates (원본은 sort(() => random - 0.5) - 치우친 섞기라 고르게 섞는 쪽으로)
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = SystemRandom.Index(rand, i + 1);
                T t = list[i];
                list[i] = list[j];
                list[j] = t;
            }
        }
    }
}
