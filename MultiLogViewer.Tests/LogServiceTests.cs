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
    }
}
