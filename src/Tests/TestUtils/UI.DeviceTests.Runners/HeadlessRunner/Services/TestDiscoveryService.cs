using Binnaculum.UI.DeviceTests.Runners.VisualRunner.Services;
using Binnaculum.UI.DeviceTests.Runners.VisualRunner.ViewModels;
using Microsoft.Extensions.Logging;

namespace Binnaculum.UI.DeviceTests.Runners.HeadlessRunner.Services;

/// <summary>
/// Service for discovering tests in headless mode by wrapping the existing VisualDeviceRunner.
/// </summary>
public class TestDiscoveryService
{
    private readonly ILogger? _logger;
    private readonly VisualDeviceRunner _deviceRunner;

    public TestDiscoveryService(ILogger? logger = null)
    {
        _logger = logger;
        _deviceRunner = new VisualDeviceRunner(CreateCompatibleLogger(logger));
    }

    /// <summary>
    /// Discovers all available tests from loaded assemblies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of discovered test cases</returns>
    public async Task<List<TestCaseViewModel>> DiscoverTestsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Starting test discovery in headless mode");

            var testAssemblies = await _deviceRunner.DiscoverTestsAsync();
            var allTests = new List<TestCaseViewModel>();

            foreach (var assembly in testAssemblies)
            {
                foreach (var testClass in assembly.TestClasses)
                {
                    allTests.AddRange(testClass.TestCases);
                }
            }

            _logger?.LogInformation($"Test discovery completed. Found {allTests.Count} tests");
            return allTests;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during test discovery");
            throw;
        }
    }

    private static ILogger<VisualDeviceRunner>? CreateCompatibleLogger(ILogger? logger)
    {
        if (logger is ILogger<VisualDeviceRunner> compatibleLogger)
        {
            return compatibleLogger;
        }
        
        // Return null if we can't create a compatible logger
        // The VisualDeviceRunner handles null loggers gracefully
        return null;
    }
}