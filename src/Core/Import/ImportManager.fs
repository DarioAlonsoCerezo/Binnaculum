namespace Binnaculum.Core.Import

open System
open System.Threading
open System.IO
open Binnaculum.Core
open Binnaculum.Core.Database
open Binnaculum.Core.UI
open Binnaculum.Core.Logging
open Binnaculum.Core.Storage
open Binnaculum.Core.DataLoader
open BrokerExtensions
open Binnaculum.Core.Storage.TickerSnapshotBatchManager

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
        // CoreLogger.logInfof "ImportManager" "importFile function called with broker %d, account %d, file=%s" brokerId brokerAccountId filePath

        task {
            // CoreLogger.logInfof "ImportManager" "Starting import for broker %d, account %d, file=%s" brokerId brokerAccountId filePath

            // CoreLogger.logDebug "ImportManager" "About to call ImportState.startImport()"
            let cancellationToken = ImportState.startImport ()
            // CoreLogger.logDebug "ImportManager" "ImportState.startImport() completed successfully"

            try
                // Validate inputs
                ImportState.updateStatus (Validating filePath)
                // CoreLogger.logDebug "ImportManager" "Validating input parameters"

                if not (File.Exists(filePath)) then
                    CoreLogger.logErrorf "ImportManager" "File not found: %s" filePath
                    return ImportResult.createError ($"File not found: {filePath}")
                else
                    // Look up broker information
                    // CoreLogger.logDebugf "ImportManager" "Looking up broker %d" brokerId
                    let! brokerOption = BrokerExtensions.Do.getById (brokerId) |> Async.AwaitTask

                    match brokerOption with
                    | None ->
                        CoreLogger.logErrorf "ImportManager" "Broker %d not found" brokerId
                        return ImportResult.createError ($"Broker with ID {brokerId} not found")
                    | Some broker ->
                        cancellationToken.ThrowIfCancellationRequested()

                        // CoreLogger.logDebugf "ImportManager" "Broker %s found, validating account %d" broker.Name brokerAccountId

                        let! brokerAccountOption =
                            BrokerAccountExtensions.Do.getById (brokerAccountId) |> Async.AwaitTask

                        match brokerAccountOption with
                        | None ->
                            CoreLogger.logErrorf "ImportManager" "Broker account %d not found" brokerAccountId
                            return ImportResult.createError ($"Broker account with ID {brokerAccountId} not found")
                        | Some account when account.BrokerId <> brokerId ->
                            CoreLogger.logErrorf
                                "ImportManager"
                                "Broker account %d does not belong to broker %d"
                                brokerAccountId
                                brokerId

                            return
                                ImportResult.createError (
                                    $"Broker account {brokerAccountId} does not belong to broker {broker.Name}"
                                )
                        | Some brokerAccount ->
                            cancellationToken.ThrowIfCancellationRequested()

                            // CoreLogger.logDebugf "ImportManager" "Validated account %s; proceeding to file processing" brokerAccount.AccountNumber

                            // Process file (handles both CSV and ZIP)
                            ImportState.updateStatus (ProcessingFile(Path.GetFileName(filePath), 0.0))
                            // CoreLogger.logDebugf "ImportManager" "Processing file %s" filePath

                            let processedFile =
                                try
                                    Some(FileProcessor.processFile filePath)
                                with ex ->
                                    CoreLogger.logErrorf
                                        "ImportManager"
                                        "Failed to process file %s: %s"
                                        filePath
                                        ex.Message

                                    ImportState.failImport ($"Failed to process file: {ex.Message}")
                                    None

                            match processedFile with
                            | None ->
                                CoreLogger.logError "ImportManager" "Processed file structure missing; returning error"
                                return ImportResult.createError ($"Failed to process file")
                            | Some pf ->
                                try
                                    cancellationToken.ThrowIfCancellationRequested()

                                    // Route to appropriate broker importer based on SupportedBroker
                                    // CoreLogger.logDebugf "ImportManager" "Routing to importer for broker type %A" broker.SupportedBroker

                                    let! importResult =
                                        if broker.SupportedBroker.ToString() = "IBKR" then
                                            // IBKR importer doesn't need broker account ID but we still validate above
                                            // CoreLogger.logDebugf "ImportManager" "Invoking IBKR importer for %d files" pf.CsvFiles.Length

                                            IBKRImporter.importMultipleWithCancellation pf.CsvFiles cancellationToken
                                        elif broker.SupportedBroker.ToString() = "Tastytrade" then
                                            // Tastytrade importer requires a specific broker account ID
                                            // CoreLogger.logDebugf "ImportManager" "Invoking Tastytrade importer for account %d" brokerAccount.Id

                                            task {
                                                // First do the parsing/validation
                                                let! parseResult =
                                                    TastytradeImporter.importMultipleWithCancellation
                                                        pf.CsvFiles
                                                        brokerAccount.Id
                                                        cancellationToken

                                                // CoreLogger.logInfof "ImportManager" "Tastytrade parse complete: success=%b, processedFiles=%d, processedRecords=%d" parseResult.Success parseResult.ProcessedFiles parseResult.ProcessedRecords

                                                // If parsing was successful, persist to database
                                                if parseResult.Success then
                                                    cancellationToken.ThrowIfCancellationRequested()

                                                    // CoreLogger.logDebug "ImportManager" "Beginning database persistence for parsed transactions"

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
                                                            else
                                                                CoreLogger.logWarningf
                                                                    "ImportManager"
                                                                    "Skipping %s due to %d parsing errors during persistence build"
                                                                    csvFile
                                                                    parsingResult.Errors.Length

                                                        // Persist transactions to database
                                                        // CoreLogger.logDebugf "ImportManager" "Persisting %d transactions to database for account %d" allTransactions.Length brokerAccount.Id

                                                        let! persistenceResult =
                                                            DatabasePersistence.persistTransactionsToDatabase
                                                                allTransactions
                                                                brokerAccount.Id
                                                                cancellationToken

                                                        // CoreLogger.logInfof "ImportManager" "Database persistence complete: brokerMovements=%d, optionTrades=%d, stockTrades=%d, errors=%d" persistenceResult.BrokerMovementsCreated persistenceResult.OptionTradesCreated persistenceResult.StockTradesCreated persistenceResult.ErrorsCount

                                                        // Log individual errors for debugging
                                                        if persistenceResult.Errors.Length > 0 then
                                                            persistenceResult.Errors
                                                            |> List.iteri (fun idx error ->
                                                                CoreLogger.logErrorf
                                                                    "ImportManager"
                                                                    "Persistence error %d/%d: %s"
                                                                    (idx + 1)
                                                                    persistenceResult.Errors.Length
                                                                    error)

                                                        // Use targeted reactive updates if ANY data was imported
                                                        // (even if there are some linkClosingTrade or other non-critical errors)
                                                        if
                                                            persistenceResult.ImportMetadata.TotalMovementsImported > 0
                                                        then
                                                            // Refresh reactive managers in dependency order
                                                            // CoreLogger.logDebug "ImportManager" "Performing targeted reactive refresh with batch snapshot processing"

                                                            do! ReactiveTickerManager.refreshAsync () // First: base ticker data
                                                            do! ReactiveMovementManager.refreshAsync () // Then: movements (depend on tickers)

                                                            // Use new batch processing for snapshots (90-95% performance improvement)
                                                            // CoreLogger.logInfo "ImportManager" "Starting batch financial snapshot processing for import"

                                                            let! batchResult =
                                                                BrokerFinancialBatchManager
                                                                    .processBatchedFinancialsForImport (
                                                                        brokerAccount.Id
                                                                    )

                                                            if batchResult.Success then
                                                                // CoreLogger.logInfof "ImportManager" "Batch snapshot processing completed: %d snapshots in %dms (Load: %dms, Calc: %dms, Save: %dms)" batchResult.SnapshotsSaved batchResult.TotalTimeMs batchResult.LoadTimeMs batchResult.CalculationTimeMs batchResult.PersistenceTimeMs
                                                                ()
                                                            else
                                                                CoreLogger.logWarningf
                                                                    "ImportManager"
                                                                    "Batch snapshot processing had errors: %s"
                                                                    (batchResult.Errors |> String.concat "; ")

                                                            // Process ticker snapshots in batch (NEW - Phase 2 of snapshot processing)
                                                            // CoreLogger.logInfo "ImportManager" "Starting batch ticker snapshot processing for import"

                                                            let! tickerBatchResult =
                                                                TickerSnapshotBatchManager.processBatchedTickersForImport
                                                                    (brokerAccount.Id)
                                                                    (persistenceResult.ImportMetadata)

                                                            if tickerBatchResult.Success then
                                                                // CoreLogger.logInfof "ImportManager" "Ticker snapshot batch processing completed: %d ticker snapshots, %d currency snapshots in %dms (Load: %dms, Calc: %dms, Save: %dms)" tickerBatchResult.TickerSnapshotsSaved tickerBatchResult.CurrencySnapshotsSaved tickerBatchResult.TotalTimeMs tickerBatchResult.LoadTimeMs tickerBatchResult.CalculationTimeMs tickerBatchResult.PersistenceTimeMs
                                                                ()
                                                            else
                                                                CoreLogger.logWarningf
                                                                    "ImportManager"
                                                                    "Ticker snapshot batch processing had errors: %s"
                                                                    (tickerBatchResult.Errors |> String.concat "; ")

                                                            // Refresh reactive snapshot manager to pick up new snapshots
                                                            do! ReactiveSnapshotManager.refreshAsync ()

                                                            // Refresh TickerSnapshots collection to pick up newly created ticker snapshots
                                                            do! TickerSnapshotLoader.load ()
                                                        else
                                                            // No movements imported, just do a basic refresh
                                                            // CoreLogger.logDebug "ImportManager" "No movements imported; skipping reactive updates"
                                                            ()

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
                                                                    && persistenceResult.ImportMetadata.TotalMovementsImported > 0 }

                                                    with
                                                    | :? OperationCanceledException ->
                                                        // CoreLogger.logInfo "ImportManager" "Database persistence canceled"

                                                        return ImportResult.createCancelled ()
                                                    | ex ->
                                                        CoreLogger.logErrorf
                                                            "ImportManager"
                                                            "Database persistence failed: %s"
                                                            ex.Message

                                                        return
                                                            ImportResult.createError (
                                                                $"Database persistence failed: {ex.Message}"
                                                            )
                                                else
                                                    CoreLogger.logErrorf
                                                        "ImportManager"
                                                        "Tastytrade parse result indicates failure with %d errors"
                                                        parseResult.Errors.Length

                                                    // Log individual error details
                                                    parseResult.Errors
                                                    |> List.iteri (fun idx error ->
                                                        let rowInfo =
                                                            match error.RowNumber with
                                                            | Some row -> $"Row {row}"
                                                            | None -> "Unknown row"

                                                        CoreLogger.logErrorf
                                                            "ImportManager"
                                                            "Error %d: %s - %s"
                                                            (idx + 1)
                                                            rowInfo
                                                            error.ErrorMessage)

                                                    return parseResult
                                            }
                                        else
                                            task {
                                                CoreLogger.logErrorf
                                                    "ImportManager"
                                                    "Unsupported broker type: %s"
                                                    broker.Name

                                                return
                                                    ImportResult.createError ($"Unsupported broker type: {broker.Name}")
                                            }

                                    // Clean up temporary files
                                    FileProcessor.cleanup pf
                                    // CoreLogger.logDebug "ImportManager" "File processing cleanup complete"

                                    // Complete import and return result
                                    ImportState.completeImport (importResult)

                                    // Trigger reactive updates now that import is complete
                                    // CoreLogger.logDebug "ImportManager" "Triggering post-import reactive updates"

                                    try
                                        do! ReactiveTickerManager.refreshAsync ()
                                        do! ReactiveMovementManager.refreshAsync ()
                                        do! ReactiveSnapshotManager.refreshAsync ()

                                        // CoreLogger.logDebug "ImportManager" "Post-import reactive updates completed successfully"
                                    with ex ->
                                        CoreLogger.logErrorf
                                            "ImportManager"
                                            "Post-import reactive updates failed: %s"
                                            ex.Message

                                    CoreLogger.logInfof
                                        "ImportManager"
                                        "Import completed: success=%b, processedRecords=%d, errors=%d"
                                        importResult.Success
                                        importResult.ProcessedRecords
                                        importResult.Errors.Length

                                    return importResult

                                with
                                | :? OperationCanceledException ->
                                    // Clean up temporary files on cancellation
                                    FileProcessor.cleanup pf
                                    // CoreLogger.logInfo "ImportManager" "Import canceled during processing"
                                    return ImportResult.createCancelled ()
                                | ex ->
                                    // Clean up temporary files on error
                                    FileProcessor.cleanup pf
                                    CoreLogger.logErrorf "ImportManager" "Import failed with exception: %s" ex.Message
                                    return ImportResult.createError ($"Import failed: {ex.Message}")

            with
            | :? OperationCanceledException ->
                // CoreLogger.logInfo "ImportManager" "Import canceled before completion"
                return ImportResult.createCancelled ()
            | ex ->
                ImportState.failImport (ex.Message)
                CoreLogger.logErrorf "ImportManager" "Import failed unexpectedly: %s" ex.Message
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
