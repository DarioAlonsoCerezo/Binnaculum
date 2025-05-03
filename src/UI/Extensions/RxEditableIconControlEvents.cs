using Binnaculum.Controls;

namespace Binnaculum.Extensions;

public class RxEditableIconControlEvents(EditableIconControl data) 
    : RxVisualElementEvents(data)
{
    private readonly EditableIconControl _data = data;

    public IObservable<EventArgs> IconClicked
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.IconClicked += handler,
                handler => _data.IconClicked -= handler);
}