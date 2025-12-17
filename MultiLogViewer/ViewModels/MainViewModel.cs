using MultiLogViewer.Models;
using MultiLogViewer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace MultiLogViewer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        private readonly ILogFileReader _logFileReader;
        private readonly IUserDialogService _userDialogService;
        private readonly ILogFormatConfigLoader _logFormatConfigLoader;
        private readonly IFileResolver _fileResolver;

        private readonly ObservableCollection<LogEntry> _logEntries = new();
        public ICollectionView LogEntriesView { get; }

        private ObservableCollection<DisplayColumnConfig> _displayColumns = new();
        public ObservableCollection<DisplayColumnConfig> DisplayColumns
        {
            get => _displayColumns;
            set => SetProperty(ref _displayColumns, value);
        }

        private string _filterText = string.Empty;
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (SetProperty(ref _filterText, value))
                {
                    LogEntriesView.Refresh();
                }
            }
        }

        public MainViewModel(
            ILogFileReader logFileReader,
            IUserDialogService userDialogService,
            ILogFormatConfigLoader logFormatConfigLoader,
            IFileResolver fileResolver)
        {
            _logFileReader = logFileReader;
            _userDialogService = userDialogService;
            _logFormatConfigLoader = logFormatConfigLoader;
            _fileResolver = fileResolver;

            LogEntriesView = CollectionViewSource.GetDefaultView(_logEntries);
            LogEntriesView.Filter = FilterLogEntries;

            InitializeLogs();
        }



        private bool FilterLogEntries(object obj)
        {
            if (obj is LogEntry entry)
            {
                if (string.IsNullOrWhiteSpace(FilterText))
                {
                    return true;
                }
                return entry.Message.Contains(FilterText, System.StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        private void InitializeLogs()
        {
            var appConfig = _logFormatConfigLoader.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.yaml"));
            if (appConfig == null)
            {
                _userDialogService.ShowError("Could not load or parse config.yaml.", "Error");
                return;
            }

            // DisplayColumns を設定
            if (appConfig.DisplayColumns != null && appConfig.DisplayColumns.Any())
            {
                DisplayColumns = new ObservableCollection<DisplayColumnConfig>(appConfig.DisplayColumns);
            }

            // LogFormats からログを読み込む
            if (appConfig.LogFormats == null || !appConfig.LogFormats.Any())
            {
                return;
            }

            var allEntries = new List<LogEntry>();
            foreach (var logFormatConfig in appConfig.LogFormats)
            {
                if (logFormatConfig.LogFilePatterns != null && logFormatConfig.LogFilePatterns.Any())
                {
                    var filePaths = _fileResolver.Resolve(logFormatConfig.LogFilePatterns);
                    var entries = _logFileReader.ReadFiles(filePaths, logFormatConfig);
                    allEntries.AddRange(entries);
                }
            }

            // エントリをタイムスタンプでソートしてコレクションに追加
            _logEntries.Clear();
            foreach (var entry in allEntries.OrderBy(e => e.Timestamp))
            {
                _logEntries.Add(entry);
            }

            LogEntriesView.Refresh();
        }
    }
}