namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Logging
open BrokerFinancialSnapshotExtensions

/// <summary>
/// Batch loader for broker financial snapshots to optimize database I/O.
/// Loads all snapshots before a date in single queries instead of per-currency queries.
/// </summary>
module internal BrokerFinancialSnapshotBatchLoader =

    /// <summary>
    /// Load all snapshots before start date to establish baseline.
    /// Returns snapshots grouped by currency for efficient lookup.
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="startDate">Start date (snapshots before this date are loaded)</param>
    /// <returns>Task containing map of currency ID to list of snapshots</returns>
    let loadBaselineSnapshots (brokerAccountId: int) (startDate: DateTimePattern) =
        task {
            CoreLogger.logDebugf
                "BrokerFinancialSnapshotBatchLoader"
                "Loading baseline snapshots for account %d before %s"
                brokerAccountId
                (startDate.ToString())

            // Get all snapshots for this account
            let! allSnapshots = BrokerFinancialSnapshotExtensions.Do.getByBrokerAccountId (brokerAccountId)

            // Filter to dates before start date
            let baselineSnapshots =
                allSnapshots |> List.filter (fun s -> s.Base.Date.Value < startDate.Value)

            CoreLogger.logDebugf
                "BrokerFinancialSnapshotBatchLoader"
                "Found %d baseline snapshots (from %d total snapshots)"
                baselineSnapshots.Length
                allSnapshots.Length

            // Group by currency and take the latest snapshot for each
            let snapshotsByCurrency =
                baselineSnapshots
                |> List.groupBy (fun s -> s.CurrencyId)
                |> List.map (fun (currencyId, snapshots) ->
                    let latestSnapshot =
                        snapshots |> List.sortByDescending (fun s -> s.Base.Date.Value) |> List.head

                    (currencyId, latestSnapshot))
                |> Map.ofList

            CoreLogger.logDebugf
                "BrokerFinancialSnapshotBatchLoader"
                "Grouped baseline snapshots into %d currencies"
                snapshotsByCurrency.Count

            return snapshotsByCurrency
        }

    /// <summary>
    /// Load existing snapshots for a specific date range.
    /// This is used to detect if snapshots already exist before creating new ones.
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <returns>Task containing map of (date, currency) to snapshot</returns>
    let loadExistingSnapshotsInRange (brokerAccountId: int) (startDate: DateTimePattern) (endDate: DateTimePattern) =
        task {
            CoreLogger.logDebugf
                "BrokerFinancialSnapshotBatchLoader"
                "Loading existing snapshots for account %d from %s to %s"
                brokerAccountId
                (startDate.ToString())
                (endDate.ToString())

            // Get all snapshots for this account
            let! allSnapshots = BrokerFinancialSnapshotExtensions.Do.getByBrokerAccountId (brokerAccountId)

            // Filter to date range
            let rangeSnapshots =
                allSnapshots
                |> List.filter (fun s -> s.Base.Date.Value >= startDate.Value && s.Base.Date.Value <= endDate.Value)

            CoreLogger.logDebugf
                "BrokerFinancialSnapshotBatchLoader"
                "Found %d existing snapshots in range"
                rangeSnapshots.Length

            // Create lookup by (date, currency)
            let snapshotLookup =
                rangeSnapshots
                |> List.map (fun s -> ((s.Base.Date, s.CurrencyId), s))
                |> Map.ofList

            return snapshotLookup
        }
