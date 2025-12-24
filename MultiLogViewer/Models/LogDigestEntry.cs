using System;

namespace MultiLogViewer.Models
{
    /// <summary>
    /// ダイジェスト・ビューで表示するための、時間差情報を含むログエントリのラップクラスです。
    /// </summary>
    public class LogDigestEntry
    {
        public LogEntry Entry { get; }

        /// <summary>
        /// 1つ前のブックマークからの経過時間。
        /// </summary>
        public TimeSpan Delta { get; }

        /// <summary>
        /// 最初のブックマークからの累積時間。
        /// </summary>
        public TimeSpan TotalElapsed { get; }

        public string DeltaText => Delta == TimeSpan.Zero ? "-" : Delta.ToString(@"\+hh\:mm\:ss\.fff");
        public string TotalElapsedText => TotalElapsed.ToString(@"hh\:mm\:ss");

        public LogDigestEntry(LogEntry entry, TimeSpan delta, TimeSpan totalElapsed)
        {
            Entry = entry;
            Delta = delta;
            TotalElapsed = totalElapsed;
        }
    }
}
