using Binnaculum.Controls;

namespace Binnaculum.Extensions;

public class RxBorderedDateTimePickerControlEvents(BorderedDateTimePickerControl data) : RxVisualElementEvents(data)
{
    public IObservable<DateTime> DateSelected
        => Observable
            .FromEvent<EventHandler<DateTime>, DateTime>(
                eventHandler => (_, e) => eventHandler(e),
                handler => data.DateSelected += handler,
                handler => data.DateSelected -= handler);
}
