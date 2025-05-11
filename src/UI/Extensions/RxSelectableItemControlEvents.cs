using Binnaculum.Controls;

namespace Binnaculum.Extensions;

public class RxSelectableItemControlEvents(SelectableItemControl data)
    : RxBindableObjectEvents(data)
{
    private readonly SelectableItemControl _data = data;
    public IObservable<SelectableItem> ItemSelected
        => Observable
            .FromEvent<EventHandler<SelectableItem>, SelectableItem>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.ItemSelected += handler,
                handler => _data.ItemSelected -= handler);
}