namespace Binnaculum.UITest.Core;

/// <summary>
/// Interface for UI element interactions.
/// Based on Microsoft MAUI UITest.Core IUIElement pattern.
/// </summary>
public interface IUIElement
{
    /// <summary>
    /// Gets the element's AutomationId.
    /// </summary>
    string? Id { get; }

    /// <summary>
    /// Gets the element's text content.
    /// </summary>
    string? Text { get; }

    /// <summary>
    /// Gets the element's class name.
    /// </summary>
    string? Class { get; }

    /// <summary>
    /// Gets whether the element is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets whether the element is displayed.
    /// </summary>
    bool IsDisplayed { get; }

    /// <summary>
    /// Gets the element's location on screen.
    /// </summary>
    Rectangle Location { get; }

    /// <summary>
    /// Gets the element's size.
    /// </summary>
    Size Size { get; }

    /// <summary>
    /// Tap/click the element.
    /// </summary>
    void Tap();

    /// <summary>
    /// Long press the element.
    /// </summary>
    void LongPress();

    /// <summary>
    /// Double tap the element.
    /// </summary>
    void DoubleTap();

    /// <summary>
    /// Enter text into the element (for input fields).
    /// </summary>
    void EnterText(string text);

    /// <summary>
    /// Clear text from the element.
    /// </summary>
    void ClearText();

    /// <summary>
    /// Get an attribute value from the element.
    /// </summary>
    string? GetAttribute(string attributeName);

    /// <summary>
    /// Wait for the element to become visible.
    /// </summary>
    bool WaitForElement(TimeSpan? timeout = null);
}

/// <summary>
/// Represents a rectangle with position and size.
/// </summary>
public record Rectangle(int X, int Y, int Width, int Height);

/// <summary>
/// Represents a size with width and height.
/// </summary>
public record Size(int Width, int Height);