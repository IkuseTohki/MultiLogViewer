using MultiLogViewer.Models;
using System;
using System.Globalization;

namespace MultiLogViewer.Utils
{
    /// <summary>
    /// LogEntry の特定の列に対応する値を取得・整形するためのユーティリティクラスです。
    /// </summary>
    public static class LogEntryValueConverter
    {
        private const string AdditionalDataPrefix = "AdditionalData[";

        /// <summary>
        /// 指定された列定義に基づいて、ログエントリから表示用の文字列を取得します。
        /// </summary>
        /// <param name="entry">対象のログエントリ。</param>
        /// <param name="column">列定義。</param>
        /// <returns>整形済みの文字列。</returns>
        public static string GetStringValue(LogEntry entry, DisplayColumnConfig column)
        {
            return GetStringValue(entry, column.BindingPath, column.StringFormat);
        }

        /// <summary>
        /// 指定されたバインドパスと書式に基づいて、ログエントリから表示用の文字列を取得します。
        /// </summary>
        /// <param name="entry">対象のログエントリ。</param>
        /// <param name="bindingPath">バインドパス。</param>
        /// <param name="format">表示書式（オプション）。</param>
        /// <returns>整形済みの文字列。</returns>
        public static string GetStringValue(LogEntry entry, string bindingPath, string? format)
        {
            if (string.IsNullOrEmpty(bindingPath)) return string.Empty;

            object? rawValue = null;
            string matchType = "None";

            if (string.Equals(bindingPath, "Timestamp", StringComparison.OrdinalIgnoreCase))
            {
                rawValue = entry.Timestamp;
                matchType = "Timestamp";
            }
            else if (string.Equals(bindingPath, "Message", StringComparison.OrdinalIgnoreCase))
            {
                rawValue = entry.Message;
                matchType = "Message";
            }
            else if (string.Equals(bindingPath, "FileName", StringComparison.OrdinalIgnoreCase))
            {
                rawValue = entry.FileName;
                matchType = "FileName";
            }
            else if (string.Equals(bindingPath, "LineNumber", StringComparison.OrdinalIgnoreCase))
            {
                rawValue = entry.LineNumber;
                matchType = "LineNumber";
            }
            else if (bindingPath.StartsWith(AdditionalDataPrefix, StringComparison.OrdinalIgnoreCase) && bindingPath.EndsWith("]"))
            {
                var key = bindingPath.Substring(AdditionalDataPrefix.Length, bindingPath.Length - AdditionalDataPrefix.Length - 1);
                matchType = $"AdditionalData[{key}]";
                if (entry.AdditionalData.TryGetValue(key, out var val))
                {
                    rawValue = val;
                }
                else
                {
                    matchType += " (KeyNotFound)";
                }
            }

            if (rawValue == null) return string.Empty;

            if (!string.IsNullOrEmpty(format))
            {
                return string.Format(CultureInfo.InvariantCulture, $"{{0:{format}}}", rawValue);
            }

            return rawValue.ToString() ?? string.Empty;
        }

        /// <summary>
        /// バインドパスから AdditionalData のキー名を抽出します。
        /// </summary>
        /// <param name="bindingPath">バインドパス (例: AdditionalData[key])。</param>
        /// <returns>抽出されたキー名。該当しない場合は null。</returns>
        public static string? ExtractAdditionalDataKey(string bindingPath)
        {
            if (string.IsNullOrEmpty(bindingPath)) return null;
            if (string.Equals(bindingPath, "Timestamp", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(bindingPath, "Message", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(bindingPath, "FileName", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(bindingPath, "LineNumber", StringComparison.OrdinalIgnoreCase)) return null;

            if (bindingPath.StartsWith(AdditionalDataPrefix, StringComparison.OrdinalIgnoreCase) && bindingPath.EndsWith("]"))
            {
                return bindingPath.Substring(AdditionalDataPrefix.Length, bindingPath.Length - AdditionalDataPrefix.Length - 1);
            }
            return null;
        }
    }
}
