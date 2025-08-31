using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Binnaculum.UI.DeviceTests.Runners.VisualRunner.ViewModels;

/// <summary>
/// Main view model for the test runner, coordinating test discovery, execution, and results.
/// </summary>
public class TestRunnerViewModel : BaseViewModel
{
    private readonly ILogger<TestRunnerViewModel>? _logger;
    private string _searchText = string.Empty;
    private bool _isDiscovering = false;
    private bool _isRunning = false;
    private double _progress = 0;
    private string _statusMessage = "Ready";
    private TestRunnerState _state = TestRunnerState.Ready;

    public TestRunnerViewModel(ILogger<TestRunnerViewModel>? logger = null)
    {
        _logger = logger;
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
                            testCase.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
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

            // TODO: Implement actual test discovery
            // For now, create some sample test data
            await Task.Delay(1000); // Simulate discovery time

            var sampleAssembly = CreateSampleTestAssembly();
            TestAssemblies.Add(sampleAssembly);

            StatusMessage = $"Discovered {GetTotalTestCount()} tests";
            State = TestRunnerState.Ready;

            _logger?.LogInformation($"Test discovery completed. Found {GetTotalTestCount()} tests");
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

    #endregion

    #region Private Methods

    private bool CanRunTests() => !IsRunning && GetSelectedTestCount() > 0;
    private bool CanStopTests() => IsRunning;

    private async Task RunSelectedTestsAsync()
    {
        // TODO: Implement test execution
        await Task.Delay(100);
    }

    private async Task RunAllTestsAsync()
    {
        SelectAllTests(true);
        await RunSelectedTestsAsync();
    }

    private async Task StopTestsAsync()
    {
        // TODO: Implement test stopping
        await Task.Delay(100);
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

    private TestAssemblyViewModel CreateSampleTestAssembly()
    {
        var assembly = new TestAssemblyViewModel
        {
            Name = "UI.DeviceTests.dll",
            FullPath = "UI.DeviceTests.dll"
        };

        // Add sample test class
        var testClass = new TestClassViewModel
        {
            Name = "BasicDeviceTests",
            FullName = "Binnaculum.UI.DeviceTests.BasicDeviceTests"
        };

        // Add sample test cases
        testClass.TestCases.Add(new TestCaseViewModel
        {
            Name = "BasicDeviceTest_Infrastructure_ShouldPass",
            FullName = "Binnaculum.UI.DeviceTests.BasicDeviceTests.BasicDeviceTest_Infrastructure_ShouldPass",
            DisplayName = "Basic Infrastructure Test",
            Status = TestCaseStatus.Pending
        });

        testClass.TestCases.Add(new TestCaseViewModel
        {
            Name = "BasicDeviceTest_CanAccessMauiControls",
            FullName = "Binnaculum.UI.DeviceTests.BasicDeviceTests.BasicDeviceTest_CanAccessMauiControls",
            DisplayName = "MAUI Controls Access Test",
            Status = TestCaseStatus.Pending
        });

        assembly.TestClasses.Add(testClass);
        assembly.UpdateTestCounts();

        return assembly;
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