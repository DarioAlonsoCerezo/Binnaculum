namespace Binnaculum.UI.DeviceTests;

/// <summary>
/// Basic device test class to verify project setup and infrastructure.
/// </summary>
public class BasicDeviceTests
{
    [Fact]
    public void BasicDeviceTest_Infrastructure_ShouldPass()
    {
        // Arrange & Act
        var result = true;
        
        // Assert
        Assert.True(result, "Basic device test infrastructure should be working");
    }
    
    [Fact]
    public void BasicDeviceTest_CanAccessMauiControls()
    {
        // Arrange & Act - Test that we can create a basic MAUI control
        var label = new Label { Text = "Test" };
        
        // Assert
        Assert.NotNull(label);
        Assert.Equal("Test", label.Text);
    }
    
    [Fact]
    public void BasicDeviceTest_CanAccessCoreProject()
    {
        // Arrange & Act - Test access to Core project types
        var coreAssembly = typeof(Binnaculum.Core.Models.Currency).Assembly;
        
        // Assert
        Assert.NotNull(coreAssembly);
        Assert.Contains("Core", coreAssembly.GetName().Name);
    }
}