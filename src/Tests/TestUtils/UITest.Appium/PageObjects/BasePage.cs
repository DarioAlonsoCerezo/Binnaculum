using Binnaculum.UITest.Core;

namespace Binnaculum.UITest.Appium.PageObjects;

/// <summary>
/// Base class for all page object models in Binnaculum.
/// Provides common functionality and patterns for page interactions.
/// </summary>
public abstract class BasePage
{
    protected readonly IApp _app;
    protected readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    protected BasePage(IApp app)
    {
        _app = app ?? throw new ArgumentNullException(nameof(app));
    }

    /// <summary>
    /// Wait for the page to be loaded and displayed.
    /// </summary>
    public abstract void WaitForPageToLoad();

    /// <summary>
    /// Verify that we are currently on this page.
    /// </summary>
    public abstract bool IsCurrentPage();

    /// <summary>
    /// Navigate back from this page (platform-specific).
    /// </summary>
    public virtual void NavigateBack()
    {
        _app.Back();
    }

    /// <summary>
    /// Take a screenshot of the current page.
    /// </summary>
    public byte[] TakeScreenshot()
    {
        return _app.Screenshot();
    }

    /// <summary>
    /// Wait for an element to be visible on the page.
    /// </summary>
    protected IUIElement WaitForElement(IQuery query, TimeSpan? timeout = null)
    {
        return _app.WaitForElement(query, timeout ?? DefaultTimeout);
    }

    /// <summary>
    /// Wait for an element to disappear from the page.
    /// </summary>
    protected void WaitForElementToDisappear(IQuery query, TimeSpan? timeout = null)
    {
        _app.WaitForNoElement(query, timeout ?? DefaultTimeout);
    }

    /// <summary>
    /// Tap an element on the page.
    /// </summary>
    protected void Tap(IQuery query)
    {
        _app.Tap(query);
    }

    /// <summary>
    /// Enter text into an element.
    /// </summary>
    protected void EnterText(IQuery query, string text)
    {
        _app.EnterText(query, text);
    }

    /// <summary>
    /// Scroll to find an element on the page.
    /// </summary>
    protected void ScrollToElement(IQuery query, ScrollDirection direction = ScrollDirection.Down)
    {
        _app.ScrollTo(query, direction);
    }
}