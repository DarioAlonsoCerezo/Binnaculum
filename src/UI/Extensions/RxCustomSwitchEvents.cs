using Binnaculum.Controls;

namespace Binnaculum.Extensions;

public class RxCustomSwitchEvents(CustomSwitch data) : RxBindableObjectEvents(data)
{
    private readonly CustomSwitch _data = data;

    public IObservable<bool> Toggled
        => Observable
            .FromEvent<EventHandler<bool>, bool>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Toggled += handler,
                handler => _data.Toggled -= handler);
}