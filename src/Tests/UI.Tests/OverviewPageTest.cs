namespace UI.Tests;

[Category("Overview")]
public class OverviewPageTest : BaseTest
{    

    [Test]
    public void Sanity()
    {
        TestContext.Out.WriteLine("=== Testing OverviewTitle Display ===");
        
        var indicators = FindElementsWithTimeout(30, UIElementId.Page.Overview.CarouselIndicator, UIElementId.Page.Overview.CollectionIndicator);

        Assert.Multiple(() =>
        {
            Assert.That(indicators, Is.Not.Null.And.Not.Empty, "At least one indicator should be found");
            foreach (var indicator in indicators)
            {
                Assert.That(indicator.Displayed, Is.True, $"Indicator '{indicator}' should be visible");
                TestContext.Out.WriteLine($"✅ Indicator found: '{indicator}'");
            }
        });

        var tabs = FindElementsWithTimeout(30, UIElementId.Shell.Overview, UIElementId.Shell.Tickers, UIElementId.Shell.Settings);

        Assert.Multiple(() =>
        {
            Assert.That(tabs, Is.Not.Null.And.Not.Empty, "At least one tab should be found");
            // There should be exactly 3 tabs: Overview, Tickers, Settings
            Assert.That(tabs.Count, Is.EqualTo(3), "There should be exactly 3 tabs");

            foreach (var tab in tabs)
            {
                Assert.That(tab.Displayed, Is.True, $"Tab '{tab}' should be visible");
                TestContext.Out.WriteLine($"✅ Tab found: '{tab}'");
            }
        });

        var element = FindElementWithTimeout(UIElementId.Page.Overview.Title, 30);

        Assert.Multiple(() =>
        {
            Assert.That(element, Is.Not.Null, "OverviewTitle element should exist");
            Assert.That(element.Displayed, Is.True, "OverviewTitle should be visible");
            
            // Verify it has some text (likely "My accounts" based on your output)
            var text = element.Text;
            Assert.That(text, Is.Not.Null.And.Not.Empty, "OverviewTitle should have text content");
            TestContext.Out.WriteLine($"✅ OverviewTitle found with text: '{text}'");
        });

        var emptyTexts = FindElementsWithTimeout(30, 
            UIElementId.Page.Overview.EmptyMovementCollectionText, 
            UIElementId.Page.Overview.EmptyAccountText);

        Assert.Multiple(() => {
            Assert.That(emptyTexts, Is.Not.Null.And.Not.Empty, "At least one empty state text should be found");
            foreach (var emptyText in emptyTexts)
            {
                Assert.That(emptyText.Displayed, Is.True, $"Empty state text '{emptyText}' should be visible");
                var text = emptyText.Text;
                Assert.That(text, Is.Not.Null.And.Not.Empty, "Empty state text should have content");
                TestContext.Out.WriteLine($"✅ Empty state text found: '{text}'");
            }
        });

        TakeScreenshot();

        var tickerTab = FindElementByAccesibilityId(UIElementId.Shell.Accessibility.Tickers);
        Assert.That(tickerTab, Is.Not.Null, "Tickers tab should exist");

        tickerTab.Click();

        var tickerTemplate = FindElementWithTimeout(UIElementId.Templates.Ticker.TickerTemplate, 30);

        Assert.That(tickerTemplate, Is.Not.Null, "TickerTemplate element should exist");

        var tickerName = FindElementWithTimeout(UIElementId.Templates.Ticker.TickerName, 30);
        Assert.That(tickerName, Is.Not.Null, "TickerName element should exist");
        Assert.That(tickerName.Displayed, Is.True, "TickerName should be visible");
        TestContext.Out.WriteLine($"✅ TickerName found with text: '{tickerName.Text}'");

        TakeScreenshot("Tickers");
    }
}
