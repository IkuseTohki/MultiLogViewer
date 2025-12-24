using MultiLogViewer.Models;
using System.Collections.Generic;

namespace MultiLogViewer.Services
{
    public interface ILogFileReader
    {
        IEnumerable<LogEntry> Read(string filePath, LogFormatConfig config);

        /// <summary>
        /// 指定された位置からログを読み込み、新しいエントリと更新された状態を返します。
        /// </summary>
        (IEnumerable<LogEntry> Entries, FileState UpdatedState) ReadIncremental(FileState currentState, LogFormatConfig config);

        /// <summary>
        /// 指定されたログフォーマット設定に基づいて、複数のログファイルを読み込み、解析します。
        /// </summary>
        /// <param name="filePaths">読み込むログファイルのパスのリスト。</param>
        /// <param name="config">ログファイルの解析に使用するフォーマット設定。</param>
        /// <returns>解析されたログエントリのリスト。</returns>
        IEnumerable<LogEntry> ReadFiles(IEnumerable<string> filePaths, LogFormatConfig config);

        /// <summary>
        /// 指定された複数のログフォーマット設定に基づいて、ログファイルを解析します。
        /// </summary>
        IEnumerable<LogEntry> Read(string filePath, IEnumerable<LogFormatConfig> configs);

        /// <summary>
        /// 指定された位置からログを読み込み、複数のフォーマット候補を使用して解析します。
        /// </summary>
        (IEnumerable<LogEntry> Entries, FileState UpdatedState) ReadIncremental(FileState currentState, IEnumerable<LogFormatConfig> configs);
    }
}
