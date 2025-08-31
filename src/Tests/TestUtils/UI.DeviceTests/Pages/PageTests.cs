namespace Binnaculum.UI.DeviceTests.Pages;

/// <summary>
/// Tests for page-level functionality and navigation on device platforms.
/// </summary>
public class PageTests
{
    [Fact]
    public void ContentPage_CreateBasicPage_ShouldWork()
    {
        // Arrange & Act
        var page = new ContentPage
        {
            Title = "Test Page",
            Content = new Label { Text = "Test Content" }
        };
        
        // Assert
        Assert.NotNull(page);
        Assert.Equal("Test Page", page.Title);
        Assert.NotNull(page.Content);
        Assert.IsType<Label>(page.Content);
    }

    [Fact]
    public void Page_SetBackgroundColor_ShouldWork()
    {
        // Arrange
        var page = new ContentPage();
        var testColor = Colors.Blue;
        
        // Act
        page.BackgroundColor = testColor;
        
        // Assert
        Assert.Equal(testColor, page.BackgroundColor);
    }
}