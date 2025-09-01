using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium;
using Xunit;

namespace Binnaculum.UI.Tests;

/// <summary>
/// Simple UI tests for the Binnaculum Android app.
/// Tests basic app launch and first page load functionality.
/// 
/// Prerequisites:
/// 1. Android emulator running
/// 2. Binnaculum app installed on the emulator
/// 3. Appium server running: appium --address 127.0.0.1 --port 4723
/// </summary>
public class BasicAppTests : IDisposable
{
    private AndroidDriver? _driver;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public void App_Launches_And_Shows_OverviewPage()
    {
        // Arrange: Start the app
        _driver = CreateAndroidDriver();

        // Act: Wait for the main page to load
        var overviewTitle = WaitForElement(By.Id("OverviewTitle"), _defaultTimeout);

        // Assert: Verify the main page loaded successfully
        Assert.NotNull(overviewTitle);
        Assert.True(overviewTitle.Displayed, "OverviewTitle should be visible");
        
        // Take a screenshot for visual confirmation
        var screenshot = _driver.GetScreenshot();
        Assert.True(screenshot.AsByteArray.Length > 0, "Screenshot should contain data");
        
        Console.WriteLine("? App launched successfully and OverviewTitle is visible");
        Console.WriteLine($"?? Screenshot captured: {screenshot.AsByteArray.Length} bytes");
    }

    [Fact]
    public void App_Launches_And_LoadsWithoutCrashing()
    {
        // Arrange: Start the app
        _driver = CreateAndroidDriver();

        // Act: Wait a reasonable time for startup
        Thread.Sleep(5000);

        // Assert: App should be running and responsive
        Assert.NotNull(_driver);
        
        // Try to get the page source to verify app is responsive
        var pageSource = _driver.PageSource;
        Assert.NotNull(pageSource);
        Assert.True(pageSource.Length > 0, "Page source should not be empty");
        
        // Take screenshot as proof of success
        var screenshot = _driver.GetScreenshot();
        Assert.True(screenshot.AsByteArray.Length > 0, "Screenshot should contain data");
        
        Console.WriteLine("? App launched without crashing");
        Console.WriteLine($"?? Page source length: {pageSource.Length} characters");
        Console.WriteLine($"?? Screenshot size: {screenshot.AsByteArray.Length} bytes");
    }

    private AndroidDriver CreateAndroidDriver()
    {
        var options = new AppiumOptions();
        
        // Basic Android capabilities
        options.AddAdditionalCapability("platformName", "Android");
        options.AddAdditionalCapability("automationName", "UiAutomator2");
        
        // App identification - adjust these based on your actual app package
        options.AddAdditionalCapability("appPackage", "com.darioalonso.binnacle");
        options.AddAdditionalCapability("appActivity", "crc64f728827fec74e9c3.MainActivity");
        
        // Don't reset app data for faster tests
        options.AddAdditionalCapability("noReset", true);
        options.AddAdditionalCapability("fullReset", false);
        
        // Timeouts
        options.AddAdditionalCapability("newCommandTimeout", 300); // 5 minutes
        
        try
        {
            var serverUri = new Uri("http://127.0.0.1:4723");
            var driver = new AndroidDriver(serverUri, options);
            
            // Set implicit wait for element finding
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
            
            return driver;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to create Android driver. Make sure:\n" +
                              $"1. Android emulator is running\n" +
                              $"2. Binnaculum app is installed\n" +
                              $"3. Appium server is running on port 4723\n" +
                              $"Original error: {ex.Message}", ex);
        }
    }

    private IWebElement WaitForElement(By by, TimeSpan timeout)
    {
        var endTime = DateTime.Now.Add(timeout);
        
        while (DateTime.Now < endTime)
        {
            try
            {
                var element = _driver!.FindElement(by);
                if (element.Displayed)
                {
                    return element;
                }
            }
            catch (NoSuchElementException)
            {
                // Element not found yet, continue waiting
            }
            catch (WebDriverException)
            {
                // Other WebDriver issues, continue waiting
            }
            
            Thread.Sleep(1000); // Wait 1 second before trying again
        }
        
        throw new TimeoutException($"Element {by} was not found within {timeout.TotalSeconds} seconds");
    }

    public void Dispose()
    {
        try
        {
            _driver?.Quit();
            _driver?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Error disposing driver: {ex.Message}");
        }
    }
}