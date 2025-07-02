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
open DividendExtensions
open DividendDateExtensions
open DividendTaxExtensions
open OptionTradeExtensions
open Binnaculum.Core.Storage.ModelsToDatabase
open System
open Binnaculum.Core.Patterns
open Binnaculum.Core.Storage
open Binnaculum.Core.Storage.Saver
open Microsoft.FSharp.Core

module Creator =
    
    let SaveBank(bank: Binnaculum.Core.Models.Bank) = task {
        do! Saver.saveBank(bank) |> Async.AwaitTask |> Async.Ignore
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

    /// <summary>
    /// Save a new or updated trade and refresh the trades collection.
    /// </summary>
    let SaveTrade(trade: Binnaculum.Core.Models.Trade) = task {
        let databaseTrade = trade.tradeToDatabase()
        do! databaseTrade.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.loadMovementsFor(None) |> Async.AwaitTask |> Async.Ignore
    }

    /// <summary>
    /// Save a new or updated dividend and refresh the dividends collection.
    /// </summary>
    let SaveDividend(dividend: Binnaculum.Core.Models.Dividend) = task {
        let databaseDividend = dividend.dividendReceivedToDatabase()
        do! databaseDividend.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.loadMovementsFor(None) |> Async.AwaitTask |> Async.Ignore
    }

    /// <summary>
    /// Save a new or updated dividend date and refresh the dividend dates collection.
    /// </summary>
    let SaveDividendDate(dividendDate: Binnaculum.Core.Models.DividendDate) = task {
        let databaseModel = dividendDate.dividendDateToDatabase()
        do! databaseModel.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.loadMovementsFor(None) |> Async.AwaitTask |> Async.Ignore
    }

    /// <summary>
    /// Save a new or updated dividend tax and refresh the dividend taxes collection.
    /// </summary>
    let SaveDividendTax(dividendTax: Binnaculum.Core.Models.DividendTax) = task {
        let databaseModel = dividendTax.dividendTaxToDatabase()
        do! databaseModel.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.loadMovementsFor(None) |> Async.AwaitTask |> Async.Ignore
    }

    /// <summary>
    /// Save option trades and refresh the movements collection.
    /// </summary>
    let SaveOptionsTrade(optionTrades: Binnaculum.Core.Models.OptionTrade list) = task {
        // Expand trades with quantity > 1 into multiple trades with quantity = 1
        let expandedTrades = 
            optionTrades 
            |> List.collect (fun trade ->
                if trade.Quantity > 1 then
                    let netPremium = trade.NetPremium / decimal trade.Quantity
                    [ for _ in 1 .. trade.Quantity -> { trade with Quantity = 1; NetPremium = netPremium;  } ]
                else
                    [trade]
            )
        
        do! expandedTrades.optionTradesToDatabase()
            |> List.map (fun model -> model.save() |> Async.AwaitTask |> Async.Ignore)
            |> Async.Parallel
            |> Async.Ignore
        
        // Refresh the movements collection to reflect the new option trades
        do! DataLoader.loadMovementsFor(None) |> Async.AwaitTask |> Async.Ignore
    }

    let UpdateOptionsTimestampNotesAndMultiplier(timestamp: DateTime, notes: string option, multiplier: decimal, trade: Binnaculum.Core.Models.OptionTrade) =
        {   trade with TimeStamp = timestamp; Multiplier = multiplier; Notes = notes }

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
            | Binnaculum.Core.Models.MovementType.ACATMoneyTransferSent 
                -> Some Binnaculum.Core.Models.BrokerMovementType.ACATMoneyTransferSent
            | Binnaculum.Core.Models.MovementType.ACATMoneyTransferReceived 
                -> Some Binnaculum.Core.Models.BrokerMovementType.ACATMoneyTransferReceived
            | Binnaculum.Core.Models.MovementType.ACATSecuritiesTransferSent 
                -> Some Binnaculum.Core.Models.BrokerMovementType.ACATSecuritiesTransferSent
            | Binnaculum.Core.Models.MovementType.ACATSecuritiesTransferReceived 
                -> Some Binnaculum.Core.Models.BrokerMovementType.ACATSecuritiesTransferReceived
            | Binnaculum.Core.Models.MovementType.InterestsPaid 
                -> Some Binnaculum.Core.Models.BrokerMovementType.InterestsPaid
            | Binnaculum.Core.Models.MovementType.Conversion 
                -> Some Binnaculum.Core.Models.BrokerMovementType.Conversion
            | _ -> None
