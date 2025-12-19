using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MultiLogViewer.Models;
using MultiLogViewer.Services;
using MultiLogViewer.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MultiLogViewer.Tests
{
    [TestClass]
    public class MainViewModelTests
    {
        private Mock<ILogFileReader> _mockLogFileReader = null!;
        private Mock<IUserDialogService> _mockUserDialogService = null!;
        private Mock<ILogFormatConfigLoader> _mockLogFormatConfigLoader = null!;
        private Mock<IFileResolver> _mockFileResolver = null!;
        private Mock<IConfigPathResolver> _mockConfigPathResolver = null!; // 追加
        private MainViewModel _viewModel = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockLogFileReader = new Mock<ILogFileReader>();
            _mockUserDialogService = new Mock<IUserDialogService>();
            _mockLogFormatConfigLoader = new Mock<ILogFormatConfigLoader>();
            _mockFileResolver = new Mock<IFileResolver>();
            _mockConfigPathResolver = new Mock<IConfigPathResolver>(); // 初期化
        }

        [TestMethod]
        public void Initialize_Successful_LoadsLogsAndColumns()
        {
            // Arrange
            var displayColumns = new List<DisplayColumnConfig>
            {
                new DisplayColumnConfig { Header = "Timestamp", BindingPath = "Timestamp" }
            };

            var logFormats = new List<LogFormatConfig>
            {
                new LogFormatConfig
                {
                    Name = "TestFormat",
                    LogFilePatterns = new List<string> { "test-*.log" }
                }
            };

            var appConfig = new AppConfig
            {
                DisplayColumns = displayColumns,
                LogFormats = logFormats
            };

            var filePaths = new List<string> { "C:\\dummy\\test-1.log" };
            var logEntries = new List<LogEntry>
            {
                new LogEntry { Message = "Entry 1" }
            };

            _mockLogFormatConfigLoader.Setup(l => l.Load(It.IsAny<string>())).Returns(appConfig);
            _mockFileResolver.Setup(r => r.Resolve(logFormats[0].LogFilePatterns)).Returns(filePaths);
            _mockLogFileReader.Setup(r => r.ReadFiles(filePaths, logFormats[0])).Returns(logEntries);

            // Act
            _viewModel = new MainViewModel(
                _mockLogFileReader.Object,
                _mockUserDialogService.Object,
                _mockLogFormatConfigLoader.Object,
                _mockFileResolver.Object,
                _mockConfigPathResolver.Object);
            _viewModel.Initialize("dummy_path");

            // Assert
            Assert.AreEqual(1, _viewModel.LogEntriesView.Cast<LogEntry>().Count());
            Assert.AreEqual("Entry 1", _viewModel.LogEntriesView.Cast<LogEntry>().First().Message);

            Assert.AreEqual(1, _viewModel.DisplayColumns.Count);
            Assert.AreEqual("Timestamp", _viewModel.DisplayColumns[0].Header);

            _mockLogFormatConfigLoader.Verify(l => l.Load(It.IsAny<string>()), Times.Once);
            _mockFileResolver.Verify(r => r.Resolve(logFormats[0].LogFilePatterns), Times.Once);
            _mockLogFileReader.Verify(r => r.ReadFiles(filePaths, logFormats[0]), Times.Once);
        }

        [TestMethod]
        public void Initialize_ConfigLoadFails_ShowsError()
        {
            // Arrange
            _mockLogFormatConfigLoader.Setup(l => l.Load(It.IsAny<string>())).Returns((AppConfig?)null);

            // Act
            _viewModel = new MainViewModel(
                _mockLogFileReader.Object,
                _mockUserDialogService.Object,
                _mockLogFormatConfigLoader.Object,
                _mockFileResolver.Object,
                _mockConfigPathResolver.Object);
            _viewModel.Initialize("dummy_path");

            // Assert
            _mockUserDialogService.Verify(s => s.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            Assert.AreEqual(0, _viewModel.LogEntriesView.Cast<LogEntry>().Count());
        }

        [TestMethod]
        public void FilterText_WhenSet_RaisesPropertyChanged()
        {
            // Arrange
            var appConfig = new AppConfig();
            _mockLogFormatConfigLoader.Setup(l => l.Load(It.IsAny<string>())).Returns(appConfig);

            _viewModel = new MainViewModel(
                _mockLogFileReader.Object,
                _mockUserDialogService.Object,
                _mockLogFormatConfigLoader.Object,
                _mockFileResolver.Object,
                _mockConfigPathResolver.Object);

            var propertyChangedFired = false;
            _viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.FilterText))
                {
                    propertyChangedFired = true;
                }
            };

            // Act
            _viewModel.FilterText = "new filter";

            // Assert
            Assert.IsTrue(propertyChangedFired, "PropertyChanged event for FilterText was not raised.");
        }

        [TestMethod]
        public void DisplayColumns_WhenSet_RaisesPropertyChanged()
        {
            // Arrange
            var appConfig = new AppConfig();
            _mockLogFormatConfigLoader.Setup(l => l.Load(It.IsAny<string>())).Returns(appConfig);

            _viewModel = new MainViewModel(
                _mockLogFileReader.Object,
                _mockUserDialogService.Object,
                _mockLogFormatConfigLoader.Object,
                _mockFileResolver.Object,
                _mockConfigPathResolver.Object);

            var propertyChangedFired = false;
            _viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.DisplayColumns))
                {
                    propertyChangedFired = true;
                }
            };

            // Act
            _viewModel.DisplayColumns = new ObservableCollection<DisplayColumnConfig>();

            // Assert
            Assert.IsTrue(propertyChangedFired, "PropertyChanged event for DisplayColumns was not raised.");
        }

        [TestMethod]
        public void RefreshCommand_WhenSubPatternsChanged_ReloadsLogWithNewPattern()
        {
            // テスト観点: Refreshコマンドが実行された際に、変更されたsub_patternsが
            //            正しくログの再パースに適用されることを確認する。

            // Arrange
            var tempLogFileName = Path.GetTempFileName();
            var logLine = "[INFO] Initial message";
            File.WriteAllText(tempLogFileName, logLine);

            try
            {
                // 1. 初期設定 (config1): sub_pattern 'level' が 'INFO' を抽出する
                var config1 = new AppConfig
                {
                    LogFormats = new List<LogFormatConfig>
                    {
                        new LogFormatConfig
                        {
                            Name = "TestFormat",
                            LogFilePatterns = new List<string> { tempLogFileName },
                            Pattern = @"^(?<message>.*)$", // ログ行全体をmessageとしてキャプチャ
                            SubPatterns = new List<SubPatternConfig>
                            {
                                new SubPatternConfig { SourceField = "message", Pattern = @"\[(?<level>\w+)\]" }
                            }
                        }
                    }
                };

                // LogFormatConfigLoaderのモック: 最初にconfig1を返す
                _mockLogFormatConfigLoader.Setup(l => l.Load(It.IsAny<string>())).Returns(config1);

                _mockFileResolver.Setup(r => r.Resolve(It.IsAny<List<string>>())).Returns(new List<string> { tempLogFileName });

                var realLogFileReader = new LogFileReader();

                // ViewModelを生成
                _viewModel = new MainViewModel(
                    realLogFileReader, // 実インスタンスを使用
                    _mockUserDialogService.Object,
                    _mockLogFormatConfigLoader.Object,
                    _mockFileResolver.Object,
                    _mockConfigPathResolver.Object);

                // Act (1): 初期読み込み
                _viewModel.Initialize("dummy_path");

                // Assert (1): 初期状態で正しくパースされていることを確認
                var entry1 = _viewModel.LogEntriesView.Cast<LogEntry>().FirstOrDefault();
                Assert.IsNotNull(entry1, "Entry should not be null after initial load.");
                Assert.AreEqual(logLine, entry1.Message.Trim(), "Message should be the full log line initially.");
                Assert.IsTrue(entry1.AdditionalData.ContainsKey("level"), "The 'level' field should be extracted initially.");
                Assert.AreEqual("INFO", entry1.AdditionalData["level"], "The 'level' should be 'INFO' initially.");

                // Arrange (2)

                // 2. 新しい設定 (config2): sub_pattern 'level' が 'DEBUG' のみを抽出する
                var config2 = new AppConfig
                {
                    LogFormats = new List<LogFormatConfig>
                    {
                        new LogFormatConfig
                        {
                            Name = "TestFormat",
                            LogFilePatterns = new List<string> { tempLogFileName },
                            Pattern = @"^(?<message>.*)$", // メインパターンは同じ
                            SubPatterns = new List<SubPatternConfig>
                            {
                                new SubPatternConfig { SourceField = "message", Pattern = @"\[(?<level>DEBUG)\]" }
                            }
                        }
                    }
                };

                // LogFormatConfigLoaderのモックを更新: 次にLoadが呼ばれたらconfig2を返す
                _mockLogFormatConfigLoader.Setup(l => l.Load(It.IsAny<string>())).Returns(config2);

                // Act (2): 更新を実行
                _viewModel.RefreshCommand.Execute(null);

                // Assert (2): 更新後、新しいパターンでパースされていることを確認
                var entry2 = _viewModel.LogEntriesView.Cast<LogEntry>().FirstOrDefault();
                Assert.IsNotNull(entry2, "Entry should not be null after refresh.");
                Assert.AreEqual(logLine, entry2.Message.Trim(), "Message should be the full log line after refresh.");

                // 'DEBUG'パターンにはマッチしないため、'level'フィールドは抽出されないはず
                Assert.IsFalse(entry2.AdditionalData.ContainsKey("level"), "The 'level' field should not be extracted after refresh.");
            }
            finally
            {
                if (File.Exists(tempLogFileName))
                {
                    File.Delete(tempLogFileName);
                }
            }
        }
        [TestMethod]
        public void RefreshCommand_WhenSubPatternSourceFieldCountChanges_AppliesNewSubPatterns()
        {
            // テスト観点: 依存関係を持つsub_patternが追加された場合に、Refreshコマンドの実行によって
            //            全てのサブパターンが正しい順序で適用されるかを確認する。

            // Arrange
            var tempLogFileName = Path.GetTempFileName();
            var logLine = "[MyCoolApp.exe] - Initial message";
            File.WriteAllText(tempLogFileName, logLine);

            try
            {
                // 1. 初期設定 (config1): サブパターンは1つ
                var config1 = new AppConfig
                {
                    LogFormats = new List<LogFormatConfig>
                    {
                        new LogFormatConfig
                        {
                            Name = "TestFormat",
                            LogFilePatterns = new List<string> { tempLogFileName },
                            Pattern = @"^(?<proc_info>\[.*?\]) - (?<message>.*)$",
                            SubPatterns = new List<SubPatternConfig>
                            {
                                new SubPatternConfig { SourceField = "proc_info", Pattern = @"\[(?<process_name>\w+\.exe)\]" }
                            }
                        }
                    }
                };

                _mockLogFormatConfigLoader.Setup(l => l.Load(It.IsAny<string>())).Returns(config1);
                _mockFileResolver.Setup(r => r.Resolve(It.IsAny<List<string>>())).Returns(new List<string> { tempLogFileName });
                var realLogFileReader = new LogFileReader();

                _viewModel = new MainViewModel(realLogFileReader, _mockUserDialogService.Object, _mockLogFormatConfigLoader.Object, _mockFileResolver.Object, _mockConfigPathResolver.Object);

                // Act (1): 初期読み込み
                _viewModel.Initialize("dummy_path");

                // Assert (1): 初期状態で正しくパースされていることを確認
                var entry1 = _viewModel.LogEntriesView.Cast<LogEntry>().FirstOrDefault();
                Assert.IsNotNull(entry1, "Entry should not be null after initial load.");
                Assert.IsTrue(entry1.AdditionalData.ContainsKey("process_name"), "The 'process_name' field should be extracted initially.");
                Assert.AreEqual("MyCoolApp.exe", entry1.AdditionalData["process_name"]);

                // Arrange (2)
                // 2. 新しい設定 (config2): サブパターンを2つに増やす。2つ目は1つ目の結果に依存する。
                var config2 = new AppConfig
                {
                    LogFormats = new List<LogFormatConfig>
                    {
                        new LogFormatConfig
                        {
                            Name = "TestFormat",
                            LogFilePatterns = new List<string> { tempLogFileName },
                            Pattern = @"^(?<proc_info>\[.*?\]) - (?<message>.*)$",
                            SubPatterns = new List<SubPatternConfig>
                            {
                                new SubPatternConfig { SourceField = "proc_info", Pattern = @"\[(?<process_name>\w+\.exe)\]" },
                                new SubPatternConfig { SourceField = "process_name", Pattern = @"(?<app_name>\w+)\.exe" } // process_nameをソースにする
                            }
                        }
                    }
                };

                _mockLogFormatConfigLoader.Setup(l => l.Load(It.IsAny<string>())).Returns(config2);

                // Act (2): 更新を実行
                _viewModel.RefreshCommand.Execute(null);

                // Assert (2): 更新後、新しいサブパターンも適用されていることを確認
                var entry2 = _viewModel.LogEntriesView.Cast<LogEntry>().FirstOrDefault();
                Assert.IsNotNull(entry2, "Entry should not be null after refresh.");
                Assert.IsTrue(entry2.AdditionalData.ContainsKey("process_name"), "The 'process_name' field should still exist after refresh.");
                Assert.IsTrue(entry2.AdditionalData.ContainsKey("app_name"), "The 'app_name' field should be extracted after refresh.");
                Assert.AreEqual("MyCoolApp", entry2.AdditionalData["app_name"]);
            }
            finally
            {
                if (File.Exists(tempLogFileName))
                {
                    File.Delete(tempLogFileName);
                }
            }
        }
    }
}
