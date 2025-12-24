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
        public Dictionary<string, string> AdditionalData { get; set; } = new Dictionary<string, string>();

        private bool _isBookmarked;
        public bool IsBookmarked
        {
            get => _isBookmarked;
            set => SetProperty(ref _isBookmarked, value);
        }

        private string _bookmarkMemo = string.Empty;
        public string BookmarkMemo
        {
            get => _bookmarkMemo;
            set => SetProperty(ref _bookmarkMemo, value);
        }
    }
}
