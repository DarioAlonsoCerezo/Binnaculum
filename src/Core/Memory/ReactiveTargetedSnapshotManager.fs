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
            // Early exit if no data was imported
            if importMetadata.TotalMovementsImported = 0 then
                return ()
            
            // Convert oldest date to DateTimePattern for snapshot manager compatibility
            match importMetadata.OldestMovementDate with
            | None -> 
                // No movements imported, nothing to update
                return ()
            | Some oldestDate ->
                let startDate = DateTimePattern.FromDateTime(oldestDate)
                let today = DateTimePattern.FromDateTime(DateTime.Today)
                
                // Validate date range
                if startDate.Value > today.Value then
                    failwith $"Invalid import date range: oldest date {startDate.Value} is in the future"
                
                // Create date range for iteration from oldest date to today
                let dateRange = generateDateRange startDate today
                
                // Update broker account snapshots for affected accounts
                let brokerAccountUpdateTasks = 
                    importMetadata.AffectedBrokerAccountIds
                    |> Set.toList
                    |> List.map (fun brokerAccountId ->
                        // Use existing handleBrokerAccountChange for each affected date
                        task {
                            for date in dateRange do
                                do! BrokerAccountSnapshotManager.handleBrokerAccountChange(brokerAccountId, date)
                        })
                
                // Resolve ticker symbols to IDs and update ticker snapshots
                let! tickerIds = 
                    importMetadata.AffectedTickerSymbols
                    |> Set.toList
                    |> List.map getTickerIdBySymbol
                    |> System.Threading.Tasks.Task.WhenAll
                
                let tickerUpdateTasks =
                    tickerIds
                    |> Array.choose id // Filter out None values
                    |> Array.map (fun tickerId ->
                        // Use existing handleTickerChange for each affected date
                        task {
                            for date in dateRange do
                                do! TickerSnapshotManager.handleTickerChange(tickerId, date)
                        })
                    |> Array.toList
                
                // Execute all update tasks concurrently for better performance
                let allUpdateTasks = brokerAccountUpdateTasks @ tickerUpdateTasks
                match allUpdateTasks with
                | [] -> return ()
                | tasks -> 
                    do! System.Threading.Tasks.Task.WhenAll(tasks |> List.toArray)
                    return ()
        }