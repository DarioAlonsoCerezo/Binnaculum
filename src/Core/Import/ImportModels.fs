namespace Binnaculum.Core.Import

/// <summary>
/// Import status tracking for real-time progress updates via BehaviorSubject
/// </summary>
type ImportStatus =
    | NotStarted
    | Validating of filePath: string
    | ProcessingFile of fileName: string * progress: float
    | ProcessingData of recordsProcessed: int * totalRecords: int
    | SavingToDatabase of message: string * progress: float
    | Completed of result: ImportResult
    | Cancelled of reason: string
    | Failed of error: string

/// <summary>
/// Comprehensive import result with detailed feedback for all processed files
/// </summary>
and ImportResult = {
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
}

/// <summary>
/// Structured error information for data validation issues
/// </summary>
and ImportError = {
    RowNumber: int option
    ErrorMessage: string
    ErrorType: ImportErrorType
    RawData: string option
}

/// <summary>
/// Warning information for non-critical issues during import
/// </summary>
and ImportWarning = {
    RowNumber: int option
    WarningMessage: string
    WarningType: ImportWarningType
    RawData: string option
}

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
and ImportedDataSummary = {
    Trades: int
    BrokerMovements: int  
    Dividends: int
    OptionTrades: int
    NewTickers: int
}

/// <summary>
/// Metadata collected during import for targeted reactive updates
/// Contains information about what data changed and needs snapshot updates
/// </summary>
and ImportMetadata = {
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
and FileImportResult = {
    FileName: string
    Success: bool
    ProcessedRecords: int
    Errors: ImportError list
    Warnings: ImportWarning list
}

/// Helper functions for creating import results
module ImportResult =
    
    /// Create a successful import result
    let createSuccess (processedFiles: int) (processedRecords: int) 
                     (importedData: ImportedDataSummary) (fileResults: FileImportResult list) 
                     (processingTimeMs: int64) =
        {
            Success = true
            ProcessedFiles = processedFiles
            ProcessedRecords = processedRecords
            SkippedRecords = 0
            TotalRecords = processedRecords
            ProcessingTimeMs = processingTimeMs
            Errors = []
            Warnings = []
            ImportedData = importedData
            FileResults = fileResults
        }
    
    /// Create a cancelled import result
    let createCancelled () =
        {
            Success = false
            ProcessedFiles = 0
            ProcessedRecords = 0
            SkippedRecords = 0
            TotalRecords = 0
            ProcessingTimeMs = 0L
            Errors = []
            Warnings = []
            ImportedData = { Trades = 0; BrokerMovements = 0; Dividends = 0; OptionTrades = 0; NewTickers = 0 }
            FileResults = []
        }
    
    /// Create an error import result
    let createError (errorMessage: string) =
        {
            Success = false
            ProcessedFiles = 0
            ProcessedRecords = 0
            SkippedRecords = 0
            TotalRecords = 0
            ProcessingTimeMs = 0L
            Errors = [{ RowNumber = None; ErrorMessage = errorMessage; ErrorType = ValidationError; RawData = None }]
            Warnings = []
            ImportedData = { Trades = 0; BrokerMovements = 0; Dividends = 0; OptionTrades = 0; NewTickers = 0 }
            FileResults = []
        }

/// Helper functions for creating file import results
module FileImportResult =
    
    /// Create a successful file import result
    let createSuccess (fileName: string) (processedRecords: int) =
        {
            FileName = fileName
            Success = true
            ProcessedRecords = processedRecords
            Errors = []
            Warnings = []
        }
    
    /// Create a failed file import result
    let createFailure (fileName: string) (errors: ImportError list) =
        {
            FileName = fileName
            Success = false
            ProcessedRecords = 0
            Errors = errors
            Warnings = []
        }

/// Helper functions for working with import metadata
module ImportMetadata =
    
    /// Create empty import metadata
    let createEmpty () =
        {
            OldestMovementDate = None
            AffectedBrokerAccountIds = Set.empty
            AffectedTickerSymbols = Set.empty
            TotalMovementsImported = 0
        }
    
    /// Combine two ImportMetadata instances (useful for multi-file imports)
    let combine (metadata1: ImportMetadata) (metadata2: ImportMetadata) =
        {
            OldestMovementDate = 
                match metadata1.OldestMovementDate, metadata2.OldestMovementDate with
                | None, None -> None
                | Some date, None | None, Some date -> Some date
                | Some date1, Some date2 -> Some (if date1 < date2 then date1 else date2)
            AffectedBrokerAccountIds = Set.union metadata1.AffectedBrokerAccountIds metadata2.AffectedBrokerAccountIds
            AffectedTickerSymbols = Set.union metadata1.AffectedTickerSymbols metadata2.AffectedTickerSymbols
            TotalMovementsImported = metadata1.TotalMovementsImported + metadata2.TotalMovementsImported
        }