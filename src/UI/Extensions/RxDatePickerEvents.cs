namespace Binnaculum.Extensions;

public class RxDatePickerEvents(DatePicker data) : RxVisualElementEvents(data)
{
    private readonly DatePicker _data = data;

    public IObservable<DateChangedEventArgs> DateSelected
        => Observable
            .FromEvent<EventHandler<DateChangedEventArgs>, DateChangedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.DateSelected += handler,
                handler => _data.DateSelected -= handler);
}
