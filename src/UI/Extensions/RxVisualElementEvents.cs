namespace Binnaculum.Extensions;

public class RxVisualElementEvents(VisualElement data) : RxElementEvents(data)
{
    private readonly VisualElement _data = data;

    public IObservable<EventArgs> ChildrenReordered
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.ChildrenReordered += handler,
                handler => _data.ChildrenReordered -= handler);

    public IObservable<FocusEventArgs> Focused
        => Observable
            .FromEvent<EventHandler<FocusEventArgs>, FocusEventArgs>(
                eventHandler => (_, args) => eventHandler(args),
                handler => _data.Focused += handler,
                handler => _data.Focused -= handler);

    public IObservable<EventArgs> MeasureInvalidated
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.MeasureInvalidated += handler,
                handler => _data.MeasureInvalidated -= handler);

    public IObservable<EventArgs> SizeChanged
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.SizeChanged += handler,
                handler => _data.SizeChanged -= handler);

    public IObservable<FocusEventArgs> Unfocused
        => Observable
            .FromEvent<EventHandler<FocusEventArgs>, FocusEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Unfocused += handler,
                handler => _data.Unfocused -= handler);

    public IObservable<EventArgs> Loaded
        => Observable
            .FromEvent((Func<Action<EventArgs>, EventHandler>)
                (eventHandler => (_, e) => eventHandler(e)),
                x => _data.Loaded += x,
                x => _data.Loaded -= x);

    public IObservable<EventArgs> Unloaded
        => Observable
            .FromEvent((Func<Action<EventArgs>, EventHandler>)
                (eventHandler => (_, e) => eventHandler(e)),
                x => _data.Unloaded += x,
                x => _data.Unloaded -= x);
}
