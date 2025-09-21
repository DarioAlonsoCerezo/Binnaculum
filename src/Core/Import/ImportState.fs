namespace Binnaculum.Core.Import

open System.Reactive.Subjects
open System.Threading

/// <summary>
/// Import state management with BehaviorSubject for UI connectivity and cancellation support
/// </summary>
module ImportState =
    
    /// Current import status - exposed via BehaviorSubject for UI connectivity
    let ImportStatus = new BehaviorSubject<ImportStatus>(NotStarted)
    
    /// Current cancellation token source
    let private _cancellationSource = ref (None: CancellationTokenSource option)
    
    /// Start new import operation and return cancellation token
    let startImport() =
        // Cancel any existing operation
        match !_cancellationSource with
        | Some existing -> 
            existing.Cancel()
            existing.Dispose()
        | None -> ()
        
        let newSource = new CancellationTokenSource()
        _cancellationSource := Some newSource
        ImportStatus.OnNext(NotStarted)
        newSource.Token
    
    /// Cancel current import operation with reason
    let cancelImport(reason: string) =
        match !_cancellationSource with
        | Some source -> 
            source.Cancel()
            ImportStatus.OnNext(Cancelled reason)
        | None -> ()
    
    /// Update import status (called by importers during processing)
    let updateStatus(status: ImportStatus) =
        ImportStatus.OnNext(status)
    
    /// Clean up cancellation resources
    let private cleanupCancellation() =
        match !_cancellationSource with
        | Some source -> source.Dispose()
        | None -> ()
        _cancellationSource := None
    
    /// Complete import and clean up
    let completeImport(result: ImportResult) =
        ImportStatus.OnNext(Completed result)
        cleanupCancellation()
    
    /// Fail import and clean up
    let failImport(error: string) =
        ImportStatus.OnNext(Failed error)
        cleanupCancellation()
    
    /// Background cancellation (app backgrounded, memory pressure, etc.)
    let cancelForBackground() =
        cancelImport("App moved to background")
    
    /// Force cleanup on disposal
    let cleanup() =
        cancelImport("System cleanup")
        cleanupCancellation()
        ImportStatus.OnNext(NotStarted)
    
    /// Get current cancellation token if available
    let getCurrentCancellationToken() =
        match !_cancellationSource with
        | Some source -> Some source.Token
        | None -> None