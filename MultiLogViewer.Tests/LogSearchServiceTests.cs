using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiLogViewer.Models;
using MultiLogViewer.Services;
using System.Collections.Generic;
using System.Linq;

namespace MultiLogViewer.Tests
{
    [TestClass]
    public class LogSearchServiceTests
    {
        private ILogSearchService _searchService = null!;

        [TestInitialize]
        public void Setup()
        {
            _searchService = new LogSearchService();
        }

        [TestMethod]
        public void IsMatch_SimpleText_MatchesCorrect()
        {
            var entry = new LogEntry { Message = "Hello World" };
            var criteria = new SearchCriteria("Hello", false, false);

            Assert.IsTrue(_searchService.IsMatch(entry, criteria));
        }

        [TestMethod]
        public void IsMatch_CaseSensitive_Respected()
        {
            var entry = new LogEntry { Message = "Hello World" };
            var criteriaTrue = new SearchCriteria("hello", true, false);
            var criteriaFalse = new SearchCriteria("hello", false, false);

            Assert.IsFalse(_searchService.IsMatch(entry, criteriaTrue));
            Assert.IsTrue(_searchService.IsMatch(entry, criteriaFalse));
        }

        [TestMethod]
        public void Find_Forward_WrapsAround()
        {
            var logs = new List<LogEntry>
            {
                new LogEntry { Message = "Target 1" },
                new LogEntry { Message = "Other" },
                new LogEntry { Message = "Target 2" }
            };
            var criteria = new SearchCriteria("Target", false, false);

            // 最後の項目を選択している状態で次を検索
            var result = _searchService.Find(logs, logs[2], criteria, true);

            Assert.AreEqual(logs[0], result, "Should wrap around to the first match.");
        }

        [TestMethod]
        public void GetSearchStatistics_ReturnsCorrectCounts()
        {
            var logs = new List<LogEntry>
            {
                new LogEntry { Message = "Target" }, // 1
                new LogEntry { Message = "Other" },
                new LogEntry { Message = "Target" }  // 2
            };
            var criteria = new SearchCriteria("Target", false, false);

            var (matchCount, currentIndex) = _searchService.GetSearchStatistics(logs, logs[2], criteria);

            Assert.AreEqual(2, matchCount);
            Assert.AreEqual(2, currentIndex);
        }

        [TestMethod]
        public void ShouldHide_AllSelectedFieldsEmpty_ReturnsTrue()
        {
            // テスト観点: 全ての指定カラムが空の場合、非表示（True）を返すこと
            var entry = new LogEntry { AdditionalData = new Dictionary<string, string> { { "Level", "" }, { "User", "  " } } };
            var filters = new[]
            {
                new LogFilter(FilterType.ColumnEmpty, "Level", default, ""),
                new LogFilter(FilterType.ColumnEmpty, "User", default, "")
            };

            Assert.IsTrue(_searchService.ShouldHide(entry, filters));
        }

        [TestMethod]
        public void ShouldHide_OneFieldHasValue_ReturnsFalse()
        {
            // テスト観点: 一つでも値があれば、カラムフィルターでは非表示にしない（False）こと
            var entry = new LogEntry { AdditionalData = new Dictionary<string, string> { { "Level", "INFO" }, { "User", "" } } };
            var filters = new[]
            {
                new LogFilter(FilterType.ColumnEmpty, "Level", default, ""),
                new LogFilter(FilterType.ColumnEmpty, "User", default, "")
            };

            Assert.IsFalse(_searchService.ShouldHide(entry, filters));
        }

        [TestMethod]
        public void ShouldHide_DateTimeAfter_FilterApplied()
        {
            // テスト観点: 指定日時より前のログが非表示になること
            var entry = new LogEntry { Timestamp = new DateTime(2023, 1, 1, 10, 0, 0) };
            var filter = new LogFilter(FilterType.DateTimeAfter, "", new DateTime(2023, 1, 1, 11, 0, 0), "");

            Assert.IsTrue(_searchService.ShouldHide(entry, new[] { filter }), "Should hide logs before criteria.");

            entry.Timestamp = new DateTime(2023, 1, 1, 12, 0, 0);
            Assert.IsFalse(_searchService.ShouldHide(entry, new[] { filter }), "Should show logs after criteria.");
        }

        [TestMethod]
        public void ShouldHide_DateTimeBefore_FilterApplied()
        {
            // テスト観点: 指定日時より後のログが非表示になること
            var entry = new LogEntry { Timestamp = new DateTime(2023, 1, 1, 12, 0, 0) };
            var filter = new LogFilter(FilterType.DateTimeBefore, "", new DateTime(2023, 1, 1, 11, 0, 0), "");

            Assert.IsTrue(_searchService.ShouldHide(entry, new[] { filter }), "Should hide logs after criteria.");

            entry.Timestamp = new DateTime(2023, 1, 1, 10, 0, 0);
            Assert.IsFalse(_searchService.ShouldHide(entry, new[] { filter }), "Should show logs before criteria.");
        }
    }
}
