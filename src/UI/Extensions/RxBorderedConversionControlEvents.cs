using Binnaculum.Controls;

namespace Binnaculum.Extensions;

public class RxBorderedConversionControlEvents(BorderedConversionControl data) : RxVisualElementEvents(data)
{
    public IObservable<Conversion> ConversionChanged
        => Observable
            .FromEvent<EventHandler<Conversion>, Conversion>(
                eventHandler => (_, e) => eventHandler(e),
                handler => data.ConversionChanged += handler,
                handler => data.ConversionChanged -= handler);
}
