using System.Collections.Generic;

namespace DefenseBattle.Core
{
    /// <summary>
    /// PvP 상대 화면 미니맵용 상태 요약 (engine.ts BoardSnapshot).
    /// 좌표는 0~255 로 양자화 (전장 720 기준).
    /// </summary>
    public sealed class BoardSnapshot
    {
        /// <summary>형식 버전 - 2 부터 상대 전장을 스프라이트로 그릴 수 있다</summary>
        public const int Version = 2;

        public int Wave;
        public int MobCount;
        public int UnitCount;
        /// <summary>보유 최고 등급 번호 (유닛이 없으면 -1)</summary>
        public int TopGrade;
        public int Gold;
        /// <summary>몹 [id, x, y, code, size] 평탄화. code = 종족 | 종류&lt;&lt;2 | 방향&lt;&lt;4 | 그림변형&lt;&lt;6</summary>
        public List<int> Mobs = new List<int>();
        /// <summary>유닛 [직업 번호, 등급 번호, x, y] 평탄화</summary>
        public List<int> Units = new List<int>();
        /// <summary>유닛마다 방향 | 공격 중 4 (Units 와 같은 순서)</summary>
        public List<int> Uc = new List<int>();
        /// <summary>몹마다 남은 체력 % 0~100 (Mobs 와 같은 순서)</summary>
        public List<int> Mh = new List<int>();
        /// <summary>몹마다 상태 비트 SnapStatus (Mobs 와 같은 순서)</summary>
        public List<int> Ms = new List<int>();
        /// <summary>사거리 빈틈: GapSamples 개 길 표본을 GapBits 개씩 비트로 (1 = 어떤 유닛도 안 닿음)</summary>
        public List<int> Gp = new List<int>();
        /// <summary>지난 스냅샷 뒤 생긴 공격 이펙트 (Game.LogFx 형식)</summary>
        public List<int[]> Fx = new List<int[]>();
        /// <summary>보스 체력 비율 (없으면 -1)</summary>
        public double BossHp = -1;
        /// <summary>유닛 덱 (Job 번호) - 상대가 무엇으로 막는지 보고 카드를 고른다</summary>
        public List<int> Deck = new List<int>();
        /// <summary>몹 카드 덱 (카드 번호) - 무엇이 날아올지</summary>
        public List<int> Atk = new List<int>();
        public int V = Version;
        /// <summary>보낸 쪽 게임 시간(초) - 받는 쪽이 도착 간격의 흔들림과 상관없이 고르게 보간한다</summary>
        public double T;

        // 스냅샷 몹 code 의 종류 값
        public const int KindNormal = 0, KindSent = 1, KindBoss = 2;
        // 스냅샷 몹 상태 비트 (Ms)
        public const int StatusSlow = 1, StatusStop = 2, StatusPoison = 4, StatusMark = 8, StatusEnraged = 16, StatusGhost = 32, StatusHidden = 64;
        /// <summary>길 한 바퀴를 몇 점으로 나눠 사거리 빈틈(빨간 곳)을 볼지</summary>
        public const int GapSamples = 160;
        /// <summary>빈틈 비트마스크: 숫자 하나에 담는 표본 수</summary>
        public const int GapBits = 20;

        /// <summary>전장 좌표 → 0~255</summary>
        public static int Quantize(double v)
        {
            return System.Math.Max(0, System.Math.Min(255, JsMath.RoundInt(v / Board.MapSize * 255)));
        }

        /// <summary>0~255 → 전장 좌표</summary>
        public static double Dequantize(int q) => q / 255.0 * Board.MapSize;
    }
}
