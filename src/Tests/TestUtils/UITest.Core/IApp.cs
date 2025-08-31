namespace Binnaculum.UITest.Core;

/// <summary>
/// Main interface for cross-platform app interaction during UI testing.
/// Based on Microsoft MAUI UITest.Core IApp pattern.
/// </summary>
public interface IApp
{
    /// <summary>
    /// Query for UI elements using various selectors.
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
    /// Wait for an element to appear.
    /// </summary>
    IUIElement WaitForElement(IQuery query, TimeSpan? timeout = null);

    /// <summary>
    /// Wait for an element to disappear.
    /// </summary>
    void WaitForNoElement(IQuery query, TimeSpan? timeout = null);

    /// <summary>
    /// Take a screenshot.
    /// </summary>
    byte[] Screenshot();

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
    AppState GetAppState();

    /// <summary>
    /// Restart the app.
    /// </summary>
    void RestartApp();

    /// <summary>
    /// Close the app.
    /// </summary>
    void CloseApp();

    /// <summary>
    /// Dispose of the app and clean up resources.
    /// </summary>
    void Dispose();
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
/// Current state of the app.
/// </summary>
public enum AppState
{
    Unknown,
    NotInstalled,
    NotRunning,
    RunningInBackground,
    RunningInForeground
}