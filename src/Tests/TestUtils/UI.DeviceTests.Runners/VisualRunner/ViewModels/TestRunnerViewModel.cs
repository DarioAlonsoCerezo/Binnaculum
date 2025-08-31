using System.Collections.ObjectModel;
using System.Windows.Input;
using Binnaculum.UI.DeviceTests.Runners.VisualRunner.Services;

namespace Binnaculum.UI.DeviceTests.Runners.VisualRunner.ViewModels;

/// <summary>
/// Main view model for the test runner, coordinating test discovery, execution, and results.
/// </summary>
public class TestRunnerViewModel : BaseViewModel
{
    private readonly ILogger<TestRunnerViewModel>? _logger;
    private readonly VisualDeviceRunner _deviceRunner;
    private string _searchText = string.Empty;
    private bool _isDiscovering = false;
    private bool _isRunning = false;
    private double _progress = 0;
    private string _statusMessage = "Ready";
    private TestRunnerState _state = TestRunnerState.Ready;
    private CancellationTokenSource? _cancellationTokenSource;

    public TestRunnerViewModel(ILogger<TestRunnerViewModel>? logger = null)
    {
        _logger = logger;
        // Create device runner with a compatible logger
        _deviceRunner = new VisualDeviceRunner(CreateCompatibleLogger(logger));
        TestAssemblies = new ObservableCollection<TestAssemblyViewModel>();
        
        // Initialize commands
        RefreshTestsCommand = new Command(async () => await RefreshTestsAsync());
        RunSelectedTestsCommand = new Command(async () => await RunSelectedTestsAsync(), CanRunTests);
        RunAllTestsCommand = new Command(async () => await RunAllTestsAsync(), CanRunTests);
        StopTestsCommand = new Command(async () => await StopTestsAsync(), CanStopTests);
        SelectAllCommand = new Command(() => SelectAllTests(true));
        UnselectAllCommand = new Command(() => SelectAllTests(false));
        
        _logger?.LogInformation("TestRunnerViewModel initialized");
    }

    private static ILogger<VisualDeviceRunner>? CreateCompatibleLogger(ILogger<TestRunnerViewModel>? logger)
    {
        // For now, just return null as the logger is optional
        // In a real implementation, you'd use a proper logger factory
        return null;
    }

    #region Properties

