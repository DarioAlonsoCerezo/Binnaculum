namespace Binnaculum.Core.Import

/// <summary>
/// Import status tracking for real-time progress updates via BehaviorSubject
/// </summary>
type ImportStatus =
    | NotStarted
    | Validating of filePath: string
    | ProcessingFile of fileName: string * progress: float
    | ProcessingData of recordsProcessed: int * totalRecords: int
    | SavingToDatabase of message: string * progress: float * recordsProcessed: int * totalRecords: int
    | CalculatingSnapshots of recordsProcessed: int * totalRecords: int * processedDate: string
    | Completed of result: ImportResult
    | Cancelled of reason: string
    | Failed of error: string

/// <summary>
/// State enum for C# interop - represents mutually exclusive import states
/// Exposed as simple enum for clean switch statements in C#
/// </summary>
and ImportStateEnum =
    | NotStarted = 0
    | Validating = 1
    | ProcessingFile = 2
    | ProcessingData = 3
    | SavingToDatabase = 4
    | CalculatingSnapshots = 5
    | Completed = 6
    | Cancelled = 7
    | Failed = 8

/// <summary>
/// State enum for chunked import C# interop - represents mutually exclusive states
/// Follows same pattern as ImportStateEnum
/// </summary>
and ChunkedImportStateEnum =
    | Idle = 0
    | Validating = 1
    | ExtractingFile = 2
    | ReadingFile = 3
    | AnalyzingDates = 4
    | ProcessingChunk = 5
    | CalculatingSnapshots = 6
    | Completed = 7
    | Failed = 8
    | Cancelled = 9

/// <summary>
/// C#-friendly import status for UI consumption via BehaviorSubject.
/// Flattens F# discriminated union into record with optional properties.
/// State field determines which optional properties are populated.
/// </summary>
and CurrentImportStatus =
    {
        /// Current state of import operation (determines which properties are populated)
        State: ImportStateEnum

        /// File path being validated (populated when State = Validating)
        FilePath: string option

        /// File name being processed (populated when State = ProcessingFile)
        FileName: string option

        /// Progress percentage 0.0-1.0 (populated for ProcessingFile, SavingToDatabase)
        Progress: float option

        /// Number of records processed (populated when State = ProcessingData, CalculatingSnapshots)
        RecordsProcessed: int option

        /// Total records to process (populated when State = ProcessingData, CalculatingSnapshots)
        TotalRecords: int option

        /// Processed date in YYYY-MM-dd format (populated when State = CalculatingSnapshots)
        ProcessedDate: string option

        /// Status message (populated for SavingToDatabase, Cancelled)
        Message: string option

        /// Import result (populated when State = Completed)
        Result: ImportResult option

        /// Error message (populated when State = Failed)
        Error: string option
    }

