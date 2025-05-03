using Binnaculum.Controls;

namespace Binnaculum.Extensions;

public class RxSelectableBrokerEvents(SelectableBrokerControl data) 
    : RxBindableObjectEvents(data)
{
    private readonly SelectableBrokerControl _data = data;

    public IObservable<Core.Models.Broker> BrokerSelected
        => Observable
            .FromEvent<EventHandler<Core.Models.Broker>, Core.Models.Broker>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.BrokerSelected += handler,
                handler => _data.BrokerSelected -= handler);
}
