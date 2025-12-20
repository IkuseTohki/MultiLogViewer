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
        private string _tempDirectory = "";

        [TestInitialize]
        public void Setup()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "MLV_Tests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        /// <summary>
        /// テスト観点: 有効なログファイルを渡した場合、正しくLogEntryのリストが生成されることを確認する。
        /// </summary>
        [TestMethod]
        public void Read_ValidFile_ReturnsLogEntries()
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
            var testLogFile = Path.Combine(_tempDirectory, "single_test.log");
            File.WriteAllLines(testLogFile, logLines);

            var reader = new LogFileReader();

            // Act
            var logEntries = reader.Read(testLogFile, config).ToList();

            // Assert
            Assert.IsNotNull(logEntries);
            Assert.AreEqual(2, logEntries.Count);

            var firstEntry = logEntries.First();
            Assert.AreEqual(new DateTime(2023, 10, 26, 10, 30, 45), firstEntry.Timestamp);
            Assert.AreEqual("INFO", firstEntry.AdditionalData["level"]);
            Assert.AreEqual("User logged in successfully.", firstEntry.Message);
            Assert.AreEqual(Path.GetFileName(testLogFile), firstEntry.FileName);
            Assert.AreEqual(1, firstEntry.LineNumber);

            var secondEntry = logEntries.Last();
            Assert.AreEqual(new DateTime(2023, 10, 26, 10, 31, 00), secondEntry.Timestamp);
            Assert.AreEqual("WARN", secondEntry.AdditionalData["level"]);
            Assert.AreEqual("Deprecated API called.", secondEntry.Message);
            Assert.AreEqual(Path.GetFileName(testLogFile), secondEntry.FileName);
            Assert.AreEqual(2, secondEntry.LineNumber);
        }

        /// <summary>
        /// テスト観点: 複数のログファイルを指定した場合、ReadFilesメソッドがそれらすべてを読み込み、
        /// 正しくLogEntryのリストを結合して返すことを確認する。
        /// </summary>
        [TestMethod]
        public void ReadFiles_MultipleFiles_ReturnsCombinedLogEntries()
        {
            // Arrange
            var config = new LogFormatConfig
            {
                Name = "ApplicationLog",
                Pattern = @"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \[(?<level>\w+)\] (?<message>.*)$",
                TimestampFormat = "yyyy-MM-dd HH:mm:ss"
            };

            var logLines1 = new[]
            {
                "2023-10-26 10:00:00 [INFO] Log from file1 line1",
                "2023-10-26 10:00:01 [WARN] Log from file1 line2"
            };
            string filePath1 = Path.Combine(_tempDirectory, "file1.log");
            File.WriteAllLines(filePath1, logLines1);

            var logLines2 = new[]
            {
                "2023-10-26 10:00:02 [DEBUG] Log from file2 line1",
                "2023-10-26 10:00:03 [ERROR] Log from file2 line2"
            };
            string filePath2 = Path.Combine(_tempDirectory, "file2.log");
            File.WriteAllLines(filePath2, logLines2);

            var filePaths = new List<string> { filePath1, filePath2 };

            var reader = new LogFileReader();

            // Act
            var logEntries = reader.ReadFiles(filePaths, config).ToList();

            // Assert
            Assert.IsNotNull(logEntries);
            Assert.AreEqual(4, logEntries.Count); // 2ファイル x 2行 = 4エントリ

            // 含まれる内容の一部を確認
            Assert.IsTrue(logEntries.Any(e => e.Message == "Log from file1 line1"));
            Assert.IsTrue(logEntries.Any(e => e.Message == "Log from file2 line2"));
        }

        /// <summary>
        /// テスト観点: Shift-JISでエンコードされたログファイルが正しく読み込まれることを確認する。
        /// </summary>
        [TestMethod]
        public void Read_ShiftJisFile_ReturnsCorrectContent()
        {
            // Arrange
            var config = new LogFormatConfig
            {
                Name = "TestLog",
                Pattern = @"^(?<message>.*)$", // メッセージ全体を読み込むシンプルなパターン
                TimestampFormat = "" // タイムスタンプは不要
            };
            var testLogFile = Path.Combine("TestLogs", "shift_jis_log.log");

            var reader = new LogFileReader();

            // Act
            var logEntries = reader.Read(testLogFile, config).ToList();

            // Assert
            Assert.IsNotNull(logEntries);
            Assert.AreEqual(1, logEntries.Count);
            Assert.AreEqual("テストログファイル(Shift-JIS)", logEntries.First().Message);
        }

        /// <summary>
        /// テスト観点: UTF-8でエンコードされたログファイルが正しく読み込まれることを確認する。
        /// </summary>
        [TestMethod]
        public void Read_Utf8File_ReturnsCorrectContent()
        {
            // Arrange
            var config = new LogFormatConfig
            {
                Name = "TestLog",
                Pattern = @"^(?<message>.*)$", // メッセージ全体を読み込むシンプルなパターン
                TimestampFormat = "" // タイムスタンプは不要
            };
            var testLogFile = Path.Combine("TestLogs", "utf8_log.log");

            var reader = new LogFileReader();

            // Act
            var logEntries = reader.Read(testLogFile, config).ToList();

            // Assert
            Assert.IsNotNull(logEntries);
            Assert.AreEqual(1, logEntries.Count);
            Assert.AreEqual("テストログファイル(UTF-8)", logEntries.First().Message);
        }
    }
}
