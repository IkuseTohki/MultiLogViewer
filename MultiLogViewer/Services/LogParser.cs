using MultiLogViewer.Models;
using System.Text.RegularExpressions;
using System.Globalization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiLogViewer.Services
{
    public class LogParser : ILogParser
    {
        private readonly LogFormatConfig _config;
        private readonly Regex _regex;
        private readonly List<SubPatternExecutor> _subPatternExecutors;

        public LogParser(LogFormatConfig config)
        {
            _config = config;
            // メインパターンには明示的なオプション指定はないが、パターン文字列内に (?s) 等を含めることは可能
            _regex = new Regex(config.Pattern, RegexOptions.Compiled);

            _subPatternExecutors = new List<SubPatternExecutor>();
            if (config.SubPatterns != null)
            {
                foreach (var subConfig in config.SubPatterns)
                {
                    _subPatternExecutors.Add(new SubPatternExecutor(subConfig));
                }
            }
        }

        public LogEntry? Parse(string logLine, string fileName, int lineNumber)
        {
            var match = _regex.Match(logLine);

            if (!match.Success)
            {
                return null;
            }

            var logEntry = new LogEntry
            {
                FileName = fileName,
                LineNumber = lineNumber
            };

            ParseTimestamp(match, logEntry);
            ParseMessage(match, logEntry);
            ParseMainCaptures(match, logEntry);
            ApplySubPatterns(logEntry);

            return logEntry;
        }

        private void ParseTimestamp(Match match, LogEntry logEntry)
        {
            var timestampGroup = FindGroup(match, "timestamp");
            if (timestampGroup != null && timestampGroup.Success)
            {
                var val = ApplyTransforms(timestampGroup.Value, "timestamp", _config.FieldTransforms);
                if (DateTime.TryParseExact(val, _config.TimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime timestamp))
                {
                    logEntry.Timestamp = timestamp;
                }
                else
                {
                    logEntry.Timestamp = DateTime.MinValue;
                }
            }
            else
            {
                logEntry.Timestamp = DateTime.MinValue;
            }
        }

        private void ParseMessage(Match match, LogEntry logEntry)
        {
            var messageGroup = FindGroup(match, "message");
            if (messageGroup != null && messageGroup.Success)
            {
                logEntry.Message = ApplyTransforms(messageGroup.Value, "message", _config.FieldTransforms);
            }
        }

        private void ParseMainCaptures(Match match, LogEntry logEntry)
        {
            foreach (Group group in match.Groups)
            {
                if (group.Name != "0" && group.Success &&
                    !string.Equals(group.Name, "timestamp", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(group.Name, "message", StringComparison.OrdinalIgnoreCase))
                {
                    logEntry.AdditionalData[group.Name] = ApplyTransforms(group.Value, group.Name, _config.FieldTransforms);
                }
            }
        }

        private void ApplySubPatterns(LogEntry logEntry)
        {
            foreach (var executor in _subPatternExecutors)
            {
                string sourceValue = ResolveSourceValue(logEntry, executor.Config.SourceField);

                if (string.IsNullOrEmpty(sourceValue))
                {
                    continue;
                }

                if (executor.Config.MatchType == MatchType.All)
                {
                    ProcessAllMatches(sourceValue, logEntry, executor);
                }
                else
                {
                    ProcessSingleMatch(sourceValue, logEntry, executor);
                }
            }
        }

        private string ResolveSourceValue(LogEntry logEntry, string sourceField)
        {
            if (string.Equals(sourceField, "message", StringComparison.OrdinalIgnoreCase))
            {
                return logEntry.Message;
            }
            else if (logEntry.AdditionalData.ContainsKey(sourceField))
            {
                return logEntry.AdditionalData[sourceField];
            }
            return string.Empty;
        }

        private void ProcessAllMatches(string sourceValue, LogEntry logEntry, SubPatternExecutor executor)
        {
            var matches = executor.Regex.Matches(sourceValue);
            var groupValues = new Dictionary<string, List<string>>();

            foreach (Match m in matches)
            {
                foreach (Group group in m.Groups)
                {
                    if (group.Name != "0" && group.Success)
                    {
                        if (!groupValues.ContainsKey(group.Name))
                        {
                            groupValues[group.Name] = new List<string>();
                        }
                        var transformed = ApplyTransforms(group.Value, group.Name, executor.Config.FieldTransforms);
                        groupValues[group.Name].Add(transformed);
                    }
                }
            }

            // 結合して AdditionalData に格納
            foreach (var kvp in groupValues)
            {
                var separator = executor.Config.Separator ?? ", ";
                logEntry.AdditionalData[kvp.Key] = string.Join(separator, kvp.Value);
            }
        }

        private void ProcessSingleMatch(string sourceValue, LogEntry logEntry, SubPatternExecutor executor)
        {
            var subMatch = executor.Regex.Match(sourceValue);
            if (subMatch.Success)
            {
                foreach (Group group in subMatch.Groups)
                {
                    if (group.Name != "0" && group.Success)
                    {
                        logEntry.AdditionalData[group.Name] = ApplyTransforms(group.Value, group.Name, executor.Config.FieldTransforms);
                    }
                }
            }
        }

        private Group? FindGroup(Match match, string groupName)
        {
            foreach (Group group in match.Groups)
            {
                if (string.Equals(group.Name, groupName, StringComparison.OrdinalIgnoreCase))
                {
                    return group;
                }
            }
            return null;
        }

        private string ApplyTransforms(string value, string fieldName, List<FieldTransformConfig> transforms)
        {
            if (transforms == null) return value;

            var transform = transforms.Find(t => t.Field == fieldName);
            if (transform == null) return value;

            string result = value;

            // 1. Map による置換
            if (transform.Map != null && transform.Map.TryGetValue(value, out var mappedValue))
            {
                result = mappedValue;
            }

            // 2. Format による整形
            if (!string.IsNullOrEmpty(transform.Format))
            {
                result = transform.Format.Replace("{value}", result);
            }

            return result;
        }

        private class SubPatternExecutor
        {
            public SubPatternConfig Config { get; }
            public Regex Regex { get; }

            public SubPatternExecutor(SubPatternConfig config)
            {
                Config = config;

                RegexOptions options = RegexOptions.None;
                if (config.Options != null)
                {
                    foreach (var optStr in config.Options)
                    {
                        if (Enum.TryParse<RegexOptions>(optStr, true, out var opt))
                        {
                            options |= opt;
                        }
                    }
                }

                // Compiledオプションをデフォルトで付与するかはトレードオフだが、
                // ログパースは繰り返し実行されるためCompiledが有利
                options |= RegexOptions.Compiled;

                Regex = new Regex(config.Pattern, options);
            }
        }
    }
}
