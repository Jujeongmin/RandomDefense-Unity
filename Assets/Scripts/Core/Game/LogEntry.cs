namespace DefenseBattle.Core
{
    /// <summary>전투 로그 한 줄·신화 배너 (types.ts LogEntry / Banner)</summary>
    public sealed class LogEntry
    {
        /// <summary>표시할 문장 (이미 번역됨)</summary>
        public string Text;
        /// <summary>남은 시간(초, 실시간)</summary>
        public double Ttl;
    }
}
