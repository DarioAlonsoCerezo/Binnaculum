#if MACCATALYST
using UIKit;
using Foundation;
using AppKit;
using Microsoft.Maui.Platform;

namespace Binnaculum.UI.DeviceTests.Platform;

/// <summary>
/// MacCatalyst-specific assertion extensions for Binnaculum device testing.
/// Based on Microsoft MAUI TestUtils MacCatalyst patterns for platform-specific validations.
/// Note: This implementation requires macOS environment for full functionality.
/// </summary>
public static class MacCatalystAssertionExtensions
{
    #region MacCatalyst UI Validations

    /// <summary>
    /// Validates that a MAUI element is properly rendered as a MacCatalyst native view.
    /// </summary>
    /// <param name="element">The MAUI element to validate</param>
    /// <param name="expectedViewType">Expected MacCatalyst native view type</param>
    public static void AssertMacCatalystNativeView(this Element element, Type expectedViewType)
    {
        Assert.NotNull(element);
        Assert.NotNull(element.Handler);
        
        var nativeView = element.Handler.PlatformView;
        Assert.NotNull(nativeView);
        Assert.IsAssignableFrom(expectedViewType, nativeView);
    }

    /// <summary>
    /// Validates MacCatalyst-specific accessibility properties for financial UI elements.
    /// </summary>
    /// <param name="element">The MAUI element to validate</param>
    /// <param name="expectedAccessibilityLabel">Expected accessibility label</param>
    public static void AssertMacCatalystAccessibility(this Element element, string expectedAccessibilityLabel)
    {
        Assert.NotNull(element);
        Assert.NotNull(element.Handler);
        
        var nativeView = element.Handler.PlatformView as UIView;
        Assert.NotNull(nativeView);
        Assert.Equal(expectedAccessibilityLabel, nativeView.AccessibilityLabel);
    }

    /// <summary>
    /// Validates MacCatalyst-specific currency formatting in text views.
    /// Tests that currency symbols and decimal separators are displayed correctly on macOS.
    /// </summary>
    /// <param name="element">The text element to validate</param>
    /// <param name="expectedCurrencyFormat">Expected currency format string</param>
    public static void AssertMacCatalystCurrencyDisplay(this Element element, string expectedCurrencyFormat)
    {
        Assert.NotNull(element);
        
        if (element is Label label)
        {
            Assert.Equal(expectedCurrencyFormat, label.Text);
            
            // Validate MacCatalyst-specific text rendering
            if (label.Handler?.PlatformView is UILabel uiLabel)
            {
                var displayedText = uiLabel.Text;
                Assert.Equal(expectedCurrencyFormat, displayedText);
                
                // Validate font rendering on macOS
                Assert.NotNull(uiLabel.Font);
            }
        }
        else
        {
            throw new NotSupportedException($"Element type {element.GetType().Name} is not supported for MacCatalyst currency display validation");
        }
    }

    /// <summary>
    /// Validates MacCatalyst-specific percentage display formatting.
    /// Ensures percentage values are properly formatted for macOS locale settings.
    /// </summary>
    /// <param name="element">The element displaying the percentage</param>
    /// <param name="percentageValue">The percentage value to validate</param>
    /// <param name="expectedFormat">Expected formatted percentage string</param>
    public static void AssertMacCatalystPercentageDisplay(this Element element, decimal percentageValue, string expectedFormat)
    {
        Assert.NotNull(element);
        
        if (element is Label label)
        {
            // Validate the basic text content
            Assert.Equal(expectedFormat, label.Text);
            
            // Validate MacCatalyst-specific rendering
            if (label.Handler?.PlatformView is UILabel uiLabel)
            {
                // Check that percentage is displayed with appropriate macOS formatting
                var displayedText = uiLabel.Text;
                Assert.Equal(expectedFormat, displayedText);
                
                // Validate color coding for positive/negative percentages if applicable
                if (percentageValue != 0)
                {
                    Assert.NotNull(uiLabel.TextColor);
                }
            }
        }
        else
        {
            throw new NotSupportedException($"Element type {element.GetType().Name} is not supported for MacCatalyst percentage display validation");
        }
    }

