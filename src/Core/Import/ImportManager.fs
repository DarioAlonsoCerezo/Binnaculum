namespace Binnaculum.Core.Import

open System
open System.Threading
open System.IO
open Binnaculum.Core
open Binnaculum.Core.Database
open Binnaculum.Core.UI
open BrokerExtensions

/// <summary>
/// Main import manager for file import operations with cancellation support
/// </summary>
module ImportManager =

    /// <summary>
    /// Import file for a specific broker with cancellation and progress tracking
    /// </summary>
    /// <param name="brokerId">ID of the broker to import for</param>
    /// <param name="brokerAccountId">ID of the broker account selected in the UI</param>
    /// <param name="filePath">Path to the file to import (CSV or ZIP)</param>
    /// <returns>ImportResult with detailed feedback</returns>
    let importFile (brokerId: int) (brokerAccountId: int) (filePath: string) =
        task {
            let cancellationToken = ImportState.startImport ()

            try
                // Validate inputs
                ImportState.updateStatus (Validating filePath)

                if not (File.Exists(filePath)) then
                    return ImportResult.createError ($"File not found: {filePath}")
                else
                    // Look up broker information
                    let! brokerOption = BrokerExtensions.Do.getById (brokerId) |> Async.AwaitTask

                    match brokerOption with
                    | None -> return ImportResult.createError ($"Broker with ID {brokerId} not found")
                    | Some broker ->
                        cancellationToken.ThrowIfCancellationRequested()

                        let! brokerAccountOption =
                            BrokerAccountExtensions.Do.getById (brokerAccountId) |> Async.AwaitTask

                        match brokerAccountOption with
                        | None ->
                            return ImportResult.createError ($"Broker account with ID {brokerAccountId} not found")
                        | Some account when account.BrokerId <> brokerId ->
                            return
                                ImportResult.createError (
                                    $"Broker account {brokerAccountId} does not belong to broker {broker.Name}"
                                )
                        | Some brokerAccount ->
                            cancellationToken.ThrowIfCancellationRequested()

                            // Process file (handles both CSV and ZIP)
                            ImportState.updateStatus (ProcessingFile(Path.GetFileName(filePath), 0.0))

                            let processedFile =
                                try
                                    Some(FileProcessor.processFile filePath)
                                with ex ->
                                    ImportState.failImport ($"Failed to process file: {ex.Message}")
                                    None

                            match processedFile with
                            | None -> return ImportResult.createError ($"Failed to process file")
                            | Some pf ->
                                try
                                    cancellationToken.ThrowIfCancellationRequested()

                                    // Route to appropriate broker importer based on SupportedBroker
                                    let! importResult =
                                        if broker.SupportedBroker.ToString() = "IBKR" then
                                            // IBKR importer doesn't need broker account ID but we still validate above
                                            IBKRImporter.importMultipleWithCancellation pf.CsvFiles cancellationToken
                                        elif broker.SupportedBroker.ToString() = "Tastytrade" then
                                            // Tastytrade importer requires a specific broker account ID
                                            task {
                                                // First do the parsing/validation
                                                let! parseResult =
                                                    TastytradeImporter.importMultipleWithCancellation
                                                        pf.CsvFiles
                                                        brokerAccount.Id
                                                        cancellationToken

                                                // If parsing was successful, persist to database
                                                if parseResult.Success then
                                                    cancellationToken.ThrowIfCancellationRequested()

                                                    try
                                                        // Parse the transactions from files again for database persistence
                                                        let mutable allTransactions = []

                                                        for csvFile in pf.CsvFiles do
                                                            let parsingResult =
                                                                TastytradeStatementParser.parseTransactionHistoryFromFile
                                                                    csvFile

                                                            if parsingResult.Errors.IsEmpty then
                                                                allTransactions <-
                                                                    allTransactions @ parsingResult.Transactions

                                                        // Persist transactions to database
                                                        let! persistenceResult =
                                                            DatabasePersistence.persistTransactionsToDatabase
                                                                allTransactions
                                                                brokerAccount.Id
                                                                cancellationToken

                                                        // Use targeted reactive updates if persistence was successful and data was imported
                                                        if
                                                            persistenceResult.ErrorsCount = 0
                                                            && persistenceResult.ImportMetadata.TotalMovementsImported > 0
                                                        then
                                                            // Refresh reactive managers in dependency order
                                                            do! ReactiveTickerManager.refreshAsync () // First: base ticker data
                                                            do! ReactiveMovementManager.refreshAsync () // Then: movements (depend on tickers)
                                                            // Use targeted snapshot updates instead of full refresh
                                                            do!
                                                                ReactiveTargetedSnapshotManager.updateFromImport (
                                                                    persistenceResult.ImportMetadata
                                                                )
                                                        elif persistenceResult.ErrorsCount = 0 then
                                                            // Fallback to full refresh if no movements were imported
                                                            do! ReactiveTickerManager.refreshAsync ()
                                                            do! ReactiveMovementManager.refreshAsync ()
                                                            do! ReactiveSnapshotManager.refreshAsync ()

                                                        // Update the ImportResult with actual database persistence results
                                                        let updatedImportedData =
                                                            { parseResult.ImportedData with
                                                                Trades = persistenceResult.StockTradesCreated
                                                                BrokerMovements =
                                                                    persistenceResult.BrokerMovementsCreated
                                                                OptionTrades = persistenceResult.OptionTradesCreated
                                                                Dividends = persistenceResult.DividendsCreated }

                                                        // Add any persistence errors to the result
                                                        let persistenceErrors =
                                                            persistenceResult.Errors
                                                            |> List.map (fun err ->
                                                                { RowNumber = None
                                                                  ErrorMessage = err
                                                                  ErrorType = ImportErrorType.ValidationError
                                                                  RawData = None })

                                                        let updatedErrors = parseResult.Errors @ persistenceErrors

                                                        return
                                                            { parseResult with
                                                                ImportedData = updatedImportedData
                                                                Errors = updatedErrors
                                                                Success =
                                                                    parseResult.Success
                                                                    && persistenceResult.ErrorsCount = 0 }

                                                    with
                                                    | :? OperationCanceledException ->
                                                        return ImportResult.createCancelled ()
                                                    | ex ->
                                                        return
                                                            ImportResult.createError (
                                                                $"Database persistence failed: {ex.Message}"
                                                            )
                                                else
                                                    return parseResult
                                            }
                                        else
                                            task {
                                                return
                                                    ImportResult.createError ($"Unsupported broker type: {broker.Name}")
                                            }

                                    // Clean up temporary files
                                    FileProcessor.cleanup pf

                                    // Complete import and return result
                                    ImportState.completeImport (importResult)
                                    return importResult

                                with
                                | :? OperationCanceledException ->
                                    // Clean up temporary files on cancellation
                                    FileProcessor.cleanup pf
                                    return ImportResult.createCancelled ()
                                | ex ->
                                    // Clean up temporary files on error
                                    FileProcessor.cleanup pf
                                    return ImportResult.createError ($"Import failed: {ex.Message}")

            with
            | :? OperationCanceledException -> return ImportResult.createCancelled ()
            | ex ->
                ImportState.failImport (ex.Message)
                return ImportResult.createError (ex.Message)
        }

    /// <summary>
    /// Cancel current import operation
    /// </summary>
    let cancelCurrentImport () =
        ImportState.cancelImport ("User requested cancellation")

    /// <summary>
    /// Cancel for app backgrounding
    /// </summary>
    let cancelForBackground () = ImportState.cancelForBackground ()

    /// <summary>
    /// Get current import status
    /// </summary>
    let getCurrentStatus () = ImportState.ImportStatus.Value

    /// <summary>
    /// Check if an import is currently in progress
    /// </summary>
    let isImportInProgress () =
        match ImportState.getCurrentCancellationToken () with
        | Some token -> not token.IsCancellationRequested
        | None -> false
