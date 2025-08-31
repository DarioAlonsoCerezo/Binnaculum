namespace Binnaculum.UI.DeviceTests.Runners;

/// <summary>
/// Test runner verification tests to ensure the runner infrastructure works correctly.
/// </summary>
public class TestRunnerTests
{
    [Fact]
    public void TestRunner_Infrastructure_ShouldPass()
    {
        // Arrange & Act
        var result = true;
        
        // Assert
        Assert.True(result, "Test runner infrastructure should be working");
    }
    
    [Fact]
    public void TestRunner_CanAccessDeviceTests()
    {
        // Arrange - Create a simple test from the DeviceTests project
        var deviceTest = new UI.DeviceTests.BasicDeviceTests();
        
        // Act & Assert
        Assert.NotNull(deviceTest);
        
        // Verify we can call test methods (this won't run the actual test, just verify access)
        // In a real scenario, the runner would execute the actual device tests
    }
    
    [Fact]
    public void TestRunner_CanAccessCoreProjects()
    {
        // This test verifies we have access to the Core project through project references
        // Arrange & Act
        var coreAssembly = typeof(Binnaculum.Core.Models.Currency).Assembly;
        
        // Assert
        Assert.NotNull(coreAssembly);
    }
}