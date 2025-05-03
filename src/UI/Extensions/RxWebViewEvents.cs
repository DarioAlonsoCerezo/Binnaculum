namespace Binnaculum.Extensions;

public class RxWebViewEvents(WebView data) : RxVisualElementEvents(data)
{
    private readonly WebView _data = data;

    public IObservable<WebNavigatingEventArgs> Navigating
        => Observable
            .FromEvent<EventHandler<WebNavigatingEventArgs>, WebNavigatingEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Navigating += handler,
                handler => _data.Navigating -= handler);

    public IObservable<WebNavigatedEventArgs> Navigated
        => Observable
            .FromEvent<EventHandler<WebNavigatedEventArgs>, WebNavigatedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Navigated += handler,
                handler => _data.Navigated -= handler);
}
