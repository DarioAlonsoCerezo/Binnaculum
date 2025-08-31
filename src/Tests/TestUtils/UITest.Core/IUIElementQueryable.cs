namespace Binnaculum.UITest.Core;

/// <summary>
/// Interface for querying collections of UI elements.
/// Provides methods for finding and filtering multiple elements.
/// </summary>
public interface IUIElementQueryable
{
    /// <summary>
    /// Find a single element using the specified query.
    /// </summary>
    /// <param name="query">Query to find the element</param>
    /// <returns>The found element, or null if not found</returns>
    IUIElement? FindElement(IQuery query);

    /// <summary>
    /// Find multiple elements using the specified query.
    /// </summary>
    /// <param name="query">Query to find elements</param>
    /// <returns>Collection of found elements</returns>
    IReadOnlyCollection<IUIElement> FindElements(IQuery query);

    /// <summary>
    /// Wait for an element to appear and return it.
    /// </summary>
    /// <param name="query">Query to find the element</param>
    /// <param name="timeout">Maximum time to wait</param>
    /// <returns>The found element</returns>
    /// <exception cref="TimeoutException">Thrown if element is not found within timeout</exception>
    IUIElement WaitForElement(IQuery query, TimeSpan? timeout = null);

    /// <summary>
    /// Wait for an element to disappear.
    /// </summary>
    /// <param name="query">Query to find the element</param>
    /// <param name="timeout">Maximum time to wait</param>
    /// <exception cref="TimeoutException">Thrown if element doesn't disappear within timeout</exception>
    void WaitForNoElement(IQuery query, TimeSpan? timeout = null);

    /// <summary>
    /// Check if an element exists without waiting.
    /// </summary>
    /// <param name="query">Query to find the element</param>
    /// <returns>True if element exists, false otherwise</returns>
    bool ElementExists(IQuery query);

    /// <summary>
    /// Get the element tree representation for debugging.
    /// </summary>
    string ElementTree { get; }
}