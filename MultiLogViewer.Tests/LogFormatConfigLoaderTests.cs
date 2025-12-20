using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiLogViewer.Models;
using MultiLogViewer.Services;
using System.IO;
using System.Linq;

namespace MultiLogViewer.Tests
{
    [TestClass]
    public class LogFormatConfigLoaderTests
    {
        /// <summary>
        /// テスト観点: TestConfigs/valid_config.yaml ファイルが正しく読み込まれ、AppConfigオブジェクトが生成されることを確認する。
        /// </summary>
        [TestMethod]
        public void LoadConfig_ValidYamlFile_ReturnsAppConfig()
        {
            // Arrange
            var configPath = "TestConfigs/valid_config.yaml";
            var loader = new LogFormatConfigLoader();

            // Act
            var config = loader.Load(configPath);

            // Assert
            Assert.IsNotNull(config);

            // トップレベルのDisplayColumnsを検証
            Assert.IsNotNull(config.DisplayColumns);
            Assert.AreEqual(4, config.DisplayColumns.Count);
            Assert.AreEqual("Timestamp", config.DisplayColumns[0].Header);
            Assert.AreEqual("AdditionalData[user]", config.DisplayColumns[3].BindingPath);

            // LogFormatsを検証
            Assert.IsNotNull(config.LogFormats);
            Assert.AreEqual(2, config.LogFormats.Count);

            var appLog = config.LogFormats[0];
            Assert.AreEqual("ApplicationLog", appLog.Name);
            Assert.AreEqual(@"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) \[(?<level>\w+)\] (?<message>.*)$", appLog.Pattern);
            Assert.AreEqual("yyyy-MM-dd HH:mm:ss", appLog.TimestampFormat);
            Assert.IsNotNull(appLog.LogFilePatterns);
            Assert.AreEqual(1, appLog.LogFilePatterns.Count);
            Assert.AreEqual("app_*.log", appLog.LogFilePatterns[0]);

            var webLog = config.LogFormats[1];
            Assert.AreEqual("WebServerLog", webLog.Name);
            Assert.AreEqual(@"^(?<level>\w+) \d+ (?<timestamp>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}.\d{3}Z) (?<message>.*)$", webLog.Pattern);
            Assert.AreEqual("yyyy-MM-ddTHH:mm:ss.fffZ", webLog.TimestampFormat);
            Assert.IsNotNull(webLog.LogFilePatterns);
            Assert.AreEqual(1, webLog.LogFilePatterns.Count);
            Assert.AreEqual("web_*.log", webLog.LogFilePatterns[0]);
        }

        /// <summary>
        /// テスト観点: 存在しないYAML設定ファイルを渡した場合、空のAppConfigが返されることを確認する。
        /// </summary>
        [TestMethod]
        public void LoadConfig_NonExistentFile_ReturnsEmptyAppConfig()
        {
            // Arrange
            var loader = new LogFormatConfigLoader();

            // Act
            var config = loader.Load("non_existent_file.yaml");

            // Assert
            Assert.IsNotNull(config);
            Assert.IsNotNull(config.LogFormats);
            Assert.AreEqual(0, config.LogFormats.Count);
            Assert.IsNotNull(config.DisplayColumns);
            Assert.AreEqual(0, config.DisplayColumns.Count);
        }
    }
}
