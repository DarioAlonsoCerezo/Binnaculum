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
/// Comprehensive import result with detailed feedback for all processed files
/// </summary>
and ImportResult =
    { Success: bool
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
      SessionId: int option }

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
