using MultiLogViewer.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ude;

namespace MultiLogViewer.Services
{
    /// <summary>
    /// ログファイルを読み込むためのリーダー実装です。
    /// </summary>
    public class LogFileReader : ILogFileReader
    {
        public LogFileReader()
        {
        }

        // --- 単一設定の実装（複数設定版へ委譲） ---

        /// <inheritdoc />
        public IEnumerable<LogEntry> Read(string filePath, LogFormatConfig config)
        {
            return Read(filePath, new[] { config });
        }

        /// <inheritdoc />
        public (IEnumerable<LogEntry> Entries, FileState UpdatedState) ReadIncremental(FileState currentState, LogFormatConfig config)
        {
            return ReadIncremental(currentState, new[] { config });
        }

        /// <inheritdoc />
        public IEnumerable<LogEntry> ReadFiles(IEnumerable<string> filePaths, LogFormatConfig config)
        {
            if (filePaths == null || !filePaths.Any())
            {
                return Enumerable.Empty<LogEntry>();
            }

            var allLogEntries = new List<LogEntry>();
            foreach (var filePath in filePaths)
            {
                allLogEntries.AddRange(Read(filePath, config));
            }
            return allLogEntries;
        }

        // --- 複数設定の実装 ---

        /// <inheritdoc />
        public IEnumerable<LogEntry> Read(string filePath, IEnumerable<LogFormatConfig> configs)
        {
            var (entries, _) = ReadInternal(filePath, 0, 0, configs);
            return entries;
        }

        /// <inheritdoc />
        public (IEnumerable<LogEntry> Entries, FileState UpdatedState) ReadIncremental(FileState currentState, IEnumerable<LogFormatConfig> configs)
        {
            if (!File.Exists(currentState.FilePath))
            {
                return (Enumerable.Empty<LogEntry>(), currentState);
            }

            var fileInfo = new FileInfo(currentState.FilePath);
            if (fileInfo.Length < currentState.LastPosition)
            {
                // ファイルサイズが小さくなった（ローテーションなど）場合は、最初から読み直します。
                var (entries, newState) = ReadInternal(currentState.FilePath, 0, 0, configs);
                return (entries, newState);
            }

            if (fileInfo.Length == currentState.LastPosition)
            {
                return (Enumerable.Empty<LogEntry>(), currentState);
            }

            var (newEntries, updatedState) = ReadInternal(currentState.FilePath, currentState.LastPosition, currentState.LastLineNumber, configs);
            return (newEntries, updatedState);
        }

        /// <summary>
        /// ログファイルを指定された位置から読み込み、パーサーを適用して解析する内部メソッドです。
        /// </summary>
        /// <param name="filePath">対象ファイルのパス。</param>
        /// <param name="startPosition">読み込み開始位置（バイト）。</param>
        /// <param name="startLineNumber">読み込み開始行番号。</param>
        /// <param name="configs">適用するログフォーマット設定のリスト。</param>
        /// <returns>解析されたログエントリと、読み込み後のファイル状態のタプル。</returns>
        private (IEnumerable<LogEntry> Entries, FileState State) ReadInternal(string filePath, long startPosition, int startLineNumber, IEnumerable<LogFormatConfig> configs)
        {
            if (!File.Exists(filePath))
            {
                return (Enumerable.Empty<LogEntry>(), new FileState(filePath, 0, 0));
            }

            // CompositeLogParser の準備（複数のフォーマット候補を順次試行します）
            var parsers = configs.Select(c => new LogParser(c)).Cast<ILogParser>().ToList();
            var parser = new CompositeLogParser(parsers);

            // マルチライン設定：いずれかのフォーマット設定で有効であれば、有効とみなします。
            bool isMultiline = configs.Any(c => c.IsMultiline);

            var results = new List<LogEntry>();
            long endPosition = startPosition;
            int currentLineNumber = startLineNumber;

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                // エンコーディングを自動判別します。
                System.Text.Encoding encoding = DetectFileEncoding(fs);
                fs.Seek(startPosition, SeekOrigin.Begin);

                using (var streamReader = new StreamReader(fs, encoding))
                {
                    string? line;
                    string fileName = Path.GetFileName(filePath);
                    LogEntry? currentEntry = null;

                    while ((line = streamReader.ReadLine()) != null)
                    {
                        currentLineNumber++;
                        var entry = parser.Parse(line, fileName, currentLineNumber);

                        if (entry != null)
                        {
                            // 新しいログの開始を検知したため、直前のエントリを確定させます。
                            if (currentEntry != null)
                            {
                                results.Add(currentEntry);
                            }
                            entry.RawLine = line;
                            entry.FileFullPath = filePath;
                            currentEntry = entry;
                        }
                        else if (currentEntry != null && isMultiline)
                        {
                            // どのフォーマットにも一致しない行を、直前のエントリの継続行として結合します。
                            currentEntry.Message += System.Environment.NewLine + line;
                            currentEntry.RawLine += System.Environment.NewLine + line;
                        }
                    }

                    if (currentEntry != null)
                    {
                        results.Add(currentEntry);
                    }

                    // 読み込み完了時点のファイル末尾位置を記録します。
                    endPosition = fs.Length;
                }
            }

            return (results, new FileState(filePath, endPosition, currentLineNumber));
        }

        /// <summary>
        /// ファイルの文字コードを自動判別します。
        /// </summary>
        /// <param name="fileStream">判別対象となるファイルの FileStream。</param>
        /// <returns>判別された文字コード。判別できない場合は UTF-8 を返します。</returns>
        private System.Text.Encoding DetectFileEncoding(FileStream fileStream)
        {
            var cdet = new CharsetDetector();
            cdet.Feed(fileStream);
            cdet.DataEnd();

            if (cdet.Charset != null)
            {
                // Shift-JIS の場合は、Windows-31J (コードページ 932) として扱います。
                if (cdet.Charset.Equals("Shift-JIS", System.StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        return System.Text.Encoding.GetEncoding(932);
                    }
                    catch (System.ArgumentException)
                    {
                        return System.Text.Encoding.UTF8;
                    }
                }

                try
                {
                    string detectedCharset = cdet.Charset;
                    string charsetName = detectedCharset.Replace('-', '_');
                    return System.Text.Encoding.GetEncoding(charsetName);
                }
                catch (System.ArgumentException)
                {
                    // 未対応のエンコーディングの場合は UTF-8 をフォールバックとして使用します。
                    return System.Text.Encoding.UTF8;
                }
            }

            return System.Text.Encoding.UTF8;
        }
    }
}
