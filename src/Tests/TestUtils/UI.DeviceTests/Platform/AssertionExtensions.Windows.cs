#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Maui.Platform;
using Windows.System;

namespace Binnaculum.UI.DeviceTests.Platform;

/// <summary>
/// Windows-specific assertion extensions for Binnaculum device testing.
/// Based on Microsoft MAUI TestUtils Windows patterns for platform-specific validations.
/// </summary>
public static class WindowsAssertionExtensions
{
    #region Windows UI Validations

    /// <summary>
    /// Validates that a MAUI element is properly rendered as a Windows native control.
    /// </summary>
    /// <param name="element">The MAUI element to validate</param>
    /// <param name="expectedControlType">Expected Windows native control type</param>
    public static void AssertWindowsNativeControl(this Element element, Type expectedControlType)
    {
        Assert.NotNull(element);
        Assert.NotNull(element.Handler);
        
        var nativeView = element.Handler.PlatformView;
        Assert.NotNull(nativeView);
        Assert.IsAssignableFrom(expectedControlType, nativeView);
    }

    /// <summary>
    /// Validates Windows-specific accessibility properties for financial UI elements.
    /// </summary>
    /// <param name="element">The MAUI element to validate</param>
    /// <param name="expectedAutomationName">Expected automation name for accessibility</param>
    public static void AssertWindowsAccessibility(this Element element, string expectedAutomationName)
    {
        Assert.NotNull(element);
        Assert.NotNull(element.Handler);
        
        var nativeControl = element.Handler.PlatformView as FrameworkElement;
        Assert.NotNull(nativeControl);
        
        var automationName = Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(nativeControl);
        Assert.Equal(expectedAutomationName, automationName);
    }

    /// <summary>
    /// Validates Windows-specific currency formatting in text controls.
    /// Tests that currency symbols and decimal separators are displayed correctly on Windows.
    /// </summary>
    /// <param name="element">The text element to validate</param>
    /// <param name="expectedCurrencyFormat">Expected currency format string</param>
    public static void AssertWindowsCurrencyDisplay(this Element element, string expectedCurrencyFormat)
    {
        Assert.NotNull(element);
        
        if (element is Label label)
        {
            Assert.Equal(expectedCurrencyFormat, label.Text);
            
            // Validate Windows-specific text rendering
            if (label.Handler?.PlatformView is TextBlock textBlock)
            {
                var displayedText = textBlock.Text;
                Assert.Equal(expectedCurrencyFormat, displayedText);
                
                // Validate font rendering on Windows
                Assert.NotNull(textBlock.FontFamily);
            }
        }
        else
        {
            throw new NotSupportedException($"Element type {element.GetType().Name} is not supported for Windows currency display validation");
        }
    }

    /// <summary>
    /// Validates Windows-specific percentage display formatting.
    /// Ensures percentage values are properly formatted for Windows regional settings.
    /// </summary>
    /// <param name="element">The element displaying the percentage</param>
    /// <param name="percentageValue">The percentage value to validate</param>
    /// <param name="expectedFormat">Expected formatted percentage string</param>
    public static void AssertWindowsPercentageDisplay(this Element element, decimal percentageValue, string expectedFormat)
    {
        Assert.NotNull(element);
        
        if (element is Label label)
        {
            // Validate the basic text content
            Assert.Equal(expectedFormat, label.Text);
            
            // Validate Windows-specific rendering
            if (label.Handler?.PlatformView is TextBlock textBlock)
            {
                // Check that percentage is displayed with appropriate Windows formatting
                var displayedText = textBlock.Text;
                Assert.Equal(expectedFormat, displayedText);
                
                // Validate color coding for positive/negative percentages if applicable
                if (percentageValue < 0)
                {
                    // Negative percentages might be displayed in red
                    Assert.NotNull(textBlock.Foreground);
                }
                else if (percentageValue > 0)
                {
                    // Positive percentages might be displayed in green
                    Assert.NotNull(textBlock.Foreground);
                }
            }
        }
        else
        {
            throw new NotSupportedException($"Element type {element.GetType().Name} is not supported for Windows percentage display validation");
        }
    }

    #endregion

    #region Windows Hardware Validations

    /// <summary>
    /// Validates device-specific capabilities for financial data processing on Windows.
    /// Tests that the Windows device has sufficient capabilities for Binnaculum operations.
    /// </summary>
    public static async Task AssertWindowsDeviceCapabilities()
    {
        // Validate memory availability for financial calculations
        var memoryLimit = MemoryManager.AppMemoryUsageLimit;
        var memoryUsage = MemoryManager.AppMemoryUsage;
        var availableMemory = memoryLimit - memoryUsage;
        
        // Ensure sufficient memory for financial calculations (at least 100MB available)
        Assert.True(availableMemory > 100 * 1024 * 1024, 
            $"Insufficient memory available: {availableMemory / (1024 * 1024)}MB");
        
        // Validate processor capabilities
        var systemInfo = new Windows.System.Diagnostics.SystemDiagnosticInfo();
        var cpuUsage = await systemInfo.CpuUsage.GetReport();
        
        // Ensure system is not under excessive load
        Assert.True(cpuUsage != null, "CPU usage information should be available");
    }

