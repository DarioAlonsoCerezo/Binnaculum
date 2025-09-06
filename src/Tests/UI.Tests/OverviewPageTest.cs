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
        
        TakeScreenshot();
    }
}
