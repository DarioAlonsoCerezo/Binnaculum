namespace Binnaculum.Core.Import

open System
open System.Reactive.Subjects
open System.Threading

/// <summary>
/// Import state management with BehaviorSubject for UI connectivity and cancellation support
/// </summary>
module ImportState =

    /// Current import status - F# discriminated union for internal F# use
    let ImportStatus = new BehaviorSubject<ImportStatus>(NotStarted)
    
    /// Current import status - C#-friendly record for UI consumption
    /// Subscribe to this from C# code for clean switch statements
    let CurrentStatus = new BehaviorSubject<CurrentImportStatus>(
        CurrentImportStatus.fromImportStatus NotStarted)

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
            let cancelledStatus = Cancelled reason
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
        let completedStatus = Completed result
        ImportStatus.OnNext(completedStatus)
        CurrentStatus.OnNext(CurrentImportStatus.fromImportStatus completedStatus)
        cleanupCancellation ()

    /// Fail import and clean up
    let failImport (error: string) =
        let failedStatus = Failed error
        ImportStatus.OnNext(failedStatus)
        CurrentStatus.OnNext(CurrentImportStatus.fromImportStatus failedStatus)
        cleanupCancellation ()

    /// Background cancellation (app backgrounded, memory pressure, etc.)
    let cancelForBackground () =
        cancelImport ("App moved to background")

    /// Force cleanup on disposal
    let cleanup () =
        cancelImport ("System cleanup")
        cleanupCancellation ()
        ImportStatus.OnNext(NotStarted)
        CurrentStatus.OnNext(CurrentImportStatus.fromImportStatus NotStarted)

    /// Get current cancellation token if available
    let getCurrentCancellationToken () =
        match _cancellationSource.Value with
        | Some source -> Some source.Token
        | None -> None

    /// Check if an import is currently in progress (to defer reactive updates during import)
    let isImportInProgress () =
        match ImportStatus.Value with
        | NotStarted
        | Completed _
        | Failed _
        | Cancelled _ -> false
        | Validating _
        | ProcessingFile _
        | ProcessingData _
        | SavingToDatabase _ -> true

// ==================== NEW REACTIVE IMPORT TYPES ====================
// These types are for the new chunked import system with enhanced progress tracking

/// <summary>
/// Represents the different phases of processing within a single chunk.
/// Each chunk goes through these phases sequentially.
/// </summary>
type ChunkPhase =
    | LoadingMovements
    | CalculatingBrokerSnapshots
    | CalculatingTickerSnapshots
    | CreatingOperations
    | PersistingData

/// <summary>
/// Progress information for the current chunk being processed.
/// Provides detailed tracking of chunk-level operations.
/// </summary>
type ChunkProgress =
    { ChunkNumber: int
      TotalChunks: int
      StartDate: DateOnly
      EndDate: DateOnly
      EstimatedMovements: int
      CurrentPhase: ChunkPhase
      Progress: decimal } // 0.0 to 100.0 within current chunk

/// <summary>
/// Progress information for snapshot calculation operations.
/// Used for fine-grained progress within calculation phases.
/// </summary>
type SnapshotProgress =
    { SnapshotType: string // "Broker Financial" | "Ticker Currency" | "Operations"
      Processed: int
      Total: int
      Progress: decimal } // 0.0 to 100.0

/// <summary>
/// Final import summary with complete statistics.
/// Shown to user when import completes successfully.
/// </summary>
type ImportSummary =
    { TotalMovements: int
      TotalChunks: int
      BrokerSnapshots: int
      TickerSnapshots: int
      Operations: int
      Duration: TimeSpan
      StartTime: DateTime
      EndTime: DateTime }

/// <summary>
/// Import processing state machine for new chunked import system.
/// Tracks the current state of an import operation from start to finish.
/// UI can subscribe to state changes for real-time progress updates.
/// </summary>
type ChunkedImportState =
    | Idle // No import in progress
    | ReadingFile of fileName: string // Reading CSV file
    | AnalyzingDates of fileName: string * progress: decimal // Parsing dates from CSV
    | ProcessingChunk of chunkInfo: ChunkProgress // Processing a weekly chunk
    | CalculatingSnapshots of snapshotInfo: SnapshotProgress // Detailed snapshot progress
    | Completed of summary: ImportSummary // Import completed successfully
    | Failed of error: string // Import failed with error
    | Cancelled // User cancelled import

/// <summary>
/// Complete importing data with reactive updates for new chunked import system.
/// Exposed to UI via BehaviorSubject for real-time progress tracking.
/// </summary>
type ChunkedImportingData =
    { BrokerAccountId: int
      BrokerAccountName: string
      FileName: string
      State: ChunkedImportState
      StartTime: DateTime
      EstimatedDuration: TimeSpan option // Estimated based on chunk count and movement volume
      CanCancel: bool }

/// <summary>
/// Helper module for calculating import progress percentages.
/// </summary>
module ImportProgressCalculator =

    /// <summary>
    /// Calculate overall progress percentage (0-100) based on current state.
    /// </summary>
    let calculateOverallProgress (state: ChunkedImportState) (totalChunks: int) : decimal =
        match state with
        | Idle -> 0.0m
        | ReadingFile _ -> 5.0m
        | AnalyzingDates(_, progress) -> 5.0m + (progress * 0.05m) // 5-10%
        | ProcessingChunk chunk ->
            // Each chunk gets equal portion of 80% (10% to 90%)
            let chunkBaseProgress =
                (decimal chunk.ChunkNumber - 1.0m) / decimal totalChunks * 80.0m

            let withinChunkProgress = chunk.Progress * 0.8m / decimal totalChunks
            10.0m + chunkBaseProgress + withinChunkProgress
        | CalculatingSnapshots snapshot ->
            // Fallback for detailed snapshot progress (rare, usually covered by chunk progress)
            90.0m + (snapshot.Progress * 0.05m)
        | Completed _ -> 100.0m
        | Failed _ -> 0.0m
        | Cancelled -> 0.0m

    /// <summary>
    /// Estimate total import duration based on movement count.
    /// Rough heuristic: 1000 movements ≈ 1 second on typical mobile device.
    /// </summary>
    let estimateDuration (totalMovements: int) : TimeSpan =
        // Conservative estimate for mobile devices
        let estimatedSeconds = float totalMovements / 1000.0
        TimeSpan.FromSeconds(estimatedSeconds)

    /// <summary>
    /// Calculate time remaining based on current progress.
    /// </summary>
    let calculateTimeRemaining (startTime: DateTime) (currentProgress: decimal) : TimeSpan option =
        if currentProgress <= 0.0m then
            None
        else
            let elapsed = DateTime.Now - startTime
            let totalEstimated = elapsed.TotalSeconds / (float currentProgress / 100.0)
            let remaining = totalEstimated - elapsed.TotalSeconds

            if remaining > 0.0 then
                Some(TimeSpan.FromSeconds(remaining))
            else
                None
