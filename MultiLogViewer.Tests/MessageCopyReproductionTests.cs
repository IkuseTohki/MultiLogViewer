using Moq;
using MultiLogViewer.Models;
using MultiLogViewer.Services;
using MultiLogViewer.ViewModels;
using MultiLogViewer.Utils;

namespace MultiLogViewer.Tests
{
    [TestClass]
    public class MessageCopyReproductionTests
    {
        private Mock<IClipboardService> _mockClipboardService = null!;
        private MainViewModel _viewModel = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockClipboardService = new Mock<IClipboardService>();

            // ViewModelの構築に必要な最小限のモック
            var logService = new Mock<ILogService>();
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

            _viewModel = new MainViewModel(
                logService.Object,
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
        public void CopyCommand_ShouldCopyMessageColumn_Exactly()
        {
            /*
             * テスト観点: Message 列が正しくコピーされることを確認する。
             * 特に、仕様書にある通り DisplayColumns に基づいて値が取得されているか。
             */
            // Arrange
            var entry = new LogEntry
            {
                Timestamp = new DateTime(2026, 1, 12, 12, 0, 0),
                Message = "This is a test message.",
                FileName = "test.log"
            };

            // 手動で DisplayColumns を構成 (実際のアプリの動作を模倣)
            _viewModel.DisplayColumns.Clear();
            _viewModel.DisplayColumns.Add(new DisplayColumnConfig { Header = "Time", BindingPath = "Timestamp" });
            _viewModel.DisplayColumns.Add(new DisplayColumnConfig { Header = "Message", BindingPath = "Message" });

            _viewModel.SelectedLogEntry = entry;

            // Act
            _viewModel.CopyCommand.Execute(null);

            // Assert
            // Timestamp(ToString) + 	 + Message
            var expectedText = entry.Timestamp.ToString() + "\t" + entry.Message;
            _mockClipboardService.Verify(c => c.SetText(expectedText), Times.Once);
        }

        [TestMethod]
        public void LogEntryValueConverter_ShouldReturnMessage_ForMessageBindingPath()
        {
            /*
             * テスト観点: Converter 単体で "Message" パスに対して正しく値が返るか。
             */
            var entry = new LogEntry { Message = "Hello World" };

            // 大文字
            Assert.AreEqual("Hello World", LogEntryValueConverter.GetStringValue(entry, "Message", null));
            // 小文字 (先ほどの修正の確認)
            Assert.AreEqual("Hello World", LogEntryValueConverter.GetStringValue(entry, "message", null));
        }
    }
}
