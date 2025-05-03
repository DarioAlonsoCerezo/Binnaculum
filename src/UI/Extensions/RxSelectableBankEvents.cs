using Binnaculum.Controls;

namespace Binnaculum.Extensions;

public class RxSelectableBankEvents(SelectableBankControl data) 
    : RxBindableObjectEvents(data)
{
    private readonly SelectableBankControl _data = data;

    public IObservable<Core.Models.Bank> BankSelected
        => Observable
            .FromEvent<EventHandler<Core.Models.Bank>, Core.Models.Bank>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.BankSelected += handler,
                handler => _data.BankSelected -= handler);
}
