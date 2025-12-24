using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiLogViewer.Models;
using MultiLogViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiLogViewer.Tests.ViewModels
{
    [TestClass]
    public class DigestViewModelTests
    {
        [TestMethod]
        public void DigestEntries_CalculatesCorrectDeltas()
        {
            // Arrange
            var baseTime = new DateTime(2023, 1, 1, 10, 0, 0);
            var logs = new List<LogEntry>
            {
                new LogEntry { Timestamp = baseTime, Message = "First" },
                new LogEntry { Timestamp = baseTime.AddMinutes(5), Message = "Second" },
                new LogEntry { Timestamp = baseTime.AddMinutes(12), Message = "Third" }
            };

            // Act
            var vm = new DigestViewModel(logs);
            var digest = vm.DigestEntries.ToList();

            // Assert
            Assert.AreEqual(3, digest.Count);

            // 1つ目: Delta は Zero
            Assert.AreEqual(TimeSpan.Zero, digest[0].Delta);
            Assert.AreEqual("-", digest[0].DeltaText);
            Assert.AreEqual("00:00:00", digest[0].TotalElapsedText);

            // 2つ目: Delta は 5分
            Assert.AreEqual(TimeSpan.FromMinutes(5), digest[1].Delta);
            Assert.AreEqual("+00:05:00.000", digest[1].DeltaText);
            Assert.AreEqual(TimeSpan.FromMinutes(5), digest[1].TotalElapsed);
            Assert.AreEqual("00:05:00", digest[1].TotalElapsedText);

            // 3つ目: Delta は 7分 (12-5), Total は 12分
            Assert.AreEqual(TimeSpan.FromMinutes(7), digest[2].Delta);
            Assert.AreEqual("+00:07:00.000", digest[2].DeltaText);
            Assert.AreEqual(TimeSpan.FromMinutes(12), digest[2].TotalElapsed);
            Assert.AreEqual("00:12:00", digest[2].TotalElapsedText);
        }
    }
}
