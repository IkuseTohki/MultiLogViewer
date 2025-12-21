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
        private List<FileState> _fileStates = new List<FileState>();
        private int _pollingIntervalMs = 1000;
        private readonly System.Windows.Threading.DispatcherTimer _tailTimer;

        private readonly ObservableCollection<LogEntry> _logEntries = new ObservableCollection<LogEntry>();
        public ICollectionView LogEntriesView { get; }

        private bool _isTailEnabled;
        public bool IsTailEnabled
        {
            get => _isTailEnabled;
            set
            {
                if (SetProperty(ref _isTailEnabled, value))
                {
                    if (value) _tailTimer.Start();
                    else _tailTimer.Stop();
                }
            }
        }

        private bool _isAtBottom = true;
        public bool IsAtBottom
        {
            get => _isAtBottom;
            set => SetProperty(ref _isAtBottom, value);
        }

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

        private bool _isDetailPanelVisible = false;
        public bool IsDetailPanelVisible
        {
            get => _isDetailPanelVisible;
            set => SetProperty(ref _isDetailPanelVisible, value);
        }

        private double _detailPanelWidth = 200;
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
        public ICommand AddExtensionFilterCommand { get; }
        public ICommand AddDateTimeFilterCommand { get; }
        public ICommand RemoveExtensionFilterCommand { get; }
        public ICommand ToggleTailCommand { get; }

        private ObservableCollection<LogFilter> _activeExtensionFilters = new ObservableCollection<LogFilter>();
        public ObservableCollection<LogFilter> ActiveExtensionFilters => _activeExtensionFilters;

        private ObservableCollection<string> _availableAdditionalDataKeys = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableAdditionalDataKeys => _availableAdditionalDataKeys;

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
            AddExtensionFilterCommand = new RelayCommand(param => AddExtensionFilter(param as string));
            AddDateTimeFilterCommand = new RelayCommand(param => AddDateTimeFilter(param));
            RemoveExtensionFilterCommand = new RelayCommand(param => RemoveExtensionFilter(param as LogFilter));
            ToggleTailCommand = new RelayCommand(_ => IsTailEnabled = !IsTailEnabled);

            _activeExtensionFilters.CollectionChanged += (s, e) => LogEntriesView.Refresh();

            _tailTimer = new System.Windows.Threading.DispatcherTimer();
            _tailTimer.Tick += TailTimer_Tick;
        }

        private void TailTimer_Tick(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_configPath)) return;

            var result = _logService.LoadIncremental(_configPath, _fileStates);
            _fileStates = result.FileStates;

            if (result.Entries.Any())
            {
                // 新しいエントリを追加
                foreach (var entry in result.Entries)
                {
                    _logEntries.Add(entry);
                }

                // 自動スクロール処理（Viewとの連携が必要なため、プロパティで通知するかBehaviorで対応）
                if (IsAtBottom)
                {
                    SelectedLogEntry = _logEntries.LastOrDefault();
                }
            }
        }

        private void AddExtensionFilter(string? key)
        {
            if (string.IsNullOrEmpty(key)) return;
            var newFilter = new LogFilter(FilterType.ColumnEmpty, key, default, key);

            // 既存にあれば削除（重複追加防止・上書き）
            if (_activeExtensionFilters.Contains(newFilter))
            {
                _activeExtensionFilters.Remove(newFilter);
            }
            _activeExtensionFilters.Add(newFilter);
        }

        private void AddDateTimeFilter(object? param)
        {
            // param: (DateTime value, bool isAfter)
            if (param is ValueTuple<DateTime, bool> data)
            {
                var (value, isAfter) = data;
                var type = isAfter ? FilterType.DateTimeAfter : FilterType.DateTimeBefore;
                var suffix = isAfter ? "以降" : "以前";

                // 表示用テキストの生成。Timestamp列の設定があればそれを使う
                var timestampConfig = DisplayColumns.FirstOrDefault(c => c.BindingPath == "Timestamp");
                var format = timestampConfig?.StringFormat ?? "yyyy/MM/dd HH:mm:ss";
                var formattedDate = value.ToString(format);
                var displayText = $"\"{formattedDate}\"{suffix}";

                var newFilter = new LogFilter(type, "", value, displayText);

                // 同一タイプの既存フィルター（以降/以前それぞれ一つずつ）があれば削除して上書き
                var existing = _activeExtensionFilters.FirstOrDefault(f => f.Type == type);
                if (existing != null)
                {
                    _activeExtensionFilters.Remove(existing);
                }
                _activeExtensionFilters.Add(newFilter);
            }
        }

        private void RemoveExtensionFilter(LogFilter? filter)
        {
            if (filter == null) return;
            _activeExtensionFilters.Remove(filter);
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

            try
            {
                var result = _logService.LoadFromConfig(configPath);

                _fileStates = result.FileStates;
                _pollingIntervalMs = result.PollingIntervalMs;
                _tailTimer.Interval = TimeSpan.FromMilliseconds(_pollingIntervalMs);

                // DisplayColumns を設定
                if (result.DisplayColumns != null && result.DisplayColumns.Any())
                {
                    DisplayColumns = new ObservableCollection<DisplayColumnConfig>(result.DisplayColumns);
                }

                // エントリをコレクションに追加
                _logEntries.Clear();
                var allKeys = new HashSet<string>();
                foreach (var entry in result.Entries)
                {
                    _logEntries.Add(entry);
                    if (entry.AdditionalData != null)
                    {
                        foreach (var key in entry.AdditionalData.Keys)
                        {
                            allKeys.Add(key);
                        }
                    }
                }

                // 利用可能なキー一覧を更新
                _availableAdditionalDataKeys.Clear();
                foreach (var key in allKeys.OrderBy(k => k))
                {
                    _availableAdditionalDataKeys.Add(key);
                }

                LogEntriesView.Refresh();
            }
            catch (System.Exception ex)
            {
                // 設定読み込みエラーをユーザーに通知
                _userDialogService.ShowError("設定エラー", ex.Message);
            }
        }

        private bool FilterLogEntries(object obj)
        {
            if (obj is LogEntry entry)
            {
                // 拡張フィルターによる非表示判定
                if (_logSearchService.ShouldHide(entry, _activeExtensionFilters))
                {
                    return false;
                }

                // キーワードフィルタによる判定
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
