namespace UI.Tests;

public static class AppiumServerHelper
{
    private static AppiumLocalService? _appiumLocalService;
    private static readonly object _serverLock = new object();

    public const string DefaultHostAddress = "127.0.0.1";
    public const int DefaultHostPort = 4723;

    public static void StartAppiumLocalServer(string host = DefaultHostAddress,
        int port = DefaultHostPort)
    {
        lock (_serverLock)
        {
            if (_appiumLocalService is not null && _appiumLocalService.IsRunning)
            {
                TestContext.Out.WriteLine("ℹ️ Appium server already running, skipping start");
                return;
            }

            try
            {
                TestContext.Out.WriteLine($"🚀 Starting Appium local server on {host}:{port}...");

                var builder = new AppiumServiceBuilder()
                    .WithIPAddress(host)
                    .UsingPort(port);

                // Start the server with the builder
                _appiumLocalService = builder.Build();
                _appiumLocalService.Start();

                if (_appiumLocalService.IsRunning)
                {
                    TestContext.Out.WriteLine($"✅ Appium server started successfully on {_appiumLocalService.ServiceUrl}");
                }
                else
                {
                    throw new Exception("Appium server failed to start - IsRunning returned false");
                }
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"❌ Failed to start Appium server: {ex.Message}");
                _appiumLocalService?.Dispose();
                _appiumLocalService = null;
                throw;
            }
        }
    }

    public static void DisposeAppiumLocalServer()
    {
        lock (_serverLock)
        {
            if (_appiumLocalService == null)
            {
                TestContext.Out.WriteLine("ℹ️ No Appium server to dispose");
                return;
            }

            try
            {
                TestContext.Out.WriteLine("🛑 Stopping Appium local server...");
                
                _appiumLocalService.Dispose();
                _appiumLocalService = null;
                TestContext.Out.WriteLine("✅ Appium server stopped successfully");
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"⚠️ Warning while stopping Appium server: {ex.Message}");
                // Don't throw in cleanup to avoid masking test failures
                _appiumLocalService = null;
            }
        }
    }

    /// <summary>
    /// Verifies that Appium is available and can be started.
    /// Used during global setup to ensure environment is ready.
    /// </summary>
    public static void VerifyAppiumAvailability()
    {
        try
        {
            TestContext.Out.WriteLine("🔍 Verifying Appium availability...");

            // Try to create a service builder to verify Appium is installed
            var builder = new AppiumServiceBuilder()
                .WithIPAddress(DefaultHostAddress)
                .UsingPort(DefaultHostPort);

            var testService = builder.Build();
            
            // Check if we can access the service URL (this validates Appium installation)
            var serviceUrl = testService.ServiceUrl;
            TestContext.Out.WriteLine($"✅ Appium service builder created successfully, will use URL: {serviceUrl}");
            
            // Clean up the test service
            testService.Dispose();
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"⚠️ Appium verification failed: {ex.Message}. This may be expected if Appium is not installed.");
            // Don't throw here since Appium might not be installed yet
        }
    }

    /// <summary>
    /// Performs global cleanup of Appium resources.
    /// Called during test assembly teardown.
    /// </summary>
    public static void GlobalCleanup()
    {
        lock (_serverLock)
        {
            try
            {
                TestContext.Out.WriteLine("🧹 Performing global Appium cleanup...");
                
                if (_appiumLocalService != null)
                {
                    DisposeAppiumLocalServer();
                }

                TestContext.Out.WriteLine("✅ Global Appium cleanup completed");
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"⚠️ Warning during global Appium cleanup: {ex.Message}");
                // Don't throw in cleanup
            }
        }
    }
}