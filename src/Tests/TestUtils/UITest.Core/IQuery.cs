namespace Binnaculum.UITest.Core;

/// <summary>
/// Query interface for finding UI elements in the app.
/// Based on Microsoft MAUI UITest.Core IQuery pattern.
/// </summary>
public interface IQuery
{
    /// <summary>
    /// Find elements by AutomationId.
    /// </summary>
    IQuery ById(string id);

    /// <summary>
    /// Find elements by text content.
    /// </summary>
    IQuery ByText(string text);

    /// <summary>
    /// Find elements by XPath expression.
    /// </summary>
    IQuery ByXPath(string xpath);

    /// <summary>
    /// Find elements by class name.
    /// </summary>
    IQuery ByClass(string className);

    /// <summary>
    /// Find elements that match a predicate.
    /// </summary>
    IQuery Where(Func<IUIElement, bool> predicate);

    /// <summary>
    /// Get the first element matching this query.
    /// </summary>
    IQuery First();

    /// <summary>
    /// Get the element at the specified index.
    /// </summary>
    IQuery Index(int index);

    /// <summary>
    /// Find child elements.
    /// </summary>
    IQuery Child();

    /// <summary>
    /// Find descendant elements.
    /// </summary>
    IQuery Descendant();

    /// <summary>
    /// Find sibling elements.
    /// </summary>
    IQuery Sibling();

    /// <summary>
    /// Get the platform-specific query string.
    /// </summary>
    string GetQueryString();
}