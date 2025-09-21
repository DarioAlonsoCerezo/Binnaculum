namespace Binnaculum.Core.Import

open System.Threading.Tasks
open System.Threading
open System
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database

/// <summary>
/// Main import manager for file import operations with cancellation support
/// </summary>
module ImportManager =
    
    /// <summary>
    /// Process import with cancellation support and cleanup
    /// </summary>
    let private processImportWithCancellation (broker: Broker) (filePath: string) (cancellationToken: CancellationToken) = task {
        let stopwatch = System.Diagnostics.Stopwatch.StartNew()
        let mutable processedFile: ProcessedFile option = None
        
        try
            // Process file(s) - handles both CSV and ZIP
            let processed = FileProcessor.processFile filePath
            processedFile <- Some processed
            
            cancellationToken.ThrowIfCancellationRequested()
            
            // Route to appropriate broker importer
            let! result = 
                match broker.SupportedBroker with
                | SupportedBroker.IBKR -> 
                    IBKRImporter.importMultipleWithCancellation processed.CsvFiles cancellationToken
                | SupportedBroker.Tastytrade -> 
                    TastytradeImporter.importMultipleWithCancellation processed.CsvFiles cancellationToken
                | _ -> 
                    failwith $"Import not supported for broker: {broker.Name}"
            
            stopwatch.Stop()
            let finalResult = { result with ProcessingTimeMs = stopwatch.ElapsedMilliseconds }
            
            // Cleanup temporary files
            match processedFile with
            | Some pf -> FileProcessor.cleanup pf
            | None -> ()
            
            ImportState.completeImport(finalResult)
            return finalResult
            
        with
        | :? OperationCanceledException ->
            stopwatch.Stop()
            // Cleanup temporary files
            match processedFile with
            | Some pf -> FileProcessor.cleanup pf
            | None -> ()
            reraise()
        | ex ->
            stopwatch.Stop()
            // Cleanup temporary files
            match processedFile with
            | Some pf -> FileProcessor.cleanup pf
            | None -> ()
            ImportState.failImport(ex.Message)
            reraise()
    }
    
    /// <summary>
    /// Import file for a specific broker with cancellation and progress tracking
    /// </summary>
    /// <param name="brokerId">ID of the broker to import for</param>
    /// <param name="filePath">Path to the file to import (CSV or ZIP)</param>
    /// <returns>ImportResult with detailed feedback</returns>
    let importFile (brokerId: int) (filePath: string) = task {
        let cancellationToken = ImportState.startImport()
        
        try
            // Validate inputs
            ImportState.updateStatus(Validating filePath)
            
            if not (System.IO.File.Exists(filePath)) then
                failwith $"File not found: {filePath}"
            
            cancellationToken.ThrowIfCancellationRequested()
            
            // Validate broker
            let! broker = BrokerExtensions.Do.getById(brokerId)
            match broker with
            | None -> 
                failwith $"Broker with ID {brokerId} not found"
            | Some b when b.SupportedBroker = SupportedBroker.Unknown ->
                failwith $"Broker {b.Name} does not support file imports"
            | Some validBroker ->
                return! processImportWithCancellation validBroker filePath cancellationToken
        
        with
        | :? OperationCanceledException ->
            return ImportResult.createCancelled()
        | ex ->
            ImportState.failImport(ex.Message)
            return ImportResult.createError(ex.Message)
    }
    
    /// <summary>
    /// Cancel current import operation
    /// </summary>
    let cancelCurrentImport() =
        ImportState.cancelImport("User requested cancellation")
    
    /// <summary>
    /// Cancel for app backgrounding
    /// </summary>
    let cancelForBackground() =
        ImportState.cancelForBackground()
    
    /// <summary>
    /// Get current import status
    /// </summary>
    let getCurrentStatus() =
        ImportState.ImportStatus.Value
    
    /// <summary>
    /// Check if an import is currently in progress
    /// </summary>
    let isImportInProgress() =
        match ImportState.getCurrentCancellationToken() with
        | Some token -> not token.IsCancellationRequested
        | None -> false