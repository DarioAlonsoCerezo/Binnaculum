namespace Binnaculum.UI.DeviceTests.Runners.VisualRunner.ViewModels;

/// <summary>
/// Represents a single test case in the visual test runner.
/// </summary>
public class TestCaseViewModel : BaseViewModel
{
    private string _name = string.Empty;
    private string _fullName = string.Empty;
    private string _displayName = string.Empty;
    private TestCaseStatus _status = TestCaseStatus.Pending;
    private string _result = string.Empty;
    private string _errorMessage = string.Empty;
    private string _stackTrace = string.Empty;
    private TimeSpan _duration = TimeSpan.Zero;
    private bool _isSelected = false;

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

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public TestCaseStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string Result
    {
        get => _result;
        set => SetProperty(ref _result, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public string StackTrace
    {
        get => _stackTrace;
        set => SetProperty(ref _stackTrace, value);
    }

    public TimeSpan Duration
    {
        get => _duration;
        set => SetProperty(ref _duration, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsCompleted => Status is TestCaseStatus.Passed or TestCaseStatus.Failed or TestCaseStatus.Skipped;
}

/// <summary>
/// Enumeration of possible test case statuses.
/// </summary>
public enum TestCaseStatus
{
    Pending,
    Running,
    Passed,
    Failed,
    Skipped
}