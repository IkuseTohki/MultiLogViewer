using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiLogViewer.Models;
using System.Collections.Generic;

namespace MultiLogViewer.Tests.Models
{
    [TestClass]
    public class LogEntryTests
    {
        [TestMethod]
        public void Indexer_ReturnsValue_WhenKeyExists()
        {
            // Arrange
            var entry = new LogEntry
            {
                AdditionalData = new Dictionary<string, string> { { "Level", "INFO" } }
            };

            // Act
            var result = entry["Level"];

            // Assert
            Assert.AreEqual("INFO", result);
        }

        [TestMethod]
        public void Indexer_ReturnsEmptyString_WhenKeyDoesNotExist()
        {
            // Arrange
            var entry = new LogEntry
            {
                AdditionalData = new Dictionary<string, string>()
            };

            // Act
            var result = entry["NonExistent"];

            // Assert
            Assert.AreEqual(string.Empty, result);
        }
    }
}
