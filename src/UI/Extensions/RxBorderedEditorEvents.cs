using Binnaculum.Controls;

namespace Binnaculum.Extensions;

public class RxBorderedEditorEvents(BorderedEditor data) : RxVisualElementEvents(data)
{
    private readonly BorderedEditor _data = data;

    public IObservable<TextChangedEventArgs> TextChanged
        => Observable
            .FromEvent<EventHandler<TextChangedEventArgs>, TextChangedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.TextChanged += handler,
                handler => _data.TextChanged -= handler);
}
