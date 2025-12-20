using MultiLogViewer.Models;
using MultiLogViewer.Services;
using MultiLogViewer.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;

namespace MultiLogViewer.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ILogService _logService;
        private readonly IUserDialogService _userDialogService;
        private readonly ISearchWindowService _searchWindowService;
        private readonly ILogSearchService _logSearchService;
        private readonly IClipboardService _clipboardService;
        private readonly IConfigPathResolver _configPathResolver;

        private string _configPath = string.Empty;

        private readonly ObservableCollection<LogEntry> _logEntries = new ObservableCollection<LogEntry>();
        public ICollectionView LogEntriesView { get; }

        private LogEntry? _selectedLogEntry;
        public LogEntry? SelectedLogEntry
        {
            get => _selectedLogEntry;
            set
            {
                if (SetProperty(ref _selectedLogEntry, value))
                {
                    UpdateSearchStatus();
                }
            }
        }

        private SearchViewModel? _searchViewModel;

        private bool _isDetailPanelVisible = true;
        public bool IsDetailPanelVisible
        {
            get => _isDetailPanelVisible;
            set => SetProperty(ref _isDetailPanelVisible, value);
        }

        private double _detailPanelWidth = 300;
        public double DetailPanelWidth
        {
            get => _detailPanelWidth;
            set => SetProperty(ref _detailPanelWidth, value);
        }

        private ObservableCollection<DisplayColumnConfig> _displayColumns = new ObservableCollection<DisplayColumnConfig>();
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

        public ICommand RefreshCommand { get; }
        public ICommand OpenSearchCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand ToggleDetailPanelCommand { get; }

        public MainViewModel(
            ILogService logService,
            IUserDialogService userDialogService,
            ISearchWindowService searchWindowService,
            ILogSearchService logSearchService,
            IClipboardService clipboardService,
            IConfigPathResolver configPathResolver)
        {
            _logService = logService;
            _userDialogService = userDialogService;
            _searchWindowService = searchWindowService;
            _logSearchService = logSearchService;
            _clipboardService = clipboardService;
            _configPathResolver = configPathResolver;

            LogEntriesView = CollectionViewSource.GetDefaultView(_logEntries);
            LogEntriesView.Filter = FilterLogEntries;

            RefreshCommand = new RelayCommand(_ => LoadLogs(_configPath));
            OpenSearchCommand = new RelayCommand(_ => OpenSearch());
            CopyCommand = new RelayCommand(_ => CopySelectedLogEntry());
            ToggleDetailPanelCommand = new RelayCommand(_ => IsDetailPanelVisible = !IsDetailPanelVisible);
        }

        private void CopySelectedLogEntry()
        {
            if (SelectedLogEntry == null) return;

            var values = new List<string>();
            foreach (var column in DisplayColumns)
            {
                values.Add(GetColumnValue(SelectedLogEntry, column.BindingPath, column.StringFormat));
            }

            var textToCopy = string.Join("\t", values);
            _clipboardService.SetText(textToCopy);
        }

        private string GetColumnValue(LogEntry entry, string bindingPath, string? format)
        {
            if (string.IsNullOrEmpty(bindingPath)) return string.Empty;

            object? rawValue = null;

            if (bindingPath == "Timestamp")
            {
                rawValue = entry.Timestamp;
            }
            else if (bindingPath == "Message")
            {
                rawValue = entry.Message;
            }
            else if (bindingPath == "FileName")
            {
                rawValue = entry.FileName;
            }
            else if (bindingPath == "LineNumber")
            {
                rawValue = entry.LineNumber;
            }
            else if (bindingPath.StartsWith("AdditionalData[") && bindingPath.EndsWith("]"))
            {
                var key = bindingPath.Substring(15, bindingPath.Length - 16);
                if (entry.AdditionalData.TryGetValue(key, out var val))
                {
                    rawValue = val;
                }
            }

            if (rawValue == null) return string.Empty;

            if (!string.IsNullOrEmpty(format))
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, $"{{0:{format}}}", rawValue);
            }

            return rawValue.ToString() ?? string.Empty;
        }

        private void OpenSearch()
        {
            if (_searchViewModel == null)
            {
                _searchViewModel = new SearchViewModel(
                    () => FindLogEntry(true),
                    () => FindLogEntry(false),
                    () => _searchWindowService.Close());

                _searchViewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(SearchViewModel.SearchText) ||
                        e.PropertyName == nameof(SearchViewModel.IsCaseSensitive) ||
                        e.PropertyName == nameof(SearchViewModel.IsRegex))
                    {
                        UpdateSearchStatus();
                    }
                };
            }
            UpdateSearchStatus();
            _searchWindowService.Show(_searchViewModel);
        }

        private void UpdateSearchStatus()
        {
            if (_searchViewModel == null) return;

            var criteria = new SearchCriteria(_searchViewModel.SearchText, _searchViewModel.IsCaseSensitive, _searchViewModel.IsRegex);
            if (string.IsNullOrEmpty(criteria.SearchText))
            {
                _searchViewModel.StatusText = "";
                return;
            }

            var items = LogEntriesView.Cast<LogEntry>();
            var (matchCount, currentIndex) = _logSearchService.GetSearchStatistics(items, SelectedLogEntry, criteria);

            if (matchCount == 0)
            {
                _searchViewModel.StatusText = "No matches";
            }
            else
            {
                string currentStr = currentIndex > 0 ? currentIndex.ToString() : "?";
                _searchViewModel.StatusText = $"{currentStr}/{matchCount}";
            }
        }

        private void FindLogEntry(bool forward)
        {
            if (_searchViewModel == null) return;

            var criteria = new SearchCriteria(_searchViewModel.SearchText, _searchViewModel.IsCaseSensitive, _searchViewModel.IsRegex);
            var items = LogEntriesView.Cast<LogEntry>();

            var foundEntry = _logSearchService.Find(items, SelectedLogEntry, criteria, forward);

            if (foundEntry != null)
            {
                SelectedLogEntry = foundEntry;
            }
        }

        /// <summary>
        /// ViewModel を初期化し、指定された設定ファイルからログを読み込みます。
        /// </summary>
        /// <param name="configPath">設定ファイルのパス。</param>
        public void Initialize(string configPath)
        {
            _configPath = configPath;
            LoadLogs(_configPath);
        }

        private void LoadLogs(string configPath)
        {
            if (string.IsNullOrEmpty(configPath)) return;

            var result = _logService.LoadFromConfig(configPath);

            // ログエントリがない場合にエラーを表示する（以前の仕様を維持）
            // 注意: appConfigがnullの場合のハンドリングをService内で行っているため、
            // ここでは結果が空かどうかで判断します。

            // DisplayColumns を設定
            if (result.DisplayColumns != null && result.DisplayColumns.Any())
            {
                DisplayColumns = new ObservableCollection<DisplayColumnConfig>(result.DisplayColumns);
            }

            // エントリをコレクションに追加
            _logEntries.Clear();
            foreach (var entry in result.Entries)
            {
                _logEntries.Add(entry);
            }

            LogEntriesView.Refresh();
        }

        private bool FilterLogEntries(object obj)
        {
            if (obj is LogEntry entry)
            {
                if (string.IsNullOrWhiteSpace(FilterText))
                {
                    return true;
                }

                var criteria = new SearchCriteria(FilterText, false, false);
                return _logSearchService.IsMatch(entry, criteria);
            }
            return false;
        }
    }
}
