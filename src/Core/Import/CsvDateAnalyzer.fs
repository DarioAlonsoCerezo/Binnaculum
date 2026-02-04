namespace Binnaculum.Core.Import

open System
open System.IO
open System.Security.Cryptography
open System.Text.RegularExpressions
open System.Globalization
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Logging

/// <summary>
/// Result of CSV date analysis containing date distribution information.
/// Used to plan chunked import processing strategy.
/// </summary>
type DateAnalysis =
    { MinDate: DateTime
      MaxDate: DateTime
      TotalMovements: int
      MovementsByDate: Map<DateOnly, int>
      UniqueDates: DateOnly list
      FileHash: string }

/// <summary>
/// Metadata extracted from a single CSV file for multi-file import analysis
/// </summary>
type CsvFileMetadata =
    { FilePath: string
      FileName: string
      EarliestDate: DateTime option
      LatestDate: DateTime option
      ExactRecordCount: int
      AllDates: DateTime list } // For gap/overlap detection

/// <summary>
/// Summary of analyzed CSV files with warnings for multi-file imports
/// </summary>
type ZipImportAnalysis =
    { TotalFiles: int
      TotalRecords: int
      OverallDateRange: (DateTime * DateTime) option
      FilesOrderedByDate: CsvFileMetadata list
      DateGaps: (DateTime * DateTime * int) list // (start, end, days missing)
      DateOverlaps: (string * string * DateTime) list // (file1, file2, overlap date)
      Warnings: string list }

