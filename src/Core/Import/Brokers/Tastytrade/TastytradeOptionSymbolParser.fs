namespace Binnaculum.Core.Import

open System
open System.Text.RegularExpressions
open TastytradeModels

/// <summary>
/// Parser for complex Tastytrade option symbols with embedded expiration dates and strikes
/// Examples:
/// - PLTR  240531C00022000 -> PLTR, 2024-05-31, CALL, 22.00
/// - AAPL  240614P00185000 -> AAPL, 2024-06-14, PUT, 185.00
/// </summary>
module TastytradeOptionSymbolParser =

    /// <summary>
    /// Regex pattern for parsing Tastytrade option symbols
    /// Format: [TICKER][SPACES][YYMMDD][C/P][STRIKE_PADDED]
    /// </summary>
    let private optionSymbolPattern = 
        @"^([A-Z]+)\s+(\d{6})([CP])(\d{8})$"
    
    let private optionSymbolRegex = Regex(optionSymbolPattern, RegexOptions.Compiled)

    /// <summary>
    /// Parse a Tastytrade option symbol into its components
    /// </summary>
    /// <param name="symbol">The option symbol to parse (e.g., "PLTR  240531C00022000")</param>
    /// <returns>Parsed option symbol components</returns>
    let parseOptionSymbol (symbol: string) : ParsedOptionSymbol =
        if String.IsNullOrWhiteSpace(symbol) then
            failwith "Option symbol cannot be null or empty"

        let trimmedSymbol = symbol.Trim()
        let regexMatch = optionSymbolRegex.Match(trimmedSymbol)
        
        if not regexMatch.Success then
            failwith $"Invalid option symbol format: '{symbol}'. Expected format: TICKER YYMMDDCXXXXXXXX"

        try
            let ticker = regexMatch.Groups.[1].Value
            let dateString = regexMatch.Groups.[2].Value
            let optionTypeChar = regexMatch.Groups.[3].Value
            let strikeString = regexMatch.Groups.[4].Value

            // Parse expiration date from YYMMDD format
            let year = 2000 + int (dateString.Substring(0, 2))
            let month = int (dateString.Substring(2, 2))
            let day = int (dateString.Substring(4, 2))
            let expirationDate = DateTime(year, month, day)

            // Parse option type
            let optionType = 
                match optionTypeChar with
                | "C" -> "CALL"
                | "P" -> "PUT"
                | _ -> failwith $"Invalid option type: {optionTypeChar}. Expected 'C' or 'P'"

            // Parse strike price from padded format (8 digits with implied decimals)
            // Format: 00022000 represents 22.000
            let strikeInt = int64 strikeString
            let strike = decimal strikeInt / 1000m

            {
                Ticker = ticker
                ExpirationDate = expirationDate
                Strike = strike
                OptionType = optionType
            }

        with
        | :? FormatException as ex ->
            failwith $"Failed to parse numeric components in option symbol '{symbol}': {ex.Message}"
        | :? ArgumentOutOfRangeException as ex ->
            failwith $"Invalid date components in option symbol '{symbol}': {ex.Message}"
        | ex ->
            failwith $"Failed to parse option symbol '{symbol}': {ex.Message}"

    /// <summary>
    /// Validate that a string looks like a Tastytrade option symbol
    /// </summary>
    /// <param name="symbol">The symbol to validate</param>
    /// <returns>True if the symbol matches the expected format</returns>
    let isValidOptionSymbol (symbol: string) : bool =
        if String.IsNullOrWhiteSpace(symbol) then false
        else optionSymbolRegex.IsMatch(symbol.Trim())

    /// <summary>
    /// Extract just the ticker from an option symbol without full parsing
    /// </summary>
    /// <param name="symbol">The option symbol</param>
    /// <returns>The ticker portion</returns>
    let extractTicker (symbol: string) : string =
        if String.IsNullOrWhiteSpace(symbol) then
            failwith "Option symbol cannot be null or empty"

        let trimmedSymbol = symbol.Trim()
        let regexMatch = optionSymbolRegex.Match(trimmedSymbol)
        
        if not regexMatch.Success then
            failwith $"Invalid option symbol format: '{symbol}'"

        regexMatch.Groups.[1].Value

    /// <summary>
    /// Parse multiple option symbols and return results with error handling
    /// </summary>
    /// <param name="symbols">List of symbols to parse</param>
    /// <returns>Tuple of (successful results, error messages)</returns>
    let parseMultipleOptionSymbols (symbols: string list) : ParsedOptionSymbol list * string list =
        let results, errors = 
            symbols
            |> List.map (fun symbol ->
                try
                    let parsed = parseOptionSymbol symbol
                    Some parsed, None
                with
                | ex -> None, Some $"Failed to parse '{symbol}': {ex.Message}")
            |> List.unzip

        let successfulResults = results |> List.choose id
        let errorMessages = errors |> List.choose id

        successfulResults, errorMessages

    /// <summary>
    /// Format a parsed option symbol back to Tastytrade format (for testing/validation)
    /// </summary>
    /// <param name="parsed">The parsed option symbol</param>
    /// <returns>Formatted symbol string</returns>
    let formatOptionSymbol (parsed: ParsedOptionSymbol) : string =
        let dateString = parsed.ExpirationDate.ToString("yyMMdd")
        let optionTypeChar = if parsed.OptionType = "CALL" then "C" else "P"
        let strikeInt = int64 (parsed.Strike * 1000m)
        let strikePadded = strikeInt.ToString("00000000")
        
        $"{parsed.Ticker}  {dateString}{optionTypeChar}{strikePadded}"