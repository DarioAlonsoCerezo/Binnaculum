namespace UI.Tests;

public abstract class BaseTest
{
    protected AppiumDriver _driver;

    [SetUp]
    public void Setup()
    {
        // Ensure app is built and installed fresh on emulator
        PrepareAndInstallApp();

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

        _driver = new AndroidDriver(new Uri("http://localhost:4723"), driverOptions);

        // Activate app
        _driver.ActivateApp("com.darioalonso.binnacle");
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
        id = $"com.darioalonso.binnacle:id/{id}";
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

    // ADB-based app management methods
    private void PrepareAndInstallApp()
    {
        const string packageName = "com.darioalonso.binnacle";

        try
        {
            TestContext.Out.WriteLine("=== Starting App Preparation and Installation ===");

            // Step 1: Check for APK and build if necessary
            var apkPath = EnsureApkExists();
            TestContext.Out.WriteLine($"APK verified at: {apkPath}");

            // Step 2: Check if app is installed on emulator and uninstall if found
            if (IsAppInstalledOnDevice(packageName))
            {
                TestContext.Out.WriteLine($"App {packageName} found on device. Uninstalling...");
                UninstallAppFromDevice(packageName);
                TestContext.Out.WriteLine("App uninstalled successfully");
            }
            else
            {
                TestContext.Out.WriteLine($"App {packageName} not found on device. Proceeding with fresh installation.");
            }

            // Step 3: Install the app
            TestContext.Out.WriteLine("Installing app on device...");
            InstallAppOnDevice(apkPath);
            TestContext.Out.WriteLine("App installed successfully");

            // Step 4: Verify installation
            if (IsAppInstalledOnDevice(packageName))
            {
                TestContext.Out.WriteLine("✅ App installation verified successfully");
            }
            else
            {
                throw new Exception("❌ App installation verification failed");
            }

        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"❌ App preparation failed: {ex.Message}");
            throw;
        }
    }

