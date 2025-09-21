namespace Binnaculum.Core.Import

open System
open System.Threading
open System.Threading.Tasks
open Binnaculum.Core
open IBKRModels
open IBKRStatementParser
open IBKRDataConverter
open IBKRCashFlowProcessor
open IBKRForexProcessor

/// <summary>
/// IBKR-specific import logic for processing CSV files with comprehensive parsing
/// Integrates all IBKR parsing components for complete statement processing
/// </summary>
module IBKRImporter =
    
    /// <summary>
    /// Import processing context for dependency injection
    /// </summary>
    type ImportContext = {
        BrokerAccountId: int
        GetCurrencyId: string -> int option
        GetOrCreateTickerId: string -> int
        SaveTrade: Models.Trade -> unit
        SaveBrokerMovement: Models.BrokerMovement -> unit
        SaveTicker: Models.Ticker -> unit
    }
    
    /// <summary>
    /// Process single IBKR CSV file with complete parsing
    /// </summary>
    let private processSingleFile (filePath: string) (context: ImportContext) (cancellationToken: CancellationToken) = task {
        let fileName = System.IO.Path.GetFileName(filePath)
        let stopwatch = System.Diagnostics.Stopwatch.StartNew()
        
        try
            // Parse IBKR CSV file
            let parseResult = parseCsvFile filePath
            
            if not parseResult.Success then
                let errors = parseResult.Errors |> List.map (fun err ->
                    { RowNumber = None; ErrorMessage = err; ErrorType = ImportErrorType.ValidationError; RawData = None })
                return FileImportResult.createFailure fileName errors
            
            match parseResult.Data with
            | None ->
                let error = { RowNumber = None; ErrorMessage = "No data parsed from file"; ErrorType = ImportErrorType.ValidationError; RawData = None }
                return FileImportResult.createFailure fileName [error]
            
            | Some data ->
                cancellationToken.ThrowIfCancellationRequested()
                
                // Process forex trades
                let forexResult = processForexTrades data.ForexTrades
                
                // Process cash flows
                let cashFlowResult = processCashFlows data.CashFlows data.ExchangeRates
                
                // Convert to Binnaculum models
                let conversionResult = convertStatementData data context.BrokerAccountId context.GetCurrencyId context.GetOrCreateTickerId
                
                cancellationToken.ThrowIfCancellationRequested()
                
                // Save converted data
                let mutable tradesCount = 0
                let mutable movementsCount = 0
                let mutable tickersCount = 0
                
                // Save trades
                for trade in conversionResult.Trades do
                    context.SaveTrade trade
                    tradesCount <- tradesCount + 1
                
                // Save broker movements
                for movement in conversionResult.BrokerMovements do
                    context.SaveBrokerMovement movement
                    movementsCount <- movementsCount + 1
                
                // Save new tickers
                for ticker in conversionResult.Tickers do
                    context.SaveTicker ticker
                    tickersCount <- tickersCount + 1
                
                stopwatch.Stop()
                
                // Create success result with warnings
                let allWarnings = 
                    parseResult.Warnings @ 
                    conversionResult.Warnings @ 
                    forexResult.Warnings @ 
                    cashFlowResult.Warnings
                
                let warnings = allWarnings |> List.map (fun warning ->
                    { RowNumber = None; WarningMessage = warning; WarningType = ImportWarningType.DataFormatWarning; RawData = None })
                
                let fileResult = {
                    FileName = fileName
                    Success = true
                    ProcessedRecords = tradesCount + movementsCount
                    Errors = []
                    Warnings = warnings
                }
                
                return fileResult
        
        with
        | :? OperationCanceledException ->
            return FileImportResult.createFailure fileName 
                [{ RowNumber = None; ErrorMessage = "Import cancelled"; ErrorType = ImportErrorType.ValidationError; RawData = None }]
        | ex ->
            return FileImportResult.createFailure fileName 
                [{ RowNumber = None; ErrorMessage = $"Error processing {fileName}: {ex.Message}"; ErrorType = ImportErrorType.ValidationError; RawData = None }]
    }
    
    /// <summary>
    /// Import multiple CSV files from IBKR with cancellation support
    /// </summary>
    /// <param name="csvFilePaths">List of CSV file paths to process</param>
    /// <param name="context">Import processing context</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Consolidated ImportResult</returns>
    let importMultipleWithCancellation (csvFilePaths: string list) (context: ImportContext) (cancellationToken: CancellationToken) = task {
        let startTime = System.Diagnostics.Stopwatch.StartNew()
        
        let mutable totalResult = {
            Success = true
            ProcessedFiles = csvFilePaths.Length
            ProcessedRecords = 0
            SkippedRecords = 0
            TotalRecords = 0
            ProcessingTimeMs = 0L
            Errors = []
            Warnings = []
            ImportedData = { Trades = 0; BrokerMovements = 0; Dividends = 0; OptionTrades = 0; NewTickers = 0 }
            FileResults = []
        }
        
        let mutable fileResults = []
        
        for (index, csvFile) in csvFilePaths |> List.mapi (fun i file -> i, file) do
            cancellationToken.ThrowIfCancellationRequested()
            
            let fileName = System.IO.Path.GetFileName(csvFile)
            let progress = float index / float csvFilePaths.Length
            ImportState.updateStatus(ProcessingFile(fileName, progress))
            
            try
                let! fileResult = processSingleFile csvFile context cancellationToken
                fileResults <- fileResult :: fileResults
                
                if fileResult.Success then
                    totalResult <- { totalResult with 
                                       ProcessedRecords = totalResult.ProcessedRecords + fileResult.ProcessedRecords
                                       TotalRecords = totalResult.TotalRecords + fileResult.ProcessedRecords }
                else
                    totalResult <- { totalResult with 
                                       Success = false
                                       Errors = totalResult.Errors @ fileResult.Errors }
            
            with
            | :? OperationCanceledException ->
                let error = { RowNumber = None; ErrorMessage = "Import cancelled"; ErrorType = ImportErrorType.ValidationError; RawData = None }
                let fileResult = FileImportResult.createFailure fileName [error]
                fileResults <- fileResult :: fileResults
                totalResult <- { totalResult with Success = false; Errors = error :: totalResult.Errors }
                return totalResult
        
        startTime.Stop()
        totalResult <- { totalResult with ProcessingTimeMs = startTime.ElapsedMilliseconds }
        
        // Final status update
        ImportState.updateStatus(ProcessingData(totalResult.ProcessedRecords, totalResult.TotalRecords))
        
        return { totalResult with FileResults = List.rev fileResults }
    }
    
    /// <summary>
    /// Import single CSV file from IBKR with context
    /// </summary>
    /// <param name="csvFilePath">Path to CSV file</param>
    /// <param name="context">Import processing context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>FileImportResult for the single file</returns>
    let importSingleWithCancellation (csvFilePath: string) (context: ImportContext) (cancellationToken: CancellationToken) = task {
        let! result = importMultipleWithCancellation [csvFilePath] context cancellationToken
        return result.FileResults |> List.tryHead |> Option.defaultValue (FileImportResult.createFailure (System.IO.Path.GetFileName(csvFilePath)) [])
    }
    
    /// <summary>
    /// Legacy import methods for backward compatibility (simplified implementation)
    /// These will be replaced with proper context-based implementations
    /// </summary>
    
    /// <summary>
    /// Import multiple CSV files from IBKR with cancellation support (simplified)
    /// </summary>
    /// <param name="csvFilePaths">List of CSV file paths to process</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Consolidated ImportResult</returns>
    let importMultipleWithCancellationLegacy (csvFilePaths: string list) (cancellationToken: CancellationToken) = task {
        let mutable totalResult = {
            Success = true
            ProcessedFiles = csvFilePaths.Length
            ProcessedRecords = 0
            SkippedRecords = 0
            TotalRecords = 0
            ProcessingTimeMs = 0L
            Errors = []
            Warnings = []
            ImportedData = { Trades = 0; BrokerMovements = 0; Dividends = 0; OptionTrades = 0; NewTickers = 0 }
            FileResults = []
        }
        
        let mutable fileResults = []
        
        for (index, csvFile) in csvFilePaths |> List.mapi (fun i file -> i, file) do
            cancellationToken.ThrowIfCancellationRequested()
            
            let fileName = System.IO.Path.GetFileName(csvFile)
            let progress = float index / float csvFilePaths.Length
            ImportState.updateStatus(ProcessingFile(fileName, progress))
            
            try
                if System.IO.File.Exists(csvFile) then
                    // Parse the file to validate structure
                    let parseResult = parseCsvFile csvFile
                    
                    if parseResult.Success then
                        let recordCount = 
                            match parseResult.Data with
                            | Some data -> 
                                data.Trades.Length + 
                                data.ForexTrades.Length + 
                                data.CashMovements.Length + 
                                data.CashFlows.Length
                            | None -> 0
                        
                        let fileResult = FileImportResult.createSuccess fileName recordCount
                        fileResults <- fileResult :: fileResults
                        
                        totalResult <- { totalResult with 
                                           ProcessedRecords = totalResult.ProcessedRecords + recordCount
                                           TotalRecords = totalResult.TotalRecords + recordCount }
                    else
                        let errors = parseResult.Errors |> List.map (fun err ->
                            { RowNumber = None; ErrorMessage = err; ErrorType = ImportErrorType.ValidationError; RawData = None })
                        let fileResult = FileImportResult.createFailure fileName errors
                        fileResults <- fileResult :: fileResults
                        
                        totalResult <- { totalResult with 
                                           Success = false
                                           Errors = totalResult.Errors @ errors }
                else
                    let error = { RowNumber = None; ErrorMessage = $"File not found: {fileName}"; ErrorType = ImportErrorType.ValidationError; RawData = None }
                    let fileResult = FileImportResult.createFailure fileName [error]
                    fileResults <- fileResult :: fileResults
                    
                    totalResult <- { totalResult with 
                                       Success = false
                                       Errors = error :: totalResult.Errors }
            with
            | ex ->
                let error = { RowNumber = None; ErrorMessage = $"Error processing {fileName}: {ex.Message}"; ErrorType = ImportErrorType.ValidationError; RawData = None }
                let fileResult = FileImportResult.createFailure fileName [error]
                fileResults <- fileResult :: fileResults
                
                totalResult <- { totalResult with 
                                   Success = false
                                   Errors = error :: totalResult.Errors }
        
        // Final status update
        ImportState.updateStatus(ProcessingData(totalResult.ProcessedRecords, totalResult.TotalRecords))
        
        return { totalResult with FileResults = List.rev fileResults }
    }
    
    /// <summary>
    /// Import single CSV file from IBKR (simplified)
    /// </summary>
    /// <param name="csvFilePath">Path to CSV file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>FileImportResult for the single file</returns>
    let importSingleWithCancellationLegacy (csvFilePath: string) (cancellationToken: CancellationToken) = task {
        let! result = importMultipleWithCancellationLegacy [csvFilePath] cancellationToken
        return result.FileResults |> List.tryHead |> Option.defaultValue (FileImportResult.createFailure (System.IO.Path.GetFileName(csvFilePath)) [])
    }
    
    /// <summary>
    /// Import single CSV file from IBKR
    /// </summary>
    /// <param name="csvFilePath">Path to CSV file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>FileImportResult for the single file</returns>
    let importSingleWithCancellation (csvFilePath: string) (cancellationToken: CancellationToken) = task {
        let! result = importMultipleWithCancellation [csvFilePath] cancellationToken
        return result.FileResults |> List.tryHead |> Option.defaultValue (FileImportResult.createFailure (System.IO.Path.GetFileName(csvFilePath)) [])
    }