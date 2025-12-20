using MultiLogViewer.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ude;

namespace MultiLogViewer.Services
{
    public class LogFileReader : ILogFileReader
    {
        public LogFileReader()
        {
        }

        public IEnumerable<LogEntry> Read(string filePath, LogFormatConfig config)
        {
            if (!File.Exists(filePath))
            {
                yield break;
            }

            var parser = new LogParser(config);

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                // エンコーディングを自動判別
                System.Text.Encoding encoding = DetectFileEncoding(fs);
                fs.Seek(0, SeekOrigin.Begin); // ストリームを先頭に戻す

                using (var streamReader = new StreamReader(fs, encoding))
                {
                    string line;
                    int lineNumber = 0; // 行番号をカウントする変数を導入
                    string fileName = Path.GetFileName(filePath); // ファイル名を事前に取得

                    while ((line = streamReader.ReadLine()) != null)
                    {
                        lineNumber++;
                        var entry = parser.Parse(line, fileName, lineNumber); // ファイル名と行番号を渡す
                        if (entry != null)
                        {
                            entry.RawLine = line; // パース前の行を保持
                            entry.FileFullPath = filePath; // フルパスを設定
                            yield return entry;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ファイルの文字コードを自動判別します。
        /// </summary>
        /// <param name="fileStream">判別するファイルのFileStream。</param>
        /// <returns>判別された文字コード。判別できなかった場合はUTF8を返します。</returns>
        private System.Text.Encoding DetectFileEncoding(FileStream fileStream)
        {
            var cdet = new CharsetDetector();
            cdet.Feed(fileStream); // ここでストリームの一部を読み込む
            cdet.DataEnd();

            if (cdet.Charset != null)
            {
                // Shift-JIS の場合は明示的にコードページ 932 を指定
                if (cdet.Charset.Equals("Shift-JIS", System.StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        return System.Text.Encoding.GetEncoding(932); // コードページ932 (Shift-JIS) を試す
                    }
                    catch (System.ArgumentException)
                    {
                        return System.Text.Encoding.UTF8; // フォールバック
                    }
                }

                try
                {
                    string detectedCharset = cdet.Charset; // ローカル変数に代入し、nullではないことを保証
                    string charsetName = detectedCharset.Replace('-', '_'); // ハイフンをアンダースコアに変換
                    return System.Text.Encoding.GetEncoding(charsetName);
                }
                catch (System.ArgumentException)
                {
                    // サポートされていないエンコーディングの場合はUTF8をフォールバック
                    return System.Text.Encoding.UTF8;
                }
            }
            // 判別できなかった場合はUTF8を返す
            return System.Text.Encoding.UTF8;
        }

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
    }
}
