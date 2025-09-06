namespace UI.Tests;

public abstract class BaseTest
{
    private const string APP_PACKAGE = "com.darioalonso.binnacle";
    private const string APPIUM_SERVER_URL = "http://localhost:4723";
    protected AppiumDriver _driver;

    [SetUp]
    public void Setup()
    {
        // Ensure app is built and installed fresh on emulator
        AppInstalation.PrepareAndInstallApp();

        // Start Appium server if not already running
        AppiumServerHelper.StartAppiumLocalServer();

        var driverOptions = new AppiumOptions();

        // Use properties instead of AddAdditionalAppiumOption for standard capabilities
        driverOptions.PlatformName = "Android";
        driverOptions.AutomationName = "UIAutomator2";
        driverOptions.DeviceName = "Android Emulator";

        // Fix: Use the actual Android version "14" instead of API level "34"
        var platformVersion = Environment.GetEnvironmentVariable("ANDROID_VERSION") ?? "14";
        driverOptions.PlatformVersion = platformVersion;

        driverOptions.AddAdditionalAppiumOption("newCommandTimeout", 300);

        _driver = new AndroidDriver(new Uri(APPIUM_SERVER_URL), driverOptions);

        // Activate app
        _driver.ActivateApp(APP_PACKAGE);
    }

    [TearDown]
    public void TearDown()
    {
        _driver?.Quit();
        _driver?.Dispose();
        AppiumServerHelper.DisposeAppiumLocalServer();
    }

    // Simple helper methods
    protected IWebElement FindElementWithTimeout(string id, int timeoutSeconds)
    {
        // For MAUI apps, the AutomationId maps to AccessibilityId in Appium
        TestContext.Out.WriteLine($"Looking for element with id '{id}'");
        id = GetFinalId(id);
        TestContext.Out.WriteLine($"Searching for element '{id}' with {timeoutSeconds}s timeout...");
        var endTime = DateTime.Now.AddSeconds(timeoutSeconds);

        while (DateTime.Now < endTime)
        {
            try
            {
                // For MAUI apps, use MobileBy.AccessibilityId to find elements by AutomationId
                var element = _driver.FindElement(By.Id(id));
                if (element.Displayed)
                {
                    TestContext.Out.WriteLine($"✅ Element '{id}' found and displayed");
                    return element;
                }
                else
                {
                    TestContext.Out.WriteLine($"⚠️ Element '{id}' found but not displayed");
                }
            }
            catch (NoSuchElementException)
            {
                // Element not found yet, continue waiting
                TestContext.Out.WriteLine($"Element '{id}' not found, retrying... ({(DateTime.Now - endTime.AddSeconds(-timeoutSeconds)).TotalSeconds:F1}s elapsed)");
            }

            // Every 5 seconds, dump available elements for debugging
            if ((DateTime.Now - endTime.AddSeconds(-timeoutSeconds)).TotalSeconds % 5 == 0)
            {
                DumpAvailableElements();
            }

            System.Threading.Thread.Sleep(500);
        }

        // Final attempt with detailed debugging before failing
        TestContext.Out.WriteLine($"❌ Final attempt to find element '{id}' failed. Dumping page information...");
        DumpPageInformation();

        throw new TimeoutException($"Element '{id}' not found within {timeoutSeconds} seconds");
    }

    protected IWebElement[] FindElementsWithTimeout(int timeoutSeconds, params string[] ids)
    {
        foreach (var id in ids)
            TestContext.Out.WriteLine($"Looking for element with id '{id}'");

        var foundElements = new List<IWebElement>();
        var foundIds = new HashSet<string>(); // Track found IDs efficiently
        var idsList = ids.Select(id => GetFinalId(id)).ToList();
        
        foreach (var id in idsList)
            TestContext.Out.WriteLine($"Searching for element '{id}' with {timeoutSeconds}s timeout...");
                
        var startTime = DateTime.Now;
        var endTime = startTime.AddSeconds(timeoutSeconds);
        var lastDebugTime = startTime;
        
        while (DateTime.Now < endTime && foundIds.Count < idsList.Count)
        {
            var remainingIds = idsList.Where(id => !foundIds.Contains(id)).ToList();
            
            foreach (var id in remainingIds)
            {
                try
                {
                    var element = _driver.FindElement(By.Id(id));
                    if (element.Displayed)
                    {
                        TestContext.Out.WriteLine($"✅ Element '{id}' found and displayed");
                        foundElements.Add(element);
                        foundIds.Add(id);
                    }
                    else
                    {
                        TestContext.Out.WriteLine($"⚠️ Element '{id}' found but not displayed");
                    }
                }
                catch (NoSuchElementException)
                {
                    // Element not found yet, continue waiting
                    var elapsed = (DateTime.Now - startTime).TotalSeconds;
                    TestContext.Out.WriteLine($"Element '{id}' not found, retrying... ({elapsed:F1}s elapsed)");
                }
                catch (StaleElementReferenceException)
                {
                    // Element became stale, remove from found collections and retry
                    TestContext.Out.WriteLine($"⚠️ Element '{id}' became stale, retrying...");
                    var elementToRemove = foundElements.FirstOrDefault(e => 
                    {
                        try 
                        { 
                            return e.GetAttribute("resourceId") == id; 
                        } 
                        catch 
                        { 
                            return false; 
                        }
                    });
                    if (elementToRemove != null)
                    {
                        foundElements.Remove(elementToRemove);
                        foundIds.Remove(id);
                    }
                }
            }

            // Every 5 seconds, dump available elements for debugging
            var timeSinceLastDebug = (DateTime.Now - lastDebugTime).TotalSeconds;
            if (timeSinceLastDebug >= 5.0)
            {
                DumpAvailableElements();
                lastDebugTime = DateTime.Now;
            }

            // Early exit if all elements found
            if (foundIds.Count == idsList.Count)
            {
                TestContext.Out.WriteLine($"✅ All {foundIds.Count} elements found successfully");
                break;
            }

            System.Threading.Thread.Sleep(500);
        }

        // Validate elements are still accessible before returning
        var validElements = new List<IWebElement>();
        foreach (var element in foundElements)
        {
            try
            {
                // Test accessibility by checking if element is still displayed
                var isDisplayed = element.Displayed;
                validElements.Add(element);
            }
            catch (StaleElementReferenceException)
            {
                TestContext.Out.WriteLine("⚠️ Removing stale element from results");
            }
        }

        // Final attempt with detailed debugging before failing
        if (validElements.Count == 0)
        {
            TestContext.Out.WriteLine($"❌ Final attempt to find elements '{string.Join(", ", idsList)}' failed. Dumping page information...");
            DumpPageInformation();
            throw new TimeoutException($"None of the elements '{string.Join(", ", idsList)}' were found within {timeoutSeconds} seconds");
        }

        // Check if we found fewer elements than requested
        if (validElements.Count < idsList.Count)
        {
            var missingIds = idsList.Where(id => !foundIds.Contains(id)).ToList();
            TestContext.Out.WriteLine($"⚠️ Only found {validElements.Count} of {idsList.Count} requested elements. Missing: {string.Join(", ", missingIds)}");
            DumpPageInformation();
        }

        return validElements.ToArray();
    }

