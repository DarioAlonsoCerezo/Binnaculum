using System.ComponentModel;

namespace Core.Platform.MauiTester.Models
{
    /// <summary>
    /// Represents the result of a test step in the Core Platform validation with enhanced reporting
    /// </summary>
    public class TestStepResult : INotifyPropertyChanged
    {
        private string _stepName = string.Empty;
        private bool _isCompleted;
        private bool _isSuccessful;
        private string _details = string.Empty;
        private string _errorMessage = string.Empty;
        private DateTime? _startTime;
        private DateTime? _endTime;
        private int _retryCount = 0;
        private List<string> _tags = new();

        public string StepName
        {
            get => _stepName;
            set
            {
                _stepName = value;
                OnPropertyChanged();
            }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                _isCompleted = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(Duration));
            }
        }

        public bool IsSuccessful
        {
            get => _isSuccessful;
            set
            {
                _isSuccessful = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusIcon));
            }
        }

        public string Details
        {
            get => _details;
            set
            {
                _details = value;
                OnPropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public DateTime? StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Duration));
            }
        }

        public DateTime? EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Duration));
            }
        }

        public int RetryCount
        {
            get => _retryCount;
            set
            {
                _retryCount = value;
                OnPropertyChanged();
            }
        }

        public List<string> Tags
        {
            get => _tags;
            set
            {
                _tags = value;
                OnPropertyChanged();
            }
        }

        public string StatusIcon => IsCompleted ? (IsSuccessful ? "✅" : "❌") : "⏳";

        public TimeSpan? Duration
        {
            get
            {
                if (StartTime == null) return null;
                return (EndTime ?? DateTime.Now) - StartTime.Value;
            }
        }

        public string DurationText => Duration?.ToString(@"mm\:ss\.fff") ?? "--";

        public void MarkStarted()
        {
            StartTime = DateTime.Now;
        }

        public void MarkCompleted(bool success)
        {
            EndTime = DateTime.Now;
            IsCompleted = true;
            IsSuccessful = success;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents the overall test execution result for Core Platform validation with enhanced metadata and reporting
    /// </summary>
    public class OverallTestResult : INotifyPropertyChanged
    {
        private bool _isRunning;
        private bool _isCompleted;
        private bool _allTestsPassed;
        private List<TestStepResult> _steps = new();
        private string _overallStatus = "Ready";
        private string _testName = "";
        private List<string> _tags = new();
        private DateTime? _startTime;
        private DateTime? _endTime;
        private string _summary = "";

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanRunTest));
            }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                _isCompleted = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanRunTest));
                OnPropertyChanged(nameof(OverallStatusIcon));
            }
        }

        public bool AllTestsPassed
        {
            get => _allTestsPassed;
            set
            {
                _allTestsPassed = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(OverallStatusIcon));
            }
        }

        public List<TestStepResult> Steps
        {
            get => _steps;
            set
            {
                _steps = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PassedStepCount));
                OnPropertyChanged(nameof(FailedStepCount));
                OnPropertyChanged(nameof(TotalStepCount));
            }
        }

        public string OverallStatus
        {
            get => _overallStatus;
            set
            {
                _overallStatus = value;
                OnPropertyChanged();
            }
        }

        public string TestName
        {
            get => _testName;
            set
            {
                _testName = value;
                OnPropertyChanged();
            }
        }

        public List<string> Tags
        {
            get => _tags;
            set
            {
                _tags = value;
                OnPropertyChanged();
            }
        }

        public DateTime? StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Duration));
            }
        }

        public DateTime? EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Duration));
            }
        }

        public string Summary
        {
            get => _summary;
            set
            {
                _summary = value;
                OnPropertyChanged();
            }
        }

        public bool CanRunTest => !IsRunning;

        public string OverallStatusIcon => IsCompleted ? (AllTestsPassed ? "✅ PASSED" : "❌ FAILED") : (IsRunning ? "⏳ RUNNING" : "⚪ READY");

        public int PassedStepCount => Steps.Count(s => s.IsCompleted && s.IsSuccessful);
        
        public int FailedStepCount => Steps.Count(s => s.IsCompleted && !s.IsSuccessful);
        
        public int TotalStepCount => Steps.Count;

        public TimeSpan? Duration
        {
            get
            {
                if (StartTime == null) return null;
                return (EndTime ?? DateTime.Now) - StartTime.Value;
            }
        }

        public string DurationText => Duration?.ToString(@"mm\:ss\.fff") ?? "--";

        public void MarkStarted(string testName)
        {
            TestName = testName;
            StartTime = DateTime.Now;
            IsRunning = true;
            IsCompleted = false;
        }

        public void MarkCompleted(bool allPassed, string summary = "")
        {
            EndTime = DateTime.Now;
            IsRunning = false;
            IsCompleted = true;
            AllTestsPassed = allPassed;
            Summary = summary;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}