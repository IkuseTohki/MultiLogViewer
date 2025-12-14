using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MultiLogViewer.Models;
using MultiLogViewer.Services;
using MultiLogViewer.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections.ObjectModel;

namespace MultiLogViewer.Tests
{
    [TestClass]
    public class MainViewModelTests
    {
        private Mock<ILogFileReader> _mockLogFileReader;
        private Mock<IUserDialogService> _mockUserDialogService;
        private Mock<ILogFormatConfigLoader> _mockLogFormatConfigLoader;
        private MainViewModel _viewModel;

        [TestInitialize]
        public void Setup()
        {
            _mockLogFileReader = new Mock<ILogFileReader>();
            _mockUserDialogService = new Mock<IUserDialogService>();
            _mockLogFormatConfigLoader = new Mock<ILogFormatConfigLoader>();
            _viewModel = new MainViewModel(_mockLogFileReader.Object, _mockUserDialogService.Object, _mockLogFormatConfigLoader.Object);
        }

        /// <summary>
        /// テスト観点: OpenFileコマンドが、ファイル選択ダイアログを表示し、ログファイルを読み込み、DisplayColumnsを更新することを確認する。
        /// </summary>
        [TestMethod]
        public void OpenFileCommand_LoadsLogFileAndUpdatesDisplayColumns()
        {
            // Arrange
            var testFilePath = "test_log.log";
            var logFormatConfig = new LogFormatConfig
            {
                Name = "TestLogFormat",
                Pattern = ".*",
                TimestampFormat = "yyyy-MM-dd",
                DisplayColumns = new List<DisplayColumnConfig>
                {
                    new DisplayColumnConfig { Header = "Col1", BindingPath = "Prop1" },
                    new DisplayColumnConfig { Header = "Col2", BindingPath = "Prop2" }
                }
            };
            var appConfig = new AppConfig { LogFormats = new List<LogFormatConfig> { logFormatConfig } };
            var logEntries = new List<LogEntry>
            {
                new LogEntry { Message = "Entry 1" },
                new LogEntry { Message = "Entry 2" }
            };

            _mockUserDialogService.Setup(s => s.OpenFileDialog()).Returns(testFilePath);
            _mockLogFormatConfigLoader.Setup(l => l.Load(It.IsAny<string>())).Returns(appConfig);
            _mockLogFileReader.Setup(r => r.Read(testFilePath, logFormatConfig)).Returns(logEntries);

            // Act
            _viewModel.OpenFileCommand.Execute(null);

            // Assert
            _mockUserDialogService.Verify(s => s.OpenFileDialog(), Times.Once);
            _mockLogFormatConfigLoader.Verify(l => l.Load(It.IsAny<string>()), Times.Once);
            _mockLogFileReader.Verify(r => r.Read(testFilePath, logFormatConfig), Times.Once);
            Assert.AreEqual(2, _viewModel.LogEntriesView.Cast<LogEntry>().Count());
            Assert.AreEqual("Entry 1", _viewModel.LogEntriesView.Cast<LogEntry>().First().Message);
            Assert.HasCount(2, _viewModel.DisplayColumns);
            Assert.AreEqual("Col1", _viewModel.DisplayColumns.First().Header);
        }

        /// <summary>
        /// テスト観点: config.yamlが見つからない場合に、LogErrorが表示されることを確認する。
        /// </summary>
        [TestMethod]
        public void OpenFileCommand_ConfigNotFound_ShowsErrorMessage()
        {
            // Arrange
            _mockUserDialogService.Setup(s => s.OpenFileDialog()).Returns("some_path.log");
            _mockLogFormatConfigLoader.Setup(l => l.Load(It.IsAny<string>())).Returns(new AppConfig { LogFormats = null });

            // Act
            _viewModel.OpenFileCommand.Execute(null);

            // Assert
            // MessageBox.Show の呼び出しを直接テストするのは難しいが、
            // Viewの変更をトリガーするようなViewModelのプロパティ変更をアサートするなど、
            // 間接的にエラーハンドリングが動作したことを確認する。
            // 今回は、LogEntriesが空のままであることを確認する。
            Assert.AreEqual(0, _viewModel.LogEntriesView.Cast<LogEntry>().Count());
        }
    }
}
