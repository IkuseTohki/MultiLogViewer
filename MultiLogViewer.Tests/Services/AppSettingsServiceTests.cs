using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MultiLogViewer.Models;
using MultiLogViewer.Services;
using System.IO;

namespace MultiLogViewer.Tests.Services
{
    [TestClass]
    public class AppSettingsServiceTests
    {
        private Mock<IConfigPathResolver> _mockConfigPathResolver = null!;
        private string _tempFilePath = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockConfigPathResolver = new Mock<IConfigPathResolver>();
            _tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            _mockConfigPathResolver.Setup(r => r.GetAppSettingsPath()).Returns(_tempFilePath);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_tempFilePath)) File.Delete(_tempFilePath);
        }

        [TestMethod]
        public void SaveAndLoad_RoundTrip_Works()
        {
            // Arrange
            var service = new AppSettingsService(_mockConfigPathResolver.Object);
            var settings = new AppSettings
            {
                PollingIntervalMs = 500,
                LogRetentionLimit = "-3d",
                SkipTailModeWarning = true
            };

            // Act
            service.Save(settings);
            var loaded = service.Load();

            // Assert
            Assert.AreEqual(500, loaded.PollingIntervalMs);
            Assert.AreEqual("-3d", loaded.LogRetentionLimit);
            Assert.AreEqual(true, loaded.SkipTailModeWarning);
        }

        [TestMethod]
        public void Load_ReturnsDefault_WhenFileMissing()
        {
            // Arrange
            var service = new AppSettingsService(_mockConfigPathResolver.Object);

            // Act
            var loaded = service.Load();

            // Assert
            Assert.IsNotNull(loaded);
            Assert.AreEqual(1000, loaded.PollingIntervalMs); // Default value
            Assert.IsFalse(loaded.SkipTailModeWarning);
        }

        [TestMethod]
        public void Save_PreservesCommentsAndFormatting()
        {
            // Arrange
            var service = new AppSettingsService(_mockConfigPathResolver.Object);
            var initialYaml = @"# This is a header comment
polling_interval_ms: 1000

# Important setting for retention
log_retention_limit: ""today""

# End of file";
            File.WriteAllText(_tempFilePath, initialYaml);

            var settings = new AppSettings
            {
                PollingIntervalMs = 2000, // Update this
                LogRetentionLimit = "today", // Keep this
                SkipTailModeWarning = true // Add this (it was missing in initialYaml)
            };

            // Act
            service.Save(settings);
            var resultYaml = File.ReadAllText(_tempFilePath);

            // Assert
            // 1. コメントが残っていること
            StringAssert.Contains(resultYaml, "# This is a header comment");
            StringAssert.Contains(resultYaml, "# Important setting for retention");
            StringAssert.Contains(resultYaml, "# End of file");

            // 2. 値が正しく更新されていること
            StringAssert.Contains(resultYaml, "polling_interval_ms: 2000");
            StringAssert.Contains(resultYaml, "log_retention_limit: \"today\"");
            StringAssert.Contains(resultYaml, "skip_tail_mode_warning: true");
        }

        [TestMethod]
        public void Save_AddsMissingKeys_ToExistingFile()
        {
            // Arrange: 別の設定項目しかないファイル
            var service = new AppSettingsService(_mockConfigPathResolver.Object);
            File.WriteAllText(_tempFilePath, "unknown_setting: true" + System.Environment.NewLine);

            var settings = new AppSettings
            {
                PollingIntervalMs = 500,
                LogRetentionLimit = "-1d",
                SkipTailModeWarning = true
            };

            // Act
            service.Save(settings);
            var resultYaml = File.ReadAllText(_tempFilePath);

            // Assert: すべてのキーが追記されていること
            StringAssert.Contains(resultYaml, "unknown_setting: true");
            StringAssert.Contains(resultYaml, "polling_interval_ms: 500");
            StringAssert.Contains(resultYaml, "log_retention_limit: \"-1d\"");
            StringAssert.Contains(resultYaml, "skip_tail_mode_warning: true");
        }

        [TestMethod]
        public void Load_HandlesMissingFields_UsingDefaultValues()
        {
            // Arrange: polling_interval_ms しか書かれていないファイル
            var service = new AppSettingsService(_mockConfigPathResolver.Object);
            File.WriteAllText(_tempFilePath, "polling_interval_ms: 777");

            // Act
            var loaded = service.Load();

            // Assert
            Assert.AreEqual(777, loaded.PollingIntervalMs);
            Assert.IsNull(loaded.LogRetentionLimit, "Missing string field should be null");
            Assert.IsFalse(loaded.SkipTailModeWarning, "Missing bool field should be default (false)");
        }
    }
}
