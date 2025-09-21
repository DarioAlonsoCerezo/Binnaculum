namespace Binnaculum.Core.Import

open System.Threading
open System.Threading.Tasks

/// <summary>
/// Tastytrade-specific import logic for processing CSV files with cancellation support
/// </summary>
module TastytradeImporter =
    
    /// <summary>
    /// Import multiple CSV files from Tastytrade with cancellation support
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
            
            // Use Tastytrade-specific CSV parsing
            try
                if System.IO.File.Exists(csvFile) then
                    let parsingResult = TastytradeStatementParser.parseTransactionHistoryFromFile csvFile
                    
                    if parsingResult.Errors.IsEmpty then
                        let fileResult = FileImportResult.createSuccess fileName parsingResult.ProcessedLines
                        fileResults <- fileResult :: fileResults
                        
                        totalResult <- { totalResult with 
                                           ProcessedRecords = totalResult.ProcessedRecords + parsingResult.ProcessedLines
                                           TotalRecords = totalResult.TotalRecords + parsingResult.ProcessedLines }
                    else
                        let importErrors = 
                            parsingResult.Errors
                            |> List.map (fun parseError -> {
                                RowNumber = Some parseError.LineNumber
                                ErrorMessage = parseError.ErrorMessage
                                ErrorType = ValidationError
                                RawData = Some parseError.RawCsvLine
                            })
                        let fileResult = FileImportResult.createFailure fileName importErrors
                        fileResults <- fileResult :: fileResults
                        
                        totalResult <- { totalResult with 
                                           Success = false
                                           Errors = totalResult.Errors @ importErrors }
                else
                    let errorMsg = sprintf "File not found: %s" fileName
                    let error = { RowNumber = None; ErrorMessage = errorMsg; ErrorType = ValidationError; RawData = None }
                    let fileResult = FileImportResult.createFailure fileName [error]
                    fileResults <- fileResult :: fileResults
                    
                    totalResult <- { totalResult with 
                                       Success = false
                                       Errors = error :: totalResult.Errors }
            with
            | ex ->
                let errorMsg = sprintf "Error processing %s: %s" fileName ex.Message
                let error = { RowNumber = None; ErrorMessage = errorMsg; ErrorType = ValidationError; RawData = None }
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
    /// Import single CSV file from Tastytrade
    /// </summary>
    /// <param name="csvFilePath">Path to CSV file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>FileImportResult for the single file</returns>
    let importSingleWithCancellation (csvFilePath: string) (cancellationToken: CancellationToken) = task {
        let! result = importMultipleWithCancellation [csvFilePath] cancellationToken
        return result.FileResults |> List.tryHead |> Option.defaultValue (FileImportResult.createFailure (System.IO.Path.GetFileName(csvFilePath)) [])
    }