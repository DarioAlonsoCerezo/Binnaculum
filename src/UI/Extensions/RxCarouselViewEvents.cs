namespace Binnaculum.Extensions;

public class RxCarouselViewEvents(CarouselView data) : RxItemsViewEvents(data)
{
    private readonly CarouselView _data = data;

    public IObservable<CurrentItemChangedEventArgs> CurrentItemChanged
        => Observable
            .FromEvent<EventHandler<CurrentItemChangedEventArgs>, CurrentItemChangedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.CurrentItemChanged += handler,
                handler => _data.CurrentItemChanged -= handler);

    public IObservable<EventArgs> PositionSelected
        => Observable
            .FromEvent<EventHandler<PositionChangedEventArgs>, PositionChangedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.PositionChanged += handler,
                handler => _data.PositionChanged -= handler);
}
