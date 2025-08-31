namespace Binnaculum.UI.DeviceTests.Controls;

/// <summary>
/// Tests for individual MAUI controls to verify they work correctly on device platforms.
/// </summary>
public class ControlTests
{
    [Fact]
    public void Label_CreateAndSetText_ShouldWork()
    {
        // Arrange
        const string testText = "Hello Device Testing!";
        
        // Act
        var label = new Label { Text = testText };
        
        // Assert
        Assert.NotNull(label);
        Assert.Equal(testText, label.Text);
    }

    [Fact]
    public void Button_CreateAndSetProperties_ShouldWork()
    {
        // Arrange
        const string buttonText = "Test Button";
        
        // Act
        var button = new Button { Text = buttonText };
        
        // Assert
        Assert.NotNull(button);
        Assert.Equal(buttonText, button.Text);
        Assert.True(button.IsEnabled); // Should be enabled by default
    }
}