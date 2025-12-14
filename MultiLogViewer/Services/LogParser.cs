using MultiLogViewer.Models;
using System.Text.RegularExpressions;
using System.Globalization;
using System;
using System.Collections.Generic;

namespace MultiLogViewer.Services
{
    public class LogParser : ILogParser
    {
        private readonly Regex _regex;
        private readonly string _timestampFormat;

        public LogParser(LogFormatConfig config)
        {
            _regex = new Regex(config.Pattern, RegexOptions.Compiled); // パフォーマンス向上のためCompiledオプションを付与
            _timestampFormat = config.TimestampFormat;
        }

        public LogEntry? Parse(string logLine)
        {
            var match = _regex.Match(logLine);

            if (!match.Success)
            {
                return null;
            }

            var logEntry = new LogEntry();

            // Timestampのパース
            if (match.Groups["timestamp"].Success)
            {
                if (DateTime.TryParseExact(match.Groups["timestamp"].Value, _timestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime timestamp))
                {
                    logEntry.Timestamp = timestamp;
                }
                else
                {
                    // 日時フォーマットが不正な場合は、現在の時刻を使用するか、エラーとして扱うか、要検討。
                    // 現時点ではデフォルト値を使用
                    logEntry.Timestamp = DateTime.MinValue;
                }
            }
            else
            {
                // timestampグループがない場合も、デフォルト値を使用
                logEntry.Timestamp = DateTime.MinValue;
            }

            // Messageの取得
            if (match.Groups["message"].Success)
            {
                logEntry.Message = match.Groups["message"].Value;
            }

            // その他のキャプチャグループをAdditionalDataに格納
            foreach (Group group in match.Groups)
            {
                if (group.Name != "0" && group.Success &&
                    group.Name != "timestamp" && group.Name != "message")
                {
                    logEntry.AdditionalData[group.Name] = group.Value;
                }
            }

            return logEntry;
        }
    }
}
