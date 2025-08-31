#if IOS
using UIKit;
using Foundation;
using Microsoft.Maui.Platform;

namespace Binnaculum.UI.DeviceTests.Platform;

/// <summary>
/// iOS-specific assertion extensions for Binnaculum device testing.
/// Based on Microsoft MAUI TestUtils iOS patterns for platform-specific validations.
/// Note: This implementation requires macOS environment for full functionality.
/// </summary>
public static class iOSAssertionExtensions
{
    #region iOS UI Validations

    /// <summary>
    /// Validates that a MAUI element is properly rendered as an iOS native view.
    /// </summary>
    /// <param name="element">The MAUI element to validate</param>
    /// <param name="expectedViewType">Expected iOS native view type</param>
    public static void AssertIOSNativeView(this Element element, Type expectedViewType)
    {
        Assert.NotNull(element);
        Assert.NotNull(element.Handler);
        
        var nativeView = element.Handler.PlatformView;
        Assert.NotNull(nativeView);
        Assert.IsAssignableFrom(expectedViewType, nativeView);
    }

    /// <summary>
    /// Validates iOS-specific accessibility properties for financial UI elements.
    /// </summary>
    /// <param name="element">The MAUI element to validate</param>
    /// <param name="expectedAccessibilityLabel">Expected accessibility label</param>
    public static void AssertIOSAccessibility(this Element element, string expectedAccessibilityLabel)
    {
        Assert.NotNull(element);
        Assert.NotNull(element.Handler);
        
        var nativeView = element.Handler.PlatformView as UIView;
        Assert.NotNull(nativeView);
        Assert.Equal(expectedAccessibilityLabel, nativeView.AccessibilityLabel);
    }

    /// <summary>
    /// Validates iOS-specific currency formatting in text views.
    /// Tests that currency symbols and decimal separators are displayed correctly on iOS.
    /// </summary>
    /// <param name="element">The text element to validate</param>
    /// <param name="expectedCurrencyFormat">Expected currency format string</param>
    public static void AssertIOSCurrencyDisplay(this Element element, string expectedCurrencyFormat)
    {
        Assert.NotNull(element);
        
        if (element is Label label)
        {
            Assert.Equal(expectedCurrencyFormat, label.Text);
            
            // Validate iOS-specific text rendering
            if (label.Handler?.PlatformView is UILabel uiLabel)
            {
                var displayedText = uiLabel.Text;
                Assert.Equal(expectedCurrencyFormat, displayedText);
                
                // Validate font rendering on iOS
                Assert.NotNull(uiLabel.Font);
            }
        }
        else
        {
            throw new NotSupportedException($"Element type {element.GetType().Name} is not supported for iOS currency display validation");
        }
    }

