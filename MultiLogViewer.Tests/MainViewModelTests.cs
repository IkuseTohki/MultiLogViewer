using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MultiLogViewer.Models;
using MultiLogViewer.Services;
using MultiLogViewer.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MultiLogViewer.Tests
{
    [TestClass]
    public class MainViewModelTests
    {
        private Mock<ILogFileReader> _mockLogFileReader = null!;
        private Mock<IUserDialogService> _mockUserDialogService = null!;
        private Mock<ILogFormatConfigLoader> _mockLogFormatConfigLoader = null!;
        private Mock<IFileResolver> _mockFileResolver = null!;
        private MainViewModel _viewModel = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockLogFileReader = new Mock<ILogFileReader>();
            _mockUserDialogService = new Mock<IUserDialogService>();
            _mockLogFormatConfigLoader = new Mock<ILogFormatConfigLoader>();
            _mockFileResolver = new Mock<IFileResolver>();
        }

        [TestMethod]
        public void Initialize_Successful_LoadsLogsAndColumns()
        {
            // Arrange
            var displayColumns = new List<DisplayColumnConfig>
            {
                new DisplayColumnConfig { Header = "Timestamp", BindingPath = "Timestamp" }
            };

            var logFormats = new List<LogFormatConfig>
            {
                new LogFormatConfig
                {
                    Name = "TestFormat",
                    LogFilePatterns = new List<string> { "test-*.log" }
                }
            };

            var appConfig = new AppConfig
            {
                DisplayColumns = displayColumns,
                LogFormats = logFormats
            };

            var filePaths = new List<string> { "C:\\dummy\\test-1.log" };
            var logEntries = new List<LogEntry>
            {
                new LogEntry { Message = "Entry 1" }
            };

            _mockLogFormatConfigLoader.Setup(l => l.Load(It.IsAny<string>())).Returns(appConfig);
            _mockFileResolver.Setup(r => r.Resolve(logFormats[0].LogFilePatterns)).Returns(filePaths);
            _mockLogFileReader.Setup(r => r.ReadFiles(filePaths, logFormats[0])).Returns(logEntries);

            // Act
            _viewModel = new MainViewModel(
                _mockLogFileReader.Object,
                _mockUserDialogService.Object,
                _mockLogFormatConfigLoader.Object,
                _mockFileResolver.Object);

            // Assert
            Assert.AreEqual(1, _viewModel.LogEntriesView.Cast<LogEntry>().Count());
            Assert.AreEqual("Entry 1", _viewModel.LogEntriesView.Cast<LogEntry>().First().Message);

            Assert.AreEqual(1, _viewModel.DisplayColumns.Count);
            Assert.AreEqual("Timestamp", _viewModel.DisplayColumns[0].Header);

            _mockLogFormatConfigLoader.Verify(l => l.Load(It.IsAny<string>()), Times.Once);
            _mockFileResolver.Verify(r => r.Resolve(logFormats[0].LogFilePatterns), Times.Once);
            _mockLogFileReader.Verify(r => r.ReadFiles(filePaths, logFormats[0]), Times.Once);
        }

        [TestMethod]
        public void Initialize_ConfigLoadFails_ShowsError()
        {
            // Arrange
            _mockLogFormatConfigLoader.Setup(l => l.Load(It.IsAny<string>())).Returns((AppConfig?)null);

            // Act
            _viewModel = new MainViewModel(
                _mockLogFileReader.Object,
                _mockUserDialogService.Object,
                _mockLogFormatConfigLoader.Object,
                _mockFileResolver.Object);

            // Assert
            _mockUserDialogService.Verify(s => s.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            Assert.AreEqual(0, _viewModel.LogEntriesView.Cast<LogEntry>().Count());
        }

        [TestMethod]
        public void FilterText_WhenSet_RaisesPropertyChanged()
        {
            // Arrange
            var appConfig = new AppConfig();
            _mockLogFormatConfigLoader.Setup(l => l.Load(It.IsAny<string>())).Returns(appConfig);

            _viewModel = new MainViewModel(
                _mockLogFileReader.Object,
                _mockUserDialogService.Object,
                _mockLogFormatConfigLoader.Object,
                _mockFileResolver.Object);

            var propertyChangedFired = false;
            _viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.FilterText))
                {
                    propertyChangedFired = true;
                }
            };

            // Act
            _viewModel.FilterText = "new filter";

            // Assert
            Assert.IsTrue(propertyChangedFired, "PropertyChanged event for FilterText was not raised.");
        }

        [TestMethod]
        public void DisplayColumns_WhenSet_RaisesPropertyChanged()
        {
            // Arrange
            var appConfig = new AppConfig();
            _mockLogFormatConfigLoader.Setup(l => l.Load(It.IsAny<string>())).Returns(appConfig);

            _viewModel = new MainViewModel(
                _mockLogFileReader.Object,
                _mockUserDialogService.Object,
                _mockLogFormatConfigLoader.Object,
                _mockFileResolver.Object);

            var propertyChangedFired = false;
            _viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.DisplayColumns))
                {
                    propertyChangedFired = true;
                }
            };

            // Act
            _viewModel.DisplayColumns = new ObservableCollection<DisplayColumnConfig>();

            // Assert
            Assert.IsTrue(propertyChangedFired, "PropertyChanged event for DisplayColumns was not raised.");
        }
    }
}
