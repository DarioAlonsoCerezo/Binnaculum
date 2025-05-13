using Binnaculum.Controls;

namespace Binnaculum.Extensions;

public class RxSelectableCurrencyEvents(SelectableCurrencyControl data)
    : RxBindableObjectEvents(data)
{
    private readonly SelectableCurrencyControl _data = data;
    public IObservable<Core.Models.Currency> CurrencySelected
        => Observable
            .FromEvent<EventHandler<Core.Models.Currency>, Core.Models.Currency>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.CurrencySelected += handler,
                handler => _data.CurrencySelected -= handler);
}


public class RxItemSelectorControlEvents(ItemSelectorControl data)
    : RxBindableObjectEvents(data)
{
    private readonly ItemSelectorControl _data = data;
    public IObservable<SelectableItem> ItemSelected
        => Observable
            .FromEvent<EventHandler<SelectableItem>, SelectableItem>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.ItemSelected += handler,
                handler => _data.ItemSelected -= handler);
}