/// <summary>
/// C#-friendly chunked import status for UI consumption via BehaviorSubject.
/// Flattens F# discriminated union into record with optional properties.
/// State field determines which optional properties are populated.
/// Follows same pattern as CurrentImportStatus.
/// </summary>
and CurrentChunkedImportStatus =
    {
        /// Current state of chunked import operation (determines which properties are populated)
        State: ChunkedImportStateEnum

        /// File name being read/analyzed (populated when State = ReadingFile, AnalyzingDates)
        FileName: string option

        /// Analysis progress 0-100 (populated when State = AnalyzingDates)
        AnalysisProgress: decimal option

        /// Current chunk number (populated when State = ProcessingChunk, CalculatingSnapshots)
        ChunkNumber: int option

        /// Total number of chunks (populated when State = ProcessingChunk, CalculatingSnapshots)
        TotalChunks: int option

        /// Chunk start date formatted string (populated when State = ProcessingChunk)
        ChunkStartDate: string option

        /// Chunk end date formatted string (populated when State = ProcessingChunk)
        ChunkEndDate: string option

        /// Estimated movements in current chunk (populated when State = ProcessingChunk)
        EstimatedMovements: int option

        /// Current phase name - raw enum string (populated when State = ProcessingChunk)
        /// Localized in UI extension methods
        CurrentPhase: string option

        /// Progress within current chunk 0-100 (populated when State = ProcessingChunk)
        ChunkProgress: decimal option

        /// Snapshot type being calculated - raw string (populated when State = CalculatingSnapshots)
        /// Localized in UI extension methods
        SnapshotType: string option

        /// Number of snapshots processed (populated when State = CalculatingSnapshots)
        SnapshotsProcessed: int option

        /// Total snapshots to process (populated when State = CalculatingSnapshots)
        SnapshotsTotal: int option

        /// Snapshot calculation progress 0-100 (populated when State = CalculatingSnapshots)
        SnapshotProgress: decimal option

        /// Overall progress percentage 0-100 (always populated during processing)
        OverallProgress: decimal

        /// Import start time (populated when processing starts)
        StartTime: System.DateTime option

        /// Elapsed time since start (populated during processing)
        ElapsedTime: System.TimeSpan option

        /// Estimated time remaining (populated during processing when calculable)
        EstimatedTimeRemaining: System.TimeSpan option

        /// Total movements imported (populated when State = Completed)
        TotalMovements: int option

        /// Total broker snapshots calculated (populated when State = Completed)
        TotalBrokerSnapshots: int option

        /// Total ticker snapshots calculated (populated when State = Completed)
        TotalTickerSnapshots: int option

        /// Total operations created (populated when State = Completed)
        TotalOperations: int option

        /// Total duration (populated when State = Completed)
        Duration: System.TimeSpan option

        /// Whether import can be cancelled (false when Completed, Failed, Cancelled)
        CanCancel: bool

        /// Error message (populated when State = Failed)
        ErrorMessage: string option
    }

/// <summary>
/// Comprehensive import result with detailed feedback for all processed files
/// </summary>
and ImportResult =
    {
        Success: bool
        ProcessedFiles: int
        ProcessedRecords: int
        SkippedRecords: int
        TotalRecords: int
        ProcessingTimeMs: int64
        Errors: ImportError list
        Warnings: ImportWarning list
        ImportedData: ImportedDataSummary
        FileResults: FileImportResult list
        /// Number of chunks processed (for chunked imports)
        ProcessedChunks: int
        /// Session ID for resumable imports
        SessionId: int option
    }

/// <summary>
/// Structured error information for data validation issues
/// </summary>
and ImportError =
    { RowNumber: int option
      ErrorMessage: string
      ErrorType: ImportErrorType
      RawData: string option
      FromFile: string }

/// <summary>
/// Warning information for non-critical issues during import
/// </summary>
and ImportWarning =
    { RowNumber: int option
      WarningMessage: string
      WarningType: ImportWarningType
      RawData: string option }

/// <summary>
/// Classification of import errors for better error handling
/// </summary>
and ImportErrorType =
    | InvalidDataFormat
    | MissingRequiredField
    | InvalidDate
    | InvalidAmount
    | DuplicateRecord
    | UnknownTicker
    | ValidationError

/// <summary>
/// Classification of import warnings
/// </summary>
and ImportWarningType =
    | DataFormatWarning
    | MissingOptionalField
    | TickerNotFound
    | DateAdjustment

/// <summary>
/// Summary of imported data by type
/// </summary>
and ImportedDataSummary =
    { Trades: int
      BrokerMovements: int
      Dividends: int
      OptionTrades: int
      NewTickers: int }

/// <summary>
/// Metadata collected during import for targeted reactive updates
/// Contains information about what data changed and needs snapshot updates
/// </summary>
and ImportMetadata =
    {
        /// Oldest movement date from all imported transactions (for range-based updates)
        OldestMovementDate: System.DateTime option
        /// Set of broker account IDs that were affected by the import
        AffectedBrokerAccountIds: Set<int>
        /// Set of ticker symbols that were created or had trades/dividends imported
        AffectedTickerSymbols: Set<string>
        /// Total number of movements imported (for performance tracking)
        TotalMovementsImported: int
    }

