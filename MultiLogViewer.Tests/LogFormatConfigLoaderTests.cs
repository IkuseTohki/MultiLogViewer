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
        private string _tempFilePath;

        [TestInitialize]
        public void Setup()
        {
            _tempFilePath = Path.GetTempFileName();
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }

        /// <summary>
        /// テスト観点: 有効なYAML設定ファイルが正しくデシリアライズされ、AppConfigオブジェクトが生成されることを確認する。
        /// </summary>
        [TestMethod]
        public void LoadConfig_ValidYamlFile_ReturnsAppConfig()
        {
            // Arrange
            var yamlContent = @"
log_formats:
  - name: ""ApplicationLog""
    pattern: ""^(?<timestamp>\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}) \\[(?<level>\\w+)\\] (?<message>.*)$""
    timestamp_format: ""yyyy-MM-dd HH:mm:ss""
    display_columns:
      - header: ""Timestamp""
        binding_path: ""Timestamp""
        width: 150
      - header: ""Level""
        binding_path: ""Level""
        width: 80
      - header: ""Message""
        binding_path: ""Message""
        width: 500
      - header: ""User""
        binding_path: ""AdditionalData[user]""
        width: 80
  - name: ""WebServerLog""
    pattern: ""^(?<level>\\w+) \\d+ (?<timestamp>\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}.\\d{3}Z) (?<message>.*)$""
    timestamp_format: ""yyyy-MM-ddTHH:mm:ss.fffZ""
    display_columns:
      - header: ""Timestamp""
        binding_path: ""Timestamp""
        width: 180
      - header: ""Level""
        binding_path: ""Level""
        width: 80
      - header: ""Message""
        binding_path: ""Message""
        width: 400
      - header: ""Method""
        binding_path: ""AdditionalData[method]""
        width: 60
";
            File.WriteAllText(_tempFilePath, yamlContent);

            var loader = new LogFormatConfigLoader();

            // Act
            var config = loader.Load(_tempFilePath);

            // Assert
            Assert.IsNotNull(config);
            Assert.IsNotNull(config.LogFormats);
            Assert.HasCount(2, config.LogFormats);

            var appLog = config.LogFormats[0];
            Assert.AreEqual("ApplicationLog", appLog.Name);
            Assert.AreEqual("^(?<timestamp>\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}) \\[(?<level>\\w+)\\] (?<message>.*)$", appLog.Pattern);
            Assert.AreEqual("yyyy-MM-dd HH:mm:ss", appLog.TimestampFormat);
            Assert.IsNotNull(appLog.DisplayColumns);
            Assert.HasCount(4, appLog.DisplayColumns);
            Assert.AreEqual("User", appLog.DisplayColumns[3].Header);
            Assert.AreEqual("AdditionalData[user]", appLog.DisplayColumns[3].BindingPath);

            var webLog = config.LogFormats[1];
            Assert.AreEqual("WebServerLog", webLog.Name);
            Assert.AreEqual("^(?<level>\\w+) \\d+ (?<timestamp>\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}.\\d{3}Z) (?<message>.*)$", webLog.Pattern);
            Assert.AreEqual("yyyy-MM-ddTHH:mm:ss.fffZ", webLog.TimestampFormat);
            Assert.IsNotNull(webLog.DisplayColumns);
            Assert.HasCount(4, webLog.DisplayColumns);
            Assert.AreEqual("Method", webLog.DisplayColumns[3].Header);
            Assert.AreEqual("AdditionalData[method]", webLog.DisplayColumns[3].BindingPath);
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
            Assert.IsEmpty(config.LogFormats);
        }
    }
}
