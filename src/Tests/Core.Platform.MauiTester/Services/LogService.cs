namespace Core.Platform.MauiTester.Services
{
    /// <summary>
    /// Simple logging service for the test app to track detailed execution steps
    /// </summary>
    public class LogService
    {
        private readonly List<string> _logs = new();
        private readonly List<string> _errors = new();

        public IReadOnlyList<string> Logs => _logs.AsReadOnly();
        public IReadOnlyList<string> Errors => _errors.AsReadOnly();

        public void Log(string message)
        {
            var timestampedMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            _logs.Add(timestampedMessage);
            System.Diagnostics.Debug.WriteLine($"LOG: {timestampedMessage}");
        }

        public void LogError(string message)
        {
            var timestampedMessage = $"[{DateTime.Now:HH:mm:ss.fff}] ERROR: {message}";
            _errors.Add(timestampedMessage);
            _logs.Add(timestampedMessage);
            System.Diagnostics.Debug.WriteLine($"ERROR: {timestampedMessage}");
        }

        public void Clear()
        {
            _logs.Clear();
            _errors.Clear();
        }

        public string GetFullLog()
        {
            return string.Join(Environment.NewLine, _logs);
        }
    }
}