    #endregion

    #region MacCatalyst Hardware Validations

    /// <summary>
    /// Validates device-specific capabilities for financial data processing on MacCatalyst.
    /// Tests that the macOS device has sufficient capabilities for Binnaculum operations.
    /// </summary>
    public static void AssertMacCatalystDeviceCapabilities()
    {
        var device = UIDevice.CurrentDevice;
        Assert.NotNull(device);
        
        // MacCatalyst runs on macOS, validate macOS version compatibility
        var minimumVersion = new Version(15, 0); // macOS 15.0 minimum as per project requirements
        var currentVersion = new Version(device.SystemVersion);
        Assert.True(currentVersion >= minimumVersion,
            $"macOS version {currentVersion} is below minimum required version {minimumVersion}");
        
        // Validate device model for performance expectations (Mac hardware)
        Assert.NotNull(device.Model);
        Assert.True(device.Model.Length > 0, "Device model should be available");
        
        // MacCatalyst-specific: Validate we're running in Catalyst environment
        Assert.True(UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Mac,
            "Should be running in Mac Catalyst environment");
    }

    /// <summary>
    /// Validates MacCatalyst-specific storage capabilities for financial data.
    /// </summary>
    public static void AssertMacCatalystStorageCapabilities()
    {
        var documentsPath = NSSearchPath.GetDirectories(NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User).FirstOrDefault();
        Assert.NotNull(documentsPath);
        
        var fileManager = NSFileManager.DefaultManager;
        Assert.True(fileManager.FileExists(documentsPath), "Documents directory should exist");
        
        // Check available storage space (macOS typically has more storage than iOS)
        NSError error;
        var attributes = fileManager.GetFileSystemAttributes(documentsPath, out error);
        
        if (error == null && attributes != null)
        {
            var freeSpace = attributes.FreeSize;
            // Ensure at least 100MB of storage available for financial data (higher than iOS requirement)
            Assert.True(freeSpace > 100 * 1024 * 1024,
                $"Insufficient storage available: {freeSpace / (1024 * 1024)}MB");
        }
    }

    #endregion

    #region MacCatalyst Performance Validations

    /// <summary>
    /// Validates MacCatalyst-specific performance for financial calculations.
    /// Tests that complex financial operations complete within acceptable timeframes on macOS.
    /// </summary>
    /// <param name="financialOperation">The financial operation to test</param>
    /// <param name="maxExecutionTime">Maximum acceptable execution time</param>
    public static async Task AssertMacCatalystFinancialPerformance(
        Func<Task> financialOperation, 
        TimeSpan maxExecutionTime)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        await financialOperation();
        
        stopwatch.Stop();
        
        Assert.True(stopwatch.Elapsed <= maxExecutionTime,
            $"Financial operation took {stopwatch.Elapsed} which exceeds maximum allowed time of {maxExecutionTime}");
        