    private string EnsureApkExists()
    {
        // Calculate workspace root from test directory
        var testDirectory = TestContext.CurrentContext.TestDirectory;

        // Navigate from test directory to workspace root
        // Test directory structure: C:\repos\Binnaculum\src\Tests\UI.Tests\bin\Debug\net9.0\
        // Need to go up 6 levels: bin -> Debug -> UI.Tests -> Tests -> src -> Binnaculum (workspace root)
        var workspaceRoot = Path.GetFullPath(Path.Combine(testDirectory, "..", "..", "..", "..", "..", ".."));

        // Use Release build for better performance and production-like testing
        var apkDirectory = Path.Combine(workspaceRoot, "src", "UI", "bin", "Release", "net9.0-android");

        TestContext.Out.WriteLine($"Test directory: {testDirectory}");
        TestContext.Out.WriteLine($"Workspace root: {workspaceRoot}");
        TestContext.Out.WriteLine($"APK directory (Release): {apkDirectory}");

        // Look for common APK naming patterns (including wildcards for generated names)
        string? apkPath = null;

        if (Directory.Exists(apkDirectory))
        {
            // First try exact naming patterns
            var possibleApkNames = new[]
            {
                "com.darioalonso.binnacle-Signed.apk",
                "com.darioalonso.binnacle.apk",
                "Binnaculum-Signed.apk",
                "Binnaculum.apk"
            };

            foreach (var apkName in possibleApkNames)
            {
                var candidatePath = Path.Combine(apkDirectory, apkName);
                if (File.Exists(candidatePath))
                {
                    apkPath = candidatePath;
                    TestContext.Out.WriteLine($"Found existing APK (Release): {apkPath}");
                    break;
                }
                else
                {
                    TestContext.Out.WriteLine($"APK not found: {candidatePath}");
                }
            }

            // If no exact match found, look for any APK files
            if (apkPath == null)
            {
                var apkFiles = Directory.GetFiles(apkDirectory, "*.apk");
                if (apkFiles.Length > 0)
                {
                    apkPath = apkFiles[0]; // Use first APK found
                    TestContext.Out.WriteLine($"Found APK by wildcard search (Release): {apkPath}");
                }
            }
        }
        else
        {
            TestContext.Out.WriteLine($"Release APK directory does not exist: {apkDirectory}");
        }

        // If no Release APK found, try Debug as fallback
        if (apkPath == null)
        {
            TestContext.Out.WriteLine("No Release APK found. Checking Debug build as fallback...");
            var debugApkDirectory = Path.Combine(workspaceRoot, "src", "UI", "bin", "Debug", "net9.0-android");

            if (Directory.Exists(debugApkDirectory))
            {
                var apkFiles = Directory.GetFiles(debugApkDirectory, "*.apk");
                if (apkFiles.Length > 0)
                {
                    apkPath = apkFiles[0];
                    TestContext.Out.WriteLine($"Found Debug APK as fallback: {apkPath}");
                }
            }
        }

        // If no APK found at all, build the project in Release mode
        if (apkPath == null)
        {
            TestContext.Out.WriteLine("No APK found. Building Binnaculum project in Release mode...");
            BuildBinnaculumProject(workspaceRoot);

            // Check again for APK after build using wildcard search (Release first, then Debug fallback)
            if (Directory.Exists(apkDirectory))
            {
                var apkFiles = Directory.GetFiles(apkDirectory, "*.apk");
                if (apkFiles.Length > 0)
                {
                    apkPath = apkFiles[0];
                    TestContext.Out.WriteLine($"APK created after Release build: {apkPath}");
                }
            }

            // Fallback to Debug directory after build
            if (apkPath == null)
            {
                var debugApkDirectory = Path.Combine(workspaceRoot, "src", "UI", "bin", "Debug", "net9.0-android");
                if (Directory.Exists(debugApkDirectory))
                {
                    var apkFiles = Directory.GetFiles(debugApkDirectory, "*.apk");
                    if (apkFiles.Length > 0)
                    {
                        apkPath = apkFiles[0];
                        TestContext.Out.WriteLine($"APK created after build (Debug fallback): {apkPath}");
                    }
                }
            }

            if (apkPath == null)
            {
                // List all files in both directories for debugging
                TestContext.Out.WriteLine($"Release directory contents of {apkDirectory}:");
                if (Directory.Exists(apkDirectory))
                {
                    var files = Directory.GetFiles(apkDirectory);
                    foreach (var file in files)
                    {
                        TestContext.Out.WriteLine($"  - {Path.GetFileName(file)}");
                    }
                }
                else
                {
                    TestContext.Out.WriteLine("  Release directory does not exist");
                }

                var debugApkDirectory = Path.Combine(workspaceRoot, "src", "UI", "bin", "Debug", "net9.0-android");
                TestContext.Out.WriteLine($"Debug directory contents of {debugApkDirectory}:");
                if (Directory.Exists(debugApkDirectory))
                {
                    var files = Directory.GetFiles(debugApkDirectory);
                    foreach (var file in files)
                    {
                        TestContext.Out.WriteLine($"  - {Path.GetFileName(file)}");
                    }
                }
                else
                {
                    TestContext.Out.WriteLine("  Debug directory does not exist");
                }

                throw new FileNotFoundException($"APK not found after build in Release or Debug directories");
            }
        }

        return apkPath;
    }

