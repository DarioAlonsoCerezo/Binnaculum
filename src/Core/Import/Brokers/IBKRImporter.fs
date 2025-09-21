namespace Binnaculum.Core.Import

open System.Threading
open System.Threading.Tasks

/// <summary>
/// IBKR-specific import logic for processing CSV files with cancellation support
/// </summary>
module IBKRImporter =
    
    /// <summary>
    /// Import multiple CSV files from IBKR with cancellation support
    /// </summary>
    /// <param name="csvFilePaths">List of CSV file paths to process</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Consolidated ImportResult</returns>
    let importMultipleWithCancellation (csvFilePaths: string list) (cancellationToken: CancellationToken) = task {
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
            
            // TODO: Implement IBKR-specific CSV parsing
            // For now, simulate processing with a basic file check
            try
                if System.IO.File.Exists(csvFile) then
                    let lines = System.IO.File.ReadAllLines(csvFile)
                    let recordCount = if lines.Length > 1 then lines.Length - 1 else 0 // Exclude header
                    
                    // Simulate processing delay
                    do! Task.Delay(100, cancellationToken)
                    
                    let fileResult = FileImportResult.createSuccess fileName recordCount
                    fileResults <- fileResult :: fileResults
                    
                    totalResult <- { totalResult with 
                                       ProcessedRecords = totalResult.ProcessedRecords + recordCount
                                       TotalRecords = totalResult.TotalRecords + recordCount }
                else
                    let error = { RowNumber = None; ErrorMessage = $"File not found: {fileName}"; ErrorType = ValidationError; RawData = None }
                    let fileResult = FileImportResult.createFailure fileName [error]
                    fileResults <- fileResult :: fileResults
                    
                    totalResult <- { totalResult with 
                                       Success = false
                                       Errors = error :: totalResult.Errors }
            with
            | ex ->
                let error = { RowNumber = None; ErrorMessage = $"Error processing {fileName}: {ex.Message}"; ErrorType = ValidationError; RawData = None }
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
    /// Import single CSV file from IBKR
    /// </summary>
    /// <param name="csvFilePath">Path to CSV file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>FileImportResult for the single file</returns>
    let importSingleWithCancellation (csvFilePath: string) (cancellationToken: CancellationToken) = task {
        let! result = importMultipleWithCancellation [csvFilePath] cancellationToken
        return result.FileResults |> List.tryHead |> Option.defaultValue (FileImportResult.createFailure (System.IO.Path.GetFileName(csvFilePath)) [])
    }