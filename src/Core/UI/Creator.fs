namespace Binnaculum.Core.UI
open Binnaculum.Core.Database.DatabaseModel
open BankExtensions
open BrokerAccountExtensions
open BankAccountExtensions
open BrokerExtensions
open BrokerMovementExtensions
open BankAccountBalanceExtensions
open TickerExtensions
open TradeExtensions
open Binnaculum.Core.Storage.ModelsToDatabase
open Binnaculum.Core.Storage.DiscriminatedToDatabase
open System
open Binnaculum.Core.Patterns
open Binnaculum.Core.Storage
open Microsoft.FSharp.Core

module Creator =
    
    let SaveBank(bank: Binnaculum.Core.Models.Bank) = task {
        let! databaseBank = bank.bankToDatabase() |> Async.AwaitTask
        do! databaseBank.save() |> Async.AwaitTask |> Async.Ignore
        if bank.Id = 0 then
            do! DataLoader.getOrRefreshBanks() |> Async.AwaitTask |> Async.Ignore
        else
            do! DataLoader.refreshBankAccount(bank.Id) |> Async.AwaitTask |> Async.Ignore        
    }

    let SaveBroker(name, icon) = task {
        let broker = { Id = 0; Name = name; Image = icon; SupportedBroker = SupportedBroker.Unknown }
        do! broker.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.getOrRefreshAllBrokers() |> Async.AwaitTask |> Async.Ignore
    }

    let SaveBankAccount(bankId, name, currencyId) = task {
        let audit = { CreatedAt = Some(DateTimePattern.FromDateTime(DateTime.Now)); UpdatedAt = None }
        let account = { Id = 0; BankId = bankId; Name = name; Description = None; CurrencyId = currencyId; Audit = audit; }
        do! account.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.getOrRefreshAllAccounts() |> Async.AwaitTask |> Async.Ignore
    }

    let SaveBrokerAccount(brokerId: int, accountNumber: string) = task {
        let audit = { CreatedAt = Some(DateTimePattern.FromDateTime(DateTime.Now)); UpdatedAt = None }
        let account = { Id = 0; BrokerId = brokerId; AccountNumber = accountNumber; Audit = audit }
        do! account.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.getOrRefreshAllAccounts() |> Async.AwaitTask |> Async.Ignore
    }

    let SaveBrokerMovement(movement: Binnaculum.Core.Models.BrokerMovement) = task {
        let databaseModel = movement.brokerMovementToDatabase()
        do! databaseModel.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.loadMovementsFor(None) |> Async.AwaitTask |> Async.Ignore
    }

    let SaveBankMovement(movement: Binnaculum.Core.Models.BankAccountMovement) = task {
        let databaseModel = movement.bankAccountMovementToDatabase()
        do! databaseModel.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.loadMovementsFor(None) |> Async.AwaitTask |> Async.Ignore
    }

    /// <summary>
    /// Save a new or updated ticker and refresh the tickers collection.
    /// </summary>
    let SaveTicker(ticker: Binnaculum.Core.Models.Ticker) = task {
        // Map UI ticker to database model and save
        let! databaseTicker = ticker.tickerToDatabase() |> Async.AwaitTask
        do! databaseTicker.save() |> Async.AwaitTask |> Async.Ignore
        // Refresh in-memory ticker collection
        do! DataLoader.getOrRefreshAllTickers() |> Async.AwaitTask |> Async.Ignore
    }

    let SaveTrade(trade: Binnaculum.Core.Models.Trade) = task {
        let databaseTrade = trade.tradeToDatabase()
        do! databaseTrade.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.loadMovementsFor(None) |> Async.AwaitTask |> Async.Ignore
    }

    let GetBrokerMovementType(uiSelectedType: Binnaculum.Core.Models.MovementType option) =
        match uiSelectedType with
        | None -> None
        | Some selected ->
            match selected with
            | Binnaculum.Core.Models.MovementType.Deposit 
                -> Some Binnaculum.Core.Models.BrokerMovementType.Deposit
            | Binnaculum.Core.Models.MovementType.Withdrawal 
                -> Some Binnaculum.Core.Models.BrokerMovementType.Withdrawal
            | Binnaculum.Core.Models.MovementType.Fee 
                -> Some Binnaculum.Core.Models.BrokerMovementType.Fee
            | Binnaculum.Core.Models.MovementType.InterestsGained 
                -> Some Binnaculum.Core.Models.BrokerMovementType.InterestsGained
            | Binnaculum.Core.Models.MovementType.Lending 
                -> Some Binnaculum.Core.Models.BrokerMovementType.Lending
            | Binnaculum.Core.Models.MovementType.ACATMoneyTransfer 
                -> Some Binnaculum.Core.Models.BrokerMovementType.ACATMoneyTransfer
            | Binnaculum.Core.Models.MovementType.ACATSecuritiesTransfer 
                -> Some Binnaculum.Core.Models.BrokerMovementType.ACATSecuritiesTransfer
            | Binnaculum.Core.Models.MovementType.InterestsPaid 
                -> Some Binnaculum.Core.Models.BrokerMovementType.InterestsPaid
            | Binnaculum.Core.Models.MovementType.Conversion 
                -> Some Binnaculum.Core.Models.BrokerMovementType.Conversion
            | _ -> None
