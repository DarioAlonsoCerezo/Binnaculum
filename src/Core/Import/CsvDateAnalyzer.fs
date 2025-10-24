namespace Binnaculum.Core.Import

open System
open System.IO
open Binnaculum.Core.Database.DatabaseModel

/// <summary>
/// Result of CSV date analysis containing date distribution information.
/// Used to plan chunked import processing strategy.
/// </summary>
type DateAnalysis =
    { MinDate: DateTime
      MaxDate: DateTime
      TotalMovements: int
      MovementsByDate: Map<DateOnly, int>
      UniqueDates: DateOnly list }

/// <summary>
/// Lightweight CSV date analyzer that extracts date information without database access.
/// Parses CSV files to understand date distribution for optimal chunking strategy.
/// </summary>
module CsvDateAnalyzer =

    /// <summary>
    /// Analyze Tastytrade CSV file to extract date distribution.
    /// Only parses dates, does not perform full transaction parsing.
    /// </summary>
    let analyzeTastytradeDates (filePath: string) : DateAnalysis =
        try
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
                        try
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
                        with _ ->
                            () // Skip invalid lines

            // Handle empty file case
            if totalMovements = 0 then
                let now = DateTime.Now

                { MinDate = now
                  MaxDate = now
                  TotalMovements = 0
                  MovementsByDate = Map.empty
                  UniqueDates = [] }
            else
                { MinDate = minDate
                  MaxDate = maxDate
                  TotalMovements = totalMovements
                  MovementsByDate = movementsByDate
                  UniqueDates = movementsByDate |> Map.toList |> List.map fst |> List.sort }
        with ex ->
            failwith $"Error analyzing Tastytrade CSV dates: {ex.Message}"

    /// <summary>
    /// Analyze IBKR CSV file to extract date distribution.
    /// IBKR format has different structure with sections.
    /// </summary>
    let analyzeIBKRDates (filePath: string) : DateAnalysis =
        try
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
                        try
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
                        with _ ->
                            () // Skip invalid lines

            // Handle empty file case
            if totalMovements = 0 then
                let now = DateTime.Now

                { MinDate = now
                  MaxDate = now
                  TotalMovements = 0
                  MovementsByDate = Map.empty
                  UniqueDates = [] }
            else
                { MinDate = minDate
                  MaxDate = maxDate
                  TotalMovements = totalMovements
                  MovementsByDate = movementsByDate
                  UniqueDates = movementsByDate |> Map.toList |> List.map fst |> List.sort }
        with ex ->
            failwith $"Error analyzing IBKR CSV dates: {ex.Message}"

    /// <summary>
    /// Analyze CSV file to extract date distribution.
    /// Automatically detects broker type and uses appropriate parser.
    /// </summary>
    let internal analyzeCsvDates (filePath: string) (brokerType: SupportedBroker) : DateAnalysis =
        match brokerType with
        | Tastytrade -> analyzeTastytradeDates filePath
        | IBKR -> analyzeIBKRDates filePath
        | Unknown -> failwith "Cannot analyze dates for unknown broker type"
