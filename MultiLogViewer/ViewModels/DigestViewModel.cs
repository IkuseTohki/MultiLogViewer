using MultiLogViewer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MultiLogViewer.ViewModels
{
    public class DigestViewModel : ViewModelBase
    {
        public ObservableCollection<LogDigestEntry> DigestEntries { get; } = new ObservableCollection<LogDigestEntry>();

        public DigestViewModel(IEnumerable<LogEntry> bookmarkedEntries)
        {
            var sortedEntries = bookmarkedEntries.OrderBy(e => e.Timestamp).ToList();
            if (!sortedEntries.Any()) return;

            DateTime? firstTime = null;
            DateTime? prevTime = null;

            foreach (var entry in sortedEntries)
            {
                if (!firstTime.HasValue)
                {
                    firstTime = entry.Timestamp;
                    prevTime = entry.Timestamp;
                    DigestEntries.Add(new LogDigestEntry(entry, TimeSpan.Zero, TimeSpan.Zero));
                }
                else
                {
                    // firstTime.HasValue が true なので、prevTime も必ず値を持っている
                    var delta = entry.Timestamp - prevTime!.Value;
                    var total = entry.Timestamp - firstTime.Value;
                    DigestEntries.Add(new LogDigestEntry(entry, delta, total));
                    prevTime = entry.Timestamp;
                }
            }
        }
    }
}
