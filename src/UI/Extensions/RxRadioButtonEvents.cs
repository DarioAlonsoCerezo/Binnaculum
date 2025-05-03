namespace Binnaculum.Extensions;

public class RxRadioButtonEvents(RadioButton data) : RxVisualElementEvents(data)
{
    private readonly RadioButton _data = data;

    public IObservable<CheckedChangedEventArgs> CheckedChanged
        => Observable
            .FromEvent<EventHandler<CheckedChangedEventArgs>, CheckedChangedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.CheckedChanged += handler,
                handler => _data.CheckedChanged -= handler);
}
