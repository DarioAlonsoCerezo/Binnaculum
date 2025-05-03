namespace Binnaculum.Extensions;

public class RxPickerEvents(Picker data) : RxVisualElementEvents(data)
{
    private readonly Picker _data = data;

    public IObservable<EventArgs> SelectedIndexChanged
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.SelectedIndexChanged += handler,
                handler => _data.SelectedIndexChanged -= handler);
}
