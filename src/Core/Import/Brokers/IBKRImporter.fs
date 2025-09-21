namespace Binnaculum.Core.Import

open System
open System.Threading
open System.Threading.Tasks
open Binnaculum.Core
open Binnaculum.Core.Models
open Binnaculum.Core.Memory
open IBKRModels
open IBKRStatementParser
open IBKRDataConverter

namespace Binnaculum.Core.Import

open System
open System.Threading
open System.Threading.Tasks
open Binnaculum.Core
open Binnaculum.Core.Memory
open Binnaculum.Core.UI
open IBKRModels
open IBKRStatementParser
open IBKRDataConverter

/// <summary>
/// IBKR-specific import logic for processing CSV files with comprehensive parsing
/// Integrates all IBKR parsing components for complete statement processing
/// </summary>
module IBKRImporter =
    
    /// <summary>
    /// Helper function to safely get currency by code, returns None if not found
    /// </summary>
    let private getCurrencyOption (code: string) : Binnaculum.Core.Models.Currency option =
        try
            Some (code.ToFastCurrency())
        with
        | _ -> None
    
    /// <summary>
    /// Helper function to safely get ticker by symbol, returns None if not found
    /// </summary>
    let private getTickerOption (symbol: string) : Binnaculum.Core.Models.Ticker option =
        try
            Some (symbol.ToFastTicker())
        with
        | _ -> None
    
    /// <summary>
    /// Helper function to get or create an IBKR broker account
    /// For now, we'll assume there's a default IBKR broker account
    /// This should be enhanced to handle multiple IBKR accounts
    /// </summary>
    let private getIBKRBrokerAccount () : Binnaculum.Core.Models.BrokerAccount option =
        try
            // For now, we'll get the first IBKR broker account
            // This should be enhanced based on requirements
            let ibkrBrokers = Collections.Brokers.Items 
                              |> Seq.filter (fun b -> b.SupportedBroker = Binnaculum.Core.Models.SupportedBroker.IBKR)
                              |> Seq.toList
            
            match ibkrBrokers with
            | broker :: _ ->
                let brokerAccounts = Collections.BrokerAccounts.Items
                                   |> Seq.filter (fun ba -> ba.Broker.Id = broker.Id)
                                   |> Seq.toList
                match brokerAccounts with
                | account :: _ -> Some account
                | [] -> None // No IBKR accounts found
            | [] -> None // No IBKR brokers found
        with
        | _ -> None
    
    /// <summary>
    /// Process parsed IBKR statement and save records to database
    /// Returns the number of records actually created
    /// </summary>
    let private processIBKRStatement (statement: IBKRStatementData) : Task<int> = task {
        match getIBKRBrokerAccount () with
        | None -> 
            return 0 // No IBKR broker account found, cannot process
        | Some brokerAccount ->
            // Convert IBKR data to database models
            let (brokerMovements, trades) = convertStatementToModels statement brokerAccount getCurrencyOption getTickerOption
            
            let mutable recordCount = 0
            
            // Save broker movements (deposits, withdrawals, forex conversions)
            for movement in brokerMovements do
                try
                    do! Creator.SaveBrokerMovement(movement)
                    recordCount <- recordCount + 1
                with
                | ex ->
                    System.Diagnostics.Debug.WriteLine($"Failed to save broker movement: {ex.Message}")
            
            // Save stock trades
            for trade in trades do
                try
                    do! Creator.SaveTrade(trade)
                    recordCount <- recordCount + 1
                with
                | ex ->
                    System.Diagnostics.Debug.WriteLine($"Failed to save trade: {ex.Message}")
            
            return recordCount
    }
    
    /// <summary>
    /// Enhanced IBKR import with actual database record creation
    /// </summary>
    let importMultipleWithDatabase (csvFilePaths: string list) (cancellationToken: CancellationToken) = task {
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
        let stopwatch = System.Diagnostics.Stopwatch.StartNew()
        
        let mutable totalBrokerMovements = 0
        let mutable totalTrades = 0
        
        for (index, csvFile) in csvFilePaths |> List.mapi (fun i file -> i, file) do
            cancellationToken.ThrowIfCancellationRequested()
            
            let fileName = System.IO.Path.GetFileName(csvFile)
            let progress = float index / float csvFilePaths.Length
            ImportState.updateStatus(ProcessingFile(fileName, progress))
            
            try
                if System.IO.File.Exists(csvFile) then
                    // Parse the file
                    let parseResult = parseCsvFile csvFile
                    
                    if parseResult.Success then
                        match parseResult.Data with
                        | Some statement ->
                            // Process statement and save records to database
                            let! actualRecordCount = processIBKRStatement statement
                            
                            // Count the types of records for reporting
                            let brokerMovementCount = statement.CashMovements.Length + statement.ForexTrades.Length
                            let tradeCount = statement.Trades |> List.filter isStockTrade |> List.length
                            
                            totalBrokerMovements <- totalBrokerMovements + brokerMovementCount
                            totalTrades <- totalTrades + tradeCount
                            
                            let fileResult = FileImportResult.createSuccess fileName actualRecordCount
                            fileResults <- fileResult :: fileResults
                            
                            totalResult <- { totalResult with 
                                               ProcessedRecords = totalResult.ProcessedRecords + actualRecordCount
                                               TotalRecords = totalResult.TotalRecords + actualRecordCount }
                        | None ->
                            let error = { RowNumber = None; ErrorMessage = "No data parsed from file"; ErrorType = ImportErrorType.ValidationError; RawData = None }
                            let fileResult = FileImportResult.createFailure fileName [error]
                            fileResults <- fileResult :: fileResults
                            
                            totalResult <- { totalResult with Success = false; Errors = error :: totalResult.Errors }
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
        
        stopwatch.Stop()
        
        // Final status update
        ImportState.updateStatus(ProcessingData(totalResult.ProcessedRecords, totalResult.TotalRecords))
        
        // Update ImportedData summary
        let importedDataSummary = {
            Trades = totalTrades
            BrokerMovements = totalBrokerMovements
            Dividends = 0
            OptionTrades = 0
            NewTickers = 0
        }
        
        return { totalResult with 
                   FileResults = List.rev fileResults
                   ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                   ImportedData = importedDataSummary }
    }
    
    /// <summary>
    /// Legacy import methods for backward compatibility (using basic parsing validation)
    /// These demonstrate the IBKR parsing capabilities without full model conversion
    /// </summary>
    
    /// <summary>
    /// Import multiple CSV files from IBKR with cancellation support (legacy implementation)
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
    /// Import single CSV file from IBKR (simplified legacy)
    /// </summary>
    /// <param name="csvFilePath">Path to CSV file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>FileImportResult for the single file</returns>
    let importSingleWithCancellationLegacy (csvFilePath: string) (cancellationToken: CancellationToken) = task {
        let! result = importMultipleWithCancellationLegacy [csvFilePath] cancellationToken
        return result.FileResults |> List.tryHead |> Option.defaultValue (FileImportResult.createFailure (System.IO.Path.GetFileName(csvFilePath)) [])
    }
    
    /// <summary>
    /// Import multiple CSV files from IBKR with cancellation support and database record creation
    /// This is the main import function that creates actual database records
    /// </summary>
    /// <param name="csvFilePaths">List of CSV file paths to process</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Consolidated ImportResult</returns>
    let importMultipleWithCancellation (csvFilePaths: string list) (cancellationToken: CancellationToken) = 
        importMultipleWithDatabase csvFilePaths cancellationToken
    
    /// <summary>
    /// Import single CSV file from IBKR with database record creation
    /// </summary>
    /// <param name="csvFilePath">Path to CSV file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>FileImportResult for the single file</returns>
    let importSingleWithCancellation (csvFilePath: string) (cancellationToken: CancellationToken) = task {
        let! result = importMultipleWithDatabase [csvFilePath] cancellationToken
        return result.FileResults |> List.tryHead |> Option.defaultValue (FileImportResult.createFailure (System.IO.Path.GetFileName(csvFilePath)) [])
    }