    public ObservableCollection<TestAssemblyViewModel> TestAssemblies { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                OnPropertyChanged(nameof(FilteredTestAssemblies));
            }
        }
    }

    public bool IsDiscovering
    {
        get => _isDiscovering;
        set => SetProperty(ref _isDiscovering, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (SetProperty(ref _isRunning, value))
            {
                ((Command)RunSelectedTestsCommand).ChangeCanExecute();
                ((Command)RunAllTestsCommand).ChangeCanExecute();
                ((Command)StopTestsCommand).ChangeCanExecute();
            }
        }
    }

    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public TestRunnerState State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }

    public ObservableCollection<TestAssemblyViewModel> FilteredTestAssemblies
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return TestAssemblies;

            var filtered = new ObservableCollection<TestAssemblyViewModel>();
            foreach (var assembly in TestAssemblies)
            {
                var filteredAssembly = new TestAssemblyViewModel
                {
                    Name = assembly.Name,
                    FullPath = assembly.FullPath
                };

                foreach (var testClass in assembly.TestClasses)
                {
                    var filteredClass = new TestClassViewModel
                    {
                        Name = testClass.Name,
                        FullName = testClass.FullName
                    };

                    foreach (var testCase in testClass.TestCases)
                    {
                        if (testCase.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                            testCase.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                            testCase.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                        {
                            filteredClass.TestCases.Add(testCase);
                        }
                    }

                    if (filteredClass.TestCases.Count > 0)
                    {
                        filteredAssembly.TestClasses.Add(filteredClass);
                    }
                }

                if (filteredAssembly.TestClasses.Count > 0)
                {
                    filteredAssembly.UpdateTestCounts();
                    filtered.Add(filteredAssembly);
                }
            }

            return filtered;
        }
    }

    #endregion

    #region Commands

    public ICommand RefreshTestsCommand { get; }
    public ICommand RunSelectedTestsCommand { get; }
    public ICommand RunAllTestsCommand { get; }
    public ICommand StopTestsCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand UnselectAllCommand { get; }

    #endregion

    #region Public Methods

    public async Task RefreshTestsAsync()
    {
        try
        {
            IsDiscovering = true;
            StatusMessage = "Discovering tests...";
            State = TestRunnerState.Discovering;

            _logger?.LogInformation("Starting test discovery");

            // Clear existing tests
            TestAssemblies.Clear();

            // Discover tests using the actual test discovery service
            var discoveredAssemblies = await _deviceRunner.DiscoverTestsAsync();
            
            foreach (var assembly in discoveredAssemblies)
            {
                TestAssemblies.Add(assembly);
            }

            var totalTestCount = GetTotalTestCount();
            StatusMessage = totalTestCount > 0 ? $"Discovered {totalTestCount} tests" : "No tests found";
            State = TestRunnerState.Ready;

            // Refresh the filtered view
            OnPropertyChanged(nameof(FilteredTestAssemblies));

            _logger?.LogInformation($"Test discovery completed. Found {totalTestCount} tests");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during test discovery");
            StatusMessage = $"Discovery failed: {ex.Message}";
            State = TestRunnerState.Error;
        }
        finally
        {
            IsDiscovering = false;
        }
    }

    public async Task RunSelectedTestsAsync()
    {
        var selectedTests = GetSelectedTests();
        if (!selectedTests.Any())
        {
            StatusMessage = "No tests selected";
            return;
        }

        await ExecuteTestsAsync(selectedTests);
    }

    public async Task RunAllTestsAsync()
    {
        SelectAllTests(true);
        await RunSelectedTestsAsync();
    }

    public Task StopTestsAsync()
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            StatusMessage = "Stopping tests...";
            _logger?.LogInformation("Test execution cancellation requested");
        }
        return Task.CompletedTask;
    }

    #endregion

    #region Private Methods

    private bool CanRunTests() => !IsRunning && !IsDiscovering && GetSelectedTestCount() > 0;
    private bool CanStopTests() => IsRunning;

    private async Task ExecuteTestsAsync(IEnumerable<TestCaseViewModel> selectedTests)
    {
        try
        {
            IsRunning = true;
            State = TestRunnerState.Running;
            Progress = 0;
            _cancellationTokenSource = new CancellationTokenSource();

            var progress = new Progress<TestExecutionProgress>(OnTestExecutionProgress);
            
            _logger?.LogInformation($"Starting execution of {selectedTests.Count()} selected tests");

            var results = await _deviceRunner.ExecuteTestsAsync(
                selectedTests,
                progress,
                _cancellationTokenSource.Token);

            // Update final status
            StatusMessage = $"Execution completed: {results.PassedCount} passed, {results.FailedCount} failed, {results.SkippedCount} skipped";
            State = TestRunnerState.Completed;
            Progress = 1.0;

            _logger?.LogInformation($"Test execution completed. Results: {results.PassedCount} passed, {results.FailedCount} failed, {results.SkippedCount} skipped");
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Test execution cancelled";
            State = TestRunnerState.Ready;
            _logger?.LogInformation("Test execution was cancelled");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during test execution");
            StatusMessage = $"Execution failed: {ex.Message}";
            State = TestRunnerState.Error;
        }
        finally
        {
            IsRunning = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            
            // Update test counts
            foreach (var assembly in TestAssemblies)
            {
                assembly.UpdateTestCounts();
            }
        }
    }

    private void OnTestExecutionProgress(TestExecutionProgress progress)
    {
        Progress = progress.Progress;
        StatusMessage = progress.StatusMessage;
    }

    private void SelectAllTests(bool isSelected)
    {
        foreach (var assembly in TestAssemblies)
        {
            assembly.IsSelected = isSelected;
        }
    }

    private int GetTotalTestCount()
    {
        return TestAssemblies.Sum(a => a.TotalTests);
    }

    private int GetSelectedTestCount()
    {
        return TestAssemblies.Sum(a => a.TestClasses.Sum(c => c.TestCases.Count(tc => tc.IsSelected)));
    }

    private List<TestCaseViewModel> GetSelectedTests()
    {
        return TestAssemblies
            .SelectMany(a => a.TestClasses)
            .SelectMany(c => c.TestCases)
            .Where(tc => tc.IsSelected)
            .ToList();
    }

    #endregion
}

/// <summary>
/// Enumeration of test runner states.
/// </summary>
public enum TestRunnerState
{
    Ready,
    Discovering,
    Running,
    Completed,
    Error
}