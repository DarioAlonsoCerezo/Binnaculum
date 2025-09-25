namespace Binnaculum.Core.UI

open System
open Binnaculum.Core.Import
open Binnaculum.Core.Patterns
open Binnaculum.Core.Storage
open Binnaculum.Core.Database

/// <summary>
/// Reactive targeted snapshot manager that provides selective snapshot updates
/// for specific entities and date ranges after imports.
/// This avoids full refresh operations and improves performance.
/// </summary>
module ReactiveTargetedSnapshotManager =
    
    /// <summary>
    /// Generate a sequence of DateTimePattern values from start date to end date (inclusive)
    /// </summary>
    /// <param name="startDate">Start date of the range</param>
    /// <param name="endDate">End date of the range</param>
    /// <returns>List of DateTimePattern values</returns>
    let private generateDateRange (startDate: DateTimePattern) (endDate: DateTimePattern) =
        let rec generateDates (currentDate: DateTimePattern) (endDate: DateTimePattern) acc =
            if currentDate.Value > endDate.Value then
                List.rev acc
            else
                let nextDate = DateTimePattern.FromDateTime(currentDate.Value.AddDays(1.0))
                generateDates nextDate endDate (currentDate :: acc)
        
        generateDates startDate endDate []
    
    /// <summary>
    /// Get ticker ID by symbol from database
    /// Returns None if ticker symbol is not found
    /// </summary>
    /// <param name="tickerSymbol">Ticker symbol to lookup</param>
    /// <returns>Task containing Option with ticker ID if found</returns>
    let private getTickerIdBySymbol (tickerSymbol: string) =
        task {
            let! tickers = TickerExtensions.Do.getAll()
            return
                tickers
                |> List.tryFind (fun ticker -> ticker.Symbol = tickerSymbol)
                |> Option.map (fun ticker -> ticker.Id)
        }
    
    /// <summary>
    /// Updates snapshots from import metadata, targeting only affected entities and dates.
    /// This method leverages existing snapshot manager patterns for targeted updates.
    /// </summary>
    /// <param name="importMetadata">Metadata collected during import process</param>  
    /// <returns>Task that completes when all targeted updates are finished</returns>
    let updateFromImport (importMetadata: ImportMetadata) =
        task {
            // Only proceed if data was imported and has movement dates
            if importMetadata.TotalMovementsImported > 0 then
                match importMetadata.OldestMovementDate with
                | Some oldestDate ->
                    let startDate = DateTimePattern.FromDateTime(oldestDate)
                    let today = DateTimePattern.FromDateTime(DateTime.Today)
                    
                    // Validate date range
                    if startDate.Value <= today.Value then
                        // Create date range for iteration from oldest date to today
                        let dateRange = generateDateRange startDate today
                        
                        // Update broker account snapshots for affected accounts
                        for brokerAccountId in importMetadata.AffectedBrokerAccountIds do
                            for date in dateRange do
                                do! BrokerAccountSnapshotManager.handleBrokerAccountChange(brokerAccountId, date)
                        
                        // Resolve ticker symbols to IDs and update ticker snapshots
                        for tickerSymbol in importMetadata.AffectedTickerSymbols do
                            let! tickerIdOption = getTickerIdBySymbol tickerSymbol
                            match tickerIdOption with
                            | Some tickerId ->
                                for date in dateRange do
                                    do! TickerSnapshotManager.handleTickerChange(tickerId, date)
                            | None ->
                                // Ticker not found - may have been created but not yet in cache
                                // This is not an error case, just skip silently
                                ()
                    else
                        failwith $"Invalid import date range: oldest date {startDate.Value} is in the future"
                | None -> 
                    // No movement date, nothing to update
                    ()
        }