namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Logging
open BrokerMovementExtensions
open TradeExtensions
open DividendExtensions
open DividendTaxExtensions
open OptionTradeExtensions

/// <summary>
/// Batch loader for broker movements to optimize database I/O.
/// Loads all movements for a date range in single queries instead of per-date queries.
/// </summary>
module internal BrokerMovementBatchLoader =

    /// <summary>
    /// Load all movements for account within date range in single query.
    /// This replaces multiple per-date queries with one efficient range query.
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <returns>Task containing all movements within the date range</returns>
    let loadMovementsForDateRange (brokerAccountId: int) (startDate: DateTimePattern) (endDate: DateTimePattern) =
        task {
            // CoreLogger.logDebugf
            //     "BrokerMovementBatchLoader"
            //     "Loading movements for account %d from %s to %s"
            //     brokerAccountId
            //     (startDate.ToString())
            //     (endDate.ToString())

            // Use existing extension methods with date filtering
            let! brokerMovements = BrokerMovementExtensions.Do.getByBrokerAccountIdFromDate (brokerAccountId, startDate)
            let! trades = TradeExtensions.Do.getByBrokerAccountIdFromDate (brokerAccountId, startDate)
            let! dividends = DividendExtensions.Do.getByBrokerAccountIdFromDate (brokerAccountId, startDate)
            let! dividendTaxes = DividendTaxExtensions.Do.getByBrokerAccountIdFromDate (brokerAccountId, startDate)
            let! optionTrades = OptionTradeExtensions.Do.getByBrokerAccountIdFromDate (brokerAccountId, startDate)

            // Filter to the end date
            let filteredBrokerMovements =
                brokerMovements |> List.filter (fun m -> m.TimeStamp.Value <= endDate.Value)

            let filteredTrades =
                trades |> List.filter (fun t -> t.TimeStamp.Value <= endDate.Value)

            let filteredDividends =
                dividends |> List.filter (fun d -> d.TimeStamp.Value <= endDate.Value)

            let filteredDividendTaxes =
                dividendTaxes |> List.filter (fun dt -> dt.TimeStamp.Value <= endDate.Value)

            let filteredOptionTrades =
                optionTrades |> List.filter (fun ot -> ot.TimeStamp.Value <= endDate.Value)

            // CoreLogger.logDebugf
            //     "BrokerMovementBatchLoader"
            //     "Loaded %d broker movements, %d trades, %d dividends, %d dividend taxes, %d option trades"
            //     filteredBrokerMovements.Length
            //     filteredTrades.Length
            //     filteredDividends.Length
            //     filteredDividendTaxes.Length
            //     filteredOptionTrades.Length

            return
                {| BrokerMovements = filteredBrokerMovements
                   Trades = filteredTrades
                   Dividends = filteredDividends
                   DividendTaxes = filteredDividendTaxes
                   OptionTrades = filteredOptionTrades |}
        }

    /// <summary>
    /// Group movements by date for efficient processing.
    /// This creates a map of date -> movements for quick lookup during batch processing.
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="movements">All movements loaded from database</param>
    /// <returns>Map of date to BrokerAccountMovementData</returns>
    let groupMovementsByDate
        (brokerAccountId: int)
        (movements:
            {| BrokerMovements: BrokerMovement list
               Trades: Trade list
               Dividends: Dividend list
               DividendTaxes: DividendTax list
               OptionTrades: OptionTrade list |})
        : Map<DateTimePattern, BrokerAccountMovementData> =

        // Get all unique dates from all movement types
        let allDates =
            [ movements.BrokerMovements
              |> List.map (fun m -> SnapshotManagerUtils.normalizeToStartOfDay m.TimeStamp)
              movements.Trades
              |> List.map (fun t -> SnapshotManagerUtils.normalizeToStartOfDay t.TimeStamp)
              movements.Dividends
              |> List.map (fun d -> SnapshotManagerUtils.normalizeToStartOfDay d.TimeStamp)
              movements.DividendTaxes
              |> List.map (fun dt -> SnapshotManagerUtils.normalizeToStartOfDay dt.TimeStamp)
              movements.OptionTrades
              |> List.map (fun ot -> SnapshotManagerUtils.normalizeToStartOfDay ot.TimeStamp) ]
            |> List.concat
            |> List.distinct
            |> List.sort

        // CoreLogger.logDebugf
        //     "BrokerMovementBatchLoader"
        //     "Grouping movements by date - found %d unique dates"
        //     allDates.Length

        // Group each movement type by date (normalized to start of day)
        let brokerMovementsByDate =
            movements.BrokerMovements
            |> List.groupBy (fun m -> SnapshotManagerUtils.normalizeToStartOfDay m.TimeStamp)
            |> Map.ofList

        let tradesByDate =
            movements.Trades
            |> List.groupBy (fun t -> SnapshotManagerUtils.normalizeToStartOfDay t.TimeStamp)
            |> Map.ofList

        let dividendsByDate =
            movements.Dividends
            |> List.groupBy (fun d -> SnapshotManagerUtils.normalizeToStartOfDay d.TimeStamp)
            |> Map.ofList

        let dividendTaxesByDate =
            movements.DividendTaxes
            |> List.groupBy (fun dt -> SnapshotManagerUtils.normalizeToStartOfDay dt.TimeStamp)
            |> Map.ofList

        let optionTradesByDate =
            movements.OptionTrades
            |> List.groupBy (fun ot -> SnapshotManagerUtils.normalizeToStartOfDay ot.TimeStamp)
            |> Map.ofList

        // Create BrokerAccountMovementData for each date
        allDates
        |> List.map (fun date ->
            let brokerMovs = brokerMovementsByDate.TryFind(date) |> Option.defaultValue []
            let trades = tradesByDate.TryFind(date) |> Option.defaultValue []
            let divs = dividendsByDate.TryFind(date) |> Option.defaultValue []
            let divTaxes = dividendTaxesByDate.TryFind(date) |> Option.defaultValue []

            // CRITICAL FIX: Pass ALL cumulative option trades up to this date (not just today's trades)
            // This allows FIFO matching in calculateOptionsSummary() to find opening trades when processing closing trades
            // For example: on 2025-10-03 with closing trades, need opening trades from 2025-08-25 and 2025-10-01
            let optTrades =
                movements.OptionTrades
                |> List.filter (fun ot ->
                    let otDate = SnapshotManagerUtils.normalizeToStartOfDay ot.TimeStamp
                    otDate.Value <= date.Value)

            let movementData =
                BrokerAccountMovementData.create date brokerAccountId brokerMovs trades divs divTaxes optTrades

            (date, movementData))
        |> Map.ofList
