namespace Binnacle.Core.Storage

module internal DataLoader =
    let getAllCurrencies() = task {
        let! databaseCurrencies = CurrencyExtensions.Do.getAll() |> Async.AwaitTask
        return ()
    }

