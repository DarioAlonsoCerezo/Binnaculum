namespace Binnaculum.Core.Import

open System
open System.Reactive.Subjects
open System.Threading
open System.Threading.Tasks
open Binnaculum.Core.Logging

/// <summary>
/// Reactive import manager with BehaviorSubject for UI connectivity.
/// Provides thread-safe import progress tracking and cancellation support.
/// UI can subscribe to ImportProgress for real-time updates.
/// </summary>
module ReactiveImportManager =

    /// <summary>
    /// Current import progress - exposed via BehaviorSubject for UI connectivity.
    /// UI subscribes to this to get real-time progress updates.
    /// None = no import in progress, Some = import active with current state.
    /// </summary>
    let ImportProgress = new BehaviorSubject<ChunkedImportingData option>(None)

    /// <summary>
    /// Current cancellation token source for active import.
    /// </summary>
    let mutable private currentCancellationSource: CancellationTokenSource option = None

    /// <summary>
    /// Update import state (thread-safe via BehaviorSubject).
    /// Called internally during import processing to notify UI of progress.
    /// </summary>
    let private updateState (newState: ChunkedImportState) =
        match ImportProgress.Value with
        | Some currentData -> ImportProgress.OnNext(Some { currentData with State = newState })
        | None -> ()

    /// <summary>
    /// Update import data with estimated duration.
    /// Called after date analysis to provide time estimate to UI.
    /// </summary>
    let private updateEstimatedDuration (duration: TimeSpan) =
        match ImportProgress.Value with
        | Some currentData ->
            ImportProgress.OnNext(
                Some
                    { currentData with
                        EstimatedDuration = Some duration }
            )
        | None -> ()

    /// <summary>
    /// Cancel current import operation.
    /// Safe to call even if no import is in progress.
    /// </summary>
    let cancelImport () =
        match currentCancellationSource with
        | Some cts ->
            try
                cts.Cancel()
                updateState Cancelled
                CoreLogger.logInfo "ReactiveImportManager" "Import cancelled by user"
            with ex ->
                CoreLogger.logError "ReactiveImportManager" $"Error cancelling import: {ex.Message}"
        | None -> ()

    /// <summary>
    /// Check if import is currently in progress.
    /// </summary>
    let isImportInProgress () =
        match ImportProgress.Value with
        | None -> false
        | Some data ->
            match data.State with
            | Idle
            | Completed _
            | Failed _
            | Cancelled -> false
            | _ -> true

    /// <summary>
    /// Start a new import operation.
    /// This is a placeholder - actual chunked import logic will be added later.
    /// For now, exposes the infrastructure for UI connectivity.
    /// </summary>
    let startImport (brokerAccountId: int) (brokerAccountName: string) (filePath: string) =
        task {
            try
                try
                    // Cancel any existing import
                    if isImportInProgress () then
                        cancelImport ()

                    // Create new cancellation token
                    let cts = new CancellationTokenSource()
                    currentCancellationSource <- Some cts

                    // Initialize import state
                    let fileName = System.IO.Path.GetFileName(filePath)

                    let initialState =
                        { BrokerAccountId = brokerAccountId
                          BrokerAccountName = brokerAccountName
                          FileName = fileName
                          State = Idle
                          StartTime = DateTime.Now
                          EstimatedDuration = None
                          CanCancel = true }

                    ImportProgress.OnNext(Some initialState)

                    CoreLogger.logInfof
                        "ReactiveImportManager"
                        "Import started for account %d (%s), file: %s"
                        brokerAccountId
                        brokerAccountName
                        fileName

                    // NOTE: Actual chunked import logic will be implemented later
                    // For now, this just sets up the reactive infrastructure

                    // Placeholder completion
                    do! Task.Delay(100)

                    updateState (
                        Completed
                            { TotalMovements = 0
                              TotalChunks = 0
                              BrokerSnapshots = 0
                              TickerSnapshots = 0
                              Operations = 0
                              Duration = TimeSpan.Zero
                              StartTime = DateTime.Now
                              EndTime = DateTime.Now }
                    )

                    // Clear state after delay
                    do! Task.Delay(5000)
                    ImportProgress.OnNext(None)

                with
                | :? OperationCanceledException ->
                    updateState Cancelled
                    CoreLogger.logInfo "ReactiveImportManager" "Import cancelled"
                    do! Task.Delay(3000)
                    ImportProgress.OnNext(None)
                | ex ->
                    let errorMsg = $"Import failed: {ex.Message}"
                    updateState (Failed errorMsg)
                    CoreLogger.logError "ReactiveImportManager" errorMsg
                    do! Task.Delay(5000)
                    ImportProgress.OnNext(None)
            finally
                currentCancellationSource <- None
        }

    /// <summary>
    /// Get current cancellation token for use in import operations.
    /// Returns None if no import is in progress.
    /// </summary>
    let getCurrentCancellationToken () : CancellationToken option =
        match currentCancellationSource with
        | Some cts -> Some cts.Token
        | None -> None

    /// <summary>
    /// Reset import state (cleanup).
    /// Should be called when disposing or when app needs to force-reset import state.
    /// </summary>
    let reset () =
        match currentCancellationSource with
        | Some cts ->
            try
                cts.Cancel()
                cts.Dispose()
            with _ ->
                ()
        | None -> ()

        currentCancellationSource <- None
        ImportProgress.OnNext(None)
        CoreLogger.logDebug "ReactiveImportManager" "Import state reset"