/// <summary>
/// Individual file import result for multi-file processing
/// </summary>
and FileImportResult =
    { FileName: string
      Success: bool
      ProcessedRecords: int
      Errors: ImportError list
      Warnings: ImportWarning list }

/// Helper functions for creating import results
module ImportResult =

    /// Create a successful import result
    let createSuccess
        (processedFiles: int)
        (processedRecords: int)
        (importedData: ImportedDataSummary)
        (fileResults: FileImportResult list)
        (processingTimeMs: int64)
        =
        { Success = true
          ProcessedFiles = processedFiles
          ProcessedRecords = processedRecords
          SkippedRecords = 0
          TotalRecords = processedRecords
          ProcessingTimeMs = processingTimeMs
          Errors = []
          Warnings = []
          ImportedData = importedData
          FileResults = fileResults
          ProcessedChunks = 0
          SessionId = None }

    /// Create a cancelled import result
    let createCancelled () =
        { Success = false
          ProcessedFiles = 0
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
          FileResults = []
          ProcessedChunks = 0
          SessionId = None }

    /// Create an error import result
    let createError (errorMessage: string) =
        { Success = false
          ProcessedFiles = 0
          ProcessedRecords = 0
          SkippedRecords = 0
          TotalRecords = 0
          ProcessingTimeMs = 0L
          Errors =
            [ { RowNumber = None
                ErrorMessage = errorMessage
                ErrorType = ValidationError
                RawData = None
                FromFile = "" } ]
          Warnings = []
          ImportedData =
            { Trades = 0
              BrokerMovements = 0
              Dividends = 0
              OptionTrades = 0
              NewTickers = 0 }
          FileResults = []
          ProcessedChunks = 0
          SessionId = None }

/// Helper functions for creating file import results
module FileImportResult =

    /// Create a successful file import result
    let createSuccess (fileName: string) (processedRecords: int) =
        { FileName = fileName
          Success = true
          ProcessedRecords = processedRecords
          Errors = []
          Warnings = [] }

    /// Create a failed file import result
    let createFailure (fileName: string) (errors: ImportError list) =
        { FileName = fileName
          Success = false
          ProcessedRecords = 0
          Errors = errors
          Warnings = [] }

/// Helper functions for working with import metadata
module ImportMetadata =

    /// Create empty import metadata
    let createEmpty () =
        { OldestMovementDate = None
          AffectedBrokerAccountIds = Set.empty
          AffectedTickerSymbols = Set.empty
          TotalMovementsImported = 0 }

    /// Combine two ImportMetadata instances (useful for multi-file imports)
    let combine (metadata1: ImportMetadata) (metadata2: ImportMetadata) =
        { OldestMovementDate =
            match metadata1.OldestMovementDate, metadata2.OldestMovementDate with
            | None, None -> None
            | Some date, None
            | None, Some date -> Some date
            | Some date1, Some date2 -> Some(if date1 < date2 then date1 else date2)
          AffectedBrokerAccountIds = Set.union metadata1.AffectedBrokerAccountIds metadata2.AffectedBrokerAccountIds
          AffectedTickerSymbols = Set.union metadata1.AffectedTickerSymbols metadata2.AffectedTickerSymbols
          TotalMovementsImported = metadata1.TotalMovementsImported + metadata2.TotalMovementsImported }

