using CommunityToolkit.Maui.Views;

namespace Binnaculum.Popups;

public class BasePopup : Popup
{
    protected readonly CompositeDisposable Disposables = new();
    protected IScheduler UiThread;
    protected IScheduler BackgroundScheduler;

    public BasePopup()
    {
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

    protected override Task OnClosed(object? result, bool wasDismissedByTappingOutsideOfPopup, CancellationToken token = default)
    {
        Disposables?.Dispose();
        return base.OnClosed(result, wasDismissedByTappingOutsideOfPopup, token);
    }
}

public static class PopupExtensions
{
    public static void Show(this Popup popup)
    {
        var appMainpage = Application.Current!.Windows[0].Page!;
        if (appMainpage is NavigationPage navigator)
        {
            navigator.ShowPopup(popup);
        }
        else
        {
            throw new InvalidOperationException("The current page is not a NavigationPage.");
        }
    }
}