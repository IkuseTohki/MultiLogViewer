using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiLogViewer.Models;
using MultiLogViewer.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MultiLogViewer.Tests
{
    [TestClass]
    public class LogFileReaderTests
    {
        private const string TestLogFileName = "test.log";

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(TestLogFileName))
            {
                File.Delete(TestLogFileName);
            }
        }

        /// <summary>
        /// テスト観点: 有効なログファイルを渡した場合、正しくLogEntryのリストが生成されることを確認する。
        /// </summary>
        [TestMethod]
        public void ReadLogFile_ValidFile_ReturnsLogEntries()
        {
            // Arrange
            var config = new LogFormatConfig
            {
                Name = "ApplicationLog",
                Pattern = @"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \[(?<level>\w+)\] (?<message>.*)$",
                TimestampFormat = "yyyy-MM-dd HH:mm:ss"
            };
            var logLines = new[]
            {
                "2023-10-26 10:30:45 [INFO] User logged in successfully.",
                "2023-10-26 10:31:00 [WARN] Deprecated API called.",
                "Invalid line should be skipped."
            };
            File.WriteAllLines(TestLogFileName, logLines);

            // Act
            // TODO: LogFileReaderクラスはまだ存在しないため、コンパイルエラーになる。
            //       後で LogFileReader クラスを作成する。
            var reader = new LogFileReader();
            var logEntries = reader.Read(TestLogFileName, config);

            // Assert
            Assert.IsNotNull(logEntries);
            Assert.AreEqual(2, logEntries.Count());

            var firstEntry = logEntries.First();
            Assert.AreEqual(new DateTime(2023, 10, 26, 10, 30, 45), firstEntry.Timestamp);
            Assert.AreEqual("INFO", firstEntry.AdditionalData["level"]);
            Assert.AreEqual("User logged in successfully.", firstEntry.Message);

            var secondEntry = logEntries.Last();
            Assert.AreEqual(new DateTime(2023, 10, 26, 10, 31, 00), secondEntry.Timestamp);
            Assert.AreEqual("WARN", secondEntry.AdditionalData["level"]);
            Assert.AreEqual("Deprecated API called.", secondEntry.Message);
        }
    }
}
