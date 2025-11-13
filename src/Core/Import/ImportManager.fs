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
open IBKRModels

/// <summary>
/// Main import manager for file import operations with cancellation support
/// </summary>
module ImportManager =

    /// <summary>
    /// Filter PersistenceInput movements by date range for chunk processing.
    /// Used to process only movements within a specific chunk's date boundaries.
    /// </summary>
    let private filterMovementsByDateRange
        (startDate: DateOnly)
        (endDate: DateOnly)
        (movements: ImportDomainTypes.PersistenceInput)
        : ImportDomainTypes.PersistenceInput =

        let startDateTime = startDate.ToDateTime(TimeOnly.MinValue)
        let endDateTime = endDate.ToDateTime(TimeOnly.MinValue).AddDays(1.0).AddTicks(-1L) // End of day

        { BrokerMovements =
            movements.BrokerMovements
            |> List.filter (fun m ->
                let dt = m.TimeStamp.Value
                dt >= startDateTime && dt <= endDateTime)

          StockTrades =
            movements.StockTrades
            |> List.filter (fun t ->
                let dt = t.TimeStamp.Value
                dt >= startDateTime && dt <= endDateTime)

          Dividends =
            movements.Dividends
            |> List.filter (fun d ->
                let dt = d.TimeStamp.Value
                dt >= startDateTime && dt <= endDateTime)

          DividendTaxes =
            movements.DividendTaxes
            |> List.filter (fun d ->
                let dt = d.TimeStamp.Value
                dt >= startDateTime && dt <= endDateTime)

          OptionTrades =
            movements.OptionTrades
            |> List.filter (fun o ->
                let dt = o.TimeStamp.Value
                dt >= startDateTime && dt <= endDateTime)

          SessionId = movements.SessionId }

    /// <summary>
    /// Extract unique ticker IDs from movements for snapshot calculation.
    /// Returns distinct list of ticker IDs that have activity in the movements.
    /// </summary>
    let private getTickerIdsFromMovements (movements: ImportDomainTypes.PersistenceInput) : int list =
        [ yield! movements.StockTrades |> List.map (fun t -> t.TickerId)
          yield! movements.Dividends |> List.map (fun d -> d.TickerId)
          yield! movements.OptionTrades |> List.map (fun o -> o.TickerId) ]
        |> List.distinct

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

                                    ImportState.failImport (ResourceKeys.Import_Failed)
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
                                            task {
                                                // PHASE 1: Analyze CSV files for date ranges
                                                CoreLogger.logInfof
                                                    "ImportManager"
                                                    "Starting chunked IBKR import for %d files"
                                                    pf.CsvFiles.Length

                                                let! analysis =
                                                    task {
                                                        // Use CsvDateAnalyzer to scan files
                                                        let analysisResult = CsvDateAnalyzer.analyzeAndSort pf.CsvFiles

                                                        // Convert to DateAnalysis format for ChunkStrategy
                                                        match analysisResult.OverallDateRange with
                                                        | Some(minDate, maxDate) ->
                                                            // Build movement count map from file metadata
                                                            let movementsByDate =
                                                                analysisResult.FilesOrderedByDate
                                                                |> List.collect (fun meta -> meta.AllDates)
                                                                |> List.groupBy DateOnly.FromDateTime
                                                                |> List.map (fun (date, dates) -> (date, dates.Length))
                                                                |> Map.ofList

                                                            return
                                                                { MinDate = minDate
                                                                  MaxDate = maxDate
                                                                  TotalMovements = analysisResult.TotalRecords
                                                                  MovementsByDate = movementsByDate
                                                                  UniqueDates =
                                                                    movementsByDate
                                                                    |> Map.toList
                                                                    |> List.map fst
                                                                    |> List.sort
                                                                  FileHash =
                                                                    CsvDateAnalyzer.calculateFileHash (pf.CsvFiles.[0]) }
                                                        | None ->
                                                            // No data in files
                                                            let now = DateTime.Now

                                                            return
                                                                { MinDate = now
                                                                  MaxDate = now
                                                                  TotalMovements = 0
                                                                  MovementsByDate = Map.empty
                                                                  UniqueDates = []
                                                                  FileHash = "" }
                                                    }

                                                // PHASE 2: Create weekly chunks
                                                let chunks = ChunkStrategy.createWeeklyChunks analysis

                                                CoreLogger.logInfof
                                                    "ImportManager"
                                                    "Created %d chunks for date range %s to %s (%d total movements)"
                                                    chunks.Length
                                                    (analysis.MinDate.ToString("yyyy-MM-dd"))
                                                    (analysis.MaxDate.ToString("yyyy-MM-dd"))
                                                    analysis.TotalMovements

                                                if chunks.IsEmpty then
                                                    // No movements to process
                                                    CoreLogger.logInfo
                                                        "ImportManager"
                                                        "No movements found in files, skipping import"

                                                    return
                                                        ImportResult.createSuccess
                                                            0
                                                            0
                                                            { Trades = 0
                                                              BrokerMovements = 0
                                                              Dividends = 0
                                                              OptionTrades = 0
                                                              NewTickers = 0 }
                                                            []
                                                            0L
                                                else
                                                    // PHASE 3: Create import session
                                                    let! sessionId =
                                                        ImportSessionManager.createSession
                                                            brokerAccount.Id
                                                            brokerAccount.AccountNumber
                                                            filePath
                                                            analysis
                                                            chunks

                                                    CoreLogger.logInfof
                                                        "ImportManager"
                                                        "Created import session %d"
                                                        sessionId

                                                    // PHASE 4: Process each chunk
                                                    let mutable totalProcessed = 0
                                                    let mutable totalMovementsImported = 0
                                                    let mutable allErrors = []
                                                    let stopwatch = System.Diagnostics.Stopwatch.StartNew()

                                                    for chunk in chunks do
                                                        cancellationToken.ThrowIfCancellationRequested()

                                                        let chunkStopwatch = System.Diagnostics.Stopwatch.StartNew()

                                                        CoreLogger.logInfof
                                                            "ImportManager"
                                                            "Processing chunk %d/%d (dates: %s to %s, estimated: %d movements)"
                                                            chunk.ChunkNumber
                                                            chunks.Length
                                                            (chunk.StartDate.ToString("yyyy-MM-dd"))
                                                            (chunk.EndDate.ToString("yyyy-MM-dd"))
                                                            chunk.EstimatedMovements

                                                        try
                                                            // Parse CSV files to get IBKR statement data
                                                            let mutable allStatementData: IBKRStatementData option =
                                                                None

                                                            for csvFile in pf.CsvFiles do
                                                                let parseResult =
                                                                    IBKRStatementParser.parseCsvFile csvFile

                                                                if parseResult.Success then
                                                                    match parseResult.Data with
                                                                    | Some statement ->
                                                                        // Merge statement data (for multi-file imports)
                                                                        match allStatementData with
                                                                        | None -> allStatementData <- Some statement
                                                                        | Some existing ->
                                                                            // Combine data from multiple files
                                                                            allStatementData <-
                                                                                Some
                                                                                    { StatementDate =
                                                                                        existing.StatementDate // Keep first statement date
                                                                                      BrokerName = existing.BrokerName // Keep first broker name
                                                                                      Trades =
                                                                                        existing.Trades
                                                                                        @ statement.Trades
                                                                                      ForexTrades =
                                                                                        existing.ForexTrades
                                                                                        @ statement.ForexTrades
                                                                                      CashMovements =
                                                                                        existing.CashMovements
                                                                                        @ statement.CashMovements
                                                                                      CashFlows =
                                                                                        existing.CashFlows
                                                                                        @ statement.CashFlows
                                                                                      OpenPositions =
                                                                                        existing.OpenPositions
                                                                                        @ statement.OpenPositions
                                                                                      Instruments =
                                                                                        existing.Instruments
                                                                                        @ statement.Instruments
                                                                                      ExchangeRates =
                                                                                        existing.ExchangeRates
                                                                                        @ statement.ExchangeRates
                                                                                      ForexBalances =
                                                                                        existing.ForexBalances
                                                                                        @ statement.ForexBalances }
                                                                    | None -> ()

                                                            match allStatementData with
                                                            | Some statement ->
                                                                // Convert to domain models
                                                                let! domainModels =
                                                                    IBKRConverter.convertToDomainModels
                                                                        statement
                                                                        brokerAccount.Id
                                                                        (Some sessionId)
                                                                        cancellationToken

                                                                // Filter movements by chunk date range
                                                                let chunkMovements =
                                                                    filterMovementsByDateRange
                                                                        chunk.StartDate
                                                                        chunk.EndDate
                                                                        domainModels

                                                                let chunkMovementCount =
                                                                    chunkMovements.BrokerMovements.Length
                                                                    + chunkMovements.StockTrades.Length
                                                                    + chunkMovements.Dividends.Length
                                                                    + chunkMovements.DividendTaxes.Length
                                                                    + chunkMovements.OptionTrades.Length

                                                                CoreLogger.logInfof
                                                                    "ImportManager"
                                                                    "Chunk %d filtered: %d movements (BrokerMovements: %d, Trades: %d, Dividends: %d, OptionTrades: %d)"
                                                                    chunk.ChunkNumber
                                                                    chunkMovementCount
                                                                    chunkMovements.BrokerMovements.Length
                                                                    chunkMovements.StockTrades.Length
                                                                    chunkMovements.Dividends.Length
                                                                    chunkMovements.OptionTrades.Length

                                                                if chunkMovementCount > 0 then
                                                                    // Persist chunk to database
                                                                    let! persistResult =
                                                                        DatabasePersistence.persistDomainModelsToDatabase
                                                                            chunkMovements
                                                                            brokerAccount.Id
                                                                            cancellationToken

                                                                    totalMovementsImported <-
                                                                        totalMovementsImported
                                                                        + persistResult.ImportMetadata.TotalMovementsImported

                                                                    allErrors <- allErrors @ persistResult.Errors

                                                                    // Calculate snapshots ONLY for this chunk
                                                                    let tickerIds =
                                                                        getTickerIdsFromMovements chunkMovements

                                                                    if not tickerIds.IsEmpty then
                                                                        let! tickerResult =
                                                                            TickerSnapshotBatchManager.processBatchedTickers
                                                                                { BrokerAccountId =
                                                                                    Some brokerAccount.Id
                                                                                  TickerIds = tickerIds
                                                                                  StartDate =
                                                                                    Patterns
                                                                                        .DateTimePattern
                                                                                        .FromDateTime(
                                                                                            chunk.StartDate.ToDateTime(
                                                                                                TimeOnly.MinValue
                                                                                            )
                                                                                        )
                                                                                  EndDate =
                                                                                    Patterns
                                                                                        .DateTimePattern
                                                                                        .FromDateTime(
                                                                                            chunk.EndDate.ToDateTime(
                                                                                                TimeOnly.MinValue
                                                                                            )
                                                                                        )
                                                                                  ForceRecalculation = false }

                                                                        if not tickerResult.Success then
                                                                            CoreLogger.logWarningf
                                                                                "ImportManager"
                                                                                "Ticker snapshot processing had errors: %s"
                                                                                (tickerResult.Errors
                                                                                 |> String.concat "; ")

                                                                        let! brokerResult =
                                                                            BrokerFinancialBatchManager.processBatchedFinancials
                                                                                { BrokerAccountId = brokerAccount.Id
                                                                                  StartDate =
                                                                                    Patterns
                                                                                        .DateTimePattern
                                                                                        .FromDateTime(
                                                                                            chunk.StartDate.ToDateTime(
                                                                                                TimeOnly.MinValue
                                                                                            )
                                                                                        )
                                                                                  EndDate =
                                                                                    Patterns
                                                                                        .DateTimePattern
                                                                                        .FromDateTime(
                                                                                            chunk.EndDate.ToDateTime(
                                                                                                TimeOnly.MinValue
                                                                                            )
                                                                                        )
                                                                                  ForceRecalculation = false }
                                                                                tickerResult.CalculatedOperations
                                                                                tickerResult.CalculatedTickerSnapshots

                                                                        if not brokerResult.Success then
                                                                            CoreLogger.logWarningf
                                                                                "ImportManager"
                                                                                "Broker snapshot processing had errors: %s"
                                                                                (brokerResult.Errors
                                                                                 |> String.concat "; ")

                                                                    // Mark chunk as completed in database (transaction handled internally)
                                                                    do!
                                                                        ImportSessionManager.markChunkCompleted
                                                                            sessionId
                                                                            chunk.ChunkNumber
                                                                            chunkMovementCount
                                                                            chunkStopwatch.ElapsedMilliseconds

                                                                    CoreLogger.logInfof
                                                                        "ImportManager"
                                                                        "Chunk %d completed in %dms"
                                                                        chunk.ChunkNumber
                                                                        chunkStopwatch.ElapsedMilliseconds
                                                                else
                                                                    CoreLogger.logInfof
                                                                        "ImportManager"
                                                                        "Chunk %d has no movements in date range, skipping"
                                                                        chunk.ChunkNumber

                                                            | None ->
                                                                // No data parsed from files
                                                                CoreLogger.logWarning
                                                                    "ImportManager"
                                                                    "No IBKR statement data parsed from CSV files"

                                                            totalProcessed <- totalProcessed + 1

                                                            // CRITICAL: Force garbage collection after each chunk
                                                            GC.Collect()
                                                            GC.WaitForPendingFinalizers()
                                                            GC.Collect()

                                                        with ex ->
                                                            CoreLogger.logErrorf
                                                                "ImportManager"
                                                                "Error processing chunk %d: %s"
                                                                chunk.ChunkNumber
                                                                ex.Message

                                                            allErrors <- ex.Message :: allErrors

                                                    // PHASE 5: Complete session
                                                    do! ImportSessionManager.completeSession sessionId

                                                    CoreLogger.logInfof
                                                        "ImportManager"
                                                        "Import session %d completed: processed %d/%d chunks, %d total movements in %dms"
                                                        sessionId
                                                        totalProcessed
                                                        chunks.Length
                                                        totalMovementsImported
                                                        stopwatch.ElapsedMilliseconds

                                                    // Refresh reactive managers after ALL chunks complete
                                                    do! ReactiveTickerManager.refreshAsync ()
                                                    do! ReactiveSnapshotManager.refreshAsync ()
                                                    do! TickerSnapshotLoader.load ()

                                                    // Return result
                                                    return
                                                        { Success = allErrors.IsEmpty
                                                          ProcessedFiles = pf.CsvFiles.Length
                                                          ProcessedRecords = totalMovementsImported
                                                          SkippedRecords = 0
                                                          TotalRecords = totalMovementsImported
                                                          ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                                                          Errors =
                                                            allErrors
                                                            |> List.map (fun err ->
                                                                { RowNumber = None
                                                                  ErrorMessage = err
                                                                  ErrorType = ImportErrorType.ValidationError
                                                                  RawData = None
                                                                  FromFile = "" })
                                                          Warnings = []
                                                          ImportedData =
                                                            { Trades = 0 // TODO: Track per type
                                                              BrokerMovements = 0
                                                              Dividends = 0
                                                              OptionTrades = 0
                                                              NewTickers = 0 }
                                                          FileResults = []
                                                          ProcessedChunks = totalProcessed
                                                          SessionId = Some sessionId }
                                            }
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

                                                        // Convert Tastytrade transactions to domain models
                                                        // CoreLogger.logDebugf "ImportManager" "Converting %d transactions to domain models for account %d" allTransactions.Length brokerAccount.Id

                                                        let! domainModels =
                                                            TastytradeConverter.convertToDomainModels
                                                                allTransactions
                                                                brokerAccount.Id
                                                                None // No session ID for now
                                                                cancellationToken

                                                        // Persist domain models to database
                                                        // CoreLogger.logDebugf "ImportManager" "Persisting domain models to database for account %d" brokerAccount.Id

                                                        let! persistenceResult =
                                                            DatabasePersistence.persistDomainModelsToDatabase
                                                                domainModels
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

                                                            // PHASE 1: Process ticker snapshots FIRST (calculates AutoImportOperations)
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

                                                            // PHASE 2: Process financial snapshots SECOND (uses AutoImportOperations from Phase 1)
                                                            // CoreLogger.logInfo "ImportManager" "Starting batch financial snapshot processing for import"

                                                            let! batchResult =
                                                                BrokerFinancialBatchManager.processBatchedFinancialsForImport
                                                                    brokerAccount.Id
                                                                    tickerBatchResult.CalculatedOperations
                                                                    tickerBatchResult.CalculatedTickerSnapshots

                                                            if batchResult.Success then
                                                                // CoreLogger.logInfof "ImportManager" "Batch snapshot processing completed: %d snapshots in %dms (Load: %dms, Calc: %dms, Save: %dms)" batchResult.SnapshotsSaved batchResult.TotalTimeMs batchResult.LoadTimeMs batchResult.CalculationTimeMs batchResult.PersistenceTimeMs
                                                                ()
                                                            else
                                                                CoreLogger.logWarningf
                                                                    "ImportManager"
                                                                    "Batch snapshot processing had errors: %s"
                                                                    (batchResult.Errors |> String.concat "; ")

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
                                                                  RawData = None
                                                                  FromFile = "" })

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
        ImportState.cancelImport (ResourceKeys.Import_Cancelled)

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
