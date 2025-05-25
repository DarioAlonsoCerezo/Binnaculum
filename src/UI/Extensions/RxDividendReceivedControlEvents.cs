using Binnaculum.Controls;
using Binnaculum.Core;

namespace Binnaculum.Extensions;

public class RxDividendReceivedControlEvents(DividendReceivedControl data) : RxVisualElementEvents(data)
{
    private readonly DividendReceivedControl _data = data;
    public IObservable<Models.Dividend?> DividendChanged
        => Observable
            .FromEvent<EventHandler<Models.Dividend?>, Models.Dividend?>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.DividendChanged += handler,
                handler => _data.DividendChanged -= handler);
}