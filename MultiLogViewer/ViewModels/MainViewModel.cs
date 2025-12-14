using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MultiLogViewer.Models;
using MultiLogViewer.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq; // OrderBy, OrderByDescending を使用するため追加
using System.Windows.Data; // for CollectionViewSource
using System.Windows.Controls; // for DataGridAutoGeneratingColumnEventArgs
using System.Windows; // for DataGridTextColumn

namespace MultiLogViewer.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ILogFileReader _logFileReader;
        private readonly IUserDialogService _userDialogService;
        private readonly ILogFormatConfigLoader _logFormatConfigLoader;

        private readonly ObservableCollection<LogEntry> _logEntries = new();
        public ICollectionView LogEntriesView { get; }

        [ObservableProperty]
        private ObservableCollection<DisplayColumnConfig> _displayColumns = new();

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

        public MainViewModel(ILogFileReader logFileReader, IUserDialogService userDialogService, ILogFormatConfigLoader logFormatConfigLoader)
        {
            _logFileReader = logFileReader;
            _userDialogService = userDialogService;
            _logFormatConfigLoader = logFormatConfigLoader;
            LogEntriesView = CollectionViewSource.GetDefaultView(_logEntries);
            LogEntriesView.Filter = FilterLogEntries;
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

        [RelayCommand]
        private void OpenFile()
        {
            var filePath = _userDialogService.OpenFileDialog();
            if (filePath != null)
            {
                // config.yaml から設定を読み込む
                // TODO: ユーザーがどのLogFormatConfigを使用するかを選択するUIが必要。
                //       現時点では、最初のConfigを決め打ちで使用する。
                //       将来的には、設定ファイルから読み込んだConfigを選択できるようにする。
                var appConfig = _logFormatConfigLoader.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.yaml"));
                if (appConfig.LogFormats == null || !appConfig.LogFormats.Any())
                {
                    // 設定ファイルが見つからないか、LogFormatsが定義されていない場合はエラー処理
                    _userDialogService.ShowError("Log format configuration not found or empty in config.yaml.", "Error");
                    return;
                }
                var logFormatConfig = appConfig.LogFormats.First(); // 最初の設定を決め打ちで使用

                _logEntries.Clear();
                foreach (var entry in _logFileReader.Read(filePath, logFormatConfig))
                {
                    _logEntries.Add(entry);
                }

                // 選択されたLogFormatConfigからDisplayColumnsを更新
                DisplayColumns.Clear();
                foreach (var column in logFormatConfig.DisplayColumns)
                {
                    DisplayColumns.Add(column);
                }

                LogEntriesView.Refresh(); // データが変更されたことをViewに通知
            }
        }
    }
}
