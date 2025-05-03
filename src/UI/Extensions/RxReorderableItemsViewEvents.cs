namespace Binnaculum.Extensions;

public class RxReorderableItemsViewEvents(ReorderableItemsView data) 
    : RxSelectableItemsViewEvents(data)
{
    private readonly ReorderableItemsView _data = data;

    public IObservable<EventArgs> ReorderCompleted
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.ReorderCompleted += handler,
                handler => _data.ReorderCompleted -= handler);
}
