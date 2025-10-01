namespace Binnaculum.Core.UI

open System.Reactive.Subjects
open System.Diagnostics
open Binnaculum.Core.Models
open Binnaculum.Core.Storage
open Binnaculum.Core.Database

module Overview =

    let Data =
        new BehaviorSubject<OverviewUI>(
            { IsDatabaseInitialized = false
              TransactionsLoaded = false }
        )

    let InitDatabase () =
        task {
            Debug.WriteLine("[Overview.InitDatabase] Starting InitDatabase")
            Debug.WriteLine("[Overview.InitDatabase] About to call DataLoader.loadBasicData()")
            do! DataLoader.loadBasicData () |> Async.AwaitTask |> Async.Ignore
            Debug.WriteLine("[Overview.InitDatabase] DataLoader.loadBasicData() completed")

            Debug.WriteLine("[Overview.InitDatabase] About to update Data.OnNext")

            Debug.WriteLine(
                $"[Overview.InitDatabase] Current Data.Value: IsDatabaseInitialized={Data.Value.IsDatabaseInitialized}, TransactionsLoaded={Data.Value.TransactionsLoaded}"
            )

            Data.OnNext
                { Data.Value with
                    IsDatabaseInitialized = true }

            Debug.WriteLine(
                $"[Overview.InitDatabase] After Data.OnNext: IsDatabaseInitialized={Data.Value.IsDatabaseInitialized}, TransactionsLoaded={Data.Value.TransactionsLoaded}"
            )

            Debug.WriteLine("[Overview.InitDatabase] InitDatabase completed successfully")
        }

    let LoadData () =
        task {
            Debug.WriteLine("[Overview.LoadData] Starting LoadData")
            Debug.WriteLine("[Overview.LoadData] About to call DataLoader.initialization()")
            do! DataLoader.initialization () |> Async.AwaitTask |> Async.Ignore
            Debug.WriteLine("[Overview.LoadData] DataLoader.initialization() completed")
            // Use reactive movement manager instead of manual loading
            Debug.WriteLine("[Overview.LoadData] About to call ReactiveMovementManager.refresh()")
            ReactiveMovementManager.refresh ()
            Debug.WriteLine("[Overview.LoadData] ReactiveMovementManager.refresh() completed")

            Data.OnNext
                { Data.Value with
                    TransactionsLoaded = true }

            Debug.WriteLine("[Overview.LoadData] LoadData completed successfully")
        }

    let LoadMovements () =
        // Use reactive movement manager instead of manual DataLoader
        ReactiveMovementManager.refresh ()
        System.Threading.Tasks.Task.CompletedTask

    let RefreshSnapshots () =
        // Use reactive snapshot manager instead of manual DataLoader
        ReactiveSnapshotManager.refresh ()
        System.Threading.Tasks.Task.CompletedTask

    /// <summary>
    /// 🚨🚨🚨 WARNING: TEST-ONLY METHOD - NEVER USE IN PRODUCTION CODE! 🚨🚨🚨
    ///
    /// Wipes all data from the database and clears all in-memory collections,
    /// intended strictly for testing purposes. This allows tests to reset the
    /// application state and re-run initialization logic as if the app was
    /// freshly installed.
    ///
    /// ⚠️ THIS METHOD PERMANENTLY DELETES ALL DATA - USE WITH EXTREME CAUTION! ⚠️
    ///
    /// Usage scenario: After calling this method, InitDatabase() and LoadData()
    /// should work as if the app is running for the first time.
    /// </summary>
    let WipeAllDataForTesting () =
        task {
            // Wipe all database tables
            do! Do.wipeAllTablesForTesting () |> Async.AwaitTask |> Async.Ignore

            // Clear all in-memory collections
            Collections.clearAllCollectionsForTesting ()

            // Reset the Overview.Data state to initial values
            Data.OnNext
                { IsDatabaseInitialized = false
                  TransactionsLoaded = false }
        }
