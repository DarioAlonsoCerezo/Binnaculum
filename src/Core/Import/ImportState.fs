namespace Binnaculum.Core.Import

open System
open System.Reactive.Subjects
open System.Threading
open Binnaculum.Core

/// <summary>
/// Import state management with BehaviorSubject for UI connectivity and cancellation support
/// </summary>
module ImportState =

    /// Current import status - F# discriminated union for internal F# use
    let ImportStatus = new BehaviorSubject<ImportStatus>(NotStarted)

    /// Current import status - C#-friendly record for UI consumption
    /// Subscribe to this from C# code for clean switch statements
    let CurrentStatus =
        new BehaviorSubject<CurrentImportStatus>(CurrentImportStatus.fromImportStatus NotStarted)

    /// Current cancellation token source
    let private _cancellationSource = ref (None: CancellationTokenSource option)

    /// Start new import operation and return cancellation token
    let startImport () =
        // Cancel any existing operation
        match _cancellationSource.Value with
        | Some existing ->
            existing.Cancel()
            existing.Dispose()
        | None -> ()

        let newSource = new CancellationTokenSource()
        _cancellationSource.Value <- Some newSource
        ImportStatus.OnNext(NotStarted)
        newSource.Token

    /// Cancel current import operation with reason
    let cancelImport (reason: string) =
        match _cancellationSource.Value with
        | Some source ->
            source.Cancel()
            let cancelledStatus = Binnaculum.Core.Import.ImportStatus.Cancelled reason
            ImportStatus.OnNext(cancelledStatus)
            CurrentStatus.OnNext(CurrentImportStatus.fromImportStatus cancelledStatus)
        | None -> ()

    /// Update import status (called by importers during processing)
    let updateStatus (status: ImportStatus) =
        ImportStatus.OnNext(status)
        CurrentStatus.OnNext(CurrentImportStatus.fromImportStatus status)

    /// Clean up cancellation resources
    let private cleanupCancellation () =
        match _cancellationSource.Value with
        | Some source -> source.Dispose()
        | None -> ()

        _cancellationSource.Value <- None

    /// Complete import and clean up
    let completeImport (result: ImportResult) =
        let completedStatus = Binnaculum.Core.Import.ImportStatus.Completed result
        ImportStatus.OnNext(completedStatus)
        CurrentStatus.OnNext(CurrentImportStatus.fromImportStatus completedStatus)
        cleanupCancellation ()

    /// Fail import and clean up
    let failImport (error: string) =
        let failedStatus = Binnaculum.Core.Import.ImportStatus.Failed error
        ImportStatus.OnNext(failedStatus)
        CurrentStatus.OnNext(CurrentImportStatus.fromImportStatus failedStatus)
        cleanupCancellation ()

    /// Background cancellation (app backgrounded, memory pressure, etc.)
    let cancelForBackground () =
        cancelImport (ResourceKeys.Import_Cancelled)

    /// Force cleanup on disposal
    let cleanup () =
        cancelImport (ResourceKeys.Import_Cancelled)
        cleanupCancellation ()
        ImportStatus.OnNext(NotStarted)
        CurrentStatus.OnNext(CurrentImportStatus.fromImportStatus NotStarted)

    /// Get current cancellation token if available
    let getCurrentCancellationToken () =
        match _cancellationSource.Value with
        | Some source -> Some source.Token
        | None -> None

    /// Clean up import resources without state emission
    /// Used when transitioning from legacy to chunked state system
    let cleanupImportResources () =
        match _cancellationSource.Value with
        | Some source ->
            source.Dispose()
            _cancellationSource.Value <- None
        | None -> ()

    /// Check if an import is currently in progress (to defer reactive updates during import)
    let isImportInProgress () =
        match ImportStatus.Value with
        | Binnaculum.Core.Import.ImportStatus.NotStarted
        | Binnaculum.Core.Import.ImportStatus.Completed _
        | Binnaculum.Core.Import.ImportStatus.Failed _
        | Binnaculum.Core.Import.ImportStatus.Cancelled _ -> false
        | Binnaculum.Core.Import.ImportStatus.Validating _
        | Binnaculum.Core.Import.ImportStatus.ProcessingFile _
        | Binnaculum.Core.Import.ImportStatus.ProcessingData _
        | Binnaculum.Core.Import.ImportStatus.SavingToDatabase _
        | Binnaculum.Core.Import.ImportStatus.CalculatingSnapshots _ -> true

    // ==================== CHUNKED IMPORT STATE MANAGEMENT ====================

    /// Current chunked import status - C#-friendly for UI consumption
    /// Subscribe from C# like: ImportState.CurrentChunkedStatus.Subscribe(status => {...})
    let CurrentChunkedStatus =
        new BehaviorSubject<CurrentChunkedImportStatus>(
            CurrentChunkedImportStatus.fromChunkedState ChunkedImportState.Idle None
        )

    /// Track start time for chunked imports (for duration/time remaining calculations)
    let private _chunkedStartTime = ref (None: DateTime option)

    /// Update chunked import state - emits to UI-friendly observable
    let updateChunkedState (state: ChunkedImportState) =
        let currentStatus =
            CurrentChunkedImportStatus.fromChunkedState state _chunkedStartTime.Value

        CurrentChunkedStatus.OnNext(currentStatus)

    /// Start chunked import operation - initializes timing
    let startChunkedImport () =
        _chunkedStartTime.Value <- Some DateTime.Now
    // Don't emit Idle - caller will emit ReadingFile immediately

    /// Complete chunked import operation - clears timing
    let completeChunkedImport (summary: ImportSummary) =
        updateChunkedState (ChunkedImportState.Completed summary)
        _chunkedStartTime.Value <- None

    /// Fail chunked import operation - clears timing
    let failChunkedImport (errorMessage: string) =
        updateChunkedState (ChunkedImportState.Failed errorMessage)
        _chunkedStartTime.Value <- None

    /// Cancel chunked import operation - clears timing
    let cancelChunkedImport () =
        updateChunkedState (ChunkedImportState.Cancelled)
        _chunkedStartTime.Value <- None
