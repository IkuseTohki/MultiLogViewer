using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MultiLogViewer.Models;
using MultiLogViewer.Services;
using System.Collections.Generic;
using System.Linq;

namespace MultiLogViewer.Tests
{
    [TestClass]
    public class LogServiceTests
    {
        private Mock<ILogFormatConfigLoader> _mockConfigLoader = null!;
        private Mock<IFileResolver> _mockFileResolver = null!;
        private Mock<ILogFileReader> _mockLogFileReader = null!;
        private LogService _logService = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockConfigLoader = new Mock<ILogFormatConfigLoader>();
            _mockFileResolver = new Mock<IFileResolver>();
            _mockLogFileReader = new Mock<ILogFileReader>();
            _logService = new LogService(_mockConfigLoader.Object, _mockFileResolver.Object, _mockLogFileReader.Object);
        }

        [TestMethod]
        public void LoadFromConfig_Success_ReturnsSortedEntriesAndColumns()
        {
            // Arrange
            var configPath = "config.yaml";
            var appConfig = new AppConfig
            {
                DisplayColumns = new List<DisplayColumnConfig> { new DisplayColumnConfig { Header = "Col1" } },
                LogFormats = new List<LogFormatConfig>
                {
                    new LogFormatConfig { Name = "F1", LogFilePatterns = new List<string> { "*.log" } }
                }
            };

            var logs = new List<LogEntry>
            {
                new LogEntry { Timestamp = new System.DateTime(2023, 1, 2), Message = "Later" },
                new LogEntry { Timestamp = new System.DateTime(2023, 1, 1), Message = "Earlier" }
            };

            _mockConfigLoader.Setup(l => l.Load(configPath)).Returns(appConfig);
            _mockFileResolver.Setup(r => r.Resolve(It.IsAny<List<string>>())).Returns(new List<string> { "file.log" });

            // LogService changed to use ReadIncremental with IEnumerable<LogFormatConfig>
            _mockLogFileReader.Setup(r => r.ReadIncremental(It.IsAny<FileState>(), It.IsAny<IEnumerable<LogFormatConfig>>()))
                .Returns((logs, new FileState("file.log", 100, 2)));

            // Act
            var result = _logService.LoadFromConfig(configPath);

            // Assert
            Assert.AreEqual(2, result.Entries.Count);
            Assert.AreEqual("Earlier", result.Entries[0].Message, "Entries should be sorted by timestamp.");
            Assert.AreEqual(1, result.DisplayColumns.Count);
            Assert.AreEqual("Col1", result.DisplayColumns[0].Header);
        }

        [TestMethod]
        public void LoadFromConfig_SameTimestamp_PreservesOriginalOrder()
        {
            // テスト観点: 同一時刻のログが複数ある場合に、ファイル内での出現順（シーケンス番号）が維持されることを確認する。
            var configPath = "config.yaml";
            var appConfig = new AppConfig
            {
                LogFormats = new List<LogFormatConfig>
                {
                    new LogFormatConfig { Name = "F1", LogFilePatterns = new List<string> { "*.log" } }
                }
            };

            // 全て同じ時刻のログ。メッセージに出現順を込める。
            var sameTime = new System.DateTime(2023, 1, 1, 10, 0, 0);
            var logs = new List<LogEntry>
            {
                new LogEntry { Timestamp = sameTime, Message = "First" },
                new LogEntry { Timestamp = sameTime, Message = "Second" },
                new LogEntry { Timestamp = sameTime, Message = "Third" }
            };

            _mockConfigLoader.Setup(l => l.Load(configPath)).Returns(appConfig);
            _mockFileResolver.Setup(r => r.Resolve(It.IsAny<List<string>>())).Returns(new List<string> { "file.log" });
            _mockLogFileReader.Setup(r => r.ReadIncremental(It.IsAny<FileState>(), It.IsAny<IEnumerable<LogFormatConfig>>()))
                .Returns((logs, new FileState("file.log", 300, 3)));

            // Act
            var result = _logService.LoadFromConfig(configPath);

            // Assert
            Assert.AreEqual(3, result.Entries.Count);
            // ソートされても順序が変わっていないことを確認（ThenBy(SequenceNumber) の効果）
            Assert.AreEqual("First", result.Entries[0].Message);
            Assert.AreEqual("Second", result.Entries[1].Message);
            Assert.AreEqual("Third", result.Entries[2].Message);

            // シーケンス番号が正しく振られていることも確認
            Assert.AreEqual(0, result.Entries[0].SequenceNumber);
            Assert.AreEqual(1, result.Entries[1].SequenceNumber);
            Assert.AreEqual(2, result.Entries[2].SequenceNumber);
        }

