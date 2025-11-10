namespace Binnaculum.Core.Import

open System
open System.Threading
open System.Diagnostics
open Binnaculum.Core
open Binnaculum.Core.Database
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Logging
open Binnaculum.Core.Storage
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

            try

                // Persist BrokerMovements
                for brokerMovement in input.BrokerMovements do
                    cancellationToken.ThrowIfCancellationRequested()

                    try
                        do! BrokerMovementExtensions.Do.save brokerMovement |> Async.AwaitTask
                        movementDates <- brokerMovement.TimeStamp.Value :: movementDates
                        processedCount <- processedCount + 1

                        let progress = float processedCount / float totalItems

                        ImportState.updateStatus (
                            SavingToDatabase(ResourceKeys.Import_SavingData, progress, processedCount, totalItems)
                        )
                    with ex ->
                        errors <- $"Error saving BrokerMovement ID={brokerMovement.Id}: {ex.Message}" :: errors

                // Persist OptionTrades
                for optionTrade in input.OptionTrades do
                    cancellationToken.ThrowIfCancellationRequested()

                    try
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
                        let progress = float processedCount / float totalItems

                        ImportState.updateStatus (
                            SavingToDatabase(ResourceKeys.Import_SavingData, progress, processedCount, totalItems)
                        )
                    with ex ->
                        errors <- $"Error saving OptionTrade ID={optionTrade.Id}: {ex.Message}" :: errors

                // Persist StockTrades
                for stockTrade in input.StockTrades do
                    cancellationToken.ThrowIfCancellationRequested()

                    try
                        do! TradeExtensions.Do.save stockTrade |> Async.AwaitTask
                        movementDates <- stockTrade.TimeStamp.Value :: movementDates

                        // Collect ticker symbol for metadata
                        let! tickerOption = TickerExtensions.Do.getById stockTrade.TickerId |> Async.AwaitTask

                        match tickerOption with
                        | Some ticker -> affectedTickerSymbols <- Set.add ticker.Symbol affectedTickerSymbols
                        | None -> ()

                        processedCount <- processedCount + 1
                        let progress = float processedCount / float totalItems

                        ImportState.updateStatus (
                            SavingToDatabase(ResourceKeys.Import_SavingData, progress, processedCount, totalItems)
                        )
                    with ex ->
                        errors <- $"Error saving Trade ID={stockTrade.Id}: {ex.Message}" :: errors

                // Persist Dividends
                for dividend in input.Dividends do
                    cancellationToken.ThrowIfCancellationRequested()

                    try
                        do! DividendExtensions.Do.save dividend |> Async.AwaitTask
                        movementDates <- dividend.TimeStamp.Value :: movementDates

                        // Collect ticker symbol for metadata
                        let! tickerOption = TickerExtensions.Do.getById dividend.TickerId |> Async.AwaitTask

                        match tickerOption with
                        | Some ticker -> affectedTickerSymbols <- Set.add ticker.Symbol affectedTickerSymbols
                        | None -> ()

                        processedCount <- processedCount + 1
                        let progress = float processedCount / float totalItems

                        ImportState.updateStatus (
                            SavingToDatabase(ResourceKeys.Import_SavingData, progress, processedCount, totalItems)
                        )
                    with ex ->
                        errors <- $"Error saving Dividend ID={dividend.Id}: {ex.Message}" :: errors

                // Persist DividendTaxes
                for dividendTax in input.DividendTaxes do
                    cancellationToken.ThrowIfCancellationRequested()

                    try
                        do! DividendTaxExtensions.Do.save dividendTax |> Async.AwaitTask
                        movementDates <- dividendTax.TimeStamp.Value :: movementDates

                        // Collect ticker symbol for metadata
                        let! tickerOption = TickerExtensions.Do.getById dividendTax.TickerId |> Async.AwaitTask

                        match tickerOption with
                        | Some ticker -> affectedTickerSymbols <- Set.add ticker.Symbol affectedTickerSymbols
                        | None -> ()

                        processedCount <- processedCount + 1
                        let progress = float processedCount / float totalItems

                        ImportState.updateStatus (
                            SavingToDatabase(ResourceKeys.Import_SavingData, progress, processedCount, totalItems)
                        )
                    with ex ->
                        errors <- $"Error saving DividendTax ID={dividendTax.Id}: {ex.Message}" :: errors

                // Final progress update
                ImportState.updateStatus (
                    SavingToDatabase(ResourceKeys.Import_Completed, 1.0, processedCount, totalItems)
                )

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

            with
            | :? OperationCanceledException ->
                ImportState.updateStatus (
                    SavingToDatabase(ResourceKeys.Import_Cancelled, 0.0, processedCount, totalItems)
                )

                return
                    { BrokerMovementsCreated = 0
                      OptionTradesCreated = 0
                      StockTradesCreated = 0
                      DividendsCreated = 0
                      ErrorsCount = 1
                      Errors = [ "Operation was cancelled" ]
                      ImportMetadata = ImportMetadata.createEmpty () }
            | ex ->
                errors <- $"Database persistence failed: {ex.Message}" :: errors

                return
                    { BrokerMovementsCreated = 0
                      OptionTradesCreated = 0
                      StockTradesCreated = 0
                      DividendsCreated = 0
                      ErrorsCount = errors.Length
                      Errors = List.rev errors
                      ImportMetadata = ImportMetadata.createEmpty () }
        }
