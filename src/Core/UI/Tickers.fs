namespace Binnaculum.Core.UI

open System
open System.Threading.Tasks
open Binnaculum.Core.Database
open Binnaculum.Core.Models
open Binnaculum.Core.ModelsToDatabase
open Binnaculum.Core.DatabaseToModels
open Binnaculum.Core.Keys
open Binnaculum.Core.Patterns
open Binnaculum.Core.Logging
open TickerPriceExtensions
open TickerSnapshotExtensions
open TickerCurrencySnapshotExtensions

/// <summary>
/// This module provides the public API for all Ticker-related operations accessible from the UI layer.
/// It follows the established patterns of model validation, database persistence via conversion,
/// and follows the project's error handling conventions (let exceptions bubble up to the UI layer).
/// </summary>
module Tickers =

    /// <summary>
    /// Saves a TickerPrice to the database.
    /// Takes a Models.TickerPrice record, converts it to the database model, and persists it.
    /// Follows project conventions for error handling - exceptions bubble up to UI layer.
    /// </summary>
    /// <param name="tickerPrice">The TickerPrice model to save</param>
    let SaveTickerPrice (tickerPrice: Binnaculum.Core.Models.TickerPrice) =
        task {
            let databaseModel = tickerPrice.tickerPriceToDatabase ()
            do! databaseModel.save () |> Async.AwaitTask |> Async.Ignore
        }

    /// <summary>
    /// Retrieves all snapshots for a specific Ticker.
    /// Takes a tickerId, queries the database for all snapshots associated with that ticker,
    /// and converts them to TickerSnapshot models for UI consumption.
    /// Follows project conventions for error handling - exceptions bubble up to UI layer.
    /// </summary>
    /// <param name="tickerId">The ID of the Ticker to retrieve snapshots for</param>
    /// <returns>A list of TickerSnapshot records representing all snapshots for the ticker</returns>
    let GetSnapshots (tickerId: int) =
        task {
            // Get all ticker snapshots from database
            let! tickerSnapshots = TickerSnapshotExtensions.Do.getByTickerId (tickerId) |> Async.AwaitTask

            // Get the ticker model using fast lookup
            let ticker = tickerId.ToFastTickerById()

            // Convert each database snapshot to TickerSnapshot model
            let tickerSnapshotTasks =
                tickerSnapshots
                |> List.map (fun dbSnapshot ->
                    task {
                        // Get related currency snapshots for this ticker snapshot
                        let! currencySnapshots =
                            TickerCurrencySnapshotExtensions.Do.getAllByTickerSnapshotId (dbSnapshot.Base.Id)
                            |> Async.AwaitTask

                        // Convert to TickerSnapshot using existing conversion function
                        return dbSnapshot.tickerSnapshotToModelWithCurrencies (currencySnapshots, ticker)
                    })

            let! snapshots = Task.WhenAll(tickerSnapshotTasks |> List.toArray)
            return snapshots |> Array.toList
        }

    /// <summary>
    /// Retrieves all TickerCurrencySnapshots for a specific TickerSnapshot.
    /// Takes a ticker snapshot (containing ID, date, and ticker ID),
    /// queries the database for all related currency snapshots, and converts them to
    /// TickerCurrencySnapshot models for UI consumption.
    /// Follows project conventions for error handling - exceptions bubble up to UI layer.
    /// </summary>
    /// <param name="tickerSnapshotId">The ID of the TickerSnapshot</param>
    /// <param name="date">The date of the snapshot</param>
    /// <param name="tickerId">The ID of the Ticker</param>
    /// <returns>A list of TickerCurrencySnapshot records for the ticker snapshot</returns>
    let GetCurrencySnapshotsForTickerSnapshot (tickerSnapshotId: int, date: DateOnly, tickerId: int) =
        task {
            // Convert DateOnly to DateTimePattern
            let dateTimePattern =
                DateTimePattern.FromDateTime(date.ToDateTime(TimeOnly.MinValue))

            // Get the ticker model using fast lookup
            let ticker = tickerId.ToFastTickerById()

            // Get all currency snapshots for this ticker snapshot
            let! dbCurrencySnapshots =
                TickerCurrencySnapshotExtensions.Do.getAllByTickerIdAndDate (tickerId, dateTimePattern)
                |> Async.AwaitTask

            // Convert database snapshots to domain models using the helper extension method
            let currencySnapshots =
                dbCurrencySnapshots
                |> List.map (fun dbCurrencySnapshot ->
                    dbCurrencySnapshot.tickerCurrencySnapshotToModel (ticker = ticker))

            return currencySnapshots
        }

    /// <summary>
    /// Retrieves all AutoImportOperations for a specific Ticker.
    /// Takes a tickerId, queries the database for all operations associated with that ticker,
    /// and converts them to AutoImportOperation models for UI consumption.
    /// Follows project conventions for error handling - exceptions bubble up to UI layer.
    /// </summary>
    /// <param name="tickerId">The ID of the Ticker to retrieve operations for</param>
    /// <returns>A list of AutoImportOperation records representing all operations for the ticker</returns>
    let GetOperations (tickerId: int) =
        task {
            // Get all operations from database for this ticker
            let! dbOperations = AutoImportOperationExtensions.Do.getByTicker (tickerId) |> Async.AwaitTask

            // Log what we retrieved from database
            // dbOperations
            // |> List.iter (fun op ->
            //     CoreLogger.logDebugf "Tickers.GetOperations" "Retrieved operation ID=%d: CreatedAt=%s, UpdatedAt=%s, IsOpen=%b" op.Id (match op.Audit.CreatedAt with | Some dt -> dt.ToString() | None -> "None") (match op.Audit.UpdatedAt with | Some dt -> dt.ToString() | None -> "None") op.IsOpen)

            // Convert database operations to domain models
            let operations = dbOperations.autoImportOperationsToModel ()

            // Log what we converted
            // operations
            // |> List.iter (fun op ->
            //     CoreLogger.logDebugf "Tickers.GetOperations" "Converted operation ID=%d: OpenDate=%s, CloseDate=%s, IsOpen=%b" op.Id (op.OpenDate.ToString("yyyy-MM-dd HH:mm:ss")) (match op.CloseDate with | Some dt -> dt.ToString("yyyy-MM-dd HH:mm:ss") | None -> "None") op.IsOpen)

            return operations
        }
