using Binnaculum.Controls;
using Binnaculum.Core;

namespace Binnaculum.Extensions;

public class RxDividendReceivedControlEvents(DividendControl data) : RxVisualElementEvents(data)
{
    private readonly DividendControl _data = data;
    public IObservable<Models.Dividend?> DividendChanged
        => Observable
            .FromEvent<EventHandler<Models.Dividend?>, Models.Dividend?>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.DividendChanged += handler,
                handler => _data.DividendChanged -= handler);

    public IObservable<Models.DividendDate?> DividendDateChanged
        => Observable
            .FromEvent<EventHandler<Models.DividendDate?>, Models.DividendDate?>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.DividendDateChanged += handler,
                handler => _data.DividendDateChanged -= handler);

    public IObservable<Models.DividendTax?> DividendTaxChanged
        => Observable
            .FromEvent<EventHandler<Models.DividendTax?>, Models.DividendTax?>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.DividendTaxChanged += handler,
                handler => _data.DividendTaxChanged -= handler);
}