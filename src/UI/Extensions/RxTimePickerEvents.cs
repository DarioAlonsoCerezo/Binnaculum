namespace Binnaculum.Extensions;

public class RxTimePickerEvents(TimePicker data) : RxVisualElementEvents(data)
{
    private readonly TimePicker _data = data;
    public IObservable<TimeChangedEventArgs> TimeSelected
        => Observable
            .FromEvent<EventHandler<TimeChangedEventArgs>, TimeChangedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.TimeSelected += handler,
                handler => _data.TimeSelected -= handler);
}
