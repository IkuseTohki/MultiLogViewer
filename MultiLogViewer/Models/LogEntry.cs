using System;
using System.Collections.Generic;

namespace MultiLogViewer.Models
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = string.Empty;
        public string RawLine { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileFullPath { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public Dictionary<string, string> AdditionalData { get; set; } = new Dictionary<string, string>();
    }
}