/// <summary>
/// Helper functions for converting between F# DU and C#-friendly record
/// </summary>
module CurrentImportStatus =

    /// <summary>
    /// Convert F# discriminated union to C#-friendly record.
    /// Called automatically when pushing status updates to BehaviorSubject.
    /// </summary>
    let fromImportStatus (status: ImportStatus) : CurrentImportStatus =
        match status with
        | NotStarted ->
            { State = ImportStateEnum.NotStarted
              FilePath = None
              FileName = None
              Progress = None
              RecordsProcessed = None
              TotalRecords = None
              ProcessedDate = None
              Message = None
              Result = None
              Error = None }

        | Validating filePath ->
            { State = ImportStateEnum.Validating
              FilePath = Some filePath
              FileName = None
              Progress = None
              RecordsProcessed = None
              TotalRecords = None
              ProcessedDate = None
              Message = None
              Result = None
              Error = None }

        | ProcessingFile(fileName, progress) ->
            { State = ImportStateEnum.ProcessingFile
              FilePath = None
              FileName = Some fileName
              Progress = Some progress
              RecordsProcessed = None
              TotalRecords = None
              ProcessedDate = None
              Message = None
              Result = None
              Error = None }

        | ProcessingData(processed, total) ->
            { State = ImportStateEnum.ProcessingData
              FilePath = None
              FileName = None
              Progress = None
              RecordsProcessed = Some processed
              TotalRecords = Some total
              ProcessedDate = None
              Message = None
              Result = None
              Error = None }

        | SavingToDatabase(msg, progress, processed, total) ->
            { State = ImportStateEnum.SavingToDatabase
              FilePath = None
              FileName = None
              Progress = Some progress
              RecordsProcessed = Some processed
              TotalRecords = Some total
              ProcessedDate = None
              Message = Some msg
              Result = None
              Error = None }

        | CalculatingSnapshots(processed, total, processedDate) ->
            { State = ImportStateEnum.CalculatingSnapshots
              FilePath = None
              FileName = None
              Progress = None
              RecordsProcessed = Some processed
              TotalRecords = Some total
              ProcessedDate = Some processedDate
              Message = None
              Result = None
              Error = None }

        | Completed result ->
            { State = ImportStateEnum.Completed
              FilePath = None
              FileName = None
              Progress = None
              RecordsProcessed = None
              TotalRecords = None
              ProcessedDate = None
              Message = None
              Result = Some result
              Error = None }

        | Cancelled reason ->
            { State = ImportStateEnum.Cancelled
              FilePath = None
              FileName = None
              Progress = None
              RecordsProcessed = None
              TotalRecords = None
              ProcessedDate = None
              Message = Some reason
              Result = None
              Error = None }

        | Failed error ->
            { State = ImportStateEnum.Failed
              FilePath = None
              FileName = None
              Progress = None
              RecordsProcessed = None
              TotalRecords = None
              ProcessedDate = None
              Message = None
              Result = None
              Error = Some error }

// ==================== CHUNKED IMPORT TYPES ====================
// Types for the new chunked import system with enhanced progress tracking

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
and ChunkProgress =
    { ChunkNumber: int
      TotalChunks: int
      StartDate: System.DateOnly
      EndDate: System.DateOnly
      EstimatedMovements: int
      CurrentPhase: ChunkPhase
      Progress: decimal } // 0.0 to 100.0 within current chunk

/// <summary>
/// Progress information for snapshot calculation operations.
/// Used for fine-grained progress within calculation phases.
/// </summary>
and SnapshotProgress =
    { SnapshotType: string // "Broker Financial" | "Ticker Currency" | "Operations"
      Processed: int
      Total: int
      Progress: decimal } // 0.0 to 100.0

/// <summary>
/// Final import summary with complete statistics.
/// Shown to user when import completes successfully.
/// </summary>
and ImportSummary =
    { TotalMovements: int
      TotalChunks: int
      BrokerSnapshots: int
      TickerSnapshots: int
      Operations: int
      Duration: System.TimeSpan
      StartTime: System.DateTime
      EndTime: System.DateTime }

/// <summary>
/// Import processing state machine for new chunked import system.
/// Tracks the current state of an import operation from start to finish.
/// UI can subscribe to state changes for real-time progress updates.
/// </summary>
and ChunkedImportState =
    | Idle // No import in progress
    | Validating of fileName: string // Validating file before import
    | ExtractingFile of fileName: string * progress: decimal // Extracting ZIP/processing file
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
and ChunkedImportingData =
    { BrokerAccountId: int
      BrokerAccountName: string
      FileName: string
      State: ChunkedImportState
      StartTime: System.DateTime
      EstimatedDuration: System.TimeSpan option // Estimated based on chunk count and movement volume
      CanCancel: bool }