/// <summary>
/// Lightweight CSV date analyzer that extracts date information without database access.
/// Parses CSV files to understand date distribution for optimal chunking strategy.
/// </summary>
module CsvDateAnalyzer =

    /// <summary>
    /// Calculate MD5 hash of file for change detection.
    /// Used to validate file hasn't been modified before resuming import.
    /// </summary>
    let calculateFileHash (filePath: string) : string =
        use md5 = MD5.Create()
        use stream = File.OpenRead(filePath)
        let hash = md5.ComputeHash(stream)
        BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()

    /// <summary>
    /// Analyze Tastytrade CSV file to extract date distribution.
    /// Only parses dates, does not perform full transaction parsing.
    /// </summary>
    let analyzeTastytradeDates (filePath: string) : DateAnalysis =
        use reader = new StreamReader(filePath)
        let mutable movementsByDate = Map.empty<DateOnly, int>
        let mutable minDate = DateTime.MaxValue
        let mutable maxDate = DateTime.MinValue
        let mutable totalMovements = 0
        let mutable lineNumber = 0

        // Skip header line
        reader.ReadLine() |> ignore
        lineNumber <- lineNumber + 1

        while not reader.EndOfStream do
            let line = reader.ReadLine()
            lineNumber <- lineNumber + 1

            if not (String.IsNullOrWhiteSpace(line)) then
                let columns = line.Split(',')

                // Tastytrade CSV format: Date column is typically at index 0 or 1
                // Format: "MM/DD/YYYY" or "YYYY-MM-DD"
                if columns.Length > 1 then
                    // Try parsing first column as date
                    let dateStr = columns.[0].Trim().Trim('"')
                    let mutable parsedDate = DateTime.MinValue

                    if DateTime.TryParse(dateStr, &parsedDate) then
                        let date = DateOnly.FromDateTime(parsedDate)

                        // Update min/max
                        if parsedDate < minDate then
                            minDate <- parsedDate

                        if parsedDate > maxDate then
                            maxDate <- parsedDate

                        // Count movements per date
                        let currentCount = movementsByDate |> Map.tryFind date |> Option.defaultValue 0
                        movementsByDate <- movementsByDate |> Map.add date (currentCount + 1)
                        totalMovements <- totalMovements + 1

        // Handle empty file case
        if totalMovements = 0 then
            let now = DateTime.Now

            { MinDate = now
              MaxDate = now
              TotalMovements = 0
              MovementsByDate = Map.empty
              UniqueDates = []
              FileHash = calculateFileHash (filePath) }
        else
            { MinDate = minDate
              MaxDate = maxDate
              TotalMovements = totalMovements
              MovementsByDate = movementsByDate
              UniqueDates = movementsByDate |> Map.toList |> List.map fst |> List.sort
              FileHash = calculateFileHash (filePath) }

    /// <summary>
    /// Analyze IBKR CSV file to extract date distribution.
    /// IBKR format has different structure with sections.
    /// </summary>
    let analyzeIBKRDates (filePath: string) : DateAnalysis =
        use reader = new StreamReader(filePath)
        let mutable movementsByDate = Map.empty<DateOnly, int>
        let mutable minDate = DateTime.MaxValue
        let mutable maxDate = DateTime.MinValue
        let mutable totalMovements = 0
        let mutable inDataSection = false

        while not reader.EndOfStream do
            let line = reader.ReadLine()

            if not (String.IsNullOrWhiteSpace(line)) then
                let columns = line.Split(',')

                // IBKR sections start with section names
                // Look for lines with dates
                if columns.Length > 2 then
                    // IBKR date format is typically in column 3 or 4
                    // Format: "YYYY-MM-DD" or "YYYYMMDD"
                    for i in 0 .. min 5 columns.Length do
                        if i < columns.Length then
                            let dateStr = columns.[i].Trim().Trim('"')
                            let mutable parsedDate = DateTime.MinValue

                            if DateTime.TryParse(dateStr, &parsedDate) && parsedDate.Year > 2000 then
                                let date = DateOnly.FromDateTime(parsedDate)

                                // Update min/max
                                if parsedDate < minDate then
                                    minDate <- parsedDate

                                if parsedDate > maxDate then
                                    maxDate <- parsedDate

                                // Count movements per date
                                let currentCount = movementsByDate |> Map.tryFind date |> Option.defaultValue 0
                                movementsByDate <- movementsByDate |> Map.add date (currentCount + 1)
                                totalMovements <- totalMovements + 1

                                // Break after finding first valid date in row
                                ()

        // Handle empty file case
        if totalMovements = 0 then
            let now = DateTime.Now

            { MinDate = now
              MaxDate = now
              TotalMovements = 0
              MovementsByDate = Map.empty
              UniqueDates = []
              FileHash = calculateFileHash (filePath) }
        else
            { MinDate = minDate
              MaxDate = maxDate
              TotalMovements = totalMovements
              MovementsByDate = movementsByDate
              UniqueDates = movementsByDate |> Map.toList |> List.map fst |> List.sort
              FileHash = calculateFileHash (filePath) }

    /// <summary>
    /// Analyze CSV file to extract date distribution.
    /// Automatically detects broker type and uses appropriate parser.
    /// </summary>
    let internal analyzeCsvDates (filePath: string) (brokerType: SupportedBroker) : DateAnalysis =
        match brokerType with
        | Tastytrade -> analyzeTastytradeDates filePath
        | IBKR -> analyzeIBKRDates filePath
        | Unknown -> failwith "Cannot analyze dates for unknown broker type"

    // ==================== Multi-File Import Support ====================

    /// <summary>
    /// Extract date from common broker filename patterns (broker-agnostic)
    /// Supports:
    ///   - IBKR: "Daily_statement.1332220.20230228.csv" → 2023-02-28
    ///   - Tastytrade: "tastytrade_transactions_history_x5WY40536_240401_to_240430.csv" → 2024-04-01
    /// </summary>
    /// <param name="fileName">The filename to parse</param>
    /// <returns>Extracted DateTime if found, None otherwise</returns>
    let extractDateFromFilename (fileName: string) : DateTime option =
        // IBKR pattern: YYYYMMDD (8 digits)
        let ibkrPattern = @"\b(\d{8})\b"
        let ibkrMatch = Regex.Match(fileName, ibkrPattern)

        if ibkrMatch.Success then
            let dateStr = ibkrMatch.Groups.[1].Value
            let mutable parsedDate = DateTime.MinValue

            if
                DateTime.TryParseExact(
                    dateStr,
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    &parsedDate
                )
            then
                Some parsedDate
            else
                None
        else
            // Tastytrade pattern: YYMMDD_to_YYMMDD (start date)
            let tastyPattern = @"_(\d{6})_to_\d{6}"
            let tastyMatch = Regex.Match(fileName, tastyPattern)

            if tastyMatch.Success then
                let dateStr = tastyMatch.Groups.[1].Value
                let mutable parsedDate = DateTime.MinValue

                if
                    DateTime.TryParseExact(
                        dateStr,
                        "yyMMdd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        &parsedDate
                    )
                then
                    Some parsedDate
                else
                    None
            else
                None

    /// <summary>
    /// Try to parse a date from a CSV row (any column)
    /// Attempts multiple date formats to be broker-agnostic:
    ///   - ISO format: "2023-02-28"
    ///   - US format: "02/28/2023" or "2/28/2023"
    ///   - Timestamp: "2023-02-28 14:30:00"
    ///   - Compact: "20230228"
    /// </summary>
    /// <param name="csvRow">Single CSV row as string</param>
    /// <returns>Parsed DateTime if found, None otherwise</returns>
    let tryParseDateFromCsvRow (csvRow: string) : DateTime option =
        if String.IsNullOrWhiteSpace(csvRow) then
            None
        else
            // Split by comma, handling quoted fields
            let parts =
                csvRow.Split(',')
                |> Array.map (fun s -> s.Trim().Trim('"'))
                |> Array.filter (fun s -> not (String.IsNullOrWhiteSpace(s)))

            // Date formats to try
            let dateFormats =
                [| "yyyy-MM-dd"
                   "yyyy-MM-dd HH:mm:ss"
                   "M/d/yyyy"
                   "MM/dd/yyyy"
                   "M/d/yyyy HH:mm:ss"
                   "MM/dd/yyyy HH:mm:ss"
                   "yyyyMMdd" |]

            // Try parsing each field
            parts
            |> Array.tryPick (fun field ->
                let mutable parsedDate = DateTime.MinValue

                if
                    DateTime.TryParseExact(
                        field,
                        dateFormats,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        &parsedDate
                    )
                    && parsedDate.Year > 2000
                then
                    Some parsedDate
                elif DateTime.TryParse(field, &parsedDate) && parsedDate.Year > 2000 then
                    Some parsedDate
                else
                    None)

    /// <summary>
    /// Extract ALL dates from a CSV file by reading it completely
    /// Reads entire file for 100% accuracy (performance acceptable for broker statements)
    /// </summary>
    /// <param name="filePath">Path to CSV file</param>
    /// <returns>List of all dates found in the file</returns>
    let extractAllDatesFromCsv (filePath: string) : DateTime list =
        if not (File.Exists(filePath)) then
            []
        else
            let lines = File.ReadAllLines(filePath)

            if lines.Length <= 1 then
                [] // Empty or header-only file
            else
                lines
                |> Array.skip 1 // Skip header row
                |> Array.choose tryParseDateFromCsvRow
                |> Array.distinct
                |> Array.sort
                |> Array.toList

    /// <summary>
    /// Analyze a single CSV file with complete date extraction
    /// </summary>
    /// <param name="filePath">Path to CSV file</param>
    /// <returns>Metadata including date range and exact record count</returns>
    let analyzeFile (filePath: string) : CsvFileMetadata =
        let fileName = Path.GetFileName(filePath)

        // Try filename-based date extraction first (fast fallback)
        let filenameDate = extractDateFromFilename fileName

        // Extract ALL dates from file content (accurate)
        let allDates = extractAllDatesFromCsv filePath

        // Get exact row count (useful for progress tracking)
        let exactRowCount = File.ReadAllLines(filePath).Length - 1 // Minus header

        { FilePath = filePath
          FileName = fileName
          EarliestDate =
            match allDates with
            | [] -> filenameDate // Fallback to filename
            | dates -> Some(List.min dates)
          LatestDate =
            match allDates with
            | [] -> filenameDate
            | dates -> Some(List.max dates)
          ExactRecordCount = exactRowCount
          AllDates = allDates }

    /// <summary>
    /// Detect gaps in date sequences across files
    /// </summary>
    /// <param name="metadata">List of analyzed CSV files</param>
    /// <returns>List of gaps as (start date, end date, days missing)</returns>
    let detectDateGaps (metadata: CsvFileMetadata list) : (DateTime * DateTime * int) list =
        if metadata.Length <= 1 then
            []
        else
            // Sort by earliest date
            let sorted =
                metadata
                |> List.sortBy (fun m ->
                    match m.EarliestDate with
                    | Some d -> d
                    | None -> DateTime.MaxValue)

            // Compare consecutive files
            sorted
            |> List.pairwise
            |> List.choose (fun (prev, next) ->
                match prev.LatestDate, next.EarliestDate with
                | Some prevEnd, Some nextStart ->
                    let daysDiff = (nextStart - prevEnd).Days
                    // Gap > 1 day (accounting for same-day or next-day transitions)
                    if daysDiff > 1 then
                        Some(prevEnd, nextStart, daysDiff)
                    else
                        None
                | _ -> None)

    /// <summary>
    /// Detect overlapping date ranges between files (potential duplicates)
    /// </summary>
    /// <param name="metadata">List of analyzed CSV files</param>
    /// <returns>List of overlaps as (file1 name, file2 name, overlap date)</returns>
    let detectDateOverlaps (metadata: CsvFileMetadata list) : (string * string * DateTime) list =
        if metadata.Length <= 1 then
            []
        else
            // Sort by earliest date
            let sorted =
                metadata
                |> List.sortBy (fun m ->
                    match m.EarliestDate with
                    | Some d -> d
                    | None -> DateTime.MaxValue)

            // Compare consecutive files for overlaps
            sorted
            |> List.pairwise
            |> List.collect (fun (prev, next) ->
                match prev.LatestDate, next.EarliestDate with
                | Some prevEnd, Some nextStart ->
                    // If previous file's latest date >= next file's earliest date, it's an overlap
                    if prevEnd >= nextStart then
                        // Find all overlapping dates
                        let prevDates = Set.ofList prev.AllDates
                        let nextDates = Set.ofList next.AllDates
                        let overlappingDates = Set.intersect prevDates nextDates |> Set.toList

                        overlappingDates |> List.map (fun date -> (prev.FileName, next.FileName, date))
                    else
                        []
                | _ -> [])

    /// <summary>
    /// Analyze multiple CSV files and provide comprehensive summary
    /// </summary>
    /// <param name="csvFiles">List of CSV file paths to analyze</param>
    /// <returns>Complete analysis with sorting, gaps, overlaps, and warnings</returns>
    let analyzeAndSort (csvFiles: string list) : ZipImportAnalysis =
        // Analyze all files
        let metadata = csvFiles |> List.map analyzeFile

        // Sort by earliest date
        let sorted =
            metadata
            |> List.sortBy (fun m ->
                match m.EarliestDate with
                | Some d -> d
                | None -> DateTime.MaxValue // Files without dates go last
            )

        // Detect issues
        let gaps = detectDateGaps sorted
        let overlaps = detectDateOverlaps sorted

        // Build warnings
        let warnings =
            [ if gaps.Length > 0 then
                  yield $"Detected {gaps.Length} date gap(s) in sequence"
              if overlaps.Length > 0 then
                  yield $"Detected {overlaps.Length} overlapping date range(s)" ]

        // Calculate overall range
        let overallRange =
            let allDatesInOrder = sorted |> List.collect (fun m -> m.AllDates)

            match allDatesInOrder with
            | [] -> None
            | dates -> Some(List.min dates, List.max dates)

        { TotalFiles = sorted.Length
          TotalRecords = sorted |> List.sumBy (fun m -> m.ExactRecordCount)
          OverallDateRange = overallRange
          FilesOrderedByDate = sorted
          DateGaps = gaps
          DateOverlaps = overlaps
          Warnings = warnings }
