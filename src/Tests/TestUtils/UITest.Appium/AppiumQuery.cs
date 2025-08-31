using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Service;
using OpenQA.Selenium;
using Binnaculum.UITest.Core;

namespace Binnaculum.UITest.Appium;

/// <summary>
/// Appium implementation of the IQuery interface for element finding.
/// Based on Microsoft MAUI UITest.Appium AppiumQuery pattern.
/// </summary>
public class AppiumQuery : IQuery
{
    private readonly List<QueryPart> _queryParts = new();
    private readonly AppiumDriver _driver;

    public AppiumQuery(AppiumDriver driver)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }

    private AppiumQuery(AppiumDriver driver, List<QueryPart> queryParts)
    {
        _driver = driver;
        _queryParts = new List<QueryPart>(queryParts);
    }

    public IQuery ById(string id)
    {
        var newQuery = Clone();
        newQuery._queryParts.Add(new QueryPart { Type = QueryType.Id, Value = id });
        return newQuery;
    }

    public IQuery ByText(string text)
    {
        var newQuery = Clone();
        newQuery._queryParts.Add(new QueryPart { Type = QueryType.Text, Value = text });
        return newQuery;
    }

    public IQuery ByXPath(string xpath)
    {
        var newQuery = Clone();
        newQuery._queryParts.Add(new QueryPart { Type = QueryType.XPath, Value = xpath });
        return newQuery;
    }

    public IQuery ByClass(string className)
    {
        var newQuery = Clone();
        newQuery._queryParts.Add(new QueryPart { Type = QueryType.Class, Value = className });
        return newQuery;
    }

    public IQuery Where(Func<IUIElement, bool> predicate)
    {
        var newQuery = Clone();
        newQuery._queryParts.Add(new QueryPart { Type = QueryType.Predicate, Predicate = predicate });
        return newQuery;
    }

    public IQuery First()
    {
        var newQuery = Clone();
        newQuery._queryParts.Add(new QueryPart { Type = QueryType.Index, Value = "0" });
        return newQuery;
    }

    public IQuery Index(int index)
    {
        var newQuery = Clone();
        newQuery._queryParts.Add(new QueryPart { Type = QueryType.Index, Value = index.ToString() });
        return newQuery;
    }

    public IQuery Child()
    {
        var newQuery = Clone();
        newQuery._queryParts.Add(new QueryPart { Type = QueryType.Child });
        return newQuery;
    }

    public IQuery Descendant()
    {
        var newQuery = Clone();
        newQuery._queryParts.Add(new QueryPart { Type = QueryType.Descendant });
        return newQuery;
    }

    public IQuery Sibling()
    {
        var newQuery = Clone();
        newQuery._queryParts.Add(new QueryPart { Type = QueryType.Sibling });
        return newQuery;
    }

    public string GetQueryString()
    {
        if (_queryParts.Count == 0)
            return "*";

        // Build XPath from query parts
        var parts = new List<string>();
        
        foreach (var part in _queryParts)
        {
            switch (part.Type)
            {
                case QueryType.Id:
                    parts.Add($"//*[@resource-id='{part.Value}' or @name='{part.Value}' or @id='{part.Value}']");
                    break;
                case QueryType.Text:
                    parts.Add($"//*[@text='{part.Value}' or @content-desc='{part.Value}']");
                    break;
                case QueryType.XPath:
                    parts.Add(part.Value!);
                    break;
                case QueryType.Class:
                    parts.Add($"//*[@class='{part.Value}']");
                    break;
                case QueryType.Child:
                    parts.Add("/*");
                    break;
                case QueryType.Descendant:
                    parts.Add("//*");
                    break;
                case QueryType.Sibling:
                    parts.Add("/following-sibling::*");
                    break;
                case QueryType.Index:
                    var index = int.Parse(part.Value!) + 1; // XPath is 1-based
                    parts.Add($"[{index}]");
                    break;
            }
        }

        return string.Join("", parts);
    }

    internal IWebElement? FindElement()
    {
        try
        {
            var xpath = GetQueryString();
            return _driver.FindElement(OpenQA.Selenium.By.XPath(xpath));
        }
        catch (OpenQA.Selenium.NoSuchElementException)
        {
            return null;
        }
    }

    internal IReadOnlyCollection<IWebElement> FindElements()
    {
        try
        {
            var xpath = GetQueryString();
            return _driver.FindElements(OpenQA.Selenium.By.XPath(xpath));
        }
        catch (OpenQA.Selenium.NoSuchElementException)
        {
            return Array.Empty<IWebElement>();
        }
    }

    private AppiumQuery Clone()
    {
        return new AppiumQuery(_driver, _queryParts);
    }
}

internal class QueryPart
{
    public QueryType Type { get; set; }
    public string? Value { get; set; }
    public Func<IUIElement, bool>? Predicate { get; set; }
}

internal enum QueryType
{
    Id,
    Text,
    XPath,
    Class,
    Predicate,
    Index,
    Child,
    Descendant,
    Sibling
}