    private void BuildBinnaculumProject(string workspaceRoot)
    {
        // Correct path based on workspace structure
        var projectPath = Path.Combine(workspaceRoot, "src", "UI", "Binnaculum.csproj");

        TestContext.Out.WriteLine($"Looking for project at: {projectPath}");

        if (!File.Exists(projectPath))
        {
            // List contents of src directory for debugging
            var srcDirectory = Path.Combine(workspaceRoot, "src");
            TestContext.Out.WriteLine($"Contents of {srcDirectory}:");
            if (Directory.Exists(srcDirectory))
            {
                var directories = Directory.GetDirectories(srcDirectory);
                foreach (var dir in directories)
                {
                    TestContext.Out.WriteLine($"  - {Path.GetFileName(dir)}");
                }
            }

            throw new FileNotFoundException($"Binnaculum project not found at: {projectPath}");
        }

        TestContext.Out.WriteLine($"Building project in Release mode: {projectPath}");

        // Build in Release mode for better performance and production-like testing
        var buildResult = ExecuteCommand("dotnet", $"build \"{projectPath}\" -f net9.0-android -c Release", workspaceRoot);

        if (buildResult.ExitCode != 0)
        {
            TestContext.Out.WriteLine("❌ Release build failed. Attempting Debug build as fallback...");

            // Fallback to Debug build if Release fails
            var debugBuildResult = ExecuteCommand("dotnet", $"build \"{projectPath}\" -f net9.0-android -c Debug", workspaceRoot);

            if (debugBuildResult.ExitCode != 0)
            {
                throw new Exception($"Both Release and Debug builds failed. Release error: {buildResult.Error}. Debug error: {debugBuildResult.Error}");
            }
            else
            {
                TestContext.Out.WriteLine("⚠️ Debug build succeeded as fallback");
                TestContext.Out.WriteLine($"Debug build output: {debugBuildResult.Output}");
            }
        }
        else
        {
            TestContext.Out.WriteLine("✅ Release build completed successfully");
            TestContext.Out.WriteLine($"Release build output: {buildResult.Output}");
        }
    }

    private bool IsAppInstalledOnDevice(string packageName)
    {
        var result = ExecuteCommand("adb", $"shell pm list packages {packageName}");
        var isInstalled = !string.IsNullOrEmpty(result.Output) && result.Output.Contains(packageName);

        TestContext.Out.WriteLine($"Checking if {packageName} is installed: {(isInstalled ? "YES" : "NO")}");

        return isInstalled;
    }

    private void UninstallAppFromDevice(string packageName)
    {
        var result = ExecuteCommand("adb", $"uninstall {packageName}");

        if (result.ExitCode != 0 && !result.Output.Contains("DELETE_FAILED_INTERNAL_ERROR"))
        {
            TestContext.Out.WriteLine($"⚠️ Uninstall warning - Exit code: {result.ExitCode}, Output: {result.Output}, Error: {result.Error}");
        }
        else
        {
            TestContext.Out.WriteLine("App uninstalled successfully");
        }
    }

    private void InstallAppOnDevice(string apkPath)
    {
        var result = ExecuteCommand("adb", $"install -r \"{apkPath}\"");

        if (result.ExitCode != 0)
        {
            throw new Exception($"APK installation failed with exit code {result.ExitCode}. Output: {result.Output}. Error: {result.Error}");
        }

        if (result.Output.Contains("INSTALL_FAILED") || result.Error.Contains("INSTALL_FAILED"))
        {
            throw new Exception($"APK installation failed: {result.Output} {result.Error}");
        }

        TestContext.Out.WriteLine($"Installation result: {result.Output}");
    }

    private (int ExitCode, string Output, string Error) ExecuteCommand(string command, string arguments, string? workingDirectory = null)
    {
        TestContext.Out.WriteLine($"Executing: {command} {arguments}");

        using var process = new Process();
        process.StartInfo.FileName = command;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        if (!string.IsNullOrEmpty(workingDirectory))
        {
            process.StartInfo.WorkingDirectory = workingDirectory;
        }

        var output = new System.Text.StringBuilder();
        var error = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                TestContext.Out.WriteLine($"[OUT] {e.Data}");
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                error.AppendLine(e.Data);
                TestContext.Out.WriteLine($"[ERR] {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Set timeout for long-running operations like builds
        var timeout = command == "dotnet" ? TimeSpan.FromMinutes(5) : TimeSpan.FromMinutes(2);

        if (!process.WaitForExit((int)timeout.TotalMilliseconds))
        {
            process.Kill();
            throw new TimeoutException($"Command '{command} {arguments}' timed out after {timeout.TotalMinutes} minutes");
        }

        return (process.ExitCode, output.ToString(), error.ToString());
    }

}