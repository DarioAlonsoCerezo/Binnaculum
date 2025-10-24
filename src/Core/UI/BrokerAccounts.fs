namespace Binnaculum.Core.UI

open System
open System.Threading.Tasks
open Binnaculum.Core.Database
open BrokerAccountSnapshotExtensions
open BrokerFinancialSnapshotExtensions
open AutoImportOperationExtensions
open Binnaculum.Core.Models
open Binnaculum.Core.DatabaseToModels
open Binnaculum.Core.Keys
open Binnaculum.Core.Patterns

/// <summary>
/// This module provides the public API for all BrokerAccount-related operations accessible from the UI layer.
/// It follows the established patterns of database access, model conversion, and follows the project's
/// error handling conventions (let exceptions bubble up to the UI layer).
/// </summary>
module BrokerAccounts =

    /// <summary>
    /// Retrieves all snapshots for a specific BrokerAccount.
    /// Takes a brokerAccountId, queries the database for all snapshots associated with that account,
    /// and converts them to OverviewSnapshot models for UI consumption.
    /// Follows project conventions for error handling - exceptions bubble up to UI layer.
    /// </summary>
    /// <param name="brokerAccountId">The ID of the BrokerAccount to retrieve snapshots for</param>
    /// <returns>A list of OverviewSnapshot records representing all snapshots for the broker account</returns>
    let GetSnapshots (brokerAccountId: int) =
        task {
            // Get all broker account snapshots from database
            let! brokerAccountSnapshots =
                BrokerAccountSnapshotExtensions.Do.getByBrokerAccountId (brokerAccountId)
                |> Async.AwaitTask

            // Get the broker account model using fast lookup
            let brokerAccount = brokerAccountId.ToFastBrokerAccountById()

            // Convert each database snapshot to OverviewSnapshot
            let overviewSnapshotTasks =
                brokerAccountSnapshots
                |> List.map (fun dbSnapshot ->
                    task {
                        // Get related financial snapshots for this broker account snapshot
                        let! financialSnapshots =
                            BrokerFinancialSnapshotExtensions.Do.getByBrokerAccountIdAndDate (
                                brokerAccountId,
                                dbSnapshot.Base.Date
                            )
                            |> Async.AwaitTask

                        // Convert to OverviewSnapshot using existing conversion function
                        return dbSnapshot.brokerAccountSnapshotToOverviewSnapshot (financialSnapshots, brokerAccount)
                    })

            let! overviewSnapshots = Task.WhenAll(overviewSnapshotTasks |> List.toArray)
            return overviewSnapshots |> Array.toList
        }

    /// <summary>
    /// Retrieves all BrokerFinancialSnapshots for a specific BrokerAccountSnapshot.
    /// Takes a broker account snapshot (containing ID, date, and broker account ID),
    /// queries the database for all related financial snapshots, and converts them to
    /// BrokerFinancialSnapshot models for UI consumption.
    /// Follows project conventions for error handling - exceptions bubble up to UI layer.
    /// </summary>
    /// <param name="brokerAccountSnapshotId">The ID of the BrokerAccountSnapshot</param>
    /// <param name="date">The date of the snapshot</param>
    /// <param name="brokerAccountId">The ID of the BrokerAccount</param>
    /// <returns>A list of BrokerFinancialSnapshot records for the broker account snapshot</returns>
    let GetFinancialSnapshotsForAccountSnapshot (brokerAccountSnapshotId: int, date: DateOnly, brokerAccountId: int) =
        task {
            // Convert DateOnly to DateTimePattern
            let dateTimePattern =
                DateTimePattern.FromDateTime(date.ToDateTime(TimeOnly.MinValue))

            // Get the broker account model using fast lookup
            let brokerAccount = brokerAccountId.ToFastBrokerAccountById()

            // Get all financial snapshots for this broker account snapshot
            let! dbFinancialSnapshots =
                BrokerFinancialSnapshotExtensions.Do.getAllByBrokerAccountIdBrokerAccountSnapshotIdAndDate (
                    brokerAccountId,
                    brokerAccountSnapshotId,
                    dateTimePattern
                )
                |> Async.AwaitTask

            // Convert database snapshots to domain models using the helper extension method
            let financialSnapshots =
                dbFinancialSnapshots
                |> List.map (fun dbFinancial ->
                    dbFinancial.brokerFinancialSnapshotToModel (brokerAccount = brokerAccount))

            return financialSnapshots
        }

    /// <summary>
    /// Retrieves all AutoImportOperations for a specific BrokerAccount.
    /// Takes a brokerAccountId, queries the database for all operations associated with that account,
    /// and converts them to AutoImportOperation models for UI consumption.
    /// Follows project conventions for error handling - exceptions bubble up to UI layer.
    /// </summary>
    /// <param name="brokerAccountId">The ID of the BrokerAccount to retrieve operations for</param>
    /// <returns>A list of AutoImportOperation records representing all operations for the broker account</returns>
    let GetOperations (brokerAccountId: int) =
        task {
            // Get all operations from database for this broker account
            let! dbOperations =
                AutoImportOperationExtensions.Do.getByBrokerAccount (brokerAccountId)
                |> Async.AwaitTask

            // Convert database operations to domain models
            let operations = dbOperations.autoImportOperationsToModel ()

            return operations
        }
