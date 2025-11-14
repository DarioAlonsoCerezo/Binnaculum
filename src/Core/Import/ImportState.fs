namespace Binnaculum.Core.Import

open System
open System.Reactive.Subjects
open System.Threading
open Binnaculum.Core

/// <summary>
/// Import state management with BehaviorSubject for UI connectivity and cancellation support
/// </summary>
module ImportState =

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
        newSource.Token

    /// Clean up cancellation resources
    let private cleanupCancellation () =
        match _cancellationSource.Value with
        | Some source -> source.Dispose()
        | None -> ()

        _cancellationSource.Value <- None

    /// Force cleanup on disposal
    let cleanup () = cleanupCancellation ()

    /// Get current cancellation token if available
    let getCurrentCancellationToken () =
        match _cancellationSource.Value with
        | Some source -> Some source.Token
        | None -> None

    // ==================== CHUNKED IMPORT STATE MANAGEMENT ====================

    /// Current chunked import status - C#-friendly for UI consumption
    /// Subscribe from C# like: ImportState.CurrentChunkedStatus.Subscribe(status => {...})
    let CurrentChunkedStatus =
        new BehaviorSubject<CurrentChunkedImportStatus>(
            CurrentChunkedImportStatus.fromChunkedState ChunkedImportState.Idle None
        )

    /// Check if an import is currently in progress (to defer reactive updates during import)
    let isImportInProgress () =
        match CurrentChunkedStatus.Value.State with
        | ChunkedImportStateEnum.Idle
        | ChunkedImportStateEnum.Completed
        | ChunkedImportStateEnum.Failed
        | ChunkedImportStateEnum.Cancelled -> false
        | ChunkedImportStateEnum.ReadingFile
        | ChunkedImportStateEnum.Validating
        | ChunkedImportStateEnum.ProcessingChunk
        | ChunkedImportStateEnum.CalculatingSnapshots
        | ChunkedImportStateEnum.ExtractingFile
        | ChunkedImportStateEnum.AnalyzingDates -> true
        | _ -> true // Unknown states treated as in-progress for safety

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
        // Cancel the token source
        match _cancellationSource.Value with
        | Some source -> source.Cancel()
        | None -> ()

        updateChunkedState (ChunkedImportState.Cancelled)
        _chunkedStartTime.Value <- None
