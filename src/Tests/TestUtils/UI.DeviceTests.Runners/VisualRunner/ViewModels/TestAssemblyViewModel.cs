using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Binnaculum.UI.DeviceTests.Runners.VisualRunner.ViewModels;

/// <summary>
/// Represents a test assembly containing multiple test classes and methods.
/// </summary>
public class TestAssemblyViewModel : BaseViewModel
{
    private string _name = string.Empty;
    private string _fullPath = string.Empty;
    private bool _isSelected = false;
    private bool _isExpanded = false;
    private int _totalTests = 0;
    private int _passedTests = 0;
    private int _failedTests = 0;
    private int _skippedTests = 0;

    public TestAssemblyViewModel()
    {
        TestClasses = new ObservableCollection<TestClassViewModel>();
        ToggleExpandCommand = new Command(() => IsExpanded = !IsExpanded);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string FullPath
    {
        get => _fullPath;
        set => SetProperty(ref _fullPath, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                // Cascade selection to all test classes
                foreach (var testClass in TestClasses)
                {
                    testClass.IsSelected = value;
                }
            }
        }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public int TotalTests
    {
        get => _totalTests;
        set => SetProperty(ref _totalTests, value);
    }

    public int PassedTests
    {
        get => _passedTests;
        set => SetProperty(ref _passedTests, value);
    }

    public int FailedTests
    {
        get => _failedTests;
        set => SetProperty(ref _failedTests, value);
    }

    public int SkippedTests
    {
        get => _skippedTests;
        set => SetProperty(ref _skippedTests, value);
    }

    public int RunningTests => TotalTests - PassedTests - FailedTests - SkippedTests;

    public double PassRate => TotalTests > 0 ? (double)PassedTests / TotalTests : 0;

    public ObservableCollection<TestClassViewModel> TestClasses { get; }
    
    public ICommand ToggleExpandCommand { get; }

    public void UpdateTestCounts()
    {
        TotalTests = TestClasses.Sum(tc => tc.TotalTests);
        PassedTests = TestClasses.Sum(tc => tc.PassedTests);
        FailedTests = TestClasses.Sum(tc => tc.FailedTests);
        SkippedTests = TestClasses.Sum(tc => tc.SkippedTests);
    }
}

/// <summary>
/// Represents a test class containing multiple test methods.
/// </summary>
public class TestClassViewModel : BaseViewModel
{
    private string _name = string.Empty;
    private string _fullName = string.Empty;
    private bool _isSelected = false;
    private bool _isExpanded = false;

    public TestClassViewModel()
    {
        TestCases = new ObservableCollection<TestCaseViewModel>();
        ToggleExpandCommand = new Command(() => IsExpanded = !IsExpanded);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string FullName
    {
        get => _fullName;
        set => SetProperty(ref _fullName, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                // Cascade selection to all test cases
                foreach (var testCase in TestCases)
                {
                    testCase.IsSelected = value;
                }
            }
        }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public ObservableCollection<TestCaseViewModel> TestCases { get; }
    
    public ICommand ToggleExpandCommand { get; }

    public int TotalTests => TestCases.Count;
    public int PassedTests => TestCases.Count(tc => tc.Status == TestCaseStatus.Passed);
    public int FailedTests => TestCases.Count(tc => tc.Status == TestCaseStatus.Failed);
    public int SkippedTests => TestCases.Count(tc => tc.Status == TestCaseStatus.Skipped);
}