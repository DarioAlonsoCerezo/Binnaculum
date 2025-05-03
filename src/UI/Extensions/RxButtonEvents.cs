using Binnaculum.Controls;

namespace Binnaculum.Extensions;

public class RxButtonEvents(Button data) : RxElementEvents(data)
{
    private readonly Button _data = data;

    public IObservable<EventArgs> Clicked
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Clicked += handler,
                handler => _data.Clicked -= handler);

    public IObservable<EventArgs> Pressed
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Pressed += handler,
                handler => _data.Pressed -= handler);

    public IObservable<EventArgs> Released
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Released += handler,
                handler => _data.Released -= handler);
}

public class RxButtonAddEvents(ButtonAdd data) : RxBindableObjectEvents(data)
{
    private readonly ButtonAdd _data = data;

    public IObservable<EventArgs> AddClicked
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.AddClicked += handler,
                handler => _data.AddClicked -= handler);
}

public class RxButtonAddOrDiscardEvents(ButtonSaveOrDiscard data) 
    : RxBindableObjectEvents(data)
{
    private readonly ButtonSaveOrDiscard _data = data;

    public IObservable<EventArgs> SaveClicked
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.SaveClicked += handler,
                handler => _data.SaveClicked -= handler);
    public IObservable<EventArgs> DiscardClicked
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.DiscardClicked += handler,
                handler => _data.DiscardClicked -= handler);
}

public class RxButtonDiscardEvents(ButtonDiscard data) : RxBindableObjectEvents(data)
{
    private readonly ButtonDiscard _data = data;

    public IObservable<EventArgs> DiscardClicked
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.DiscardClicked += handler,
                handler => _data.DiscardClicked -= handler);
}

public class RxButtonSaveEvents(ButtonSave data) : RxBindableObjectEvents(data)
{
    private readonly ButtonSave _data = data;

    public IObservable<EventArgs> SaveClicked
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.SaveClicked += handler,
                handler => _data.SaveClicked -= handler);
}