/// <summary>
/// Helper module for calculating import progress percentages.
/// </summary>
module ImportProgressCalculator =

    open System

    /// <summary>
    /// Calculate overall progress percentage (0-100) based on current state.
    /// </summary>
    let calculateOverallProgress (state: ChunkedImportState) (totalChunks: int) : decimal =
        match state with
        | Idle -> 0.0m
        | Validating _ -> 2.0m
        | ExtractingFile(_, progress) -> 2.0m + (progress * 0.03m) // 2-5%
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
    /// Ignores first 3 chunks to avoid inaccurate early estimates that increase over time.
    /// </summary>
    let calculateTimeRemaining
        (startTime: DateTime)
        (currentProgress: decimal)
        (currentChunk: int option)
        : TimeSpan option =
        // Don't show time estimate until at least 3 chunks are complete
        // This prevents early overoptimistic estimates that increase as import progresses
        match currentChunk with
        | Some chunkNum when chunkNum < 3 -> None
        | _ when currentProgress <= 0.0m -> None
        | _ ->
            let elapsed = DateTime.Now - startTime
            let totalEstimated = elapsed.TotalSeconds / (float currentProgress / 100.0)
            let remaining = totalEstimated - elapsed.TotalSeconds

            if remaining > 0.0 then
                Some(TimeSpan.FromSeconds(remaining))
            else
                None

