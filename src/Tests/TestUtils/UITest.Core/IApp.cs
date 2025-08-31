namespace Binnaculum.UITest.Core;

/// <summary>
/// Main interface for cross-platform app interaction during UI testing.
/// Based on Microsoft MAUI UITest.Core IApp pattern.
/// </summary>
public interface IApp : IScreenshotSupportedApp, IUIElementQueryable, IDisposable
{
    /// <summary>
    /// Configuration used by this app instance.
    /// </summary>
    IConfig Config { get; }

    /// <summary>
    /// Current application state.
    /// </summary>
    ApplicationState AppState { get; }

    /// <summary>
    /// Command execution framework.
    /// </summary>
    ICommandExecution CommandExecutor { get; }

    /// <summary>
    /// Find a single element by AutomationId.
    /// </summary>
    /// <param name="id">AutomationId to search for</param>
    /// <returns>Found element</returns>
    /// <exception cref="ElementNotFoundException">Thrown if element is not found</exception>
    IUIElement FindElement(string id);

    /// <summary>
    /// Query for UI elements using various selectors (legacy method).
    /// Use By.* methods and IUIElementQueryable interface for better type safety.
    /// </summary>
    IQuery Query(string? query = null);

    /// <summary>
    /// Tap an element by query.
    /// </summary>
    void Tap(IQuery query);

    /// <summary>
    /// Tap an element by coordinates.
    /// </summary>
    void Tap(int x, int y);

    /// <summary>
    /// Enter text into an element.
    /// </summary>
    void EnterText(IQuery query, string text);

    /// <summary>
    /// Enter text into the currently focused element.
    /// </summary>
    void EnterText(string text);

    /// <summary>
    /// Clear text from an element.
    /// </summary>
    void ClearText(IQuery query);

    /// <summary>
    /// Swipe between two points.
    /// </summary>
    void Swipe(int startX, int startY, int endX, int endY, int duration = 1000);

    /// <summary>
    /// Scroll to an element.
    /// </summary>
    void ScrollTo(IQuery query, ScrollDirection direction = ScrollDirection.Down);

    /// <summary>
    /// Wait for an element to appear (legacy method).
    /// This method is inherited from IUIElementQueryable.
    /// </summary>
    // IUIElement WaitForElement(IQuery query, TimeSpan? timeout = null); // Inherited from IUIElementQueryable

    /// <summary>
    /// Wait for an element to disappear (legacy method).
    /// This method is inherited from IUIElementQueryable.
    /// </summary>
    // void WaitForNoElement(IQuery query, TimeSpan? timeout = null); // Inherited from IUIElementQueryable

    /// <summary>
    /// Take a screenshot (legacy method).
    /// This method is inherited from IScreenshotSupportedApp.
    /// </summary>
    // byte[] Screenshot(); // Inherited from IScreenshotSupportedApp

    /// <summary>
    /// Navigate back (Android back button, iOS navigation back).
    /// </summary>
    void Back();

    /// <summary>
    /// Dismiss any keyboard that might be open.
    /// </summary>
    void DismissKeyboard();

    /// <summary>
    /// Get the current app state.
    /// </summary>
    ApplicationState GetAppState();

    /// <summary>
    /// Restart the app.
    /// </summary>
    void RestartApp();

    /// <summary>
    /// Close the app.
    /// </summary>
    void CloseApp();

    /// <summary>
    /// Dispose of the app and clean up resources (legacy method).
    /// This method is inherited from IDisposable.
    /// </summary>
    // void Dispose(); // Inherited from IDisposable
}

/// <summary>
/// Scroll direction for scrolling operations.
/// </summary>
public enum ScrollDirection
{
    Up,
    Down,
    Left,
    Right
}

/// <summary>
/// Current state of the application.
/// </summary>
public enum ApplicationState
{
    Unknown,
    NotInstalled,
    NotRunning,
    RunningInBackground,
    RunningInForeground
}

/// <summary>
/// Legacy alias for ApplicationState - kept for compatibility.
/// </summary>
public enum AppState
{
    Unknown = ApplicationState.Unknown,
    NotInstalled = ApplicationState.NotInstalled,
    NotRunning = ApplicationState.NotRunning,
    RunningInBackground = ApplicationState.RunningInBackground,
    RunningInForeground = ApplicationState.RunningInForeground
}