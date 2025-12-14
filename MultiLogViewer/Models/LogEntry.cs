using System;
using System.Collections.Generic;

namespace MultiLogViewer.Models
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, string> AdditionalData { get; set; } = new Dictionary<string, string>();
    }
}
