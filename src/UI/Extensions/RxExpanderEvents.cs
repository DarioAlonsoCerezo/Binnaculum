using CommunityToolkit.Maui.Core;

namespace Binnaculum.Extensions;

public class RxExpanderEvents(Expander data) : RxVisualElementEvents(data)
{
    private readonly Expander _data = data;

    public IObservable<EventArgs> ExpandedChanged
        => Observable
            .FromEvent<EventHandler<ExpandedChangedEventArgs>, ExpandedChangedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.ExpandedChanged += handler,
                handler => _data.ExpandedChanged -= handler);
}
