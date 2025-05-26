using Binnaculum.Controls;
using Binnaculum.Core;

namespace Binnaculum.Extensions;

public class RxOptionTradeControlEvents(OptionTradeControl data) : RxVisualElementEvents(data)
{
    private readonly OptionTradeControl _data = data;
    public IObservable<List<Models.OptionTrade?>?> OptionTradesChanged
        => Observable
            .FromEvent<EventHandler<List<Models.OptionTrade?>?>, List<Models.OptionTrade?>?>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.OptionTradesChanged += handler,
                handler => _data.OptionTradesChanged -= handler);
}