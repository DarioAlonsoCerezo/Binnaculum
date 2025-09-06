namespace UI.Tests;

/// <summary>
/// Global setup fixture that runs once for the entire test assembly.
/// This ensures that expensive operations like APK building happen only once,
/// regardless of test execution order or count.
/// </summary>
[SetUpFixture]
public class TestSetupFixture
{
    private static bool _globalSetupCompleted = false;
    private static readonly object _setupLock = new object();

    /// <summary>
    /// Runs once before any tests in the assembly execute.
    /// Performs global initialization like ensuring APK is built and Appium server is ready.
    /// </summary>
    [OneTimeSetUp]
    public void GlobalSetup()
    {
        lock (_setupLock)
        {
            if (_globalSetupCompleted)
            {
                TestContext.Out.WriteLine("?? Global setup already completed, skipping...");
                return;
            }

            TestContext.Out.WriteLine("?? Starting global UI test setup...");

            try
            {
                // Pre-build the APK to avoid race conditions during individual tests
                TestContext.Out.WriteLine("?? Pre-building APK for all UI tests...");
                var testDirectory = TestContext.CurrentContext.TestDirectory;
                var workspaceRoot = AppInstalation.FindWorkspaceRoot(testDirectory);
                
                // This will build the APK once and cache it for all tests
                var apkPath = AppInstalation.EnsureApkExistsThreadSafe();
                TestContext.Out.WriteLine($"? APK pre-built successfully: {apkPath}");

                // Verify Appium server helper is available
                TestContext.Out.WriteLine("?? Verifying Appium server availability...");
                AppiumServerHelper.VerifyAppiumAvailability();
                TestContext.Out.WriteLine("? Appium server helper verified");

                _globalSetupCompleted = true;
                TestContext.Out.WriteLine("?? Global UI test setup completed successfully!");
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"? Global setup failed: {ex.Message}");
                TestContext.Out.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; // Re-throw to fail the entire test run if setup fails
            }
        }
    }

    /// <summary>
    /// Runs once after all tests in the assembly have executed.
    /// Performs global cleanup operations.
    /// </summary>
    [OneTimeTearDown]
    public void GlobalTeardown()
    {
        TestContext.Out.WriteLine("?? Starting global UI test teardown...");

        try
        {
            // Clean up any global resources
            AppiumServerHelper.GlobalCleanup();
            TestContext.Out.WriteLine("? Global cleanup completed");
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"?? Global teardown warning: {ex.Message}");
            // Don't throw in teardown to avoid masking test failures
        }

        TestContext.Out.WriteLine("?? Global UI test teardown completed");
    }
}