using Binnaculum.Controls;

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

public class RxCustomRadioButtonEvents(CustomRadioButton data) : RxVisualElementEvents(data)
{
    private readonly CustomRadioButton _data = data;
    public IObservable<bool> CheckedChanged
        => Observable
            .FromEvent<EventHandler<bool>, bool>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.CheckedChanged += handler,
                handler => _data.CheckedChanged -= handler);

    public IObservable<EventArgs> Clicked
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Clicked += handler,
                handler => _data.Clicked -= handler);
}