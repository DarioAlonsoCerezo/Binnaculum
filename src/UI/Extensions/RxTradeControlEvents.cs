using Binnaculum.Controls;
using Binnaculum.Core;

namespace Binnaculum.Extensions;

public class RxTradeControlEvents(TradeControl data) : RxVisualElementEvents(data)
{
    private readonly TradeControl _data = data;

    public IObservable<Models.Trade> TradeChanged
        => Observable
            .FromEvent<EventHandler<Models.Trade>, Models.Trade>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.TradeChanged += handler,
                handler => _data.TradeChanged -= handler);

}
