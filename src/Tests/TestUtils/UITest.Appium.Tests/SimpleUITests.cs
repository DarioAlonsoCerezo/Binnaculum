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
        
        // First verify Appium server is accessible
        using var httpClient = new HttpClient();
        try 
        {
            var response = httpClient.GetAsync("http://127.0.0.1:4723/status").Result;
            if (!response.IsSuccessStatusCode)
            {
                Skip.If(true, "Appium server is not running. Please start it manually with: appium --address 127.0.0.1 --port 4723 --relaxed-security");
            }
        }
        catch 
        {
            Skip.If(true, "Appium server is not running. Please start it manually with: appium --address 127.0.0.1 --port 4723 --relaxed-security");
        }
        
        try
        {
            // Configure for Android testing
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
            Console.WriteLine($"✅ App launched successfully! State: {appState}");
            Console.WriteLine($"📸 Screenshot size: {screenshot.Length} bytes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}");
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
            
            // Wait a bit for the app to fully load
            Thread.Sleep(3000);
            
            // Try to find any element by XPath (this should work for any MAUI app)
            var query = _app.Query().ByXPath("//*[@clickable='true']").First();
            var elements = _app.FindElements(query);
            
            Assert.True(elements.Count > 0, "Should find at least one clickable element");
            
            Console.WriteLine($"✅ Found {elements.Count} clickable elements");
            
            // Log element details
            for (int i = 0; i < Math.Min(3, elements.Count); i++)
            {
                var element = elements.ElementAt(i);
                Console.WriteLine($"Element {i}: Text='{element.Text}', Class='{element.Class}', Id='{element.Id}'");
            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}");
            throw;
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