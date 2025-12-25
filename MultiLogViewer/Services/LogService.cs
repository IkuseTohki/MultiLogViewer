using MultiLogViewer.Models;
using System.Collections.Generic;
using System.Linq;

namespace MultiLogViewer.Services
{
    public class LogService : ILogService
    {
        private readonly ILogFormatConfigLoader _logFormatConfigLoader;
        private readonly IFileResolver _fileResolver;
        private readonly ILogFileReader _logFileReader;

        public LogService(
            ILogFormatConfigLoader logFormatConfigLoader,
            IFileResolver fileResolver,
            ILogFileReader logFileReader)
        {
            _logFormatConfigLoader = logFormatConfigLoader;
            _fileResolver = fileResolver;
            _logFileReader = logFileReader;
        }

        public LogDataResult LoadFromConfig(string configPath)
        {
            var appConfig = _logFormatConfigLoader.Load(configPath);
            if (appConfig == null)
            {
                return new LogDataResult(new List<LogEntry>(), new List<DisplayColumnConfig>(), new List<FileState>());
            }

            var allEntries = new List<LogEntry>();
            var fileStates = new List<FileState>();
            long currentSequence = 0;

            if (appConfig.LogFormats != null)
            {
                // ファイルパスごとのConfigリストを作成
                var fileConfigs = ResolveFileConfigs(appConfig.LogFormats);

                // ファイルごとに読み込み
                foreach (var kvp in fileConfigs)
                {
                    var path = kvp.Key;
                    var configs = kvp.Value;

                    var (entries, state) = _logFileReader.ReadIncremental(new FileState(path, 0, 0), configs);

                    foreach (var entry in entries)
                    {
                        entry.SequenceNumber = currentSequence++;
                    }

                    allEntries.AddRange(entries);
                    fileStates.Add(state);
                }
            }

            var sortedEntries = allEntries.OrderBy(e => e.Timestamp).ThenBy(e => e.SequenceNumber).ToList();
            var displayColumns = appConfig.DisplayColumns?.ToList() ?? new List<DisplayColumnConfig>();

            // ColumnStyles を DisplayColumns に紐付け
            if (appConfig.ColumnStyles != null && displayColumns.Any())
            {
                foreach (var col in displayColumns)
                {
                    var style = appConfig.ColumnStyles.FirstOrDefault(s => s.ColumnHeader == col.Header);
                    if (style != null)
                    {
                        col.StyleConfig = style;
                    }
                }
            }

            return new LogDataResult(sortedEntries, displayColumns, fileStates, appConfig.PollingIntervalMs);
        }

        public LogDataResult LoadIncremental(string configPath, List<FileState> currentStates, long startSequenceNumber)
        {
            var appConfig = _logFormatConfigLoader.Load(configPath);
            if (appConfig == null || appConfig.LogFormats == null)
            {
                return new LogDataResult(new List<LogEntry>(), new List<DisplayColumnConfig>(), currentStates);
            }

            var newEntries = new List<LogEntry>();
            var updatedStates = new List<FileState>();
            long currentSequence = startSequenceNumber;

            // ファイルパスごとのConfigリストを作成
            var fileConfigs = ResolveFileConfigs(appConfig.LogFormats);

            // ファイルごとに読み込み
            foreach (var kvp in fileConfigs)
            {
                var path = kvp.Key;
                var configs = kvp.Value;

                var currentState = currentStates.FirstOrDefault(s => s.FilePath == path) ?? new FileState(path, 0, 0);

                var (entries, state) = _logFileReader.ReadIncremental(currentState, configs);

                foreach (var entry in entries)
                {
                    entry.SequenceNumber = currentSequence++;
                }

                newEntries.AddRange(entries);
                updatedStates.Add(state);
            }

            // まだ処理していない（設定から消えた？）状態も引き継ぐ（オプション）
            foreach (var oldState in currentStates)
            {
                if (!updatedStates.Any(s => s.FilePath == oldState.FilePath))
                {
                    updatedStates.Add(oldState);
                }
            }

            return new LogDataResult(newEntries.OrderBy(e => e.Timestamp).ThenBy(e => e.SequenceNumber).ToList(), new List<DisplayColumnConfig>(), updatedStates, appConfig.PollingIntervalMs);
        }

        private Dictionary<string, List<LogFormatConfig>> ResolveFileConfigs(IEnumerable<LogFormatConfig> logFormats)
        {
            var fileConfigs = new Dictionary<string, List<LogFormatConfig>>();

            foreach (var logFormatConfig in logFormats)
            {
                if (logFormatConfig.LogFilePatterns != null && logFormatConfig.LogFilePatterns.Any())
                {
                    var filePaths = _fileResolver.Resolve(logFormatConfig.LogFilePatterns);
                    foreach (var path in filePaths)
                    {
                        if (!fileConfigs.ContainsKey(path))
                        {
                            fileConfigs[path] = new List<LogFormatConfig>();
                        }
                        fileConfigs[path].Add(logFormatConfig);
                    }
                }
            }

            return fileConfigs;
        }
    }
}
