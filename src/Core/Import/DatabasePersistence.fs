namespace Binnaculum.Core.Import

open System
open System.Threading
open Binnaculum.Core
open Binnaculum.Core.Database.DatabaseModel
open OptionTradeExtensions

/// <summary>
/// Database persistence module for converting parsed import data into domain objects and saving to SQLite
/// </summary>
module DatabasePersistence =

    /// <summary>
    /// Result of database persistence operations with counts and import metadata
    /// </summary>
    type PersistenceResult =
        {
            BrokerMovementsCreated: int
            OptionTradesCreated: int
            StockTradesCreated: int
            DividendsCreated: int
            ErrorsCount: int
            Errors: string list
            /// Metadata collected during persistence for targeted snapshot updates
            ImportMetadata: ImportMetadata
        }

    /// <summary>
    /// Persist domain models to database (broker-agnostic)
    /// Supports all brokers by accepting pre-converted domain models
    /// Integrates with session tracking from PR #420 for resumable imports
    /// </summary>
    let internal persistDomainModelsToDatabase
        (input: ImportDomainTypes.PersistenceInput)
        (brokerAccountId: int)
        (cancellationToken: CancellationToken)
        =
        task {
            let mutable errors = []

            // Collect metadata for targeted snapshot updates
            let mutable affectedTickerSymbols = Set.empty<string>
            let mutable movementDates = []

            let totalItems =
                input.BrokerMovements.Length
                + input.OptionTrades.Length
                + input.StockTrades.Length
                + input.Dividends.Length
                + input.DividendTaxes.Length

            let mutable processedCount = 0

            // Persist BrokerMovements
            for brokerMovement in input.BrokerMovements do
                cancellationToken.ThrowIfCancellationRequested()

                do! BrokerMovementExtensions.Do.save brokerMovement |> Async.AwaitTask
                movementDates <- brokerMovement.TimeStamp.Value :: movementDates
                processedCount <- processedCount + 1

            // Persist OptionTrades
            for optionTrade in input.OptionTrades do
                cancellationToken.ThrowIfCancellationRequested()

                let! persistedTrade = OptionTradeExtensions.Do.saveAndReturn optionTrade
                movementDates <- optionTrade.TimeStamp.Value :: movementDates

                // Collect ticker symbol for metadata (need to look it up)
                let! tickerOption = TickerExtensions.Do.getById optionTrade.TickerId |> Async.AwaitTask

                match tickerOption with
                | Some ticker -> affectedTickerSymbols <- Set.add ticker.Symbol affectedTickerSymbols
                | None -> ()

                // Link closing trades
                if isClosingCode persistedTrade.Code then
                    let! linkResult = OptionTradeExtensions.Do.linkClosingTrade persistedTrade

                    match linkResult with
                    | Ok _ -> ()
                    | Error _ -> () // Non-critical linking failure

                processedCount <- processedCount + 1

            // Persist StockTrades
            for stockTrade in input.StockTrades do
                cancellationToken.ThrowIfCancellationRequested()

                do! TradeExtensions.Do.save stockTrade |> Async.AwaitTask
                movementDates <- stockTrade.TimeStamp.Value :: movementDates

                // Collect ticker symbol for metadata
                let! tickerOption = TickerExtensions.Do.getById stockTrade.TickerId |> Async.AwaitTask

                match tickerOption with
                | Some ticker -> affectedTickerSymbols <- Set.add ticker.Symbol affectedTickerSymbols
                | None -> ()

                processedCount <- processedCount + 1

            // Persist Dividends
            for dividend in input.Dividends do
                cancellationToken.ThrowIfCancellationRequested()

                do! DividendExtensions.Do.save dividend |> Async.AwaitTask
                movementDates <- dividend.TimeStamp.Value :: movementDates

                // Collect ticker symbol for metadata
                let! tickerOption = TickerExtensions.Do.getById dividend.TickerId |> Async.AwaitTask

                match tickerOption with
                | Some ticker -> affectedTickerSymbols <- Set.add ticker.Symbol affectedTickerSymbols
                | None -> ()

                processedCount <- processedCount + 1

            // Persist DividendTaxes
            for dividendTax in input.DividendTaxes do
                cancellationToken.ThrowIfCancellationRequested()

                do! DividendTaxExtensions.Do.save dividendTax |> Async.AwaitTask
                movementDates <- dividendTax.TimeStamp.Value :: movementDates

                // Collect ticker symbol for metadata
                let! tickerOption = TickerExtensions.Do.getById dividendTax.TickerId |> Async.AwaitTask

                match tickerOption with
                | Some ticker -> affectedTickerSymbols <- Set.add ticker.Symbol affectedTickerSymbols
                | None -> ()

                processedCount <- processedCount + 1

            // Create import metadata for targeted snapshot updates
            let oldestMovementDate =
                if List.isEmpty movementDates then
                    None
                else
                    Some(movementDates |> List.min)

            let importMetadata =
                { OldestMovementDate = oldestMovementDate
                  AffectedBrokerAccountIds = Set.singleton brokerAccountId
                  AffectedTickerSymbols = affectedTickerSymbols
                  TotalMovementsImported = totalItems }

            return
                { BrokerMovementsCreated = input.BrokerMovements.Length
                  OptionTradesCreated = input.OptionTrades.Length
                  StockTradesCreated = input.StockTrades.Length
                  DividendsCreated = input.Dividends.Length + input.DividendTaxes.Length
                  ErrorsCount = errors.Length
                  Errors = List.rev errors
                  ImportMetadata = importMetadata }
        }
