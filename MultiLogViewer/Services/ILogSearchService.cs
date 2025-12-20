using MultiLogViewer.Models;
using System.Collections.Generic;

namespace MultiLogViewer.Services
{
    /// <summary>
    /// ログエントリの検索ロジックを提供するサービスのインターフェースです。
    /// </summary>
    public interface ILogSearchService
    {
        /// <summary>
        /// 指定されたログエントリが検索条件に一致するかどうかを判定します。
        /// </summary>
        bool IsMatch(LogEntry entry, SearchCriteria criteria);

        /// <summary>
        /// 現在の選択位置から、次の（または前の）一致するログエントリを探索します。
        /// </summary>
        LogEntry? Find(IEnumerable<LogEntry> entries, LogEntry? currentSelection, SearchCriteria criteria, bool forward);

        /// <summary>
        /// 全ヒット数と、現在の選択位置が何番目のヒットかを取得します。
        /// </summary>
        (int matchCount, int currentIndex) GetSearchStatistics(IEnumerable<LogEntry> entries, LogEntry? currentSelection, SearchCriteria criteria);

        /// <summary>
        /// 指定されたフィルターに基づいて、そのログエントリを非表示にすべきかどうかを判定します。
        /// </summary>
        /// <param name="entry">判定対象のログエントリ。</param>
        /// <param name="filters">適用されている拡張フィルターの一覧。</param>
        /// <returns>非表示にすべきなら true、そうでなければ false。</returns>
        bool ShouldHide(LogEntry entry, IEnumerable<LogFilter> filters);
    }
}