    /// <summary>
    /// Validates iOS-specific percentage display formatting.
    /// Ensures percentage values are properly formatted for iOS locale settings.
    /// </summary>
    /// <param name="element">The element displaying the percentage</param>
    /// <param name="percentageValue">The percentage value to validate</param>
    /// <param name="expectedFormat">Expected formatted percentage string</param>
    public static void AssertIOSPercentageDisplay(this Element element, decimal percentageValue, string expectedFormat)
    {
        Assert.NotNull(element);
        
        if (element is Label label)
        {
            // Validate the basic text content
            Assert.Equal(expectedFormat, label.Text);
            
            // Validate iOS-specific rendering
            if (label.Handler?.PlatformView is UILabel uiLabel)
            {
                // Check that percentage is displayed with appropriate iOS formatting
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
            throw new NotSupportedException($"Element type {element.GetType().Name} is not supported for iOS percentage display validation");
        }
    }

    #endregion

    #region iOS Hardware Validations

    /// <summary>
    /// Validates device-specific capabilities for financial data processing on iOS.
    /// Tests that the iOS device has sufficient capabilities for Binnaculum operations.
    /// </summary>
    public static void AssertIOSDeviceCapabilities()
    {
        var device = UIDevice.CurrentDevice;
        Assert.NotNull(device);
        
        // Validate iOS version compatibility
        var minimumVersion = new Version(15, 0); // iOS 15.0 minimum as per project requirements
        var currentVersion = new Version(device.SystemVersion);
        Assert.True(currentVersion >= minimumVersion,
            $"iOS version {currentVersion} is below minimum required version {minimumVersion}");
        
        // Validate device model for performance expectations
        Assert.NotNull(device.Model);
        Assert.True(device.Model.Length > 0, "Device model should be available");
    }

    /// <summary>
    /// Validates iOS-specific storage capabilities for financial data.
    /// </summary>
    public static void AssertIOSStorageCapabilities()
    {
        var documentsPath = NSSearchPath.GetDirectories(NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User).FirstOrDefault();
        Assert.NotNull(documentsPath);
        
        var fileManager = NSFileManager.DefaultManager;
        Assert.True(fileManager.FileExists(documentsPath), "Documents directory should exist");
        
        // Check available storage space
        NSError error;
        var attributes = fileManager.GetFileSystemAttributes(documentsPath, out error);
        
        if (error == null && attributes != null)
        {
            var freeSpace = attributes.FreeSize;
            // Ensure at least 50MB of storage available for financial data
            Assert.True(freeSpace > 50 * 1024 * 1024,
                $"Insufficient storage available: {freeSpace / (1024 * 1024)}MB");
        }
    }

    #endregion

    #region iOS Performance Validations

    /// <summary>
    /// Validates iOS-specific performance for financial calculations.
    /// Tests that complex financial operations complete within acceptable timeframes on iOS.
    /// </summary>
    /// <param name="financialOperation">The financial operation to test</param>
    /// <param name="maxExecutionTime">Maximum acceptable execution time</param>
    public static async Task AssertIOSFinancialPerformance(
        Func<Task> financialOperation, 
        TimeSpan maxExecutionTime)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        await financialOperation();
        
        stopwatch.Stop();
        
        Assert.True(stopwatch.Elapsed <= maxExecutionTime,
            $"Financial operation took {stopwatch.Elapsed} which exceeds maximum allowed time of {maxExecutionTime}");
        
        // iOS-specific: Check for main thread blocking
        if (NSThread.IsMain && stopwatch.Elapsed > TimeSpan.FromMilliseconds(100))
        {
            Assert.Fail($"Financial operation took {stopwatch.Elapsed} on main thread which could cause UI freezing on iOS");
        }
    }

    /// <summary>
    /// Validates iOS UI thread responsiveness during financial operations.
    /// </summary>
    /// <param name="uiOperation">UI operation to test</param>
    /// <param name="maxUiBlockTime">Maximum time UI can be blocked</param>
    public static async Task AssertIOSUIResponsiveness(
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

    #region iOS Integration Validations

    /// <summary>
    /// Validates iOS-specific integration with system features.
    /// Tests currency locale handling and system integration.
    /// </summary>
    public static void AssertIOSCurrencyLocale()
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
    /// Validates iOS-specific theme and styling integration (Dark/Light mode).
    /// </summary>
    /// <param name="element">Element to validate styling for</param>
    public static void AssertIOSTheming(this Element element)
    {
        Assert.NotNull(element);
        Assert.NotNull(element.Handler);
        
        var nativeView = element.Handler.PlatformView as UIView;
        Assert.NotNull(nativeView);
        
        // Validate that the view respects iOS themes (light/dark mode)
        if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
        {
            var traitCollection = nativeView.TraitCollection;
            Assert.NotNull(traitCollection);
            
            var userInterfaceStyle = traitCollection.UserInterfaceStyle;
            Assert.True(userInterfaceStyle == UIUserInterfaceStyle.Light || 
                       userInterfaceStyle == UIUserInterfaceStyle.Dark,
                $"View should use either Light or Dark interface style, but uses {userInterfaceStyle}");
        }
    }

    /// <summary>
    /// Validates iOS-specific VoiceOver accessibility support.
    /// </summary>
    /// <param name="element">Element to validate VoiceOver support for</param>
    public static void AssertIOSVoiceOverSupport(this Element element)
    {
        Assert.NotNull(element);
        Assert.NotNull(element.Handler);
        
        var nativeView = element.Handler.PlatformView as UIView;
        Assert.NotNull(nativeView);
        
        // Validate accessibility properties for VoiceOver
        if (UIAccessibility.IsVoiceOverRunning)
        {
            Assert.True(nativeView.IsAccessibilityElement, 
                "Financial UI elements should be accessible when VoiceOver is running");
            Assert.NotNull(nativeView.AccessibilityLabel);
            Assert.True(nativeView.AccessibilityLabel.Length > 0, 
                "Accessibility label should not be empty for VoiceOver");
        }
    }

    #endregion
}
#endif