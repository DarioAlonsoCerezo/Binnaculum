namespace Binnaculum.Extensions;

public class RxStepperEvents(Stepper data) : RxVisualElementEvents(data)
{
    private readonly Stepper _data = data;

    public IObservable<ValueChangedEventArgs> ValueChanged
        => Observable
            .FromEvent<EventHandler<ValueChangedEventArgs>, ValueChangedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.ValueChanged += handler,
                handler => _data.ValueChanged -= handler);
}
