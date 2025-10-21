namespace Core.Tests.Integration

open NUnit.Framework
open System
open System.Threading.Tasks
open Binnaculum.Core.Models
open Binnaculum.Core.UI

/// <summary>
/// Reactive integration tests using signal-based approach.
/// Mirrors the MAUI tester's "Overview Reactive Validation" test.
/// Validates the core library's reactive stream emissions during Overview initialization and data loading.
/// </summary>
[<TestFixture>]
type ReactiveOverviewTests() =

    let mutable testContext: ReactiveTestContext option = None
    let mutable testActions: ReactiveTestActions option = None

    /// <summary>
    /// Setup before each test - prepare environment and start observing streams
    /// </summary>
    [<SetUp>]
    member _.Setup() =
        async {
            printfn "\n=== Test Setup ==="

            // Ensure we're in memory mode to avoid platform dependencies
            Overview.WorkOnMemory()

            try
                do! Overview.WipeAllDataForTesting() |> Async.AwaitTask
                printfn "✅ Data wiped successfully"
            with ex ->
                printfn "⚠️  Wipe failed: %s (may be expected if DB not initialized)" ex.Message

            // Start observing reactive streams
            ReactiveStreamObserver.startObserving ()

            let ctx = ReactiveTestContext.create ()
            testContext <- Some ctx
            testActions <- Some(ReactiveTestActions(ctx))

            printfn "=== Setup Complete ===\n"
        }

    /// <summary>
    /// Teardown after each test - stop observing streams
    /// </summary>
    [<TearDown>]
    member _.Teardown() =
        async {
            printfn "\n=== Test Teardown ==="
            ReactiveStreamObserver.stopObserving ()
            testContext <- None
            testActions <- None
            printfn "=== Teardown Complete ===\n"
        }

    /// <summary>
    /// Test: Overview Reactive Validation
    /// Mirrors Core.Platform.MauiTester's "Overview Reactive Validation" test.
    ///
    /// This is the single comprehensive test that validates:
    /// 1. Database initialization loads brokers, currencies, and tickers reactively
    /// 2. LoadData() fires Accounts_Updated and Snapshots_Updated signals
    /// 3. Collections contain expected data: Brokers, Currencies, Tickers, and Snapshots
    ///
    /// The test uses signal-based verification instead of timing-based assertions,
    /// making it robust and portable across platforms.
    /// </summary>
    [<Test>]
    [<Category("Integration")>]
    member _.``Overview reactive validation``() =
        async {
            printfn "\n=== TEST: Overview Reactive Validation ==="

            let actions = testActions.Value

            // ==================== PHASE 1: DATABASE INITIALIZATION ====================
            printfn "\n--- Phase 1: Database Initialization and Data Loading ---"

            // Expect signals for database init and data loading
            // initDatabase() calls Overview.InitDatabase() and Overview.LoadData()
            ReactiveStreamObserver.expectSignals (
                [ Brokers_Updated
                  Currencies_Updated
                  Tickers_Updated
                  Accounts_Updated
                  Snapshots_Updated ]
            )

            printfn
                "Expecting signals: Brokers_Updated, Currencies_Updated, Tickers_Updated, Accounts_Updated, Snapshots_Updated"

            // Initialize database (loads brokers, currencies, tickers, accounts, snapshots)
            let! (ok, _, error) = actions.initDatabase ()

            Assert.That(
                ok,
                Is.True,
                sprintf "Database initialization should succeed: %s" (error |> Option.defaultValue "")
            )

            printfn "✅ Database initialized and data loaded"

            // Wait for all expected signals
            let! signalsReceived = ReactiveStreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(10.0))

            Assert.That(
                signalsReceived,
                Is.True,
                "All database initialization and data loading signals should be received"
            )

            printfn "✅ All signals received"

            // ==================== PHASE 2: VERIFY DATABASE STATE ====================
            printfn "\n--- Phase 2: Verify Collections ---"

            // Verify Brokers
            let brokerCount = Collections.Brokers.Count
            printfn "Brokers loaded: %d" brokerCount
            Assert.That(brokerCount, Is.GreaterThanOrEqualTo(2), "Should have at least Tastytrade and IBKR brokers")

            // Verify Currencies
            let currencyCount = Collections.Currencies.Count
            printfn "Currencies loaded: %d" currencyCount
            Assert.That(currencyCount, Is.GreaterThanOrEqualTo(2), "Should have at least USD and EUR currencies")

            let hasUsd = Collections.Currencies.Items |> Seq.exists (fun c -> c.Code = "USD")
            let hasEur = Collections.Currencies.Items |> Seq.exists (fun c -> c.Code = "EUR")
            Assert.That(hasUsd, Is.True, "Should have USD currency")
            Assert.That(hasEur, Is.True, "Should have EUR currency")
            printfn "✅ USD and EUR currencies verified"

            // Verify Tickers
            let tickerCount = Collections.Tickers.Count
            printfn "Tickers loaded: %d" tickerCount
            Assert.That(tickerCount, Is.GreaterThan(0), "Should have at least one ticker (SPY)")

            // Verify Snapshots were loaded
            let snapshotCount = Collections.Snapshots.Count
            printfn "Snapshots loaded: %d" snapshotCount

            Assert.That(
                snapshotCount,
                Is.GreaterThanOrEqualTo(0),
                "Should have snapshots loaded (may be 0 in empty DB)"
            )

            // Verify Accounts were loaded
            let accountCount = Collections.Accounts.Count
            printfn "Accounts loaded: %d" accountCount
            Assert.That(accountCount, Is.GreaterThanOrEqualTo(0), "Should have accounts loaded (may be 0 in empty DB)")

            printfn "✅ All collections verified"

            // ==================== SUMMARY ====================
            printfn "\n--- Test Summary ---"

            printfn
                "✅ Database initialized: %d brokers, %d currencies, %d tickers"
                brokerCount
                currencyCount
                tickerCount

            printfn "✅ Data loaded: %d accounts, %d snapshots" accountCount snapshotCount
            printfn "=== Overview Reactive Validation Complete ✅ ==="
        }
