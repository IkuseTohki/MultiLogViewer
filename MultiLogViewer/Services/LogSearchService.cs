using MultiLogViewer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MultiLogViewer.Services
{
    public class LogSearchService : ILogSearchService
    {
        public bool IsMatch(LogEntry entry, SearchCriteria criteria)
        {
            if (string.IsNullOrEmpty(criteria.SearchText)) return false;

            if (criteria.IsRegex)
            {
                try
                {
                    var options = criteria.IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                    var regex = new Regex(criteria.SearchText, options);

                    if (regex.IsMatch(entry.Message)) return true;
                    if (regex.IsMatch(entry.FileName)) return true;
                    foreach (var value in entry.AdditionalData.Values)
                    {
                        if (regex.IsMatch(value)) return true;
                    }
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                var comparison = criteria.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                if (entry.Message.IndexOf(criteria.SearchText, comparison) >= 0) return true;
                if (entry.FileName.IndexOf(criteria.SearchText, comparison) >= 0) return true;
                foreach (var value in entry.AdditionalData.Values)
                {
                    if (value.IndexOf(criteria.SearchText, comparison) >= 0) return true;
                }
            }
            return false;
        }

        public LogEntry? Find(IEnumerable<LogEntry> entries, LogEntry? currentSelection, SearchCriteria criteria, bool forward)
        {
            var items = entries.ToList();
            if (!items.Any() || string.IsNullOrEmpty(criteria.SearchText)) return null;

            int startIndex = currentSelection != null ? items.IndexOf(currentSelection) : -1;
            int count = items.Count;

            for (int i = 1; i <= count; i++)
            {
                int k = forward
                    ? (startIndex + i) % count
                    : (startIndex - i + count) % count;

                if (IsMatch(items[k], criteria))
                {
                    return items[k];
                }
            }

            return null;
        }

        public (int matchCount, int currentIndex) GetSearchStatistics(IEnumerable<LogEntry> entries, LogEntry? currentSelection, SearchCriteria criteria)
        {
            if (string.IsNullOrEmpty(criteria.SearchText)) return (0, 0);

            int matchCount = 0;
            int currentIndex = 0;
            bool selectionFound = false;

            foreach (var entry in entries)
            {
                if (IsMatch(entry, criteria))
                {
                    matchCount++;
                    if (entry == currentSelection)
                    {
                        currentIndex = matchCount;
                        selectionFound = true;
                    }
                }
            }

            return (matchCount, selectionFound ? currentIndex : 0);
        }

        public bool ShouldHide(LogEntry entry, IEnumerable<LogFilter> filters)
        {
            if (filters == null || !filters.Any()) return false;

            // 1. カラムフィルター（空チェック）の判定
            // 全ての指定項目が空の場合にのみ非表示とするため、項目を抽出して一括判定する
            var columnKeys = filters.Where(f => f.Type == FilterType.ColumnEmpty).Select(f => f.Key).ToList();
            if (columnKeys.Any())
            {
                bool allEmpty = true;
                foreach (var key in columnKeys)
                {
                    if (entry.AdditionalData.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                    {
                        allEmpty = false;
                        break;
                    }
                }
                if (allEmpty) return true; // 全て空なので非表示
            }

            // 2. 日時フィルターの判定
            foreach (var filter in filters)
            {
                if (filter.Type == FilterType.DateTimeAfter)
                {
                    if (entry.Timestamp < filter.Value) return true; // 指定より前なので非表示
                }
                else if (filter.Type == FilterType.DateTimeBefore)
                {
                    if (entry.Timestamp > filter.Value) return true; // 指定より後なので非表示
                }
            }

            return false;
        }
    }
}
