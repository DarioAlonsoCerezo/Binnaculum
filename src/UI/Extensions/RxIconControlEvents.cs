using Binnaculum.Controls;

namespace Binnaculum.Extensions;

public class RxIconControlEvents(IconControl data) : RxVisualElementEvents(data)
{
    private readonly IconControl _data = data;

    public IObservable<EventArgs> IconClicked
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.IconClicked += handler,
                handler => _data.IconClicked -= handler);
}
