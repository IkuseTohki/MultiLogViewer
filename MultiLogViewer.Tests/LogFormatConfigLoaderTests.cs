using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiLogViewer.Models;
using MultiLogViewer.Services;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace MultiLogViewer.Tests
{
    [TestClass]
    public class LogFormatConfigLoaderTests
    {
        // ... (existing tests) ...

        /// <summary>
        /// テスト観点: sub_patterns の拡張設定 (match_type, separator, options) が正しく読み込まれることを確認する。
        /// </summary>
        [TestMethod]
        public void LoadConfig_SubPatternConfig_ReturnsExtendedProperties()
        {
            // Arrange
            var logProfilePath = "TestConfigs/subpattern_log_profile.yaml";
            var loader = new LogFormatConfigLoader();

            // Act
            var config = loader.Load(logProfilePath, "");

            // Assert
            Assert.IsNotNull(config);
            Assert.AreEqual(1, config.LogFormats.Count);

            var subPattern = config.LogFormats[0].SubPatterns.FirstOrDefault();
            Assert.IsNotNull(subPattern);

            Assert.AreEqual(MultiLogViewer.Models.MatchType.All, subPattern.MatchType);
            Assert.AreEqual(";", subPattern.Separator);

            Assert.IsNotNull(subPattern.Options);
            Assert.IsTrue(subPattern.Options.Contains("Singleline"));
            Assert.IsTrue(subPattern.Options.Contains("IgnoreCase"));
        }

        /// <summary>
        /// テスト観点: TestConfigs/valid_log_profile.yaml (ログプロファイル) ファイルが正しく読み込まれ、AppConfigオブジェクトが生成されることを確認する。
        /// </summary>
        [TestMethod]
        public void LoadConfig_ValidYamlFile_ReturnsAppConfig()
        {
            // Arrange
            var logProfilePath = "TestConfigs/valid_log_profile.yaml";
            var loader = new LogFormatConfigLoader();

            // Act
            // 既存のファイルをログプロファイルとして読み込む
            var config = loader.Load(logProfilePath, "");

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
        }

        /// <summary>
        /// テスト観点: どちらの設定ファイルも存在しない場合、デフォルト値を持つAppConfigが返されることを確認する。
        /// </summary>
        [TestMethod]
        public void LoadConfig_NoFiles_ReturnsAppConfigWithDefaults()
        {
            // Arrange
            var loader = new LogFormatConfigLoader();

            // Act
            var config = loader.Load("non_existent_profile.yaml", "non_existent_settings.yaml");

            // Assert
            Assert.IsNotNull(config);
            Assert.AreEqual(1000, config.PollingIntervalMs, "Should use default value when settings file is missing.");
            Assert.AreEqual(0, config.LogFormats.Count);
            Assert.AreEqual(0, config.DisplayColumns.Count);
        }

        /// <summary>
        /// テスト観点: ログプロファイルのみ存在し、アプリケーション設定が欠落している場合、
        /// 設定はデフォルト値のままプロファイルが読み込まれることを確認する。
        /// </summary>
        [TestMethod]
        public void LoadConfig_OnlyLogProfile_ReturnsCombinedWithDefaultSettings()
        {
            // Arrange
            var logProfilePath = "TestConfigs/valid_log_profile.yaml";
            var loader = new LogFormatConfigLoader();

            // Act
            var config = loader.Load(logProfilePath, "non_existent_settings.yaml");

            // Assert
            Assert.IsNotNull(config);
            Assert.AreEqual(1000, config.PollingIntervalMs, "Should use default value.");
            Assert.IsTrue(config.LogFormats.Any(), "Should load LogFormats from profile.");
        }

        /// <summary>
        /// テスト観点: アプリケーション設定のみ存在し、ログプロファイルが欠落している場合、
        /// 設定値が反映され、プロファイルの内容は空であることを確認する。
        /// </summary>
        [TestMethod]
        public void LoadConfig_OnlyAppSettings_ReturnsEmptyProfileWithSettings()
        {
            // Arrange
            var appSettingsPath = Path.Combine(Path.GetTempPath(), "test_app_settings_only.yaml");
            File.WriteAllText(appSettingsPath, "polling_interval_ms: 500");
            var loader = new LogFormatConfigLoader();

            try
            {
                // Act
                var config = loader.Load("non_existent_profile.yaml", appSettingsPath);

                // Assert
                Assert.IsNotNull(config);
                Assert.AreEqual(500, config.PollingIntervalMs);
                Assert.AreEqual(0, config.LogFormats.Count);
            }
            finally
            {
                if (File.Exists(appSettingsPath)) File.Delete(appSettingsPath);
            }
        }

        /// <summary>
        /// テスト観点: TestConfigs/style_log_profile.yaml ファイルから column_styles が正しく読み込まれることを確認する。
        /// </summary>
        [TestMethod]
        public void LoadConfig_StyleConfig_ReturnsColumnStyles()
        {
            // Arrange
            var logProfilePath = "TestConfigs/style_log_profile.yaml";
            var loader = new LogFormatConfigLoader();

            // Act
            var config = loader.Load(logProfilePath, "");

            // Assert
            Assert.IsNotNull(config);
            Assert.AreEqual(1000, config.PollingIntervalMs, "PollingIntervalMs should remain default (1000) as it is not part of LogProfile.");
            Assert.IsNotNull(config.ColumnStyles);
            Assert.AreEqual(2, config.ColumnStyles.Count);

            // Level カラムのスタイル検証
            var levelStyle = config.ColumnStyles.FirstOrDefault(c => c.ColumnHeader == "Level");
            Assert.IsNotNull(levelStyle);
            Assert.IsFalse(levelStyle.SemanticColoring);
            Assert.AreEqual(2, levelStyle.Rules.Count);

            var errorRule = levelStyle.Rules[0];
            Assert.AreEqual("^(ERROR|FATAL)$", errorRule.Pattern);
            Assert.AreEqual("White", errorRule.Foreground);
            Assert.AreEqual("#D32F2F", errorRule.Background);
            Assert.AreEqual("Bold", errorRule.FontWeight);
        }

        /// <summary>
        /// テスト観点: ログプロファイルとアプリケーション設定の両方が指定された場合、正しくマージされることを確認する。
        /// </summary>
        [TestMethod]
        public void LoadConfig_SplitFiles_ReturnsCombinedAppConfig()
        {
            // Arrange
            var logProfilePath = "TestConfigs/valid_log_profile.yaml";
            var appSettingsPath = Path.Combine(Path.GetTempPath(), "test_app_settings.yaml");
            File.WriteAllText(appSettingsPath, "polling_interval_ms: 2000");

            var loader = new LogFormatConfigLoader();

            try
            {
                // Act
                var config = loader.Load(logProfilePath, appSettingsPath);

                // Assert
                Assert.IsNotNull(config);
                Assert.AreEqual(2000, config.PollingIntervalMs, "PollingIntervalMs should be loaded from AppSettings.");
                Assert.IsTrue(config.LogFormats.Any(), "LogFormats should be loaded from LogProfile.");
            }
            finally
            {
                if (File.Exists(appSettingsPath)) File.Delete(appSettingsPath);
            }
        }

        /// <summary>
        /// テスト観点: log_retention_limit が AppSettings.yaml から正しく読み込まれることを確認する。
        /// </summary>
        [TestMethod]
        public void LoadConfig_RetentionLimit_ReturnsValue()
        {
            // Arrange
            var appSettingsPath = Path.Combine(Path.GetTempPath(), "test_retention_settings.yaml");
            File.WriteAllText(appSettingsPath, "log_retention_limit: \"-1w\"");
            var loader = new LogFormatConfigLoader();

            try
            {
                // Act
                var config = loader.Load("", appSettingsPath);

                // Assert
                Assert.AreEqual("-1w", config.LogRetentionLimit);
            }
            finally
            {
                if (File.Exists(appSettingsPath)) File.Delete(appSettingsPath);
            }
        }

        /// <summary>
        /// テスト観点: 不正な形式のYAMLファイルを読み込んだ際、ユーザー向けの親切なエラーメッセージを含む例外が投げられることを確認する。
        /// </summary>
        [TestMethod]
        public void LoadConfig_InvalidYaml_ThrowsExceptionWithFriendlyMessage()
        {
            // Arrange
            var logProfilePath = Path.Combine(Path.GetTempPath(), "invalid_profile.yaml");
            File.WriteAllText(logProfilePath, "invalid: [ : : yaml"); // 構文エラーになるYAML
            var loader = new LogFormatConfigLoader();

            try
            {
                // Act
                loader.Load(logProfilePath, "");
                Assert.Fail("Should have thrown an exception.");
            }
            catch (System.Exception ex)
            {
                // Assert
                // メッセージは "ログプロファイル(...)の解析に失敗しました" になるはず
                Assert.IsTrue(ex.Message.Contains("ログプロファイル"), $"Actual message: {ex.Message}");
            }
            finally
            {
                if (File.Exists(logProfilePath)) File.Delete(logProfilePath);
            }
        }
    }
}
