using Binnaculum.UI.DeviceTests.Runners.VisualRunner;

namespace Binnaculum.UI.DeviceTests.Runners;

/// <summary>
/// Entry point for launching the Visual Test Runner.
/// This can be used to start the visual runner from within existing test infrastructure.
/// </summary>
public static class VisualTestRunnerLauncher
{
    /// <summary>
    /// Launches the Visual Test Runner application.
    /// This creates a new MAUI application instance and starts the visual runner.
    /// </summary>
    /// <returns>The application instance for further configuration if needed.</returns>
    public static Application LaunchVisualRunner()
    {
        var app = new VisualTestRunnerApp();
        
        // The app will handle its own lifecycle through MAUI
        // In a real device scenario, this would launch the app UI
        
        return app;
    }

    /// <summary>
    /// Creates a MAUI application builder configured for the Visual Test Runner.
    /// This is useful for more advanced configuration scenarios.
    /// </summary>
    /// <returns>A configured MauiAppBuilder.</returns>
    public static MauiAppBuilder CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        
        builder
            .UseMauiApp<VisualTestRunnerApp>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Add logging
        builder.Logging.AddDebug();

        return builder;
    }
}

/// <summary>
/// Example test class that demonstrates how the Visual Test Runner would discover and run tests.
/// </summary>
public class SampleVisualTestRunnerTests
{
    [Fact]
    public void VisualRunner_CanLaunchApplication()
    {
        // Arrange & Act
        var app = VisualTestRunnerLauncher.LaunchVisualRunner();
        
        // Assert
        Assert.NotNull(app);
        Assert.IsType<VisualTestRunnerApp>(app);
    }
    
    [Fact] 
    public void VisualRunner_CanCreateMauiAppBuilder()
    {
        // Arrange & Act
        var builder = VisualTestRunnerLauncher.CreateMauiApp();
        
        // Assert
        Assert.NotNull(builder);
        
        // This would typically be followed by builder.Build() in a real app
        // but we're just testing the builder creation here
    }

    [Fact]
    public void SampleTest_AlwaysPasses()
    {
        // This is a sample test that the Visual Runner should discover
        Assert.True(true, "This test always passes");
    }

    [Fact]
    public void SampleTest_WithErrorHandling()
    {
        // This demonstrates how the Visual Runner handles different types of tests
        var value = 42;
        Assert.Equal(42, value);
    }

    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(5, 5, 10)]
    public void SampleTheoryTest_Addition(int a, int b, int expected)
    {
        // The Visual Runner should discover Theory tests as well
        var result = a + b;
        Assert.Equal(expected, result);
    }
}