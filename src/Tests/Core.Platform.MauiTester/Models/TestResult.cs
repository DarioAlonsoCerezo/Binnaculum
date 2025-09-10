using System.ComponentModel;

namespace Core.Platform.MauiTester.Models
{
    /// <summary>
    /// Represents the result of a test step in the Core Platform validation
    /// </summary>
    public class TestStepResult : INotifyPropertyChanged
    {
        private string _stepName = string.Empty;
        private bool _isCompleted;
        private bool _isSuccessful;
        private string _details = string.Empty;
        private string _errorMessage = string.Empty;

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

        public string StatusIcon => IsCompleted ? (IsSuccessful ? "✅" : "❌") : "⏳";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents the overall test execution result for Core Platform validation
    /// </summary>
    public class OverallTestResult : INotifyPropertyChanged
    {
        private bool _isRunning;
        private bool _isCompleted;
        private bool _allTestsPassed;
        private List<TestStepResult> _steps = new();
        private string _overallStatus = "Ready";

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

        public bool CanRunTest => !IsRunning;

        public string OverallStatusIcon => IsCompleted ? (AllTestsPassed ? "✅ PASSED" : "❌ FAILED") : (IsRunning ? "⏳ RUNNING" : "⚪ READY");

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}