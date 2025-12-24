using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MultiLogViewer.Models;
using MultiLogViewer.Services;
using MultiLogViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MultiLogViewer.Tests
{
    [TestClass]
    public class MainViewModelTests
    {
        private Mock<ILogService> _mockLogService = null!;
        private Mock<IUserDialogService> _mockUserDialogService = null!;
        private Mock<ISearchWindowService> _mockSearchWindowService = null!;
        private ILogSearchService _logSearchService = null!;
        private Mock<IClipboardService> _mockClipboardService = null!;
        private Mock<IConfigPathResolver> _mockConfigPathResolver = null!;
        private Mock<IFilterPresetService> _mockFilterPresetService = null!;
        private Mock<IDispatcherService> _mockDispatcherService = null!;
        private Mock<ITaskRunner> _mockTaskRunner = null!;
        private Mock<IGoToDateDialogService> _mockGoToDateDialogService = null!;
        private MainViewModel _viewModel = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockLogService = new Mock<ILogService>();
            _mockUserDialogService = new Mock<IUserDialogService>();
            _mockSearchWindowService = new Mock<ISearchWindowService>();
            _logSearchService = new LogSearchService();
            _mockClipboardService = new Mock<IClipboardService>();
            _mockConfigPathResolver = new Mock<IConfigPathResolver>();
            _mockFilterPresetService = new Mock<IFilterPresetService>();
            _mockDispatcherService = new Mock<IDispatcherService>();
            _mockTaskRunner = new Mock<ITaskRunner>();
            _mockGoToDateDialogService = new Mock<IGoToDateDialogService>();

            // Dispatcher: テストスレッドで即時実行
            _mockDispatcherService.Setup(d => d.Invoke(It.IsAny<Action>())).Callback<Action>(a => a());

            // TaskRunner: テストスレッドで同期的に即時実行
            _mockTaskRunner.Setup(r => r.Run(It.IsAny<Action>()))
                .Returns((Action a) =>
                {
                    a();
                    return Task.CompletedTask;
                });
        }

        private MainViewModel CreateViewModel()
        {
            return new MainViewModel(
                _mockLogService.Object,
                _mockUserDialogService.Object,
                _mockSearchWindowService.Object,
                _logSearchService,
                _mockClipboardService.Object,
                _mockConfigPathResolver.Object,
                _mockFilterPresetService.Object,
                _mockDispatcherService.Object,
                _mockTaskRunner.Object,
                _mockGoToDateDialogService.Object);
        }

        [TestMethod]
        public async Task CopyCommand_CopiesSelectedEntryToClipboard()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var logs = new List<LogEntry>
            {
                new LogEntry
                {
                    Message = "Test Message",
                    Timestamp = new System.DateTime(2023, 1, 1, 12, 0, 0),
                    AdditionalData = new Dictionary<string, string> { { "Level", "INFO" } }
                }
            };

            // Set up Display Columns
            _viewModel.DisplayColumns = new ObservableCollection<DisplayColumnConfig>
            {
                new DisplayColumnConfig { Header = "Time", BindingPath = "Timestamp", StringFormat = "yyyy-MM-dd HH:mm:ss" },
                new DisplayColumnConfig { Header = "Lvl", BindingPath = "AdditionalData[Level]" },
                new DisplayColumnConfig { Header = "Msg", BindingPath = "Message" }
            };

            await SetLogsToViewModel(_viewModel, logs);
            _viewModel.SelectedLogEntry = logs[0];

            // Act
            _viewModel.CopyCommand.Execute(null);

            // Assert
            var expectedText = "2023-01-01 12:00:00\tINFO\tTest Message";
            _mockClipboardService.Verify(c => c.SetText(expectedText), Times.Once);
        }

        [TestMethod]
        public async Task AddExtensionFilterCommand_AddsKeyAndRefreshesView()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var logs = new List<LogEntry>
            {
                new LogEntry { Message = "Entry with Value", AdditionalData = new Dictionary<string, string> { { "Level", "INFO" } } },
                new LogEntry { Message = "Entry without Value", AdditionalData = new Dictionary<string, string> { { "Level", "" } } }
            };
            await SetLogsToViewModel(_viewModel, logs);

            // Act
            _viewModel.AddExtensionFilterCommand.Execute("Level");

            // Assert
            Assert.IsTrue(_viewModel.ActiveExtensionFilters.Any(f => f.Key == "Level"));
            var view = _viewModel.LogEntriesView.Cast<LogEntry>().ToList();
            Assert.AreEqual(1, view.Count, "Should hide the entry where 'Level' is empty.");
            Assert.AreEqual("Entry with Value", view[0].Message);
        }

        [TestMethod]
        public async Task RemoveExtensionFilterCommand_RemovesKeyAndRefreshesView()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var logs = new List<LogEntry>
            {
                new LogEntry { Message = "Empty", AdditionalData = new Dictionary<string, string> { { "Level", "" } } }
            };
            await SetLogsToViewModel(_viewModel, logs);
            _viewModel.AddExtensionFilterCommand.Execute("Level");
            Assert.AreEqual(0, _viewModel.LogEntriesView.Cast<LogEntry>().Count());

            var filter = _viewModel.ActiveExtensionFilters.First();

            // Act
            _viewModel.RemoveExtensionFilterCommand.Execute(filter);

            // Assert
            Assert.IsFalse(_viewModel.ActiveExtensionFilters.Contains(filter));
            Assert.AreEqual(1, _viewModel.LogEntriesView.Cast<LogEntry>().Count(), "Should show the entry after filter is removed.");
        }

        [TestMethod]
        public async Task LoadLogs_PopulatesAvailableAdditionalDataKeys()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var logs = new List<LogEntry>
            {
                new LogEntry { AdditionalData = new Dictionary<string, string> { { "User", "A" }, { "Level", "INFO" } } },
                new LogEntry { AdditionalData = new Dictionary<string, string> { { "Tag", "T1" }, { "Level", "DEBUG" } } }
            };
            await SetLogsToViewModel(_viewModel, logs);

            // Assert
            Assert.AreEqual(3, _viewModel.AvailableAdditionalDataKeys.Count);
            CollectionAssert.Contains(_viewModel.AvailableAdditionalDataKeys.ToList(), "User");
            CollectionAssert.Contains(_viewModel.AvailableAdditionalDataKeys.ToList(), "Level");
            CollectionAssert.Contains(_viewModel.AvailableAdditionalDataKeys.ToList(), "Tag");
        }

        [TestMethod]
        public async Task Initialize_Successful_LoadsLogsAndColumns()
        {
            // Arrange
            var result = new LogDataResult(
                new List<LogEntry> { new LogEntry { Message = "Entry 1" } },
                new List<DisplayColumnConfig> { new DisplayColumnConfig { Header = "Timestamp" } },
                new List<FileState>()
            );

            _mockLogService.Setup(s => s.LoadFromConfig(It.IsAny<string>())).Returns(result);

            // Act
            _viewModel = CreateViewModel();
            await _viewModel.Initialize("dummy_path");

            // Verify no error occurred
            _mockUserDialogService.Verify(s => s.ShowError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            // Assert
            Assert.AreEqual(1, _viewModel.LogEntriesView.Cast<LogEntry>().Count());
            Assert.AreEqual("Entry 1", _viewModel.LogEntriesView.Cast<LogEntry>().First().Message);
            Assert.AreEqual(1, _viewModel.DisplayColumns.Count);

            _mockLogService.Verify(s => s.LoadFromConfig("dummy_path"), Times.Once);
        }

        [TestMethod]
        public async Task Initialize_ConfigLoadFails_DoesNotThrow()
        {
            // Arrange
            _mockLogService.Setup(s => s.LoadFromConfig(It.IsAny<string>())).Returns(new LogDataResult(new List<LogEntry>(), new List<DisplayColumnConfig>(), new List<FileState>()));

            // Act
            _viewModel = CreateViewModel();
            await _viewModel.Initialize("dummy_path");

            // Assert
            Assert.AreEqual(0, _viewModel.LogEntriesView.Cast<LogEntry>().Count());
        }

        [TestMethod]
        public async Task LoadLogs_ShowsErrorDialog_WhenExceptionOccurs()
        {
            // Arrange
            var errorMessage = "Invalid YAML format";
            _mockLogService.Setup(s => s.LoadFromConfig(It.IsAny<string>()))
                .Throws(new System.Exception(errorMessage));

            _viewModel = CreateViewModel();

            // Act
            await _viewModel.Initialize("invalid_config.yaml");

            // Assert
            _mockUserDialogService.Verify(s => s.ShowError("設定エラー", errorMessage), Times.Once);
        }

        [TestMethod]
        public void SavePresetCommand_OpensDialogAndCallsService()
        {
            // Arrange
            _viewModel = CreateViewModel();
            _viewModel.FilterText = "TestFilter";
            _mockUserDialogService.Setup(s => s.SaveFileDialog(It.IsAny<string>(), It.IsAny<string>()))
                .Returns("preset.yaml");

            // Act
            _viewModel.SavePresetCommand.Execute(null);

            // Assert
            _mockUserDialogService.Verify(s => s.SaveFileDialog(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _mockFilterPresetService.Verify(s => s.Save("preset.yaml", It.Is<FilterPreset>(p => p.FilterText == "TestFilter")), Times.Once);
        }

        [TestMethod]
        public void LoadPresetCommand_OpensDialogAndUpdatesViewModel()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var preset = new FilterPreset { FilterText = "LoadedFilter" };
            _mockUserDialogService.Setup(s => s.OpenFileDialog(It.IsAny<string>()))
                .Returns("preset.yaml");
            _mockFilterPresetService.Setup(s => s.Load("preset.yaml"))
                .Returns(preset);

            // Act
            _viewModel.LoadPresetCommand.Execute(null);

            // Assert
            Assert.AreEqual("LoadedFilter", _viewModel.FilterText);
            _mockUserDialogService.Verify(s => s.OpenFileDialog(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void LoadPresetCommand_IgnoresNonExistentKeys()
        {
            // Arrange
            _viewModel = CreateViewModel();
            _viewModel.AvailableAdditionalDataKeys.Add("ExistKey");

            var preset = new FilterPreset
            {
                ExtensionFilters = new List<LogFilter>
                {
                    new LogFilter(FilterType.ColumnEmpty, "ExistKey", default, "Exist"),
                    new LogFilter(FilterType.ColumnEmpty, "NonExistKey", default, "NonExist"),
                    new LogFilter(FilterType.DateTimeAfter, "", DateTime.Now, "Date") // 日時フィルタは常に許可
                }
            };

            _mockUserDialogService.Setup(s => s.OpenFileDialog(It.IsAny<string>())).Returns("preset.yaml");
            _mockFilterPresetService.Setup(s => s.Load("preset.yaml")).Returns(preset);

            // Act
            _viewModel.LoadPresetCommand.Execute(null);

            // Assert
            Assert.AreEqual(2, _viewModel.ActiveExtensionFilters.Count);
            Assert.IsTrue(_viewModel.ActiveExtensionFilters.Any(f => f.Key == "ExistKey"));
            Assert.IsTrue(_viewModel.ActiveExtensionFilters.Any(f => f.Type == FilterType.DateTimeAfter));
            Assert.IsFalse(_viewModel.ActiveExtensionFilters.Any(f => f.Key == "NonExistKey"), "Non-existent key should be ignored.");
        }

        [TestMethod]
        public void FilterText_WhenSet_RaisesPropertyChanged()
        {
            // Arrange
            _viewModel = CreateViewModel();

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
        public async Task FilterText_MatchesAdditionalData()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var logs = new List<LogEntry>
            {
                new LogEntry { Message = "Msg1", AdditionalData = new Dictionary<string, string> { { "Level", "INFO" } } },
                new LogEntry { Message = "Msg2", AdditionalData = new Dictionary<string, string> { { "Level", "ERROR" } } }
            };
            await SetLogsToViewModel(_viewModel, logs);

            // Act
            _viewModel.FilterText = "ERROR";

            // Assert
            var view = _viewModel.LogEntriesView.Cast<LogEntry>().ToList();
            Assert.AreEqual(1, view.Count);
            Assert.AreEqual("Msg2", view[0].Message);
        }

        [TestMethod]
        public async Task FilterText_MatchesFileName()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var logs = new List<LogEntry>
            {
                new LogEntry { Message = "Msg1", FileName = "App.log" },
                new LogEntry { Message = "Msg2", FileName = "Error.log" }
            };
            await SetLogsToViewModel(_viewModel, logs);

            // Act
            _viewModel.FilterText = "Error.log";

            // Assert
            var view = _viewModel.LogEntriesView.Cast<LogEntry>().ToList();
            Assert.AreEqual(1, view.Count);
            Assert.AreEqual("Msg2", view[0].Message);
        }

        [TestMethod]
        public async Task RefreshCommand_ReloadsLogs()
        {
            // Arrange
            _mockLogService.Setup(s => s.LoadFromConfig(It.IsAny<string>()))
                .Returns(new LogDataResult(new List<LogEntry>(), new List<DisplayColumnConfig>(), new List<FileState>()));

            _viewModel = CreateViewModel();
            await _viewModel.Initialize("config.yaml");
            _mockLogService.Invocations.Clear();

            // Act
            _viewModel.RefreshCommand.Execute(null);
            // 同期実行されるため待機不要

            // Assert
            _mockLogService.Verify(s => s.LoadFromConfig("config.yaml"), Times.Once);
        }

        // --- Search Function Tests ---

        [TestMethod]
        public void OpenSearchCommand_ShowsSearchWindow()
        {
            // Arrange
            _viewModel = CreateViewModel();

            // Act
            _viewModel.OpenSearchCommand.Execute(null);

            // Assert
            _mockSearchWindowService.Verify(s => s.Show(It.IsAny<SearchViewModel>()), Times.Once);
        }

        [TestMethod]
        public async Task FindNext_WhenFound_SelectsEntry()
        {
            // Arrange
            _viewModel = CreateViewModel();

            var logs = new List<LogEntry>
            {
                new LogEntry { Message = "Alpha" },
                new LogEntry { Message = "Beta" },
                new LogEntry { Message = "Gamma" }
            };
            await SetLogsToViewModel(_viewModel, logs);

            // Capture the SearchViewModel passed to Show
            SearchViewModel? capturedSearchVM = null;
            _mockSearchWindowService.Setup(s => s.Show(It.IsAny<SearchViewModel>()))
                .Callback<object>(vm => capturedSearchVM = vm as SearchViewModel);

            _viewModel.OpenSearchCommand.Execute(null);
            Assert.IsNotNull(capturedSearchVM);

            capturedSearchVM.SearchText = "Beta";

            // Act
            capturedSearchVM.FindNextCommand.Execute(null);

            // Assert
            Assert.IsNotNull(_viewModel.SelectedLogEntry);
            Assert.AreEqual("Beta", _viewModel.SelectedLogEntry.Message);
        }

        [TestMethod]
        public async Task FindNext_MatchesAdditionalData()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var logs = new List<LogEntry>
            {
                new LogEntry { Message = "Msg1", AdditionalData = new Dictionary<string, string> { { "Level", "INFO" } } },
                new LogEntry { Message = "Msg2", AdditionalData = new Dictionary<string, string> { { "Level", "ERROR" } } },
                new LogEntry { Message = "Msg3", AdditionalData = new Dictionary<string, string> { { "Level", "INFO" } } }
            };
            await SetLogsToViewModel(_viewModel, logs);

            SearchViewModel capturedSearchVM = GetSearchViewModel(_viewModel);
            capturedSearchVM.SearchText = "ERROR";

            // Act
            capturedSearchVM.FindNextCommand.Execute(null);

            // Assert
            Assert.IsNotNull(_viewModel.SelectedLogEntry);
            Assert.AreEqual("Msg2", _viewModel.SelectedLogEntry.Message);
        }

        [TestMethod]
        public async Task FindNext_MatchesFileName()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var logs = new List<LogEntry>
            {
                new LogEntry { Message = "Msg1", FileName = "App.log" },
                new LogEntry { Message = "Msg2", FileName = "Error.log" },
            };
            await SetLogsToViewModel(_viewModel, logs);

            SearchViewModel capturedSearchVM = GetSearchViewModel(_viewModel);
            capturedSearchVM.SearchText = "Error.log";

            // Act
            capturedSearchVM.FindNextCommand.Execute(null);

            // Assert
            Assert.IsNotNull(_viewModel.SelectedLogEntry);
            Assert.AreEqual("Msg2", _viewModel.SelectedLogEntry.Message);
        }

        [TestMethod]
        public async Task FindNext_CaseSensitivity_Respected()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var logs = new List<LogEntry>
            {
                new LogEntry { Message = "apple" },
                new LogEntry { Message = "Apple" },
            };
            await SetLogsToViewModel(_viewModel, logs);

            SearchViewModel capturedSearchVM = GetSearchViewModel(_viewModel);
            capturedSearchVM.SearchText = "Apple";
            capturedSearchVM.IsCaseSensitive = true;

            // Act
            capturedSearchVM.FindNextCommand.Execute(null);

            // Assert
            Assert.IsNotNull(_viewModel.SelectedLogEntry);
            Assert.AreEqual("Apple", _viewModel.SelectedLogEntry.Message, "Should skip 'apple' and find 'Apple' when case sensitive.");
        }

        [TestMethod]
        public async Task FindNext_InvalidRegex_DoesNotCrash()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var logs = new List<LogEntry> { new LogEntry { Message = "Test" } };
            await SetLogsToViewModel(_viewModel, logs);

            SearchViewModel capturedSearchVM = GetSearchViewModel(_viewModel);
            capturedSearchVM.SearchText = "[Invalid Regex"; // Missing closing bracket
            capturedSearchVM.IsRegex = true;

            // Act
            try
            {
                capturedSearchVM.FindNextCommand.Execute(null);
            }
            catch (System.Exception ex)
            {
                Assert.Fail($"Should not throw exception on invalid regex: {ex.Message}");
            }

            // Assert
            // Should just not match anything, keeping selection null or same
            // (In this test setup, expected behavior is silent failure/no move)
        }

        [TestMethod]
        public async Task FindNext_UpdatesStatusText()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var logs = new List<LogEntry>
            {
                new LogEntry { Message = "Target" },
                new LogEntry { Message = "Target" },
                new LogEntry { Message = "Target" }
            };
            await SetLogsToViewModel(_viewModel, logs);

            SearchViewModel capturedSearchVM = GetSearchViewModel(_viewModel);
            capturedSearchVM.SearchText = "Target";

            // Act
            // Trigger search via property change (simulated by setting text above)
            // But MainViewModel subscribes to PropertyChanged which is triggered by setter.
            // Wait, GetSearchViewModel uses OpenSearchCommand, which creates the VM and subscribes.
            // So setting SearchText above should trigger UpdateSearchStatus.

            // Assert
            // Initially, since no item is selected, it might say "?/3" or "0/3" depending on implementation.
            // In my impl: if currentFound is false, it shows "?/3".
            Assert.AreEqual("?/3", capturedSearchVM.StatusText);

            // Act 2: Find Next (Selects first one)
            capturedSearchVM.FindNextCommand.Execute(null);

            // Assert 2: First item selected, so "1/3"
            Assert.AreEqual("1/3", capturedSearchVM.StatusText);

            // Act 3: Find Next again (Selects second one)
            capturedSearchVM.FindNextCommand.Execute(null);
            Assert.AreEqual("2/3", capturedSearchVM.StatusText);
        }

        [TestMethod]
        public async Task FindNext_WrapsAround()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var logs = new List<LogEntry>
            {
                new LogEntry { Message = "Target" },
                new LogEntry { Message = "Other" },
                new LogEntry { Message = "Target" }
            };
            await SetLogsToViewModel(_viewModel, logs);

            SearchViewModel capturedSearchVM = GetSearchViewModel(_viewModel);
            capturedSearchVM.SearchText = "Target";

            // Select last item
            _viewModel.SelectedLogEntry = logs[2];

            // Act
            capturedSearchVM.FindNextCommand.Execute(null);

            // Assert
            Assert.IsNotNull(_viewModel.SelectedLogEntry);
            Assert.AreEqual(logs[0], _viewModel.SelectedLogEntry, "Should wrap around to the first item.");
        }

        [TestMethod]
        public async Task FindPrevious_WhenFound_SelectsEntry()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var logs = new List<LogEntry>
            {
                new LogEntry { Message = "Target" }, // 0
                new LogEntry { Message = "Other" },  // 1
                new LogEntry { Message = "Target" }  // 2
            };
            await SetLogsToViewModel(_viewModel, logs);

            SearchViewModel capturedSearchVM = GetSearchViewModel(_viewModel);
            capturedSearchVM.SearchText = "Target";

            // Select middle item
            _viewModel.SelectedLogEntry = logs[1];

            // Act
            capturedSearchVM.FindPreviousCommand.Execute(null);

            // Assert
            Assert.AreEqual(logs[0], _viewModel.SelectedLogEntry, "Should find the previous target.");
        }

        [TestMethod]
        public async Task GoToDateCommand_AbsoluteJump_UpdatesSelection()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var targetTime = new DateTime(2023, 12, 25, 10, 30, 0);
            var targetEntry = new LogEntry { Timestamp = targetTime, Message = "Target" };
            // Ensure logs are sorted ascending by timestamp for binary search
            var logs = new List<LogEntry> { targetEntry, new LogEntry { Timestamp = DateTime.Now } };
            await SetLogsToViewModel(_viewModel, logs);

            Action<DateTime> capturedOnJump = null!;
            _mockGoToDateDialogService.Setup(s => s.Show(It.IsAny<GoToDateViewModel>(), It.IsAny<Action<DateTime>>()))
                .Callback<GoToDateViewModel, Action<DateTime>>((vm, onJump) => capturedOnJump = onJump);

            // Act
            _viewModel.GoToDateCommand.Execute(null);
            capturedOnJump(targetTime);

            // Assert
            Assert.AreEqual(targetEntry, _viewModel.SelectedLogEntry);
        }

        [TestMethod]
        public async Task ToggleBookmarkCommand_TogglesSelection()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var entry = new LogEntry { Message = "Test" };
            var logs = new List<LogEntry> { entry };
            await SetLogsToViewModel(_viewModel, logs);
            _viewModel.SelectedLogEntry = entry;

            // Act 1: Mark
            _viewModel.ToggleBookmarkCommand.Execute(null);
            // Assert 1
            Assert.IsTrue(entry.IsBookmarked, "Should be marked");

            // Act 2: Unmark
            _viewModel.ToggleBookmarkCommand.Execute(null);
            // Assert 2
            Assert.IsFalse(entry.IsBookmarked, "Should be unmarked");
        }

        [TestMethod]
        public async Task FindNextBookmarkCommand_SelectsNextMarkedEntry()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var logs = new List<LogEntry>
            {
                new LogEntry { Message = "1", IsBookmarked = true },
                new LogEntry { Message = "2" },
                new LogEntry { Message = "3", IsBookmarked = true }
            };
            await SetLogsToViewModel(_viewModel, logs);
            _viewModel.SelectedLogEntry = logs[0];

            // Act
            _viewModel.NextBookmarkCommand.Execute(null);

            // Assert
            Assert.AreEqual(logs[2], _viewModel.SelectedLogEntry, "Should jump to the next bookmarked entry.");

            // Act 2: Wrap around
            _viewModel.NextBookmarkCommand.Execute(null);
            Assert.AreEqual(logs[0], _viewModel.SelectedLogEntry, "Should wrap around to the first bookmarked entry.");
        }

        [TestMethod]
        public async Task FindPreviousBookmarkCommand_SelectsPreviousMarkedEntry()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var logs = new List<LogEntry>
            {
                new LogEntry { Message = "1", IsBookmarked = true },
                new LogEntry { Message = "2" },
                new LogEntry { Message = "3", IsBookmarked = true }
            };
            await SetLogsToViewModel(_viewModel, logs);
            _viewModel.SelectedLogEntry = logs[2];

            // Act
            _viewModel.PreviousBookmarkCommand.Execute(null);

            // Assert
            Assert.AreEqual(logs[0], _viewModel.SelectedLogEntry, "Should jump to the previous bookmarked entry.");
        }

        [TestMethod]
        public async Task ClearBookmarksCommand_RemovesAllMarks()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var logs = new List<LogEntry>
            {
                new LogEntry { Message = "1", IsBookmarked = true },
                new LogEntry { Message = "2", IsBookmarked = true }
            };
            await SetLogsToViewModel(_viewModel, logs);

            // Act
            _viewModel.ClearBookmarksCommand.Execute(null);

            // Assert
            Assert.IsFalse(logs.Any(l => l.IsBookmarked), "All bookmarks should be cleared.");
        }

        [TestMethod]
        public async Task BookmarkOnlyFilter_WhenActive_HidesUnmarkedEntries()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var logs = new List<LogEntry>
            {
                new LogEntry { Message = "Marked", IsBookmarked = true },
                new LogEntry { Message = "Unmarked", IsBookmarked = false }
            };
            await SetLogsToViewModel(_viewModel, logs);

            // Act
            _viewModel.AddBookmarkFilterCommand.Execute(null);

            // Assert
            var view = _viewModel.LogEntriesView.Cast<LogEntry>().ToList();
            Assert.AreEqual(1, view.Count);
            Assert.AreEqual("Marked", view[0].Message);

            // Act 2: Disable filter (remove from active filters)
            var filter = _viewModel.ActiveExtensionFilters.First(f => f.Type == FilterType.Bookmark);
            _viewModel.RemoveExtensionFilterCommand.Execute(filter);
            Assert.AreEqual(2, _viewModel.LogEntriesView.Cast<LogEntry>().Count());
        }

        [TestMethod]
        public async Task AddBookmarkFilterCommand_AddsFilterExactlyOnce()
        {
            // Arrange
            _viewModel = CreateViewModel();
            await SetLogsToViewModel(_viewModel, new List<LogEntry>());

            // Act
            _viewModel.AddBookmarkFilterCommand.Execute(null);
            _viewModel.AddBookmarkFilterCommand.Execute(null); // 2回実行

            // Assert
            var bookmarkFilters = _viewModel.ActiveExtensionFilters.Where(f => f.Type == FilterType.Bookmark).ToList();
            Assert.AreEqual(1, bookmarkFilters.Count, "Should add only one bookmark filter even if executed multiple times.");
            Assert.AreEqual("Bookmark", bookmarkFilters[0].DisplayText);
        }

        private async Task SetLogsToViewModel(MainViewModel vm, List<LogEntry> logs)
        {
            var result = new LogDataResult(logs, new List<DisplayColumnConfig>(), new List<FileState>());
            _mockLogService.Setup(s => s.LoadFromConfig(It.IsAny<string>())).Returns(result);

            await vm.Initialize("dummy");
        }

        private SearchViewModel GetSearchViewModel(MainViewModel vm)
        {
            SearchViewModel? captured = null;
            _mockSearchWindowService.Setup(s => s.Show(It.IsAny<SearchViewModel>()))
                .Callback<object>(obj => captured = obj as SearchViewModel);

            vm.OpenSearchCommand.Execute(null);
            return captured!;
        }
    }
}