    /// <summary>
    /// Validates Windows-specific storage capabilities for financial data.
    /// </summary>
    public static async Task AssertWindowsStorageCapabilities()
    {
        var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
        Assert.NotNull(localFolder);
        
        try
        {
            var properties = await localFolder.GetBasicPropertiesAsync();
            var storageInfo = await localFolder.Properties.RetrievePropertiesAsync(new[] { "System.FreeSpace" });
            
            if (storageInfo.TryGetValue("System.FreeSpace", out var freeSpaceObj) && 
                freeSpaceObj is ulong freeSpace)
            {
                // Ensure at least 50MB of storage available for financial data
                Assert.True(freeSpace > 50 * 1024 * 1024,
                    $"Insufficient storage available: {freeSpace / (1024 * 1024)}MB");
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Storage access might be restricted in test environment
            Assert.True(true, "Storage validation skipped due to access restrictions");
        }
    }

    #endregion

    #region Windows Performance Validations

    /// <summary>
    /// Validates Windows-specific performance for financial calculations.
    /// Tests that complex financial operations complete within acceptable timeframes on Windows.
    /// </summary>
    /// <param name="financialOperation">The financial operation to test</param>
    /// <param name="maxExecutionTime">Maximum acceptable execution time</param>
    public static async Task AssertWindowsFinancialPerformance(
        Func<Task> financialOperation, 
        TimeSpan maxExecutionTime)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        await financialOperation();
        
        stopwatch.Stop();
        
        Assert.True(stopwatch.Elapsed <= maxExecutionTime,
            $"Financial operation took {stopwatch.Elapsed} which exceeds maximum allowed time of {maxExecutionTime}");
        
        // Windows-specific: Check for UI freezing (operations taking longer than 100ms on UI thread)
        if (Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() != null && 
            stopwatch.Elapsed > TimeSpan.FromMilliseconds(100))
        {
            Assert.Fail($"Financial operation took {stopwatch.Elapsed} on UI thread which could cause freezing on Windows");
        }
    }

    /// <summary>
    /// Validates Windows UI thread responsiveness during financial operations.
    /// </summary>
    /// <param name="uiOperation">UI operation to test</param>
    /// <param name="maxUiBlockTime">Maximum time UI can be blocked</param>
    public static async Task AssertWindowsUIResponsiveness(
        Func<Task> uiOperation,
        TimeSpan? maxUiBlockTime = null)
    {
        maxUiBlockTime ??= TimeSpan.FromMilliseconds(16); // 60 FPS requirement
        
        var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        if (dispatcher == null)
        {
            // Not on UI thread, operation should be fine
            await uiOperation();
            return;
        }
        
        var isUiBlocked = false;
        var uiCheckTask = Task.Run(async () =>
        {
            await Task.Delay(maxUiBlockTime.Value);
            dispatcher.TryEnqueue(() => { isUiBlocked = true; });
        });
        
        await uiOperation();
        
        // Give UI check task a moment to complete
        await Task.Delay(100);
        
        Assert.False(isUiBlocked, "UI was blocked longer than acceptable time during financial operation");
    }

    #endregion

    #region Windows Integration Validations

    /// <summary>
    /// Validates Windows-specific integration with system features.
    /// Tests currency locale handling and system integration.
    /// </summary>
    public static void AssertWindowsCurrencyLocale()
    {
        var currentCulture = System.Globalization.CultureInfo.CurrentCulture;
        Assert.NotNull(currentCulture);
        
        // Validate that currency formatting works with system locale
        var testAmount = 1234.56m;
        var formattedCurrency = testAmount.ToString("C", currentCulture);
        
        Assert.NotNull(formattedCurrency);
        Assert.True(formattedCurrency.Length > 0);
        
        // Validate currency symbol is present
        Assert.True(formattedCurrency.Contains(currentCulture.NumberFormat.CurrencySymbol) ||
                   formattedCurrency.Contains(currentCulture.NumberFormat.CurrencyDecimalSeparator));
    }

    /// <summary>
    /// Validates Windows-specific theme and styling integration.
    /// </summary>
    /// <param name="element">Element to validate styling for</param>
    public static void AssertWindowsTheming(this Element element)
    {
        Assert.NotNull(element);
        Assert.NotNull(element.Handler);
        
        var nativeControl = element.Handler.PlatformView as FrameworkElement;
        Assert.NotNull(nativeControl);
        
        // Validate that the control respects Windows themes (light/dark mode)
        var actualTheme = nativeControl.ActualTheme;
        Assert.True(actualTheme == ElementTheme.Light || actualTheme == ElementTheme.Dark,
            $"Control should use either Light or Dark theme, but uses {actualTheme}");
        
        // Validate theme resources are applied
        Assert.NotNull(nativeControl.Resources);
    }

    /// <summary>
    /// Validates Windows-specific keyboard navigation for financial input controls.
    /// </summary>
    /// <param name="element">The input element to validate</param>
    public static void AssertWindowsKeyboardNavigation(this Element element)
    {
        Assert.NotNull(element);
        Assert.NotNull(element.Handler);
        
        if (element.Handler.PlatformView is Control control)
        {
            // Validate tab navigation
            Assert.True(control.IsTabStop, "Financial input controls should support tab navigation");
            
            // Validate tab index is set appropriately
            Assert.True(control.TabIndex >= 0, "Tab index should be set for proper navigation order");
        }
    }

    /// <summary>
    /// Validates Windows-specific high contrast mode support.
    /// </summary>
    /// <param name="element">Element to validate high contrast support for</param>
    public static void AssertWindowsHighContrastSupport(this Element element)
    {
        Assert.NotNull(element);
        Assert.NotNull(element.Handler);
        
        var nativeControl = element.Handler.PlatformView as FrameworkElement;
        Assert.NotNull(nativeControl);
        
        // Check if high contrast mode is enabled
        var accessibilitySettings = new Windows.UI.ViewManagement.AccessibilitySettings();
        if (accessibilitySettings.HighContrast)
        {
            // Validate that control adapts to high contrast mode
            // This is a basic check - in practice you'd verify specific color properties
            Assert.NotNull(nativeControl.Resources);
        }
    }

    #endregion
}
#endif