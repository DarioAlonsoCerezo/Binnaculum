using Xunit;
using Microsoft.Extensions.Logging;
using Binnaculum.UITest.Core;
using Binnaculum.UITest.Appium;

namespace Binnaculum.UITest.Appium.Tests;

/// <summary>
/// Simple UI test that connects directly to a running Appium server.
/// Run this with: appium --address 127.0.0.1 --port 4723 --relaxed-security
/// </summary>
public class SimpleUITests : IDisposable
{
    private IApp? _app;

    [Fact]
    public void SimpleAppLaunch_VerifyAppStarts()
    {
        // This test requires you to manually start Appium first:
        // appium --address 127.0.0.1 --port 4723 --relaxed-security
        
        // Check if Appium server is accessible with safer synchronous approach
        if (!IsAppiumServerRunning("http://127.0.0.1:4723/status"))
        {
            Skip.If(true, "Appium server is not running. Please start it manually with: appium --address 127.0.0.1 --port 4723 --relaxed-security");
        }
        
        try
        {
            // Configure for Android testing with dynamic discovery (testing the new approach)
            var config = AppiumConfig.ForBinnaculumAndroid();
            
            // Create app directly with known server
            var serverUri = new Uri("http://127.0.0.1:4723");
            _app = BinnaculumAppFactory.CreateApp(config, serverUri);
            
            // Simple test - just verify the app launches
            var appState = _app.GetAppState();
            Assert.NotEqual(ApplicationState.NotRunning, appState);
            
            // Take a screenshot to prove it's working
            var screenshot = _app.Screenshot();
            Assert.True(screenshot.Length > 0, "Screenshot should contain data");
            
            // Log success
            Console.WriteLine($"\u2705 App launched successfully! State: {appState}");
            Console.WriteLine($"\ud83d\udcf8 Screenshot size: {screenshot.Length} bytes");
            Console.WriteLine($"\ud83d\udd27 Used dynamic activity discovery");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\u274c Test failed: {ex.Message}");
            throw;
        }
    }

    [Fact]
    public void SimpleAppLaunch_VerifyAppStarts_CIFriendly()
    {
        // This test uses the CI-friendly approach (no hardcoded activity)
        // appium --address 127.0.0.1 --port 4723 --relaxed-security
        
        // Check if Appium server is accessible with safer synchronous approach
        if (!IsAppiumServerRunning("http://127.0.0.1:4723/status"))
        {
            Skip.If(true, "Appium server is not running. Please start it manually with: appium --address 127.0.0.1 --port 4723 --relaxed-security");
        }
        
        try
        {
            // Configure for Android testing with automatic activity discovery (CI-friendly)
            var config = AppiumConfig.ForBinnaculumAndroidSimple();
            
            // Create app directly with known server
            var serverUri = new Uri("http://127.0.0.1:4723");
            _app = BinnaculumAppFactory.CreateApp(config, serverUri);
            
            // Simple test - just verify the app launches
            var appState = _app.GetAppState();
            Assert.NotEqual(ApplicationState.NotRunning, appState);
            
            // Take a screenshot to prove it's working
            var screenshot = _app.Screenshot();
            Assert.True(screenshot.Length > 0, "Screenshot should contain data");
            
            // Log success
            Console.WriteLine($"\u2705 App launched successfully! State: {appState}");
            Console.WriteLine($"\ud83d\udcf8 Screenshot size: {screenshot.Length} bytes");
            Console.WriteLine($"\ud83d\udd27 Used automatic activity discovery (CI-friendly)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\u274c Test failed: {ex.Message}");
            throw;
        }
    }

    [Fact(Skip = "Manual test - requires Appium server running")]
    public void SimpleElementInteraction_FindAndTapElement()
    {
        // This test requires you to manually start Appium first:
        // appium --address 127.0.0.1 --port 4723 --relaxed-security
        
        try
        {
            var config = AppiumConfig.ForBinnaculumAndroid();
            var serverUri = new Uri("http://127.0.0.1:4723");
            _app = BinnaculumAppFactory.CreateApp(config, serverUri);
            
            // Wait for the app to fully load with proper wait condition
            WaitForAppReady(_app);
            
            // Try to find any element by XPath (this should work for any MAUI app)
            var query = _app.Query().ByXPath("//*[@clickable='true']").First();
            var elements = _app.FindElements(query);
            
            Assert.True(elements.Count > 0, "Should find at least one clickable element");
            
            Console.WriteLine($"\u2705 Found {elements.Count} clickable elements");
            
            // Log element details
            for (int i = 0; i < Math.Min(3, elements.Count); i++)
            {
                var element = elements.ElementAt(i);
                Console.WriteLine($"Element {i}: Text='{element.Text}', Class='{element.Class}', Id='{element.Id}'");
            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\u274c Test failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Waits for the app to be ready instead of using hard-coded sleep.
    /// Uses polling with timeout to ensure the app is in a ready state.
    /// </summary>
    private static void WaitForAppReady(IApp app, TimeSpan? timeout = null)
    {
        var actualTimeout = timeout ?? TimeSpan.FromSeconds(10);
        var endTime = DateTime.UtcNow.Add(actualTimeout);
        
        while (DateTime.UtcNow < endTime)
        {
            try
            {
                var appState = app.GetAppState();
                if (appState == ApplicationState.RunningInForeground)
                {
                    // App is ready, give it a small additional moment to fully render
                    Thread.Sleep(500);
                    return;
                }
            }
            catch
            {
                // App state check failed, continue polling
            }
            
            Thread.Sleep(250); // Poll every 250ms
        }
        
        throw new System.TimeoutException($"App did not become ready within {actualTimeout.TotalSeconds} seconds");
    }

    /// <summary>
    /// Safely checks if Appium server is running using synchronous HTTP request.
    /// Avoids async anti-patterns in test methods.
    /// </summary>
    private static bool IsAppiumServerRunning(string statusUrl)
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            
            // Use Task.Run to safely execute async operation synchronously in tests
            // This avoids deadlock issues while maintaining synchronous test behavior
            var task = Task.Run(async () =>
            {
                try
                {
                    var response = await httpClient.GetAsync(statusUrl);
                    return response.IsSuccessStatusCode;
                }
                catch (HttpRequestException)
                {
                    return false; // Server not running
                }
                catch (TaskCanceledException)
                {
                    return false; // Request timed out
                }
            });

            // Wait for the task with a reasonable timeout
            return task.Wait(TimeSpan.FromSeconds(10)) && task.Result;
        }
        catch
        {
            return false; // Any other error means server is not accessible
        }
    }

    public void Dispose()
    {
        try
        {
            _app?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Error disposing app: {ex.Message}");
        }
    }
}