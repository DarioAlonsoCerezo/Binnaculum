namespace Binnaculum.Core.Import

open System
open System.Globalization
open TastytradeModels

/// <summary>
/// Parser for Tastytrade CSV transaction history files
/// Handles various transaction types including options, equities, money movements, and ACAT transfers
/// </summary>
module TastytradeStatementParser =

    /// <summary>
    /// Expected CSV column headers for Tastytrade transaction files
    /// </summary>
    let private expectedHeaders = [
        "Date"; "Type"; "Sub Type"; "Action"; "Symbol"; "Instrument Type";
        "Description"; "Value"; "Quantity"; "Average Price"; "Commissions";
        "Fees"; "Multiplier"; "Root Symbol"; "Underlying Symbol"; 
        "Expiration Date"; "Strike Price"; "Call or Put"; "Order #"; "Currency"
    ]

    /// <summary>
    /// Parse a decimal value from CSV, handling various Tastytrade formats
    /// </summary>
    let private parseDecimal (value: string) : decimal =
        if String.IsNullOrWhiteSpace(value) || value = "--" then
            0m
        else
            try
                Decimal.Parse(value.Replace(",", ""), CultureInfo.InvariantCulture)
            with
            | _ -> failwith $"Invalid decimal value: '{value}'"

    /// <summary>
    /// Parse a date value from Tastytrade CSV format
    /// Handles ISO format with timezone: "2024-05-31T14:42:13+0100"
    /// </summary>
    let private parseDate (dateStr: string) : DateTime =
        if String.IsNullOrWhiteSpace(dateStr) then
            failwith "Date cannot be empty"
        
        try
            // Handle ISO format with timezone offset
            if dateStr.Contains("T") then
                DateTimeOffset.Parse(dateStr, CultureInfo.InvariantCulture).DateTime
            else
                // Handle simple date format
                DateTime.Parse(dateStr, CultureInfo.InvariantCulture)
        with
        | ex -> failwith $"Invalid date format: '{dateStr}'. Expected ISO format like '2024-05-31T14:42:13+0100'. Error: {ex.Message}"

    /// <summary>
    /// Parse expiration date from MM/dd/yy format used in Tastytrade CSV
    /// </summary>
    let private parseExpirationDate (dateStr: string) : DateTime option =
        if String.IsNullOrWhiteSpace(dateStr) then
            None
        else
            try
                // Handle MM/dd/yy format: "5/31/24"
                let parsedDate = DateTime.ParseExact(dateStr, [|"M/d/yy"; "MM/dd/yy"; "M/dd/yy"; "MM/d/yy"|], 
                                                   CultureInfo.InvariantCulture, DateTimeStyles.None)
                Some parsedDate
            with
            | ex -> 
                let errorMsg = sprintf "Invalid expiration date format: '%s'. Expected format like '5/31/24'. Error: %s" dateStr ex.Message
                failwith errorMsg

    /// <summary>
    /// Split CSV line respecting quoted fields that may contain commas
    /// </summary>
    let private splitCsvLine (line: string) : string array =
        let mutable fields = []
        let mutable currentField = ""
        let mutable insideQuotes = false
        let mutable i = 0
        
        while i < line.Length do
            let char = line.[i]
            match char with
            | '"' -> 
                insideQuotes <- not insideQuotes
            | ',' when not insideQuotes ->
                fields <- currentField :: fields
                currentField <- ""
            | _ ->
                currentField <- currentField + string char
            i <- i + 1
        
        // Add the final field
        fields <- currentField :: fields
        
        fields 
        |> List.rev 
        |> List.map (fun f -> f.Trim().Trim('"'))
        |> List.toArray

    /// <summary>
    /// Validate CSV headers match expected Tastytrade format
    /// </summary>
    let private validateHeaders (headers: string array) : Result<unit, string> =
        let normalizedHeaders = headers |> Array.map (fun h -> h.Trim())
        let normalizedExpected = expectedHeaders |> List.toArray
        
        if normalizedHeaders.Length <> normalizedExpected.Length then
            Error $"Expected {normalizedExpected.Length} columns, found {normalizedHeaders.Length}"
        else
            let mismatches = 
                Array.zip normalizedHeaders normalizedExpected
                |> Array.mapi (fun i (actual, expected) -> 
                    if actual.Equals(expected, StringComparison.OrdinalIgnoreCase) then None
                    else Some $"Column {i}: expected '{expected}', found '{actual}'")
                |> Array.choose id
            
            if mismatches.Length > 0 then
                let errorMsg = sprintf "Header mismatches: %s" (String.Join("; ", mismatches))
                Error errorMsg
            else
                Ok ()

    /// <summary>
    /// Parse a single CSV line into a TastytradeTransaction
    /// </summary>
    let private parseTransactionLine (fields: string array) (lineNumber: int) (rawLine: string) : Result<TastytradeTransaction, TastytradeParsingError> =
        try
            if fields.Length < expectedHeaders.Length then
                Error {
                    LineNumber = lineNumber
                    ErrorMessage = $"Expected {expectedHeaders.Length} fields, found {fields.Length}"
                    RawCsvLine = rawLine
                    ErrorType = MissingRequiredField("All fields")
                }
            else
                let date = parseDate fields.[0]
                let transactionType = TransactionTypeDetection.parseTransactionType fields.[1] fields.[2] fields.[3]
                let symbol = if String.IsNullOrWhiteSpace(fields.[4]) then None else Some fields.[4]
                let instrumentType = if String.IsNullOrWhiteSpace(fields.[5]) then None else Some fields.[5]
                let value = parseDecimal fields.[7]
                let quantity = parseDecimal fields.[8]
                let avgPrice = if String.IsNullOrWhiteSpace(fields.[9]) then None else Some (parseDecimal fields.[9])
                let commissions = parseDecimal fields.[10]
                let fees = parseDecimal fields.[11]
                let multiplier = if String.IsNullOrWhiteSpace(fields.[12]) then None else Some (parseDecimal fields.[12])
                let rootSymbol = if String.IsNullOrWhiteSpace(fields.[13]) then None else Some fields.[13]
                let underlyingSymbol = if String.IsNullOrWhiteSpace(fields.[14]) then None else Some fields.[14]
                let expirationDate = parseExpirationDate fields.[15]
                let strikePrice = if String.IsNullOrWhiteSpace(fields.[16]) then None else Some (parseDecimal fields.[16])
                let callOrPut = if String.IsNullOrWhiteSpace(fields.[17]) then None else Some fields.[17]
                let orderNumber = if String.IsNullOrWhiteSpace(fields.[18]) then None else Some fields.[18]

                Ok {
                    Date = date
                    TransactionType = transactionType
                    Symbol = symbol
                    InstrumentType = instrumentType
                    Description = fields.[6]
                    Value = value
                    Quantity = quantity
                    AveragePrice = avgPrice
                    Commissions = commissions
                    Fees = fees
                    Multiplier = multiplier
                    RootSymbol = rootSymbol
                    UnderlyingSymbol = underlyingSymbol
                    ExpirationDate = expirationDate
                    StrikePrice = strikePrice
                    CallOrPut = callOrPut
                    OrderNumber = orderNumber
                    Currency = fields.[19]
                    RawCsvLine = rawLine
                    LineNumber = lineNumber
                }

        with
        | ex -> 
            Error {
                LineNumber = lineNumber
                ErrorMessage = ex.Message
                RawCsvLine = rawLine
                ErrorType = 
                    if ex.Message.Contains("date") then InvalidDateFormat
                    elif ex.Message.Contains("decimal") || ex.Message.Contains("numeric") then InvalidNumericValue(ex.Message)
                    elif ex.Message.Contains("transaction type") then InvalidTransactionType
                    else TastytradeErrorType.InvalidNumericValue(ex.Message)
            }

    /// <summary>
    /// Parse complete Tastytrade transaction history CSV content
    /// </summary>
    /// <param name="csvContent">The complete CSV file content</param>
    /// <returns>Parsing result with transactions and any errors</returns>
    let parseTransactionHistory (csvContent: string) : TastytradeParsingResult =
        if String.IsNullOrWhiteSpace(csvContent) then
            {
                Transactions = []
                Strategies = []
                Errors = []
                ProcessedLines = 0
                SkippedLines = 0
            }
        else
            let lines = csvContent.Split([|'\r'; '\n'|], StringSplitOptions.RemoveEmptyEntries)
            
            if lines.Length = 0 then
                {
                    Transactions = []
                    Strategies = []
                    Errors = []
                    ProcessedLines = 0
                    SkippedLines = 0
                }
            else
                // Parse and validate headers
                let headers = splitCsvLine lines.[0]
                match validateHeaders headers with
                | Error headerError ->
                    {
                        Transactions = []
                        Strategies = []
                        Errors = [{
                            LineNumber = 1
                            ErrorMessage = headerError
                            RawCsvLine = lines.[0]
                            ErrorType = MissingRequiredField("Headers")
                        }]
                        ProcessedLines = 0
                        SkippedLines = lines.Length
                    }
                | Ok () ->
                    // Parse data lines
                    let dataLines = lines.[1..] // Skip header
                    let mutable transactions = []
                    let mutable errors = []
                    let mutable processedLines = 0
                    let mutable skippedLines = 0

                    for i, line in dataLines |> Array.indexed do
                        let lineNumber = i + 2 // Account for header and 1-based indexing
                        
                        if String.IsNullOrWhiteSpace(line) then
                            skippedLines <- skippedLines + 1
                        else
                            let fields = splitCsvLine line
                            match parseTransactionLine fields lineNumber line with
                            | Ok transaction ->
                                transactions <- transaction :: transactions
                                processedLines <- processedLines + 1
                            | Error error ->
                                errors <- error :: errors
                                skippedLines <- skippedLines + 1

                    {
                        Transactions = List.rev transactions
                        Strategies = [] // Will be populated by strategy detector
                        Errors = List.rev errors
                        ProcessedLines = processedLines
                        SkippedLines = skippedLines
                    }

    /// <summary>
    /// Parse a single CSV file from file path
    /// </summary>
    /// <param name="filePath">Path to the CSV file</param>
    /// <returns>Parsing result</returns>
    let parseTransactionHistoryFromFile (filePath: string) : TastytradeParsingResult =
        try
            let content = System.IO.File.ReadAllText(filePath)
            parseTransactionHistory content
        with
        | ex ->
            {
                Transactions = []
                Strategies = []
                Errors = [{
                    LineNumber = 0
                    ErrorMessage = $"Failed to read file '{filePath}': {ex.Message}"
                    RawCsvLine = ""
                    ErrorType = MissingRequiredField("File access")
                }]
                ProcessedLines = 0
                SkippedLines = 0
            }