        [TestMethod]
        public void LoadFromConfig_MultipleFiles_SameTimestamp_PreservesFileOrder()
        {
            // テスト観点: 異なるファイルに同一時刻のログがある場合、ファイルが読み込まれた順序が維持されることを確認する。
            var configPath = "config.yaml";
            var sameTime = new System.DateTime(2023, 1, 1, 10, 0, 0);

            var appConfig = new AppConfig
            {
                LogFormats = new List<LogFormatConfig>
                {
                    new LogFormatConfig { Name = "F1", LogFilePatterns = new List<string> { "fileA.log", "fileB.log" } }
                }
            };

            _mockConfigLoader.Setup(l => l.Load(configPath)).Returns(appConfig);
            // Resolverが2つのファイルを返す
            _mockFileResolver.Setup(r => r.Resolve(It.IsAny<List<string>>())).Returns(new List<string> { "fileA.log", "fileB.log" });

            // fileA.log の内容
            var logsA = new List<LogEntry> { new LogEntry { Timestamp = sameTime, Message = "A1" }, new LogEntry { Timestamp = sameTime, Message = "A2" } };
            _mockLogFileReader.Setup(r => r.ReadIncremental(It.Is<FileState>(s => s.FilePath == "fileA.log"), It.IsAny<IEnumerable<LogFormatConfig>>()))
                .Returns((logsA, new FileState("fileA.log", 100, 2)));

            // fileB.log の内容
            var logsB = new List<LogEntry> { new LogEntry { Timestamp = sameTime, Message = "B1" }, new LogEntry { Timestamp = sameTime, Message = "B2" } };
            _mockLogFileReader.Setup(r => r.ReadIncremental(It.Is<FileState>(s => s.FilePath == "fileB.log"), It.IsAny<IEnumerable<LogFormatConfig>>()))
                .Returns((logsB, new FileState("fileB.log", 100, 2)));

            // Act
            var result = _logService.LoadFromConfig(configPath);

            // Assert
            Assert.AreEqual(4, result.Entries.Count);
            Assert.AreEqual("A1", result.Entries[0].Message);
            Assert.AreEqual("A2", result.Entries[1].Message);
            Assert.AreEqual("B1", result.Entries[2].Message);
            Assert.AreEqual("B2", result.Entries[3].Message);

            // 全体にわたって連番が振られていること
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(i, result.Entries[i].SequenceNumber);
            }
        }

        [TestMethod]
        public void LoadIncremental_ContinuesSequenceNumber()
        {
            // テスト観点: インクリメンタル読み込み時に、前回の終わりの番号から連番が継続されることを確認する。
            var configPath = "config.yaml";
            var sameTime = new System.DateTime(2023, 1, 1, 10, 0, 0);
            var appConfig = new AppConfig
            {
                LogFormats = new List<LogFormatConfig>
                {
                    new LogFormatConfig { Name = "F1", LogFilePatterns = new List<string> { "file.log" } }
                }
            };

            _mockConfigLoader.Setup(l => l.Load(configPath)).Returns(appConfig);
            _mockFileResolver.Setup(r => r.Resolve(It.IsAny<List<string>>())).Returns(new List<string> { "file.log" });

            // 10行読み込み済みの状態から、5行新しく読み込む想定
            var nextLogs = new List<LogEntry>
            {
                new LogEntry { Timestamp = sameTime, Message = "New 1" },
                new LogEntry { Timestamp = sameTime, Message = "New 2" }
            };

            _mockLogFileReader.Setup(r => r.ReadIncremental(It.IsAny<FileState>(), It.IsAny<IEnumerable<LogFormatConfig>>()))
                .Returns((nextLogs, new FileState("file.log", 200, 12)));

            // Act: 10 から開始するように指定
            var result = _logService.LoadIncremental(configPath, new List<FileState>(), 10);

            // Assert
            Assert.AreEqual(2, result.Entries.Count);
            Assert.AreEqual(10, result.Entries[0].SequenceNumber, "Should start from the given sequence number.");
            Assert.AreEqual(11, result.Entries[1].SequenceNumber, "Should increment correctly.");
        }
    }
}
