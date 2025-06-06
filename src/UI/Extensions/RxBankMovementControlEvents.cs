﻿using Binnaculum.Controls;

namespace Binnaculum.Extensions;

public class RxBankMovementControlEvents(BankMovementControl data) : RxVisualElementEvents(data)
{
    public IObservable<DepositControl> DepositChanged
        => Observable
            .FromEvent<EventHandler<DepositControl>, DepositControl>(
                eventHandler => (_, e) => eventHandler(e),
                handler => data.DepositChanged += handler,
                handler => data.DepositChanged -= handler);
}