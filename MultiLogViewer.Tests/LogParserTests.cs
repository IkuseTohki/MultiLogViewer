using MultiLogViewer.Tests;
using MultiLogViewer.Models;
using MultiLogViewer.Services;
using System;
using System.Globalization;

namespace MultiLogViewer.Tests
{
    [TestClass]
    public class LogParserTests
    {
        /// <summary>
        /// テスト観点: 有効なLogFormatConfigとログ行を渡した場合、正しくLogEntryオブジェクトにパースされることを確認する。
        ///             正規表現でキャプチャグループ名が指定された項目が、LogEntryの対応するプロパティに、
        ///             またはAdditionalDataに正しく格納されることを確認する。
        /// </summary>
        [TestMethod]
        public void ParseLogEntry_ValidLogAndConfig_ReturnsCorrectLogEntry()
        {
            // Arrange
            var config = new LogFormatConfig
            {
                Name = "ApplicationLog",
                Pattern = @"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \[(?<level>\w+)\] (?<message>.*)$",
                TimestampFormat = "yyyy-MM-dd HH:mm:ss"
            };
            var logLine = "2023-10-26 10:30:45 [INFO] User logged in successfully.";
            var parser = new LogParser(config);

            // Act
            var logEntry = parser.Parse(logLine);

            // Assert
            Assert.IsNotNull(logEntry);
            Assert.AreEqual(DateTime.ParseExact("2023-10-26 10:30:45", "yyyy-MM-dd HH:mm:ss", CultureInfo
.InvariantCulture), logEntry.Timestamp);
            Assert.AreEqual("INFO", logEntry.AdditionalData["level"]);
            Assert.AreEqual("User logged in successfully.", logEntry.Message);
            Assert.AreEqual(1, logEntry.AdditionalData.Count); // levelのみが格納されるため、Countは1
        }

        /// <summary>
        /// テスト観点: 複数のキャプチャグループを持つパターンで、AdditionalDataにデータが正しく格納されることを確認する。
        /// </summary>
        [TestMethod]
        public void ParseLogEntry_WithAdditionalCaptureGroups_StoresInAdditionalData()
        {
            // Arrange
            var config = new LogFormatConfig
            {
                Name = "ComplexLog",
                Pattern = @"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \[(?<level>\w+)\] (?<message>.*) \(User:(?<user>\w+), Session:(?<session>\d+)\)$",
                TimestampFormat = "yyyy-MM-dd HH:mm:ss"
            };
            var logLine = "2023-10-26 11:00:00 [DEBUG] Data processed. (User:alice, Session:12345)";
            var parser = new LogParser(config);

            // Act
            var logEntry = parser.Parse(logLine);

            // Assert

            Assert.IsNotNull(logEntry);

            Assert.AreEqual(DateTime.ParseExact("2023-10-26 11:00:00", "yyyy-MM-dd HH:mm:ss", CultureInfo

.InvariantCulture), logEntry.Timestamp);

            Assert.AreEqual("Data processed.", logEntry.Message);

            Assert.AreEqual(3, logEntry.AdditionalData.Count);

            Assert.IsTrue(logEntry.AdditionalData.ContainsKey("level"));

            Assert.AreEqual("DEBUG", logEntry.AdditionalData["level"]);

            Assert.IsTrue(logEntry.AdditionalData.ContainsKey("user"));

            Assert.AreEqual("alice", logEntry.AdditionalData["user"]);

            Assert.IsTrue(logEntry.AdditionalData.ContainsKey("session"));

            Assert.AreEqual("12345", logEntry.AdditionalData["session"]);

        }

        /// <summary>
        /// テスト観点: パターンに一致しないログ行を渡した場合、nullが返されることを確認する。
        /// </summary>
        [TestMethod]
        public void ParseLogEntry_MismatchPattern_ReturnsNull()
        {
            // Arrange
            var config = new LogFormatConfig
            {
                Name = "ApplicationLog",
                Pattern = @"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \[(?<level>\w+)\] (?<message>.*)$",
                TimestampFormat = "yyyy-MM-dd HH:mm:ss"
            };
            var logLine = "This log line does not match the pattern.";
            var parser = new LogParser(config);

            // Act
            var logEntry = parser.Parse(logLine);

            // Assert
            Assert.IsNull(logEntry);
        }

        /// <summary>
        /// テスト観点: sub_patternsが定義されている場合、source_fieldで指定された項目の値からさらにデータが抽出され、AdditionalDataに追加されることを確認する。
        /// </summary>
        [TestMethod]
        public void ParseLogEntry_WithSubPattern_ExtractsAdditionalData()
        {
            // Arrange
            var config = new LogFormatConfig
            {
                Name = "AppLogWithDetails",
                Pattern = @"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \[(?<level>\w+)\] (?<message>.*)$",
                TimestampFormat = "yyyy-MM-dd HH:mm:ss",
                SubPatterns = new List<SubPatternConfig>
                {
                    new SubPatternConfig
                    {
                        SourceField = "message",
                        Pattern = @"user=(?<user>\w+), duration=(?<duration>\d+), status=(?<status_code>\d+)"
                    }
                }
            };
            var logLine = "2023-10-27 12:00:00 [INFO] Request processed: user=admin, duration=123, status=200";
            var parser = new LogParser(config);

            // Act
            var logEntry = parser.Parse(logLine);

            // Assert
            Assert.IsNotNull(logEntry);
            Assert.AreEqual("Request processed: user=admin, duration=123, status=200", logEntry.Message);
            Assert.AreEqual(4, logEntry.AdditionalData.Count);
            Assert.AreEqual("INFO", logEntry.AdditionalData["level"]);
            Assert.AreEqual("admin", logEntry.AdditionalData["user"]);
            Assert.AreEqual("123", logEntry.AdditionalData["duration"]);
            Assert.AreEqual("200", logEntry.AdditionalData["status_code"]);
        }
    }
}
