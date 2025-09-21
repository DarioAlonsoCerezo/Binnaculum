namespace Binnaculum.Core.Import

open System
open System.Threading
open System.Threading.Tasks
open Binnaculum.Core
open IBKRModels
open IBKRStatementParser
// Temporarily disabled modules for compilation
// open IBKRDataConverter
// open IBKRCashFlowProcessor
// open IBKRForexProcessor

/// <summary>
/// IBKR-specific import logic for processing CSV files with comprehensive parsing
/// Integrates all IBKR parsing components for complete statement processing
/// </summary>
module IBKRImporter =
    
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
    /// Import multiple CSV files from IBKR with cancellation support (simplified wrapper)
    /// </summary>
    /// <param name="csvFilePaths">List of CSV file paths to process</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Consolidated ImportResult</returns>
    let importMultipleWithCancellation (csvFilePaths: string list) (cancellationToken: CancellationToken) = 
        importMultipleWithCancellationLegacy csvFilePaths cancellationToken
    
    /// <summary>
    /// Import single CSV file from IBKR
    /// </summary>
    /// <param name="csvFilePath">Path to CSV file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>FileImportResult for the single file</returns>
    let importSingleWithCancellation (csvFilePath: string) (cancellationToken: CancellationToken) = task {
        let! result = importMultipleWithCancellationLegacy [csvFilePath] cancellationToken
        return result.FileResults |> List.tryHead |> Option.defaultValue (FileImportResult.createFailure (System.IO.Path.GetFileName(csvFilePath)) [])
    }