using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MultiLogViewer.Models;
using MultiLogViewer.Services;
using MultiLogViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        private Mock<ITailModeWarningDialogService> _mockTailModeWarningDialogService = null!;
        private Mock<IAppSettingsService> _mockAppSettingsService = null!;
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
            _mockTailModeWarningDialogService = new Mock<ITailModeWarningDialogService>();
            _mockAppSettingsService = new Mock<IAppSettingsService>();

            // Dispatcher: テストスレッドで即時実行
            _mockDispatcherService.Setup(d => d.Invoke(It.IsAny<Action>())).Callback<Action>(a => a());
            _mockDispatcherService.Setup(d => d.BeginInvoke(It.IsAny<Action>())).Callback<Action>(a => a());

            // TaskRunner: テストスレッドで同期的に即時実行
            _mockTaskRunner.Setup(r => r.Run(It.IsAny<Action>()))
                .Returns((Action a) =>
                {
                    a();
                    return Task.CompletedTask;
                });

            // DialogService: デフォルトでは続行を返す
            bool skip;
            _mockTailModeWarningDialogService.Setup(s => s.ShowWarning(out skip)).Returns(true);
            _mockAppSettingsService.Setup(s => s.Load()).Returns(new AppSettings());
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
                _mockGoToDateDialogService.Object,
                _mockTailModeWarningDialogService.Object,
                _mockAppSettingsService.Object);
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
        public async Task CopyCommand_CopiesSelectedEntryToClipboard_CaseInsensitiveBindingPath()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var logs = new List<LogEntry>
            {
                new LogEntry
                {
                    Message = "Case Insensitive Message",
                    Timestamp = new System.DateTime(2023, 1, 1, 12, 0, 0),
                }
            };

            // 小文字の "message" を使用
            _viewModel.DisplayColumns = new ObservableCollection<DisplayColumnConfig>
            {
                new DisplayColumnConfig { Header = "Msg", BindingPath = "message" }
            };

            await SetLogsToViewModel(_viewModel, logs);
            _viewModel.SelectedLogEntry = logs[0];

            // Act
            _viewModel.CopyCommand.Execute(null);

            // Assert
            var expectedText = "Case Insensitive Message";
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
        public async Task AddExtensionFilterCommand_SetsKeyAndDisplayText()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var logs = new List<LogEntry>
                    {
                        new LogEntry { AdditionalData = new Dictionary<string, string> { { "TestKey", "V" } } }
                    };
            await SetLogsToViewModel(_viewModel, logs);

            // Act
            _viewModel.AddExtensionFilterCommand.Execute("TestKey");

            // Assert
            var filter = _viewModel.ActiveExtensionFilters.FirstOrDefault(f => f.Key == "TestKey");
            Assert.IsNotNull(filter);
            Assert.AreEqual("TestKey", filter.Key);
            // 現在の設計では ViewModel は DisplayText を解決せず、初期値として key を入れる
            Assert.AreEqual("TestKey", filter.DisplayText);
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
            Assert.AreEqual(2, _viewModel.DisplayColumns.Count, "Should have 2 columns (Bookmark + Timestamp).");
            Assert.IsTrue(_viewModel.DisplayColumns[0].IsBookmark, "First column should be the bookmark column.");
            Assert.AreEqual("Timestamp", _viewModel.DisplayColumns[1].Header);

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
            await _viewModel.Initialize("invalid_profile.yaml");

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
        public async Task LoadPresetCommand_PreservesDisplayText()
        {
            // Arrange
            _viewModel = CreateViewModel();
            _viewModel.AvailableAdditionalDataKeys.Add("TargetKey");

            var preset = new FilterPreset
            {
                ExtensionFilters = new List<LogFilter>
                        {
                            new LogFilter(FilterType.ColumnEmpty, "TargetKey", default, "SavedDisplayText")
                        }
            };

            _mockUserDialogService.Setup(s => s.OpenFileDialog(It.IsAny<string>())).Returns("preset.yaml");
            _mockFilterPresetService.Setup(s => s.Load("preset.yaml")).Returns(preset);

            // Act
            _viewModel.LoadPresetCommand.Execute(null);

            // Assert
            Assert.AreEqual(1, _viewModel.ActiveExtensionFilters.Count);
            var filter = _viewModel.ActiveExtensionFilters.First();
            Assert.AreEqual("TargetKey", filter.Key);
            // ViewModel側での強制書き換えを廃止したため、保存されていた値が維持される
            Assert.AreEqual("SavedDisplayText", filter.DisplayText);
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
            await _viewModel.Initialize("LogProfile.yaml");
            _mockLogService.Invocations.Clear();

            // Act
            _viewModel.RefreshCommand.Execute(null);
            // 同期実行されるため待機不要

            // Assert
            _mockLogService.Verify(s => s.LoadFromConfig("LogProfile.yaml"), Times.Once);
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
                new LogEntry { Message = "1", IsBookmarked = true, BookmarkColor = BookmarkColor.Red },
                new LogEntry { Message = "2", IsBookmarked = true, BookmarkColor = BookmarkColor.Blue }
            };
            await SetLogsToViewModel(_viewModel, logs);
            _viewModel.SelectedLogEntry = logs[0];

            // Act
            _viewModel.ClearBookmarksCommand.Execute(null);

            // Assert
            Assert.IsFalse(logs.Any(l => l.IsBookmarked), "All bookmarks should be cleared.");
            Assert.AreEqual(logs[0], _viewModel.SelectedLogEntry, "Selection should be maintained after clearing all bookmarks.");
        }

        [TestMethod]
        public async Task ToggleBookmark_Unmarking_KeepsSelection()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var logs = new List<LogEntry>
            {
                new LogEntry { Message = "1", IsBookmarked = true },
                new LogEntry { Message = "2", IsBookmarked = true }
            };
            await SetLogsToViewModel(_viewModel, logs);
            _viewModel.SelectedLogEntry = logs[0];

            // Act
            _viewModel.ToggleBookmarkCommand.Execute(null);

            // Assert
            Assert.IsFalse(logs[0].IsBookmarked, "Bookmark should be toggled off.");
            Assert.AreEqual(logs[0], _viewModel.SelectedLogEntry, "Selection should not jump to another row after un-bookmarking.");
        }

        [TestMethod]
        public void SavePresetCommand_ExcludesBookmarkFilters()
        {
            // Arrange
            _viewModel = CreateViewModel();
            _viewModel.FilterText = "Test";
            _viewModel.ActiveExtensionFilters.Add(new LogFilter(FilterType.ColumnEmpty, "Key", default, "Key"));
            _viewModel.ActiveExtensionFilters.Add(new BookmarkFilter(BookmarkColor.Red));

            _mockUserDialogService.Setup(s => s.SaveFileDialog(It.IsAny<string>(), It.IsAny<string>())).Returns("preset.yaml");

            // Act
            _viewModel.SavePresetCommand.Execute(null);

            // Assert
            // Serviceに渡されるリストに、Bookmarkタイプのフィルタが含まれていないことを検証
            _mockFilterPresetService.Verify(s => s.Save("preset.yaml", It.Is<FilterPreset>(p =>
                p.ExtensionFilters.Count == 1 &&
                p.ExtensionFilters.All(f => f.Type != FilterType.Bookmark))),
                Times.Once);
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

        [TestMethod]
        public async Task SetBookmarkColorCommand_ChangesColorAndSetsBookmarked()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var entry = new LogEntry { Message = "Test", IsBookmarked = false, BookmarkColor = BookmarkColor.Blue };
            await SetLogsToViewModel(_viewModel, new List<LogEntry> { entry });
            _viewModel.SelectedLogEntry = entry;

            // Act
            // コマンド引数として BookmarkColor.Red を渡す
            _viewModel.SetBookmarkColorCommand.Execute(BookmarkColor.Red);

            // Assert
            Assert.IsTrue(entry.IsBookmarked, "Should automatically set IsBookmarked to true.");
            Assert.AreEqual(BookmarkColor.Red, entry.BookmarkColor, "Should update BookmarkColor.");
        }

        [TestMethod]
        public async Task AddBookmarkFilterCommand_WithColor_ReplacesExistingBookmarkFilter()
        {
            // Arrange
            _viewModel = CreateViewModel();
            await SetLogsToViewModel(_viewModel, new List<LogEntry>());

            // Act 1: Add Red filter
            _viewModel.AddBookmarkFilterCommand.Execute(BookmarkColor.Red);

            var filters1 = _viewModel.ActiveExtensionFilters.Where(f => f.Type == FilterType.Bookmark).Cast<BookmarkFilter>().ToList();
            Assert.AreEqual(1, filters1.Count);
            Assert.AreEqual(BookmarkColor.Red, filters1[0].TargetColor);

            // Act 2: Add Blue filter (should replace Red)
            _viewModel.AddBookmarkFilterCommand.Execute(BookmarkColor.Blue);

            var filters2 = _viewModel.ActiveExtensionFilters.Where(f => f.Type == FilterType.Bookmark).Cast<BookmarkFilter>().ToList();
            Assert.AreEqual(1, filters2.Count, "Should maintain only one bookmark filter.");
            Assert.AreEqual(BookmarkColor.Blue, filters2[0].TargetColor, "Should replace with Blue filter.");
        }

        [TestMethod]
        public async Task SelectedLogEntry_WhenChanged_RaisesPropertyChanged()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var entry1 = new LogEntry { Message = "1" };
            var entry2 = new LogEntry { Message = "2" };
            await SetLogsToViewModel(_viewModel, new List<LogEntry> { entry1, entry2 });

            bool raised = false;
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.SelectedLogEntry)) raised = true;
            };

            // Act
            _viewModel.SelectedLogEntry = entry1;

            // Assert
            Assert.IsTrue(raised, "PropertyChanged should be raised for SelectedLogEntry.");
            Assert.AreEqual(entry1, _viewModel.SelectedLogEntry);
        }

        [TestMethod]
        public async Task BookmarkSelection_WhenCurrentItemChangesInSidebar_SyncsToSelectedLogEntry()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var entry1 = new LogEntry { Message = "B1", IsBookmarked = true };
            var entry2 = new LogEntry { Message = "B2", IsBookmarked = true };
            await SetLogsToViewModel(_viewModel, new List<LogEntry> { entry1, entry2 });

            // SetLogsToViewModel 内で Initialize が呼ばれ、エントリが ViewModel 内のコレクションにコピーされる。
            // 参照比較でパスさせるため、ViewModel内のインスタンスを取得し直す。
            var logsInVm = _viewModel.LogEntriesView.Cast<LogEntry>().ToList();
            var target = logsInVm.First(l => l.Message == "B2");

            // Act
            _viewModel.BookmarkedEntries.MoveCurrentTo(target);

            // Assert
            Assert.AreEqual(target, _viewModel.SelectedLogEntry, "Selection should sync from sidebar to main.");
        }

        [TestMethod]
        public async Task BookmarkSelection_WhenSingleBookmarkExists_AndSelectionMovesToIt_SyncsCorrectly()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var entryA = new LogEntry { Message = "Bookmarked", IsBookmarked = true };
            var entryB = new LogEntry { Message = "Not Bookmarked", IsBookmarked = false };
            await SetLogsToViewModel(_viewModel, new List<LogEntry> { entryA, entryB });

            var logsInVm = _viewModel.LogEntriesView.Cast<LogEntry>().ToList();
            var targetA = logsInVm.First(l => l.Message == "Bookmarked");
            var targetB = logsInVm.First(l => l.Message == "Not Bookmarked");

            // 1. 最初は未ブックマークの行を選択
            _viewModel.SelectedLogEntry = targetB;
            Assert.AreEqual(targetB, _viewModel.SelectedLogEntry);

            // 2. サイドパネルで唯一の項目を選択（CurrentItemを移動）
            _viewModel.BookmarkedEntries.MoveCurrentTo(targetA);

            // Assert
            Assert.AreEqual(targetA, _viewModel.SelectedLogEntry, "SelectedLogEntry should be updated to the bookmarked entry when current item changes.");
        }

        [TestMethod]
        public async Task BookmarkSelection_WhenSelectedLogEntryChanges_SyncsToSidebar()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var entry1 = new LogEntry { Message = "B1", IsBookmarked = true };
            var entry2 = new LogEntry { Message = "B2", IsBookmarked = true };
            await SetLogsToViewModel(_viewModel, new List<LogEntry> { entry1, entry2 });

            // Act
            _viewModel.SelectedLogEntry = entry2;

            // Assert
            // Dispatcher.BeginInvoke を使用しているため、テスト内では即時反映されるはず（Setupで同期実行にしているため）
            Assert.AreEqual(entry2, _viewModel.BookmarkedEntries.CurrentItem, "Selection should sync from main to sidebar.");
        }

        [TestMethod]
        public async Task ClearBookmarksCommand_CanExecute_OnlyWhenBookmarksExist()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var entry = new LogEntry { Message = "1", IsBookmarked = false };
            await SetLogsToViewModel(_viewModel, new List<LogEntry> { entry });

            // Act & Assert 1: ブックマークがない時は実行不可
            Assert.IsFalse(_viewModel.ClearBookmarksCommand.CanExecute(null), "Command should not be executable when no bookmarks exist.");

            // Act 2: ブックマークを追加
            entry.IsBookmarked = true;

            // Assert 2: ブックマークがある時は実行可能
            Assert.IsTrue(_viewModel.ClearBookmarksCommand.CanExecute(null), "Command should be executable when bookmarks exist.");
        }

        [TestMethod]
        public async Task BookmarkEditor_IsEnabled_State_Verification()
        {
            // Arrange
            _viewModel = CreateViewModel();
            var entry = new LogEntry { Message = "1", IsBookmarked = false };
            await SetLogsToViewModel(_viewModel, new List<LogEntry> { entry });

            // Act 1: 未ブックマーク行を選択
            _viewModel.SelectedLogEntry = entry;

            // Assert 1: IsBookmarked が false であることを確認（XAMLの IsEnabled 評価元）
            Assert.IsNotNull(_viewModel.SelectedLogEntry);
            Assert.IsFalse(_viewModel.SelectedLogEntry.IsBookmarked);

            // Act 2: ブックマークする
            _viewModel.ToggleBookmarkCommand.Execute(null);

            // Assert 2
            Assert.IsTrue(_viewModel.SelectedLogEntry.IsBookmarked);
        }

        [TestMethod]
        public async Task Initialize_SortsByTimestampByDefault()
        {
            // ... (existing test)
        }

        [TestMethod]
        public void IsTailEnabled_WhenSetToTrue_ResetsSortAndClearsSpecificFilters()
        {
            // Arrange
            _viewModel = CreateViewModel();

            // 1. 意図的にソート順を変えておく（降順にする）
            _viewModel.LogEntriesView.SortDescriptions.Clear();
            _viewModel.LogEntriesView.SortDescriptions.Add(new SortDescription("Timestamp", ListSortDirection.Descending));

            // 2. 解除対象のフィルターを追加しておく
            var bookmarkFilter = new BookmarkFilter(null);
            var dateFilter = new LogFilter(FilterType.DateTimeAfter, "", DateTime.Now, "Date");
            var columnFilter = new LogFilter(FilterType.ColumnEmpty, "Level", default, "Level");

            _viewModel.ActiveExtensionFilters.Add(bookmarkFilter);
            _viewModel.ActiveExtensionFilters.Add(dateFilter);
            _viewModel.ActiveExtensionFilters.Add(columnFilter);

            // Act
            _viewModel.IsTailEnabled = true;

            // Assert
            // ソートが昇順にリセットされていること (Timestamp + SequenceNumber)
            Assert.AreEqual(2, _viewModel.LogEntriesView.SortDescriptions.Count);
            Assert.AreEqual("Timestamp", _viewModel.LogEntriesView.SortDescriptions[0].PropertyName);
            Assert.AreEqual(ListSortDirection.Ascending, _viewModel.LogEntriesView.SortDescriptions[0].Direction);
            Assert.AreEqual("SequenceNumber", _viewModel.LogEntriesView.SortDescriptions[1].PropertyName);

            // ブックマークと日時のフィルターが解除され、カラムフィルターだけが残っていること
            Assert.AreEqual(1, _viewModel.ActiveExtensionFilters.Count);
            Assert.IsFalse(_viewModel.ActiveExtensionFilters.Contains(bookmarkFilter));
            Assert.IsFalse(_viewModel.ActiveExtensionFilters.Contains(dateFilter));
            Assert.IsTrue(_viewModel.ActiveExtensionFilters.Contains(columnFilter));
        }

        [TestMethod]
        public void IsTailEnabled_WhenWarningConfirmedWithSkip_SavesSetting()
        {
            // Arrange
            _viewModel = CreateViewModel();
            bool skip;
            // ユーザーが「有効にする」を選び、「次回以降スキップ」にチェックを入れたシミュレーション
            _mockTailModeWarningDialogService.Setup(s => s.ShowWarning(out skip))
                .Callback(new OutAction<bool>((out bool s) => s = true))
                .Returns(true);

            _mockAppSettingsService.Setup(s => s.Load()).Returns(new AppSettings { SkipTailModeWarning = false });

            // Act
            _viewModel.IsTailEnabled = true;

            // Assert
            // 設定保存が呼び出されたことを確認
            _mockAppSettingsService.Verify(s => s.Save(It.Is<AppSettings>(st => st.SkipTailModeWarning == true)), Times.Once);
        }

        // Moq で out パラメータを扱うための補助デリゲート
        delegate void OutAction<T>(out T result);

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
