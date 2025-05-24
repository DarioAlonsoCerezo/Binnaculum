namespace Binnaculum.Extensions;

public class RxEditorEvents(Editor data) : RxInputViewEvents(data)
{
    private readonly Editor _data = data;
    public IObservable<EventArgs> Completed
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Completed += handler,
                handler => _data.Completed -= handler);
}
