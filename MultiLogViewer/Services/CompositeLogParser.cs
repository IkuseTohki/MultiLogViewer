using MultiLogViewer.Models;
using System.Collections.Generic;

namespace MultiLogViewer.Services
{
    public class CompositeLogParser : ILogParser
    {
        private readonly IEnumerable<ILogParser> _parsers;

        public CompositeLogParser(IEnumerable<ILogParser> parsers)
        {
            _parsers = parsers ?? new List<ILogParser>();
        }

        public LogEntry? Parse(string logLine, string fileName, int lineNumber)
        {
            foreach (var parser in _parsers)
            {
                var entry = parser.Parse(logLine, fileName, lineNumber);
                if (entry != null)
                {
                    return entry;
                }
            }
            return null;
        }
    }
}
