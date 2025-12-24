using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MultiLogViewer.Models;
using MultiLogViewer.Services;
using System.Collections.Generic;

namespace MultiLogViewer.Tests.Services
{
    [TestClass]
    public class CompositeLogParserTests
    {
        private Mock<ILogParser> _mockParser1 = null!;
        private Mock<ILogParser> _mockParser2 = null!;
        private CompositeLogParser _compositeParser = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockParser1 = new Mock<ILogParser>();
            _mockParser2 = new Mock<ILogParser>();
            _compositeParser = new CompositeLogParser(new List<ILogParser> { _mockParser1.Object, _mockParser2.Object });
        }

        [TestMethod]
        public void Parse_FirstParserSucceeds_ReturnsEntryFromFirst()
        {
            // Arrange
            var expectedEntry = new LogEntry { Message = "Matched by 1" };
            _mockParser1.Setup(p => p.Parse(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(expectedEntry);

            // Act
            var result = _compositeParser.Parse("line", "file.log", 1);

            // Assert
            Assert.AreSame(expectedEntry, result);
            _mockParser2.Verify(p => p.Parse(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void Parse_FirstParserFails_SecondParserSucceeds_ReturnsEntryFromSecond()
        {
            // Arrange
            _mockParser1.Setup(p => p.Parse(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns((LogEntry?)null);

            var expectedEntry = new LogEntry { Message = "Matched by 2" };
            _mockParser2.Setup(p => p.Parse(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(expectedEntry);

            // Act
            var result = _compositeParser.Parse("line", "file.log", 1);

            // Assert
            Assert.AreSame(expectedEntry, result);
        }

        [TestMethod]
        public void Parse_AllParsersFail_ReturnsNull()
        {
            // Arrange
            _mockParser1.Setup(p => p.Parse(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns((LogEntry?)null);
            _mockParser2.Setup(p => p.Parse(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns((LogEntry?)null);

            // Act
            var result = _compositeParser.Parse("line", "file.log", 1);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Parse_NoParsers_ReturnsNull()
        {
            // Arrange
            var parser = new CompositeLogParser(new List<ILogParser>());

            // Act
            var result = parser.Parse("line", "file.log", 1);

            // Assert
            Assert.IsNull(result);
        }
    }
}
