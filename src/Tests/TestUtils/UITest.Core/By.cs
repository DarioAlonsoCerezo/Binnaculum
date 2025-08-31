namespace Binnaculum.UITest.Core;

/// <summary>
/// Static factory class for creating element queries.
/// Provides a fluent interface for building element selectors.
/// </summary>
public static class By
{
    /// <summary>
    /// Find elements by AutomationId.
    /// </summary>
    /// <param name="id">The AutomationId to search for</param>
    /// <returns>Query for elements with the specified AutomationId</returns>
    public static IQuery Id(string id)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("ID cannot be null or empty", nameof(id));
        
        return new BaseQuery().ById(id);
    }

    /// <summary>
    /// Find elements by visible text content.
    /// </summary>
    /// <param name="text">The text to search for</param>
    /// <returns>Query for elements containing the specified text</returns>
    public static IQuery Text(string text)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Text cannot be null or empty", nameof(text));
        
        return new BaseQuery().ByText(text);
    }

    /// <summary>
    /// Find elements by XPath expression.
    /// </summary>
    /// <param name="xpath">The XPath expression to use</param>
    /// <returns>Query using the specified XPath</returns>
    public static IQuery XPath(string xpath)
    {
        if (string.IsNullOrEmpty(xpath))
            throw new ArgumentException("XPath cannot be null or empty", nameof(xpath));
        
        return new BaseQuery().ByXPath(xpath);
    }

    /// <summary>
    /// Find elements by class name or control type.
    /// </summary>
    /// <param name="className">The class name to search for</param>
    /// <returns>Query for elements with the specified class</returns>
    public static IQuery Class(string className)
    {
        if (string.IsNullOrEmpty(className))
            throw new ArgumentException("Class name cannot be null or empty", nameof(className));
        
        return new BaseQuery().ByClass(className);
    }

    /// <summary>
    /// Find elements that match a predicate function.
    /// </summary>
    /// <param name="predicate">Function to test elements</param>
    /// <returns>Query for elements matching the predicate</returns>
    public static IQuery Where(Func<IUIElement, bool> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));
        
        return new BaseQuery().Where(predicate);
    }

    /// <summary>
    /// Create a query that matches all elements.
    /// </summary>
    /// <returns>Query that matches any element</returns>
    public static IQuery All()
    {
        return new BaseQuery();
    }
}

/// <summary>
/// Basic implementation of IQuery for the By factory methods.
/// This is used as a starting point for query building.
/// </summary>
internal class BaseQuery : IQuery
{
    private readonly List<QueryOperation> _operations = new();

    public IQuery ById(string id)
    {
        var newQuery = Clone();
        newQuery._operations.Add(new QueryOperation(QueryOperationType.ById, id));
        return newQuery;
    }

    public IQuery ByText(string text)
    {
        var newQuery = Clone();
        newQuery._operations.Add(new QueryOperation(QueryOperationType.ByText, text));
        return newQuery;
    }

    public IQuery ByXPath(string xpath)
    {
        var newQuery = Clone();
        newQuery._operations.Add(new QueryOperation(QueryOperationType.ByXPath, xpath));
        return newQuery;
    }

    public IQuery ByClass(string className)
    {
        var newQuery = Clone();
        newQuery._operations.Add(new QueryOperation(QueryOperationType.ByClass, className));
        return newQuery;
    }

    public IQuery Where(Func<IUIElement, bool> predicate)
    {
        var newQuery = Clone();
        newQuery._operations.Add(new QueryOperation(QueryOperationType.Where, null, predicate));
        return newQuery;
    }

    public IQuery First()
    {
        var newQuery = Clone();
        newQuery._operations.Add(new QueryOperation(QueryOperationType.First));
        return newQuery;
    }

    public IQuery Index(int index)
    {
        var newQuery = Clone();
        newQuery._operations.Add(new QueryOperation(QueryOperationType.Index, index.ToString()));
        return newQuery;
    }

    public IQuery Child()
    {
        var newQuery = Clone();
        newQuery._operations.Add(new QueryOperation(QueryOperationType.Child));
        return newQuery;
    }

    public IQuery Descendant()
    {
        var newQuery = Clone();
        newQuery._operations.Add(new QueryOperation(QueryOperationType.Descendant));
        return newQuery;
    }

    public IQuery Sibling()
    {
        var newQuery = Clone();
        newQuery._operations.Add(new QueryOperation(QueryOperationType.Sibling));
        return newQuery;
    }

    public string GetQueryString()
    {
        if (_operations.Count == 0)
            return "*";

        var parts = new List<string>();
        
        foreach (var operation in _operations)
        {
            switch (operation.Type)
            {
                case QueryOperationType.ById:
                    parts.Add($"//*[@resource-id='{operation.Value}' or @name='{operation.Value}' or @id='{operation.Value}']");
                    break;
                case QueryOperationType.ByText:
                    parts.Add($"//*[@text='{operation.Value}' or @content-desc='{operation.Value}']");
                    break;
                case QueryOperationType.ByXPath:
                    parts.Add(operation.Value!);
                    break;
                case QueryOperationType.ByClass:
                    parts.Add($"//*[@class='{operation.Value}']");
                    break;
                case QueryOperationType.Child:
                    parts.Add("/*");
                    break;
                case QueryOperationType.Descendant:
                    parts.Add("//*");
                    break;
                case QueryOperationType.Sibling:
                    parts.Add("/following-sibling::*");
                    break;
                case QueryOperationType.First:
                    parts.Add("[1]");
                    break;
                case QueryOperationType.Index:
                    var index = int.Parse(operation.Value!) + 1; // XPath is 1-based
                    parts.Add($"[{index}]");
                    break;
            }
        }

        return string.Join("", parts);
    }

    private BaseQuery Clone()
    {
        var clone = new BaseQuery();
        clone._operations.AddRange(_operations);
        return clone;
    }
}

internal record QueryOperation(
    QueryOperationType Type,
    string? Value = null,
    Func<IUIElement, bool>? Predicate = null);

internal enum QueryOperationType
{
    ById,
    ByText,
    ByXPath,
    ByClass,
    Where,
    First,
    Index,
    Child,
    Descendant,
    Sibling
}