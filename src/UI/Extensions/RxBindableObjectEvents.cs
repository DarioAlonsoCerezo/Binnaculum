using PropertyChangingEventArgs = Microsoft.Maui.Controls.PropertyChangingEventArgs;
using PropertyChangingEventHandler = Microsoft.Maui.Controls.PropertyChangingEventHandler;

namespace Binnaculum.Extensions;

public class RxBindableObjectEvents(BindableObject data)
{
    private readonly BindableObject _data = data;

    public IObservable<EventArgs> BindingContextChanged
        => Observable
            .FromEvent((Func<Action<EventArgs>, EventHandler>)
                (eventHandler => (_, e) => eventHandler(e)),
                x => _data.BindingContextChanged += x,
                x => _data.BindingContextChanged -= x);

    public IObservable<PropertyChangedEventArgs> PropertyChanged
        => Observable
            .FromEvent((Func<Action<PropertyChangedEventArgs>, PropertyChangedEventHandler>)
                (eventHandler => (_, e) => eventHandler(e)),
                x => _data.PropertyChanged += x,
                x => _data.PropertyChanged -= x);

    public IObservable<PropertyChangingEventArgs> PropertyChanging
        => Observable
            .FromEvent((Func<Action<PropertyChangingEventArgs>, PropertyChangingEventHandler>)
                (eventHandler => (_, e) => eventHandler(e)),
                x => _data.PropertyChanging += x,
                x => _data.PropertyChanging -= x);
}

