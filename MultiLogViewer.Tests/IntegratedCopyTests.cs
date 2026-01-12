using Moq;
using MultiLogViewer.Models;
using MultiLogViewer.Services;
using MultiLogViewer.ViewModels;

namespace MultiLogViewer.Tests
{
    [TestClass]
    public class IntegratedCopyTests
    {
        private Mock<IClipboardService> _mockClipboardService = null!;
        private MainViewModel _viewModel = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockClipboardService = new Mock<IClipboardService>();

            var dialogService = new Mock<IUserDialogService>();
            var searchService = new Mock<ISearchWindowService>();
            var logSearchService = new LogSearchService();
            var configResolver = new Mock<IConfigPathResolver>();
            var presetService = new Mock<IFilterPresetService>();
            var dispatcher = new Mock<IDispatcherService>();
            var taskRunner = new Mock<ITaskRunner>();
            var gotoDateService = new Mock<IGoToDateDialogService>();
            var tailWarningService = new Mock<ITailModeWarningDialogService>();
            var appSettingsService = new Mock<IAppSettingsService>();

            dispatcher.Setup(d => d.Invoke(It.IsAny<Action>())).Callback<Action>(a => a());
            taskRunner.Setup(r => r.Run(It.IsAny<Action>())).Returns((Action a) => { a(); return Task.CompletedTask; });
            appSettingsService.Setup(s => s.Load()).Returns(new AppSettings());

            // 重要：ここで Message 列を含む列定義を返す
            var mockLogService = new Mock<ILogService>();

            // 重要：ここで Message 列を含む列定義を返す
            var columns = new List<DisplayColumnConfig>
            {
                new DisplayColumnConfig { Header = "Time", BindingPath = "Timestamp" },
                new DisplayColumnConfig { Header = "Level", BindingPath = "AdditionalData[Level]" },
                new DisplayColumnConfig { Header = "Msg", BindingPath = "Message" }
            };
            var entries = new List<LogEntry>
            {
                new LogEntry { Timestamp = DateTime.Now, Message = "INTEGRATED TEST MESSAGE", AdditionalData = new Dictionary<string, string> { { "Level", "INFO" } } }
            };
            mockLogService.Setup(s => s.LoadFromConfig(It.IsAny<string>())).Returns(new LogDataResult(entries, columns, new List<FileState>()));

            _viewModel = new MainViewModel(
                mockLogService.Object,
                dialogService.Object,
                searchService.Object,
                logSearchService,
                _mockClipboardService.Object,
                configResolver.Object,
                presetService.Object,
                dispatcher.Object,
                taskRunner.Object,
                gotoDateService.Object,
                tailWarningService.Object,
                appSettingsService.Object);
        }

        [TestMethod]
        public async Task InitializedViewModel_ShouldCopyMessageCorrectly()
        {
            // Arrange
            await _viewModel.Initialize("dummy_config.yaml");
            _viewModel.SelectedLogEntry = _viewModel.LogEntriesView.Cast<LogEntry>().First();

            // Act
            _viewModel.CopyCommand.Execute(null);

            // Assert
            // DisplayColumns は [Bookmark, Time, Level, Msg] の順になるはず
            // なので値は "\tTimeValue\tINFO\tINTEGRATED TEST MESSAGE"
            var entry = _viewModel.SelectedLogEntry;
            var expectedText = "\t" + entry.Timestamp.ToString() + "\tINFO\tINTEGRATED TEST MESSAGE";

            _mockClipboardService.Verify(c => c.SetText(expectedText), Times.Once);
        }
    }
}