/// <summary>
/// Helper functions for converting between ChunkedImportState DU and C#-friendly record
/// </summary>
module CurrentChunkedImportStatus =

    open System

    /// <summary>
    /// Convert F# discriminated union to C#-friendly record.
    /// Called automatically when pushing status updates to BehaviorSubject.
    /// Does NOT apply localization - that happens in UI extension methods.
    /// </summary>
    let fromChunkedState (state: ChunkedImportState) (startTime: DateTime option) : CurrentChunkedImportStatus =
        match state with
        | Idle ->
            { State = ChunkedImportStateEnum.Idle
              FileName = None
              AnalysisProgress = None
              ChunkNumber = None
              TotalChunks = None
              ChunkStartDate = None
              ChunkEndDate = None
              EstimatedMovements = None
              CurrentPhase = None
              ChunkProgress = None
              SnapshotType = None
              SnapshotsProcessed = None
              SnapshotsTotal = None
              SnapshotProgress = None
              OverallProgress = 0m
              StartTime = None
              ElapsedTime = None
              EstimatedTimeRemaining = None
              TotalMovements = None
              TotalBrokerSnapshots = None
              TotalTickerSnapshots = None
              TotalOperations = None
              Duration = None
              CanCancel = false
              ErrorMessage = None }

        | Validating fileName ->
            { State = ChunkedImportStateEnum.Validating
              FileName = Some fileName
              AnalysisProgress = None
              ChunkNumber = None
              TotalChunks = None
              ChunkStartDate = None
              ChunkEndDate = None
              EstimatedMovements = None
              CurrentPhase = None
              ChunkProgress = None
              SnapshotType = None
              SnapshotsProcessed = None
              SnapshotsTotal = None
              SnapshotProgress = None
              OverallProgress = 2m
              StartTime = startTime
              ElapsedTime = startTime |> Option.map (fun st -> DateTime.Now - st)
              EstimatedTimeRemaining = None
              TotalMovements = None
              TotalBrokerSnapshots = None
              TotalTickerSnapshots = None
              TotalOperations = None
              Duration = None
              CanCancel = true
              ErrorMessage = None }

        | ExtractingFile(fileName, progress) ->
            { State = ChunkedImportStateEnum.ExtractingFile
              FileName = Some fileName
              AnalysisProgress = Some progress
              ChunkNumber = None
              TotalChunks = None
              ChunkStartDate = None
              ChunkEndDate = None
              EstimatedMovements = None
              CurrentPhase = None
              ChunkProgress = None
              SnapshotType = None
              SnapshotsProcessed = None
              SnapshotsTotal = None
              SnapshotProgress = None
              OverallProgress = 2m + (progress * 0.03m) // 2-5%
              StartTime = startTime
              ElapsedTime = startTime |> Option.map (fun st -> DateTime.Now - st)
              EstimatedTimeRemaining = None
              TotalMovements = None
              TotalBrokerSnapshots = None
              TotalTickerSnapshots = None
              TotalOperations = None
              Duration = None
              CanCancel = true
              ErrorMessage = None }

        | ReadingFile fileName ->
            { State = ChunkedImportStateEnum.ReadingFile
              FileName = Some fileName
              AnalysisProgress = None
              ChunkNumber = None
              TotalChunks = None
              ChunkStartDate = None
              ChunkEndDate = None
              EstimatedMovements = None
              CurrentPhase = None
              ChunkProgress = None
              SnapshotType = None
              SnapshotsProcessed = None
              SnapshotsTotal = None
              SnapshotProgress = None
              OverallProgress = 5m
              StartTime = startTime
              ElapsedTime = startTime |> Option.map (fun st -> DateTime.Now - st)
              EstimatedTimeRemaining = None
              TotalMovements = None
              TotalBrokerSnapshots = None
              TotalTickerSnapshots = None
              TotalOperations = None
              Duration = None
              CanCancel = true
              ErrorMessage = None }

        | AnalyzingDates(fileName, progress) ->
            { State = ChunkedImportStateEnum.AnalyzingDates
              FileName = Some fileName
              AnalysisProgress = Some progress
              ChunkNumber = None
              TotalChunks = None
              ChunkStartDate = None
              ChunkEndDate = None
              EstimatedMovements = None
              CurrentPhase = None
              ChunkProgress = None
              SnapshotType = None
              SnapshotsProcessed = None
              SnapshotsTotal = None
              SnapshotProgress = None
              OverallProgress = 5m + (progress * 0.05m) // 5-10%
              StartTime = startTime
              ElapsedTime = startTime |> Option.map (fun st -> DateTime.Now - st)
              EstimatedTimeRemaining = None
              TotalMovements = None
              TotalBrokerSnapshots = None
              TotalTickerSnapshots = None
              TotalOperations = None
              Duration = None
              CanCancel = true
              ErrorMessage = None }

        | ProcessingChunk chunkInfo ->
            let overallProgress =
                ImportProgressCalculator.calculateOverallProgress state chunkInfo.TotalChunks

            let elapsed = startTime |> Option.map (fun st -> DateTime.Now - st)

            let timeRemaining =
                startTime
                |> Option.bind (fun st ->
                    ImportProgressCalculator.calculateTimeRemaining st overallProgress (Some chunkInfo.ChunkNumber))

            { State = ChunkedImportStateEnum.ProcessingChunk
              FileName = None
              AnalysisProgress = None
              ChunkNumber = Some chunkInfo.ChunkNumber
              TotalChunks = Some chunkInfo.TotalChunks
              ChunkStartDate = Some(chunkInfo.StartDate.ToString())
              ChunkEndDate = Some(chunkInfo.EndDate.ToString())
              EstimatedMovements = Some chunkInfo.EstimatedMovements
              CurrentPhase = Some(chunkInfo.CurrentPhase.ToString())
              ChunkProgress = Some chunkInfo.Progress
              SnapshotType = None
              SnapshotsProcessed = None
              SnapshotsTotal = None
              SnapshotProgress = None
              OverallProgress = overallProgress
              StartTime = startTime
              ElapsedTime = elapsed
              EstimatedTimeRemaining = timeRemaining
              TotalMovements = None
              TotalBrokerSnapshots = None
              TotalTickerSnapshots = None
              TotalOperations = None
              Duration = None
              CanCancel = true
              ErrorMessage = None }

        | CalculatingSnapshots snapshotInfo ->
            let overallProgress = 90m + (snapshotInfo.Progress * 0.05m)
            let elapsed = startTime |> Option.map (fun st -> DateTime.Now - st)

            let timeRemaining =
                startTime
                |> Option.bind (fun st -> ImportProgressCalculator.calculateTimeRemaining st overallProgress None)

            { State = ChunkedImportStateEnum.CalculatingSnapshots
              FileName = None
              AnalysisProgress = None
              ChunkNumber = None
              TotalChunks = None
              ChunkStartDate = None
              ChunkEndDate = None
              EstimatedMovements = None
              CurrentPhase = None
              ChunkProgress = None
              SnapshotType = Some snapshotInfo.SnapshotType
              SnapshotsProcessed = Some snapshotInfo.Processed
              SnapshotsTotal = Some snapshotInfo.Total
              SnapshotProgress = Some snapshotInfo.Progress
              OverallProgress = overallProgress
              StartTime = startTime
              ElapsedTime = elapsed
              EstimatedTimeRemaining = timeRemaining
              TotalMovements = None
              TotalBrokerSnapshots = None
              TotalTickerSnapshots = None
              TotalOperations = None
              Duration = None
              CanCancel = true
              ErrorMessage = None }

        | Completed summary ->
            { State = ChunkedImportStateEnum.Completed
              FileName = None
              AnalysisProgress = None
              ChunkNumber = None
              TotalChunks = Some summary.TotalChunks
              ChunkStartDate = None
              ChunkEndDate = None
              EstimatedMovements = None
              CurrentPhase = None
              ChunkProgress = None
              SnapshotType = None
              SnapshotsProcessed = None
              SnapshotsTotal = None
              SnapshotProgress = None
              OverallProgress = 100m
              StartTime = Some summary.StartTime
              ElapsedTime = Some summary.Duration
              EstimatedTimeRemaining = None
              TotalMovements = Some summary.TotalMovements
              TotalBrokerSnapshots = Some summary.BrokerSnapshots
              TotalTickerSnapshots = Some summary.TickerSnapshots
              TotalOperations = Some summary.Operations
              Duration = Some summary.Duration
              CanCancel = false
              ErrorMessage = None }

        | Failed errorMessage ->
            { State = ChunkedImportStateEnum.Failed
              FileName = None
              AnalysisProgress = None
              ChunkNumber = None
              TotalChunks = None
              ChunkStartDate = None
              ChunkEndDate = None
              EstimatedMovements = None
              CurrentPhase = None
              ChunkProgress = None
              SnapshotType = None
              SnapshotsProcessed = None
              SnapshotsTotal = None
              SnapshotProgress = None
              OverallProgress = 0m
              StartTime = startTime
              ElapsedTime = startTime |> Option.map (fun st -> DateTime.Now - st)
              EstimatedTimeRemaining = None
              TotalMovements = None
              TotalBrokerSnapshots = None
              TotalTickerSnapshots = None
              TotalOperations = None
              Duration = None
              CanCancel = false
              ErrorMessage = Some errorMessage }

        | Cancelled ->
            { State = ChunkedImportStateEnum.Cancelled
              FileName = None
              AnalysisProgress = None
              ChunkNumber = None
              TotalChunks = None
              ChunkStartDate = None
              ChunkEndDate = None
              EstimatedMovements = None
              CurrentPhase = None
              ChunkProgress = None
              SnapshotType = None
              SnapshotsProcessed = None
              SnapshotsTotal = None
              SnapshotProgress = None
              OverallProgress = 0m
              StartTime = startTime
              ElapsedTime = startTime |> Option.map (fun st -> DateTime.Now - st)
              EstimatedTimeRemaining = None
              TotalMovements = None
              TotalBrokerSnapshots = None
              TotalTickerSnapshots = None
              TotalOperations = None
              Duration = None
              CanCancel = false
              ErrorMessage = None }
