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
                return ImportResult.createError($"File not found: {filePath}")
            else
                // For now, return a simple success result - TODO: implement full logic
                let result = ImportResult.createSuccess 1 0 { Trades = 0; BrokerMovements = 0; Dividends = 0; OptionTrades = 0; NewTickers = 0 } [] 0L
                ImportState.completeImport(result)
                return result
        
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