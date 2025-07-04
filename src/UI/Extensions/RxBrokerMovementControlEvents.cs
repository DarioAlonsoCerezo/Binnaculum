using Binnaculum.Controls;

namespace Binnaculum.Extensions;

public class RxBrokerMovementControlEvents(BrokerMovementControl data) : RxVisualElementEvents(data)
{
    public IObservable<DepositControl> DepositChanged
        => Observable
            .FromEvent<EventHandler<DepositControl>, DepositControl>(
                eventHandler => (_, e) => eventHandler(e),
                handler => data.DepositChanged += handler,
                handler => data.DepositChanged -= handler);

    public IObservable<ConversionControl> ConversionChanged
        => Observable
            .FromEvent<EventHandler<ConversionControl>, ConversionControl>(
                eventHandler => (_, e) => eventHandler(e),
                handler => data.ConversionChanged += handler,
                handler => data.ConversionChanged -= handler);
}
