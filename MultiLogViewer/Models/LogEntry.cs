using MultiLogViewer.ViewModels;
using System;
using System.Collections.Generic;

namespace MultiLogViewer.Models
{
    public class LogEntry : ViewModelBase
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = string.Empty;
        public string RawLine { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileFullPath { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public long SequenceNumber { get; set; }
        public Dictionary<string, string> AdditionalData { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// AdditionalData の値を安全に取得するためのインデクサです。
        /// キーが存在しない場合は空文字を返します。
        /// </summary>
        /// <param name="key">追加データのキー名。</param>
        /// <returns>対応する値、または空文字。</returns>
        public string this[string key] => AdditionalData.TryGetValue(key, out var value) ? value : string.Empty;

        private bool _isBookmarked;
        public bool IsBookmarked
        {
            get => _isBookmarked;
            set => SetProperty(ref _isBookmarked, value);
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        private BookmarkColor _bookmarkColor = BookmarkColor.Blue;
        public BookmarkColor BookmarkColor
        {
            get => _bookmarkColor;
            set => SetProperty(ref _bookmarkColor, value);
        }

        private string _bookmarkMemo = string.Empty;
        public string BookmarkMemo
        {
            get => _bookmarkMemo;
            set => SetProperty(ref _bookmarkMemo, value);
        }
    }
}
