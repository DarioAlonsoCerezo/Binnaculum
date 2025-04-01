using CommunityToolkit.Maui.Views;

namespace Binnaculum.Popups;

public class BasePopup : Popup
{
    protected readonly CompositeDisposable Disposables = new();

    public BasePopup()
    {
        
    }

    protected override Task OnClosed(object? result, bool wasDismissedByTappingOutsideOfPopup, CancellationToken token = default)
    {
        Disposables?.Dispose();
        return base.OnClosed(result, wasDismissedByTappingOutsideOfPopup, token);
    }
}