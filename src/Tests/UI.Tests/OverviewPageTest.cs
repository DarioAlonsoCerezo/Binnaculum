using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using System.Runtime.CompilerServices;

namespace UI.Tests;

[Category("Overview")]
public class OverviewPageTest
{
    private AndroidDriver _driver;

    [SetUp]
    public void Setup()
    {
        var driverOptions = new AppiumOptions();
        
        // Use properties instead of AddAdditionalAppiumOption for standard capabilities
        driverOptions.PlatformName = "Android";
        driverOptions.AutomationName = "UIAutomator2";
        driverOptions.DeviceName = "Android Emulator";
        
        // Fix: Use the actual Android version "14" instead of API level "34"
        var platformVersion = Environment.GetEnvironmentVariable("ANDROID_VERSION") ?? "14";
        driverOptions.PlatformVersion = platformVersion;
        
        // Add app-specific options using AddAdditionalAppiumOption
        //driverOptions.AddAdditionalAppiumOption("appPackage", "com.darioalonso.binnacle");
        //driverOptions.AddAdditionalAppiumOption("appActivity", "crc6460ce1c2ed4fc81a9.MainActivity");

        // Clean state for financial app testing (recommended for accuracy)
        //driverOptions.AddAdditionalAppiumOption("noReset", true);        // Clear app data between tests
        //driverOptions.AddAdditionalAppiumOption("fullReset", false);      // Don't reinstall, just clear data
        driverOptions.AddAdditionalAppiumOption("newCommandTimeout", 300);

        _driver = new AndroidDriver(new Uri("http://localhost:4723"), driverOptions);
        
        // Activate app
        _driver.ActivateApp("com.darioalonso.binnacle");
        
        // Wait longer for clean startup (app needs to initialize fresh database, etc.)
        System.Threading.Thread.Sleep(8000);
    }

    [Test]
    public void OverviewTitle_IsDisplayed()
    {
        // Simple element finding with timeout
        var element = FindElementWithTimeout("OverviewTitle", 15);
        
        Assert.Multiple(() =>
        {
            Assert.That(element, Is.Not.Null, "OverviewTitle element should exist");
            Assert.That(element.Displayed, Is.True, "OverviewTitle should be visible");
        });
        
        TakeScreenshot();
    }
    
    //[Test]
    //public void App_LaunchesSuccessfully()
    //{
    //    // Simple responsiveness check
    //    var pageSource = _driver.PageSource;
    //    Assert.That(pageSource, Is.Not.Null.And.Not.Empty, "App should return page source");
        
    //    TakeScreenshot();
    //}

    [TearDown]
    public void TearDown()
    {
        _driver?.Quit();
        _driver?.Dispose();
    }
    
    // Simple helper methods
    private IWebElement FindElementWithTimeout(string id, int timeoutSeconds)
    {
        var endTime = DateTime.Now.AddSeconds(timeoutSeconds);
        
        while (DateTime.Now < endTime)
        {
            try
            {
                var element = _driver.FindElement(By.Id(id));
                if (element.Displayed)
                    return element;
            }
            catch (NoSuchElementException)
            {
                // Element not found yet, continue waiting
            }
            
            System.Threading.Thread.Sleep(500);
        }
        
        throw new TimeoutException($"Element '{id}' not found within {timeoutSeconds} seconds");
    }
    
    private void TakeScreenshot([CallerMemberName] string testName = "")
    {
        try
        {
            var fileName = $"{testName}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            _driver.GetScreenshot().SaveAsFile(fileName);
            TestContext.Out.WriteLine($"Screenshot saved: {fileName}");
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"Screenshot failed: {ex.Message}");
        }
    }
}