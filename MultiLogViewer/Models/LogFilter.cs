using System;

namespace MultiLogViewer.Models
{
    public enum FilterType
    {
        ColumnEmpty,    // カラムが空なら非表示
        DateTimeAfter,  // 指定日時以降を表示
        DateTimeBefore, // 指定日時以前を表示
        Bookmark        // ブックマークされた行のみ表示
    }

    /// <summary>
    /// 拡張フィルターの1項目を表すクラスです。
    /// </summary>
    public class LogFilter
    {
        public FilterType Type { get; set; }
        public string Key { get; set; } = string.Empty;      // カラムフィルターの場合はキー名
        public DateTime Value { get; set; }  // 日時フィルターの場合は基準日時
        public string DisplayText { get; set; } = string.Empty; // バッジに表示する文字列

        public LogFilter() { }

        public LogFilter(FilterType type, string key, DateTime value, string displayText)
        {
            Type = type;
            Key = key;
            Value = value;
            DisplayText = displayText;
        }

        /// <summary>
        /// フィルターの設定内容が有効かどうかを検証します。
        /// </summary>
        /// <exception cref="System.Exception">設定が無効な場合にスローされます。</exception>
        public void Validate()
        {
            if (Type == FilterType.ColumnEmpty && string.IsNullOrWhiteSpace(Key))
            {
                throw new System.Exception("カラムフィルターに有効なキーが指定されていません。");
            }

            // 必要に応じて他のバリデーション（日時の範囲チェック等）もここに追加可能
        }

        // 同一性の判定（同じ種類のフィルターを上書きするため）
        public override bool Equals(object? obj)
        {
            if (obj is LogFilter other)
            {
                if (Type != other.Type) return false;
                if (Type == FilterType.ColumnEmpty) return Key == other.Key;
                return true; // 日時フィルターの場合はタイプが同じなら同一とみなす（上書き用）
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Type == FilterType.ColumnEmpty ? (Type, Key).GetHashCode() : Type.GetHashCode();
        }
    }
}
