namespace Binnaculum.Extensions;

public class RxInputViewEvents(InputView data) : RxVisualElementEvents(data)
{
    private readonly InputView _data = data;

    public IObservable<TextChangedEventArgs> TextChanged
        => Observable
            .FromEvent<EventHandler<TextChangedEventArgs>, TextChangedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.TextChanged += handler,
                handler => _data.TextChanged -= handler);
}
