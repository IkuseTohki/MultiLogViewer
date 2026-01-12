using MultiLogViewer.Models;
using MultiLogViewer.Services;

namespace MultiLogViewer.Tests
{
    [TestClass]
    public class LogParserCaseSensitivityTests
    {
        [TestMethod]
        public void Parse_ShouldBeCaseInsensitive_ForSpecialGroupNames()
        {
            /*
             * テスト観点: 正規表現のキャプチャグループ名が "Message" や "Timestamp" (大文字開始)
             * であっても、正しく LogEntry のプロパティに格納されることを確認する。
             */
            // Arrange
            var config = new LogFormatConfig
            {
                Name = "UppercaseGroups",
                // グループ名を大文字で定義
                Pattern = @"^(?<Timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \[(?<Level>\w+)\] (?<Message>.*)$",
                TimestampFormat = "yyyy-MM-dd HH:mm:ss"
            };
            var logLine = "2023-10-26 10:30:45 [INFO] Important message content.";
            var parser = new LogParser(config);

            // Act
            var logEntry = parser.Parse(logLine, "test.log", 1);

            // Assert
            Assert.IsNotNull(logEntry);

            // プロパティに入っているべき (現状の実装では失敗し、Message は空、AdditionalData["Message"] に入るはず)
            Assert.AreEqual("Important message content.", logEntry.Message, "Message should be populated even if group name is 'Message'.");
            Assert.AreEqual(new DateTime(2023, 10, 26, 10, 30, 45), logEntry.Timestamp, "Timestamp should be populated even if group name is 'Timestamp'.");

            // AdditionalData に "Message" や "Timestamp" という名前で入っていないことも確認
            Assert.IsFalse(logEntry.AdditionalData.ContainsKey("Message"), "Should not store 'Message' in AdditionalData.");
            Assert.IsFalse(logEntry.AdditionalData.ContainsKey("Timestamp"), "Should not store 'Timestamp' in AdditionalData.");
        }
    }
}
