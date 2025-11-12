namespace Binnaculum.Core.Import

open System.Threading

/// <summary>
/// Tastytrade-specific import logic for processing CSV files with cancellation support
/// </summary>
module TastytradeImporter =

    /// <summary>
    /// Import multiple CSV files from Tastytrade with cancellation support
    /// </summary>
    /// <param name="csvFilePaths">List of CSV file paths to process</param>
    /// <param name="brokerAccountId">ID of the broker account to import for</param>
    /// <param name="cancellationToken">Cancellation token for operation</param>
    /// <returns>Consolidated ImportResult</returns>
    let importMultipleWithCancellation
        (csvFilePaths: string list)
        (brokerAccountId: int)
        (cancellationToken: CancellationToken)
        =
        task {
            let stopwatch = System.Diagnostics.Stopwatch.StartNew()

            let mutable totalResult =
                { Success = true
                  ProcessedFiles = csvFilePaths.Length
                  ProcessedRecords = 0
                  SkippedRecords = 0
                  TotalRecords = 0
                  ProcessingTimeMs = 0L
                  Errors = []
                  Warnings = []
                  ImportedData =
                    { Trades = 0
                      BrokerMovements = 0
                      Dividends = 0
                      OptionTrades = 0
                      NewTickers = 0 }
                  FileResults = [] }

            let mutable fileResults = []
            let mutable totalBrokerMovements = 0
            let mutable totalOptionTrades = 0
            let mutable totalStockTrades = 0

            for (index, csvFile) in csvFilePaths |> List.mapi (fun i file -> i, file) do
                cancellationToken.ThrowIfCancellationRequested()

                let fileName = System.IO.Path.GetFileName(csvFile)
                let progress = float index / float csvFilePaths.Length
                ImportState.updateStatus (ProcessingFile(fileName, progress))

                // Use Tastytrade-specific CSV parsing
                try
                    if System.IO.File.Exists(csvFile) then
                        let parsingResult =
                            TastytradeStatementParser.parseTransactionHistoryFromFile csvFile

                        if parsingResult.Errors.IsEmpty then
                            // Convert parsed transactions to counts by type
                            let conversionStats =
                                TastytradeTransactionConverter.convertTransactions parsingResult.Transactions

                            // Create file result based on conversion success
                            if conversionStats.ErrorsCount = 0 then
                                let fileResult =
                                    FileImportResult.createSuccess fileName parsingResult.ProcessedLines

                                fileResults <- fileResult :: fileResults

                                // Update totals with actual created records
                                totalBrokerMovements <- totalBrokerMovements + conversionStats.BrokerMovementsCreated
                                totalOptionTrades <- totalOptionTrades + conversionStats.OptionTradesCreated
                                totalStockTrades <- totalStockTrades + conversionStats.StockTradesCreated

                                totalResult <-
                                    { totalResult with
                                        ProcessedRecords = totalResult.ProcessedRecords + parsingResult.ProcessedLines
                                        TotalRecords = totalResult.TotalRecords + parsingResult.ProcessedLines }
                            else
                                // Convert conversion errors to import errors
                                let conversionErrors =
                                    conversionStats.Errors
                                    |> List.map (fun errorMsg ->
                                        { RowNumber = None
                                          ErrorMessage = errorMsg
                                          ErrorType = ValidationError
                                          RawData = None
                                          FromFile = fileName })

                                let fileResult = FileImportResult.createFailure fileName conversionErrors
                                fileResults <- fileResult :: fileResults

                                totalResult <-
                                    { totalResult with
                                        Success = false
                                        Errors = totalResult.Errors @ conversionErrors
                                        SkippedRecords = totalResult.SkippedRecords + conversionStats.ErrorsCount }
                        else
                            let importErrors =
                                parsingResult.Errors
                                |> List.map (fun parseError ->
                                    { RowNumber = Some parseError.LineNumber
                                      ErrorMessage = parseError.ErrorMessage
                                      ErrorType = ValidationError
                                      RawData = Some parseError.RawCsvLine
                                      FromFile = fileName })

                            let fileResult = FileImportResult.createFailure fileName importErrors
                            fileResults <- fileResult :: fileResults

                            totalResult <-
                                { totalResult with
                                    Success = false
                                    Errors = totalResult.Errors @ importErrors
                                    SkippedRecords = totalResult.SkippedRecords + parsingResult.Errors.Length }
                    else
                        let errorMsg = sprintf "File not found: %s" fileName

                        let error =
                            { RowNumber = None
                              ErrorMessage = errorMsg
                              ErrorType = ValidationError
                              RawData = None
                              FromFile = fileName }

                        let fileResult = FileImportResult.createFailure fileName [ error ]
                        fileResults <- fileResult :: fileResults

                        totalResult <-
                            { totalResult with
                                Success = false
                                Errors = error :: totalResult.Errors }
                with ex ->
                    let errorMsg = sprintf "Error processing %s: %s" fileName ex.Message

                    let error =
                        { RowNumber = None
                          ErrorMessage = errorMsg
                          ErrorType = ValidationError
                          RawData = None
                          FromFile = fileName }

                    let fileResult = FileImportResult.createFailure fileName [ error ]
                    fileResults <- fileResult :: fileResults

                    totalResult <-
                        { totalResult with
                            Success = false
                            Errors = error :: totalResult.Errors }

            stopwatch.Stop()

            // Final status update with actual imported data counts
            let importedData =
                { Trades = totalStockTrades
                  BrokerMovements = totalBrokerMovements
                  Dividends = 0
                  OptionTrades = totalOptionTrades
                  NewTickers = 0 // TODO: Track new tickers created during import
                }

            ImportState.updateStatus (ProcessingData(totalResult.ProcessedRecords, totalResult.TotalRecords))

            return
                { totalResult with
                    FileResults = List.rev fileResults
                    ImportedData = importedData
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds }
        }

    /// <summary>
    /// Import single CSV file from Tastytrade
    /// </summary>
    /// <param name="csvFilePath">Path to CSV file</param>
    /// <param name="brokerAccountId">ID of the broker account to import for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>FileImportResult for the single file</returns>
    let importSingleWithCancellation
        (csvFilePath: string)
        (brokerAccountId: int)
        (cancellationToken: CancellationToken)
        =
        task {
            let! result = importMultipleWithCancellation [ csvFilePath ] brokerAccountId cancellationToken

            return
                result.FileResults
                |> List.tryHead
                |> Option.defaultValue (FileImportResult.createFailure (System.IO.Path.GetFileName(csvFilePath)) [])
        }
