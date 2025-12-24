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
        private LogFileReader _reader = null!;
        private string _testLogsDir = "TestLogs";

        [TestInitialize]
        public void Setup()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "MLV_Tests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDirectory);
            _reader = new LogFileReader();
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
                TimestampFormat = "yyyy-MM-dd HH:mm:ss",
                IsMultiline = false // Disable multiline to skip invalid lines
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

        [TestMethod]
        public void Read_MultilineLog_CombinedCorrectly()
        {
            // Arrange
            var logPath = Path.Combine(_testLogsDir, "test_multiline.log");
            var config = new LogFormatConfig
            {
                Name = "TestFormat",
                Pattern = @"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \[(?<level>\w+)\] (?<message>.*)$",
                TimestampFormat = "yyyy-MM-dd HH:mm:ss",
                IsMultiline = true
            };

            // Act
            var logEntries = _reader.Read(logPath, config).ToList();

            // Assert
            Assert.AreEqual(3, logEntries.Count, "Should have 3 log entries.");

            // Entry 1
            Assert.AreEqual("Line 1", logEntries[0].Message);

            // Entry 2 (Multiline)
            var expectedMessage = "Multi-line start" + System.Environment.NewLine + "  at StackTrace.Line1" + System.Environment.NewLine + "  at StackTrace.Line2";
            Assert.AreEqual(expectedMessage, logEntries[1].Message);
            Assert.AreEqual("ERROR", logEntries[1].AdditionalData["level"]);

            // Entry 3
            Assert.AreEqual("Line 3", logEntries[2].Message);
        }

        [TestMethod]
        public void ReadFiles_EmptyList_ReturnsEmpty()
        {
            // Act
            var logEntries = _reader.ReadFiles(new List<string>(), new LogFormatConfig()).ToList();

            // Assert
            Assert.AreEqual(0, logEntries.Count);
        }

        [TestMethod]
        public void ReadIncremental_ContinuousWrite_ReadsNewLinesOnly()
        {
            // テスト観点: ReadIncrementalが前回の続きから新しい行だけを読み込み、状態を更新すること。
            var config = new LogFormatConfig
            {
                Pattern = @"^(?<message>.*)$",
                IsMultiline = false
            };
            var testLogFile = Path.Combine(_tempDirectory, "incremental.log");

            // 1. 初回の書き込み
            File.WriteAllLines(testLogFile, new[] { "Line 1", "Line 2" });
            var state0 = new FileState(testLogFile, 0, 0);

            // 2. 初回の読み込み
            var (entries1, state1) = _reader.ReadIncremental(state0, config);
            Assert.AreEqual(2, entries1.Count());
            Assert.AreEqual(2, state1.LastLineNumber);
            Assert.IsTrue(state1.LastPosition > 0);

            // 3. 追記
            using (var sw = File.AppendText(testLogFile))
            {
                sw.WriteLine("Line 3");
            }

            // 4. 逐次読み込み
            var (entries2, state2) = _reader.ReadIncremental(state1, config);
            Assert.AreEqual(1, entries2.Count());
            Assert.AreEqual("Line 3", entries2.First().Message);
            Assert.AreEqual(3, state2.LastLineNumber);
        }

        [TestMethod]
        public void ReadIncremental_FileRotated_ReadsFromStart()
        {
            // テスト観点: ファイルサイズが小さくなった（ログローテーション）場合、最初から読み直すこと。
            var config = new LogFormatConfig { Pattern = @"^(?<message>.*)$" };
            var testLogFile = Path.Combine(_tempDirectory, "rotation.log");

            // 1. 初回読み込み
            File.WriteAllLines(testLogFile, new[] { "Old Log 1", "Old Log 2" });
            var (_, state1) = _reader.ReadIncremental(new FileState(testLogFile, 0, 0), config);

            // 2. ファイルを上書き（サイズを小さくする）
            File.WriteAllLines(testLogFile, new[] { "New Log 1" });

            // 3. 逐次読み込み
            var (entries2, state2) = _reader.ReadIncremental(state1, config);

            // Assert
            Assert.AreEqual(1, entries2.Count(), "Should read from the beginning of the new file.");
            Assert.AreEqual("New Log 1", entries2.First().Message);
            Assert.AreEqual(1, state2.LastLineNumber);
        }

        [TestMethod]
        public void ReadIncremental_NonExistentFile_ReturnsEmptyAndPreservesState()
        {
            // テスト観点: ファイルが存在しない場合、空リストを返し、クラッシュしないこと。
            var state = new FileState("non_existent.log", 100, 10);
            var (entries, updatedState) = _reader.ReadIncremental(state, new LogFormatConfig());

            Assert.AreEqual(0, entries.Count());
            Assert.AreEqual(state.FilePath, updatedState.FilePath);
            Assert.AreEqual(state.LastPosition, updatedState.LastPosition);
        }

        [TestMethod]
        public void Read_MixedFormats_ReturnsCorrectEntries()
        {
            // Arrange
            // 2つの異なるフォーマット設定
            var config1 = new LogFormatConfig
            {
                Name = "AppLog",
                Pattern = @"^APP (?<message>.*)$",
                IsMultiline = false
            };
            var config2 = new LogFormatConfig
            {
                Name = "SysLog",
                Pattern = @"^SYS (?<message>.*)$",
                IsMultiline = false
            };
            var configs = new List<LogFormatConfig> { config1, config2 };

            var logLines = new[]
            {
                "APP Application started",
                "SYS System initialization",
                "APP User action",
                "UNKNOWN Line" // どちらにもマッチしない
            };
            var testLogFile = Path.Combine(_tempDirectory, "mixed.log");
            File.WriteAllLines(testLogFile, logLines);

            // Act
            var logEntries = _reader.Read(testLogFile, configs).ToList();

            // Assert
            Assert.AreEqual(3, logEntries.Count);
            Assert.AreEqual("Application started", logEntries[0].Message);
            Assert.AreEqual("System initialization", logEntries[1].Message);
            Assert.AreEqual("User action", logEntries[2].Message);
        }
    }
}
