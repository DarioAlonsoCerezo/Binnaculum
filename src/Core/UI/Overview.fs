namespace Binnaculum.Core.UI

open System.Reactive.Subjects
open Binnaculum.Core.Models
open Binnaculum.Core.Storage
open Binnaculum.Core.Database
open Binnaculum.Core.Providers

module Overview =

    let Data =
        new BehaviorSubject<OverviewUI>(
            { IsDatabaseInitialized = false
              TransactionsLoaded = false }
        )

    let InitDatabase () =
        task {
            // CoreLogger.logDebug "Overview.InitDatabase" "Starting InitDatabase"
            // CoreLogger.logDebug "Overview.InitDatabase" "About to call DataLoader.loadBasicData()"
            do! DataLoader.loadBasicData () |> Async.AwaitTask |> Async.Ignore
            // CoreLogger.logDebug "Overview.InitDatabase" "DataLoader.loadBasicData() completed"

            // CoreLogger.logDebug "Overview.InitDatabase" "About to update Data.OnNext"

            // CoreLogger.logDebug "Overview.InitDatabase" $"Current Data.Value: IsDatabaseInitialized={Data.Value.IsDatabaseInitialized}, TransactionsLoaded={Data.Value.TransactionsLoaded}"

            Data.OnNext
                { Data.Value with
                    IsDatabaseInitialized = true }

            // CoreLogger.logDebug "Overview.InitDatabase" $"After Data.OnNext: IsDatabaseInitialized={Data.Value.IsDatabaseInitialized}, TransactionsLoaded={Data.Value.TransactionsLoaded}"

            // CoreLogger.logDebug "Overview.InitDatabase" "InitDatabase completed successfully"
        }

    let LoadData () =
        task {
            // CoreLogger.logDebug "Overview.LoadData" "Starting LoadData"
            // CoreLogger.logDebug "Overview.LoadData" "About to call DataLoader.initialization()"
            do! DataLoader.initialization () |> Async.AwaitTask |> Async.Ignore
            // CoreLogger.logDebug "Overview.LoadData" "DataLoader.initialization() completed"
            // Use reactive movement manager instead of manual loading
            // CoreLogger.logDebug "Overview.LoadData" "About to call ReactiveMovementManager.refresh()"
            ReactiveMovementManager.refresh ()
            // CoreLogger.logDebug "Overview.LoadData" "ReactiveMovementManager.refresh() completed"

            Data.OnNext
                { Data.Value with
                    TransactionsLoaded = true }

            // CoreLogger.logDebug "Overview.LoadData" "LoadData completed successfully"
        }

    let InitCore () = task {
        do! InitDatabase() |> Async.AwaitTask |> Async.Ignore
        do! LoadData() |> Async.AwaitTask |> Async.Ignore
    }

    /// <summary>
    /// 🚨🚨🚨 WARNING: TEST-ONLY METHOD - NEVER USE IN PRODUCTION CODE! 🚨🚨🚨
    ///
    /// Configures both the database and preferences systems to operate entirely in
    /// memory, enabling comprehensive integration testing without requiring MAUI
    /// platform services or file system access.
    ///
    /// This method MUST be called BEFORE any other initialization methods
    /// (InitDatabase, LoadData) to ensure the in-memory configuration is active.
    ///
    /// ✅ WHEN TO USE:
    /// - Integration tests that need fresh database + preferences state
    /// - Headless/CI environments without MAUI platform services
    /// - Testing import workflows and preference persistence
    /// - Parallel test execution (each test gets isolated in-memory storage)
    ///
    /// ❌ NEVER USE IN:
    /// - Production code or any production-like environments
    /// - Tests that need persistent state across app restarts
    /// - Scenarios requiring real platform API behavior
    ///
    /// USAGE EXAMPLE:
    /// ```fsharp
    /// test "Import and verify with preferences" {
    ///     // Configure in-memory mode FIRST
    ///     Overview.WorkOnMemory()
    ///
    ///     // Initialize system
    ///     do! Overview.InitDatabase()
    ///     do! Overview.LoadData()
    ///
    ///     // Configure preferences
    ///     SavedPrefereces.ChangeLanguage("es")
    ///     SavedPrefereces.ChangeCurrency("EUR")
    ///
    ///     // Run test logic
    ///     let! result = ImportCsv(testFile)
    ///
    ///     // Verify results (all in-memory, isolated, fast)
    ///     Assert.That(Collections.Accounts.Items.Count, Is.GreaterThan(0))
    ///     Assert.That(SavedPrefereces.UserPreferences.Value.Language, Is.EqualTo("es"))
    /// }
    /// ```
    ///
    /// ISOLATION & CLEANUP:
    /// Each call to WorkOnMemory() creates fresh in-memory storage.
    /// Use WipeAllDataForTesting() between test scenarios to reset while
    /// maintaining in-memory mode.
    /// </summary>
    let WorkOnMemory () =
        Do.setConnectionMode (DatabaseMode.InMemory "test_memory_db")
        SavedPrefereces.setPreferencesMode PreferencesMode.InMemory

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

            // Clear in-memory preferences storage if operating in in-memory mode
            SavedPrefereces.clearInMemoryPreferences ()

            // Reset the Overview.Data state to initial values
            Data.OnNext
                { IsDatabaseInitialized = false
                  TransactionsLoaded = false }
        }
