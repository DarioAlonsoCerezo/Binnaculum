namespace Binnaculum.Extensions;

public class RxGestureRecognizerEvents(GestureRecognizer data) 
    : RxBindableObjectEvents(data)
{
    private readonly GestureRecognizer _data = data;
}

public class RxTapGestureRecognizerEvents(TapGestureRecognizer data) 
    : RxGestureRecognizerEvents(data)
{
    private readonly TapGestureRecognizer _data = data;

    public IObservable<TappedEventArgs> Tapped
        => Observable
            .FromEvent((Func<Action<TappedEventArgs>, EventHandler<TappedEventArgs>>)
                (eventHandler => (_, e) => eventHandler(e)),
                x => _data.Tapped += x,
                x => _data.Tapped -= x);
}

public class RxPanGestureRecognizerEvents(PanGestureRecognizer data) 
    : RxGestureRecognizerEvents(data)
{
    private readonly PanGestureRecognizer _data = data;

    public IObservable<PanUpdatedEventArgs> PanUpdated
        => Observable
            .FromEvent((Func<Action<PanUpdatedEventArgs>, EventHandler<PanUpdatedEventArgs>>)
                (eventHandler => (_, e) => eventHandler(e)),
                x => _data.PanUpdated += x,
                x => _data.PanUpdated -= x);
}