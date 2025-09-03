namespace UI.Tests;

[Category("Overview")]
public class OverviewPageTest : BaseTest
{    

    [Test]
    public void OverviewTitle_IsDisplayed()
    {
        TestContext.Out.WriteLine("=== Testing OverviewTitle Display ===");
        
        // Try multiple strategies to find the OverviewTitle element
        var element = FindElementWithTimeout("OverviewTitle", 30);

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
