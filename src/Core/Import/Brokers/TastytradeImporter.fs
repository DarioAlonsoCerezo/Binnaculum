namespace Binnaculum.Core.Import

open System
open System.Threading
open System.Threading.Tasks
open System.Diagnostics
open TastytradeStatementParser
open TastytradeStrategyDetector
open TastytradeDataConverter
open Binnaculum.Core.Database.DatabaseExtensions
open Binnaculum.Core.Models

/// <summary>
/// Tastytrade-specific import logic for processing CSV files with cancellation support
/// </summary>
module TastytradeImporter =
    
    /// <summary>
    /// Process a single Tastytrade CSV file and save to database
    /// </summary>
    /// <param name="csvFilePath">Path to CSV file</param>
    /// <param name="brokerAccountId">Target broker account ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>FileImportResult with processing details</returns>
    let private processSingleFile (csvFilePath: string) (brokerAccountId: int) (cancellationToken: CancellationToken) = task {
        let fileName = System.IO.Path.GetFileName(csvFilePath)
        let stopwatch = Stopwatch.StartNew()
        
        try
            cancellationToken.ThrowIfCancellationRequested()
            
            // Parse the CSV file
            let parsingResult = parseTransactionHistoryFromFile csvFilePath
            
            if not parsingResult.Errors.IsEmpty then
                // Convert parsing errors to import errors
                let importErrors = 
                    parsingResult.Errors
                    |> List.map (fun parseError -> {
                        RowNumber = Some parseError.LineNumber
                        ErrorMessage = parseError.ErrorMessage
                        ErrorType = 
                            match parseError.ErrorType with
                            | TastytradeModels.InvalidDateFormat -> ImportModels.InvalidDate
                            | TastytradeModels.InvalidOptionSymbol -> ImportModels.InvalidDataFormat
                            | TastytradeModels.MissingRequiredField(_) -> ImportModels.MissingRequiredField
                            | TastytradeModels.InvalidTransactionType -> ImportModels.ValidationError
                            | TastytradeModels.InvalidNumericValue(_) -> ImportModels.InvalidAmount
                            | TastytradeModels.UnsupportedInstrumentType(_) -> ImportModels.ValidationError
                        RawData = Some parseError.RawCsvLine
                    })
                
                return FileImportResult.createFailure fileName importErrors
            
            cancellationToken.ThrowIfCancellationRequested()
            
            // Detect multi-leg strategies
            let strategies = detectStrategies parsingResult.Transactions
            
            // Convert transactions to database models
            let (optionTrades, equityTrades, brokerMovements) = convertAllTransactions parsingResult brokerAccountId
            
            cancellationToken.ThrowIfCancellationRequested()
            
            // For now just validate conversion worked - TODO: Implement actual database saving
            let savedOptionTrades = optionTrades.Length
            let savedEquityTrades = equityTrades.Length  
            let savedBrokerMovements = brokerMovements.Length
            
            cancellationToken.ThrowIfCancellationRequested()
            
            stopwatch.Stop()
            
            return {
                FileName = fileName
                Success = true
                ProcessedRecords = parsingResult.ProcessedLines
                Errors = []
                Warnings = []
            }
            
        with
        | :? OperationCanceledException ->
            stopwatch.Stop()
            return {
                FileName = fileName
                Success = false
                ProcessedRecords = 0
                Errors = [{ RowNumber = None; ErrorMessage = "Import was cancelled"; ErrorType = ValidationError; RawData = None }]
                Warnings = []
            }
        | ex ->
            stopwatch.Stop()
            return {
                FileName = fileName
                Success = false
                ProcessedRecords = 0
                Errors = [{ RowNumber = None; ErrorMessage = ex.Message; ErrorType = ValidationError; RawData = None }]
                Warnings = []
            }
    }
    
    /// <summary>
    /// Import multiple CSV files from Tastytrade with cancellation support
    /// </summary>
    /// <param name="csvFilePaths">List of CSV file paths to process</param>
    /// <param name="brokerAccountId">Target broker account ID</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Consolidated ImportResult</returns>
    let importMultipleWithCancellation (csvFilePaths: string list) (brokerAccountId: int) (cancellationToken: CancellationToken) = task {
        let stopwatch = Stopwatch.StartNew()
        
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
        let mutable totalOptionTrades = 0
        let mutable totalEquityTrades = 0
        let mutable totalBrokerMovements = 0
        
        try
            for (index, csvFile) in csvFilePaths |> List.mapi (fun i file -> i, file) do
                cancellationToken.ThrowIfCancellationRequested()
                
                let fileName = System.IO.Path.GetFileName(csvFile)
                let progress = float index / float csvFilePaths.Length
                ImportState.updateStatus(ProcessingFile(fileName, progress))
                
                let! fileResult = processSingleFile csvFile brokerAccountId cancellationToken
                fileResults <- fileResult :: fileResults
                
                if fileResult.Success then
                    // Parse file to get counts for summary
                    let parsingResult = parseTransactionHistoryFromFile csvFile
                    let (optionTrades, equityTrades, brokerMovements) = convertAllTransactions parsingResult brokerAccountId
                    
                    totalOptionTrades <- totalOptionTrades + optionTrades.Length
                    totalEquityTrades <- totalEquityTrades + equityTrades.Length
                    totalBrokerMovements <- totalBrokerMovements + brokerMovements.Length
                    
                    totalResult <- { totalResult with 
                        ProcessedRecords = totalResult.ProcessedRecords + fileResult.ProcessedRecords
                        TotalRecords = totalResult.TotalRecords + fileResult.ProcessedRecords 
                    }
                else
                    totalResult <- { totalResult with 
                        Success = false
                        Errors = totalResult.Errors @ fileResult.Errors
                        SkippedRecords = totalResult.SkippedRecords + 1 
                    }
            
            stopwatch.Stop()
            
            // Calculate new tickers
            let newTickersCount = 
                csvFilePaths
                |> List.map parseTransactionHistoryFromFile
                |> List.collect (fun result -> result.Transactions)
                |> List.choose (fun t -> 
                    match t.RootSymbol, t.UnderlyingSymbol, t.Symbol with
                    | Some root, _, _ -> Some root
                    | None, Some underlying, _ -> Some underlying
                    | None, None, Some symbol -> Some symbol
                    | _ -> None)
                |> List.distinct
                |> List.length
            
            // Final status update
            ImportState.updateStatus(ProcessingData(totalResult.ProcessedRecords, totalResult.TotalRecords))
            
            return { totalResult with 
                FileResults = List.rev fileResults
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                ImportedData = { 
                    Trades = totalEquityTrades
                    BrokerMovements = totalBrokerMovements
                    Dividends = 0
                    OptionTrades = totalOptionTrades
                    NewTickers = newTickersCount
                }
            }
            
        with
        | :? OperationCanceledException ->
            stopwatch.Stop()
            return { totalResult with 
                Success = false
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                Errors = [{ RowNumber = None; ErrorMessage = "Import was cancelled by user"; ErrorType = ValidationError; RawData = None }]
                FileResults = List.rev fileResults
            }
        | ex ->
            stopwatch.Stop()
            return { totalResult with 
                Success = false
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                Errors = [{ RowNumber = None; ErrorMessage = ex.Message; ErrorType = ValidationError; RawData = None }]
                FileResults = List.rev fileResults
            }
    }
    
    /// <summary>
    /// Import single CSV file from Tastytrade
    /// </summary>
    /// <param name="csvFilePath">Path to CSV file</param>
    /// <param name="brokerAccountId">Target broker account ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>FileImportResult for the single file</returns>
    let importSingleWithCancellation (csvFilePath: string) (brokerAccountId: int) (cancellationToken: CancellationToken) = task {
        let! result = importMultipleWithCancellation [csvFilePath] brokerAccountId cancellationToken
        return result.FileResults |> List.tryHead |> Option.defaultValue (FileImportResult.createFailure (System.IO.Path.GetFileName(csvFilePath)) [])
    }