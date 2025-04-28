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

    protected override Task OnClosed(object? result, bool wasDismissedByTappingOutsideOfPopup, CancellationToken token = default)
    {
        Disposables?.Dispose();
        return base.OnClosed(result, wasDismissedByTappingOutsideOfPopup, token);
    }
}