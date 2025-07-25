﻿using Binnaculum.Controls;

namespace Binnaculum.Extensions;

public class RxBorderedEntry : RxVisualElementEvents
{
    public RxBorderedEntry(BorderedEntry data) : base(data)
    {
        _data = data;
    }

    private readonly BorderedEntry _data;

    public IObservable<TextChangedEventArgs> TextChanged
        => Observable
            .FromEvent<EventHandler<TextChangedEventArgs>, TextChangedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.TextChanged += handler,
                handler => _data.TextChanged -= handler);

    public IObservable<EventArgs> Completed
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Completed += handler,
                handler => _data.Completed -= handler);

    public IObservable<string> CurrencyChanged
        => Observable
            .FromEvent<EventHandler<string>, string>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.CurrencyChanged += handler,
                handler => _data.CurrencyChanged -= handler);

    public IObservable<string> TickerChanged
        => Observable
            .FromEvent<EventHandler<string>, string>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.TickerChanged += handler,
                handler => _data.TickerChanged -= handler);
}