    private string GetFinalId(string id)
    {
        // For MAUI apps, the AutomationId maps to AccessibilityId in Appium
        return $"com.darioalonso.binnacle:id/{id}";
    }

    private void DumpAvailableElements()
    {
        try
        {
            TestContext.Out.WriteLine("=== Available Elements with Accessibility IDs ===");

            // Try to find all elements with accessibility identifiers
            var elementsWithAccessibilityId = _driver.FindElements(MobileBy.XPath("//*[@content-desc]"));

            if (elementsWithAccessibilityId.Count > 0)
            {
                TestContext.Out.WriteLine($"Found {elementsWithAccessibilityId.Count} elements with accessibility IDs:");
                foreach (var elem in elementsWithAccessibilityId.Take(10)) // Limit to first 10
                {
                    try
                    {
                        var contentDesc = elem.GetAttribute("content-desc");
                        var className = elem.GetAttribute("class");
                        var text = elem.Text;
                        TestContext.Out.WriteLine($"  - AccessibilityId: '{contentDesc}', Class: '{className}', Text: '{text}'");
                    }
                    catch (Exception ex)
                    {
                        TestContext.Out.WriteLine($"  - Error reading element: {ex.Message}");
                    }
                }
            }
            else
            {
                TestContext.Out.WriteLine("No elements with accessibility IDs found");
            }
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"Error dumping elements: {ex.Message}");
        }
    }

    private void DumpPageInformation()
    {
        try
        {
            TestContext.Out.WriteLine("=== Page Source Analysis ===");

            // Get page source for analysis
            var pageSource = _driver.PageSource;
            TestContext.Out.WriteLine($"Page source length: {pageSource.Length} characters");

            // Look for MAUI-specific elements
            if (pageSource.Contains("OverviewTitle"))
            {
                TestContext.Out.WriteLine("✅ 'OverviewTitle' found in page source");
            }
            else
            {
                TestContext.Out.WriteLine("❌ 'OverviewTitle' NOT found in page source");
            }

            // Look for common MAUI indicators
            var mauiIndicators = new[] { "androidx", "MAUI", "Label", "Grid", "ContentPage" };
            foreach (var indicator in mauiIndicators)
            {
                if (pageSource.Contains(indicator))
                {
                    TestContext.Out.WriteLine($"✅ MAUI indicator '{indicator}' found in page source");
                }
            }

            // Try different locator strategies as fallback
            TestContext.Out.WriteLine("=== Alternative Locator Attempts ===");

            // Try by text content (if Overview_Title is translated)
            var possibleTexts = new[] { "Overview", "Resumen", "Vista general" };
            foreach (var text in possibleTexts)
            {
                try
                {
                    var elementByText = _driver.FindElement(MobileBy.XPath($"//*[contains(@text,'{text}')]"));
                    TestContext.Out.WriteLine($"✅ Found element by text '{text}': {elementByText.GetAttribute("class")}");
                }
                catch
                {
                    TestContext.Out.WriteLine($"❌ No element found by text '{text}'");
                }
            }

            // Try by class name (Android TextView for Label)
            try
            {
                var labelElements = _driver.FindElements(MobileBy.ClassName("android.widget.TextView"));
                TestContext.Out.WriteLine($"Found {labelElements.Count} TextView elements (potential Labels)");

                foreach (var label in labelElements.Take(5))
                {
                    try
                    {
                        var text = label.Text;
                        var contentDesc = label.GetAttribute("content-desc");
                        TestContext.Out.WriteLine($"  - TextView text: '{text}', content-desc: '{contentDesc}'");
                    }
                    catch { /* ignore */ }
                }
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Error finding TextView elements: {ex.Message}");
            }

        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"Error dumping page information: {ex.Message}");
        }
    }

    protected void TakeScreenshot([CallerMemberName] string testName = "")
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