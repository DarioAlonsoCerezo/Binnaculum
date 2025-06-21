using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Controls.Shapes;

namespace Binnaculum.Popups;

public class BasePopup : Popup<PopupCustomResult>
{
    protected readonly CompositeDisposable Disposables = new();
    protected IScheduler UiThread;
    protected IScheduler BackgroundScheduler;

    public BasePopup()
    {
        this.SetAppThemeColor(BackgroundColorProperty, Colors.White, Colors.Black);
        Margin = new Thickness(24, 0);
        Padding = new Thickness(0);

        while (SynchronizationContext.Current == null)
            Thread.Sleep(10);        

        UiThread = new SynchronizationContextScheduler(SynchronizationContext.Current);
        BackgroundScheduler = new NewThreadScheduler(t => new Thread(t) { IsBackground = true });
    }

    /// <summary>
    /// Calculates a height value based on a percentage of the screen height
    /// </summary>
    /// <param name="percentage">A value between 0 and 1 representing the percentage of screen height to use</param>
    /// <returns>The calculated height in device-independent units</returns>
    protected double GetHeightByPercentage(double percentage)
    {
        // Ensure the percentage is within valid range (0-1)
        percentage = Math.Clamp(percentage, 0, 1);

        // Get current display metrics
        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;

        // Calculate screen height in device-independent units
        double screenHeight = displayInfo.Height / displayInfo.Density;

        // Return the calculated height
        return screenHeight * percentage;
    }

    /// <summary>
    /// Calculates a width value based on a percentage of the screen width
    /// </summary>
    /// <param name="percentage"></param>
    /// <returns></returns>
    protected double GetWidthByPercentage(double percentage)
    {
        // Ensure the percentage is within valid range (0-1)
        percentage = Math.Clamp(percentage, 0, 1);
        // Get current display metrics
        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
        // Calculate screen width in device-independent units
        double screenWidth = displayInfo.Width / displayInfo.Density;
        // Return the calculated width
        return screenWidth * percentage;
    }

    /// <summary>
    /// Applies a percentage-based height to a view
    /// </summary>
    /// <param name="view">The view to apply height to</param>
    /// <param name="percentage">A value between 0 and 1 representing the percentage of screen height</param>
    /// <param name="applyAsMinMax">Whether to also apply the height to MinimumHeightRequest and MaximumHeightRequest</param>
    protected void ApplyHeightPercentage(View view, double percentage, bool applyAsMinMax = true)
    {
        double height = GetHeightByPercentage(percentage);        

        view.HeightRequest = height;

        if (applyAsMinMax)
        {
            view.MinimumHeightRequest = height;
            view.MaximumHeightRequest = height;
        }
    }

    protected void ForceFillWidth()
    {
        WidthRequest = GetWidthByPercentage(0.76);
    }

    public void Close()
    {
        Disposables?.Dispose();
        CloseAsync(new PopupCustomResult(null, false));
    }
    public void Close(object? result)
    {
        Disposables?.Dispose();
        CloseAsync(new PopupCustomResult(result, false));
    }
}

public static class PopupExtensions
{
    public static void Show(this Popup popup)
    {

        var appMainpage = Application.Current!.Windows[0].Page!;
        if (appMainpage is Shell navigator)
        {
            navigator.ShowPopup(popup, new PopupOptions
            {
                CanBeDismissedByTappingOutsideOfPopup = false,
                Shadow = null,
                Shape = new RoundRectangle { CornerRadius = 24 }
            });
        }
        else
        {
            throw new InvalidOperationException("The current page is not a NavigationPage.");
        }
    }

    public static async Task<PopupCustomResult> ShowAndWait(this Popup popup)
    {
        var appMainpage = Application.Current!.Windows[0].Page!;
        if (appMainpage is Shell navigator)
        {
            var awaited = await navigator.ShowPopupAsync(popup, new PopupOptions
            {
                CanBeDismissedByTappingOutsideOfPopup = false,
                Shadow = null,
                Shape = new RoundRectangle { CornerRadius = 24 }
            }).ConfigureAwait(false);
            
            if (awaited is IPopupResult<PopupCustomResult> iPopup)
                return new PopupCustomResult(iPopup.Result?.Result, false);
            
            return new PopupCustomResult(null, false);
        }
        else
        {
            throw new InvalidOperationException("The current page is not a NavigationPage.");
        }
    }
}

public class PopupCustomResult : IPopupResult
{
    public object? Result { get; }
    public bool WasDismissedByTappingOutsideOfPopup { get; }
    public PopupCustomResult(object? result, bool wasDismissedByTappingOutsideOfPopup)
    {
        Result = result;
        WasDismissedByTappingOutsideOfPopup = wasDismissedByTappingOutsideOfPopup;
    }
}