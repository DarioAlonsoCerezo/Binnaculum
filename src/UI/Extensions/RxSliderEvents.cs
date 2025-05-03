namespace Binnaculum.Extensions;

public class RxSliderEvents(Slider data) : RxVisualElementEvents(data)
{
    private readonly Slider _data = data;

    public IObservable<ValueChangedEventArgs> ValueChanged
        => Observable
            .FromEvent<EventHandler<ValueChangedEventArgs>, ValueChangedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.ValueChanged += handler,
                handler => _data.ValueChanged -= handler);
}
