using Binnaculum.Controls;

namespace Binnaculum.Extensions;

public class RxBorderedFeeAndCommissionControlEvents(BorderedFeeAndCommissionControl data) : RxVisualElementEvents(data)
{
    public IObservable<FeeAndCommission> FeeAndCommissionChanged
        => Observable
            .FromEvent<EventHandler<FeeAndCommission>, FeeAndCommission>(
                eventHandler => (_, e) => eventHandler(e),
                handler => data.FeeAndCommissionChanged += handler,
                handler => data.FeeAndCommissionChanged -= handler);
}