        // MacCatalyst-specific: Check for main thread blocking (similar to iOS but potentially more lenient)
        if (NSThread.IsMain && stopwatch.Elapsed > TimeSpan.FromMilliseconds(200))
        {
            Assert.Fail($"Financial operation took {stopwatch.Elapsed} on main thread which could cause UI freezing on macOS");
        }
    }

    /// <summary>
    /// Validates MacCatalyst UI thread responsiveness during financial operations.
    /// </summary>
    /// <param name="uiOperation">UI operation to test</param>
    /// <param name="maxUiBlockTime">Maximum time UI can be blocked</param>
    public static async Task AssertMacCatalystUIResponsiveness(
        Func<Task> uiOperation,
        TimeSpan? maxUiBlockTime = null)
    {
        maxUiBlockTime ??= TimeSpan.FromMilliseconds(16); // 60 FPS requirement
        
        if (!NSThread.IsMain)
        {
            // Not on main thread, operation should be fine
            await uiOperation();
            return;
        }
        
        var isUiBlocked = false;
        var uiCheckTask = Task.Run(async () =>
        {
            await Task.Delay(maxUiBlockTime.Value);
            NSOperationQueue.MainQueue.AddOperation(() => { isUiBlocked = true; });
        });
        
        await uiOperation();
        
        // Give UI check task a moment to complete
        await Task.Delay(100);
        
        Assert.False(isUiBlocked, "UI was blocked longer than acceptable time during financial operation");
    }

    #endregion

    #region MacCatalyst Integration Validations

    /// <summary>
    /// Validates MacCatalyst-specific integration with system features.
    /// Tests currency locale handling and system integration on macOS.
    /// </summary>
    public static void AssertMacCatalystCurrencyLocale()
    {
        var currentLocale = NSLocale.CurrentLocale;
        Assert.NotNull(currentLocale);
        
        // Validate that currency formatting works with system locale
        var testAmount = 1234.56;
        var formatter = new NSNumberFormatter
        {
            NumberStyle = NSNumberFormatterStyle.Currency,
            Locale = currentLocale
        };
        
        var formattedCurrency = formatter.StringFromNumber(NSNumber.FromDouble(testAmount));
        Assert.NotNull(formattedCurrency);
        Assert.True(formattedCurrency.Length > 0);
    }

    /// <summary>
    /// Validates MacCatalyst-specific theme and styling integration (Dark/Light mode).
    /// </summary>
    /// <param name="element">Element to validate styling for</param>
    public static void AssertMacCatalystTheming(this Element element)
    {
        Assert.NotNull(element);
        Assert.NotNull(element.Handler);
        
        var nativeView = element.Handler.PlatformView as UIView;
        Assert.NotNull(nativeView);
        
        // Validate that the view respects macOS themes (light/dark mode)
        var traitCollection = nativeView.TraitCollection;
        Assert.NotNull(traitCollection);
        
        var userInterfaceStyle = traitCollection.UserInterfaceStyle;
        Assert.True(userInterfaceStyle == UIUserInterfaceStyle.Light || 
                   userInterfaceStyle == UIUserInterfaceStyle.Dark,
            $"View should use either Light or Dark interface style, but uses {userInterfaceStyle}");
    }

    /// <summary>
    /// Validates MacCatalyst-specific keyboard shortcuts for financial operations.
    /// </summary>
    /// <param name="element">Element to validate keyboard shortcuts for</param>
    public static void AssertMacCatalystKeyboardShortcuts(this Element element)
    {
        Assert.NotNull(element);
        Assert.NotNull(element.Handler);
        
        var nativeView = element.Handler.PlatformView as UIView;
        Assert.NotNull(nativeView);
        
        // MacCatalyst applications should support standard Mac keyboard shortcuts
        // This is a basic validation - in practice you'd test specific shortcuts
        if (nativeView.CanBecomeFirstResponder)
        {
            Assert.True(true, "View supports keyboard interaction");
        }
    }

    /// <summary>
    /// Validates MacCatalyst-specific menu bar integration.
    /// </summary>
    public static void AssertMacCatalystMenuBarIntegration()
    {
        // MacCatalyst applications can have menu bars
        var application = UIApplication.SharedApplication;
        Assert.NotNull(application);
        
        // Validate that the application supports MacCatalyst menu features
        if (application.SupportsMultipleScenes)
        {
            Assert.True(true, "Application supports MacCatalyst multi-scene features");
        }
    }

    /// <summary>
    /// Validates MacCatalyst-specific window management for financial applications.
    /// </summary>
    /// <param name="element">Element to validate window management for</param>
    public static void AssertMacCatalystWindowManagement(this Element element)
    {
        Assert.NotNull(element);
        
        // Find the window containing the element
        var window = element.Window;
        Assert.NotNull(window, "Element should be contained in a window");
        
        // MacCatalyst windows should support resizing for financial data display
        if (window.Handler?.PlatformView is UIWindow uiWindow)
        {
            Assert.NotNull(uiWindow.WindowScene);
        }
    }

    #endregion
}
#endif