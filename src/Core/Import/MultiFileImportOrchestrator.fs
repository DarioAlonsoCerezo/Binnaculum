namespace Binnaculum.Core.Import

open System.Threading
open System.Threading.Tasks
open Binnaculum.Core.Logging

/// <summary>
/// Orchestrates multi-file imports with analysis, sorting, and progress tracking
/// </summary>
module MultiFileImportOrchestrator =
    
    /// <summary>
    /// Import multiple CSV files with automatic chronological sorting and progress tracking
    /// This is the main entry point for ZIP imports
    /// </summary>
    /// <param name="csvFiles">Unsorted list of CSV files from ZIP extraction</param>
    /// <param name="brokerImportFunc">Broker-specific import function (IBKRImporter or TastytradeImporter)</param>
    /// <param name="cancellationToken">Cancellation token for abort support</param>
    /// <returns>ImportResult with detailed feedback</returns>
    let importWithProgressTracking 
        (csvFiles: string list)
        (brokerImportFunc: string list -> CancellationToken -> Task<ImportResult>)
        (cancellationToken: CancellationToken)
        : Task<ImportResult> =
        
        task {
            try
                // Step 1: Analyze and sort files by date
                CoreLogger.logInfof "MultiFileImport" "Analyzing %d CSV files..." csvFiles.Length
                
                let analysis = CsvDateAnalyzer.analyzeAndSort csvFiles
                let sortedFilePaths = analysis.FilesOrderedByDate |> List.map (fun m -> m.FilePath)
                
                // Step 2: Log import plan
                match analysis.OverallDateRange with
                | Some (earliest, latest) ->
                    CoreLogger.logInfof "MultiFileImport" 
                        "Files sorted chronologically: %s to %s (%d files, %d total records)"
                        (earliest.ToString("yyyy-MM-dd"))
                        (latest.ToString("yyyy-MM-dd"))
                        analysis.TotalFiles
                        analysis.TotalRecords
                | None ->
                    CoreLogger.logWarning "MultiFileImport" "Could not determine date range for import"
                
                // Step 3: Log warnings (gaps, overlaps)
                analysis.Warnings |> List.iter (fun warning ->
                    CoreLogger.logWarningf "MultiFileImport" "Import Warning: %s" warning
                )
                
                if analysis.DateGaps.Length > 0 then
                    CoreLogger.logInfo "MultiFileImport" "Date gaps detected:"
                    analysis.DateGaps |> List.iter (fun (startDate, endDate, daysMissing) ->
                        CoreLogger.logInfof "MultiFileImport" 
                            "  Gap: %s to %s (%d days missing)"
                            (startDate.ToString("yyyy-MM-dd"))
                            (endDate.ToString("yyyy-MM-dd"))
                            daysMissing
                    )
                
                if analysis.DateOverlaps.Length > 0 then
                    CoreLogger.logInfo "MultiFileImport" "Date overlaps detected:"
                    analysis.DateOverlaps |> List.iter (fun (file1, file2, overlapDate) ->
                        CoreLogger.logInfof "MultiFileImport" 
                            "  Overlap: %s and %s both contain %s"
                            file1
                            file2
                            (overlapDate.ToString("yyyy-MM-dd"))
                    )
                
                // Step 4: Initialize progress tracking
                ImportProgressTracker.startTracking sortedFilePaths.Length
                
                // Step 5: Execute import with sorted files
                // The broker importer already handles multi-file processing
                // It will call ImportState.updateStatus for each file
                let! result = brokerImportFunc sortedFilePaths cancellationToken
                
                // Step 6: Complete progress tracking
                ImportProgressTracker.completeTracking()
                
                CoreLogger.logInfof "MultiFileImport" 
                    "Import completed: %d files processed, %d records imported"
                    result.ProcessedFiles
                    result.ProcessedRecords
                
                return result
                
            with ex ->
                ImportProgressTracker.completeTracking()
                CoreLogger.logErrorf "MultiFileImport" "Import failed: %s" ex.Message
                return ImportResult.createError ex.Message
        }
