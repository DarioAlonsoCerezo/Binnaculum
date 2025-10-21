/// <summary>
/// REACTIVE INTEGRATION TEST PATTERN - Implementation Guide
/// ========================================================
///
/// This document explains the reactive integration testing pattern used in Core.Tests.
/// It mirrors the Core.Platform.MauiTester approach and enables signal-based testing
/// for headless environments.
///
/// QUICK START FOR DEVELOPERS
/// ==========================
///
/// 1. Create a new test file: ReactiveBrokerAccountTests.fs
/// 2. Inherit from ReactiveTestFixtureBase
/// 3. Write test methods using the provided actions and verifications
/// 4. Run with: dotnet test --filter "Category=Integration"
///
/// PATTERN OVERVIEW
/// ================
///
/// Instead of brittle timing-based tests (Thread.Sleep), we use signal-based verification:
///
///     OLD (DON'T):
///     -----------
///     let! result = actions.createBrokerAccount("Test")
///     Thread.Sleep(1000)  // ❌ Fragile timing
///     Assert.That(Collections.Accounts.Count, Is.EqualTo(1))
///
///     NEW (DO):
///     --------
///     ReactiveStreamObserver.expectSignals([ Accounts_Updated ])
///     let! result = actions.createBrokerAccount("Test")
///     let! signalsReceived = ReactiveStreamObserver.waitForAllSignalsAsync(...)
///     Assert.That(signalsReceived, Is.True)  // ✅ Signal-based waiting
///
/// WHY THIS MATTERS
/// ================
///
/// 1. DETERMINISTIC: Tests don't rely on arbitrary timeouts
/// 2. FAST: Signal-based tests complete in ~100-200ms vs seconds with delays
/// 3. RELIABLE: Works in CI, on slow/fast machines, headless environments
/// 4. HEADLESS-COMPATIBLE: No UI/platform dependencies needed
/// 5. REUSABLE: Same pattern works everywhere
///
/// ARCHITECTURE
/// =============
///
/// Integration Tests Directory Structure:
///
///     Integration/
///     ├── ReactiveTestEnvironment.fs          (Environment detection)
///     ├── ReactiveTestContext.fs              (Test state container)
///     ├── ReactiveStreamObserver.fs           (Signal monitoring)
///     ├── ReactiveTestActions.fs              (Test operations)
///     ├── ReactiveTestVerifications.fs        (Assertion helpers)
///     ├── ReactiveTestSetup.fs                (Setup/teardown utilities)
///     ├── ReactiveTestFixtureBase.fs          (Base class for tests)
///     ├── ReactiveOverviewTests.fs            (Example: Overview test)
///     ├── ReactiveBrokerAccountTests.fs       (Example: BrokerAccount test)
///     └── README.md                           (Full documentation)
///
/// KEY CONCEPTS
/// ============
///
/// 1. REACTIVE SIGNAL
///    A signal represents a specific reactive stream event:
///    - Brokers_Updated: Collections.Brokers.Items changed
///    - Currencies_Updated: Collections.Currencies.Items changed
///    - Tickers_Updated: Collections.Tickers.Items changed
///    - Accounts_Updated: Collections.Accounts.Items changed
///    - Snapshots_Updated: Collections.Snapshots.Items changed
///    - Movements_Updated: Collections.Movements.Items changed
///    - Banks_Updated: Collections.Banks.Items changed
///
/// 2. SIGNAL WAITING
///    Instead of Thread.Sleep(), we wait for signals:
///
///    ReactiveStreamObserver.expectSignals([ Accounts_Updated ])
///    let! (ok, _) = actions.createBrokerAccount("Test")
///    let! received = ReactiveStreamObserver.waitForAllSignalsAsync(timeout)
///    Assert.That(received, Is.True)
///
/// 3. INMEMORY MODE
///    All tests run with InMemory database to avoid platform dependencies
///    (No filesystem, no platform-specific paths, works anywhere)
///
/// IMPLEMENTATION STEPS
/// ====================
///
/// Step 1: Create Test Class
/// -------------------------
/// Inherit from ReactiveTestFixtureBase - it provides:
/// - this.Actions: Access to test operations
/// - this.Context: Access to test context
/// - Automatic Setup/Teardown with signal observation
///
///     [<TestFixture>]
///     type MyReactiveTests() =
///         inherit ReactiveTestFixtureBase()
///
/// Step 2: Write Test Method
/// -------------------------
/// Follow the Setup/Expect/Execute/Wait/Verify pattern:
///
///     [<Test>]
///     [<Category("Integration")>]
///     member this.``My test``() =
///         async {
///             let actions = this.Actions
///
///             // SETUP: Initialize state
///             let! (ok, _, _) = actions.initDatabase()
///             Assert.That(ok, Is.True)
///
///             // EXPECT: Declare signals before operation
///             ReactiveStreamObserver.expectSignals([ Accounts_Updated ])
///
///             // EXECUTE: Perform operation
///             let! (ok, details) = actions.createBrokerAccount("Test")
///             Assert.That(ok, Is.True)
///
///             // WAIT: Wait for signals (not arbitrary delays!)
///             let! signalsReceived = ReactiveStreamObserver.waitForAllSignalsAsync(
///                 TimeSpan.FromSeconds(10.0)
///             )
///             Assert.That(signalsReceived, Is.True)
///
///             // VERIFY: Verify outcome
///             let! (verified, count, _) = actions.verifyAccountCount(1)
///             Assert.That(verified, Is.True)
///         }
///
/// Step 3: Use Available Actions
/// ----------------------------
/// ReactiveTestActions provides common operations:
///
///     - initDatabase() : Async<bool * string * string option>
///       Initializes database, loads brokers/currencies/tickers
///       Signals: Brokers_Updated, Currencies_Updated, Tickers_Updated
///
///     - createBrokerAccount(name) : Async<bool * string * string option>
///       Creates a BrokerAccount with given name
///       Signals: Accounts_Updated, Snapshots_Updated
///
///     - verifyAccountCount(expected) : Async<bool * string * string option>
///       Verifies account count matches expected
///
/// Step 4: Use Available Verifications
/// -----------------------------------
/// ReactiveTestVerifications provides assertion helpers:
///
///     - verifyBrokers(minCount)
///     - verifyCurrencies(minCount)
///     - verifyTickers(minCount)
///     - verifySnapshots(minCount)
///     - verifyAccounts(minCount)
///     - verifyStandardCurrencies()  // USD and EUR present
///     - verifyFullDatabaseState()   // Run all verifications
///
/// Usage:
///     let verifications = ReactiveTestVerifications.verifyFullDatabaseState()
///     for (success, message) in verifications do
///         Assert.That(success, Is.True, message)
///
/// SIGNAL CHAIN EXAMPLE
/// ====================
///
/// When you create a BrokerAccount, here's the signal flow:
///
///     Timeline:
///     ─────────────────────────────────────────────────
///
///      0ms  actions.createBrokerAccount("Test")
///           │
///      5ms  ├─ Creator.SaveBrokerAccount() called
///           │
///      8ms  ├─ Collections.Accounts changed
///           │     └─ signalReceived(Accounts_Updated)   ← SIGNAL #1
///           │
///     15ms  ├─ ReactiveSnapshotManager calculates snapshot
///           │
///     18ms  ├─ Collections.Snapshots changed
///           │     └─ signalReceived(Snapshots_Updated)  ← SIGNAL #2
///           │
///     20ms  └─ Both signals received!
///                TaskCompletionSource.SetResult(true)
///
///     22ms  WaitForAllSignalsAsync() returns true
///           Test continues...
///
/// TEST ISOLATION
/// ==============
///
/// Each test is isolated:
/// 1. Setup: InMemory database + fresh context
/// 2. Execution: Test runs with clean state
/// 3. Teardown: Stream observation stopped, resources cleaned
///
/// Tests don't interfere with each other.
///
/// EXAMPLE: BROKER ACCOUNT TEST
/// ============================
///
/// Here's a complete example for issue #388:
///
///     [<TestFixture>]
///     type ReactiveBrokerAccountTests() =
///         inherit ReactiveTestFixtureBase()
///
///         [<Test>]
///         [<Category("Integration")>]
///         member this.``BrokerAccount creation updates collections``() =
///             async {
///                 let actions = this.Actions
///
///                 // Initialize database
///                 let! (ok, _, _) = actions.initDatabase()
///                 Assert.That(ok, Is.True, "Database init should succeed")
///
///                 // Expect signals
///                 ReactiveStreamObserver.expectSignals([
///                     Accounts_Updated
///                     Snapshots_Updated
///                 ])
///
///                 // Create account
///                 let! (ok, details) = actions.createBrokerAccount("TestAccount")
///                 Assert.That(ok, Is.True, $"Account creation should succeed: {details}")
///
///                 // Wait for signals
///                 let! signalsReceived =
///                     ReactiveStreamObserver.waitForAllSignalsAsync(TimeSpan.FromSeconds(10.0))
///                 Assert.That(signalsReceived, Is.True, "Should receive both signals")
///
///                 // Verify account count
///                 let! (verified, count, _) = actions.verifyAccountCount(1)
///                 Assert.That(verified, Is.True, "Account count should be 1")
///             }
///
/// ADDING NEW TESTS
/// ================
///
/// To add a new reactive test following this pattern:
///
/// 1. Create: ReactiveBrokerAccountDepositTests.fs
/// 2. Inherit: from ReactiveTestFixtureBase
/// 3. Add method to ReactiveTestActions for your operation
/// 4. Add verification method to ReactiveTestActions
/// 5. Write test using Setup/Expect/Execute/Wait/Verify pattern
/// 6. Add signals expected in your scenario
/// 7. Run: dotnet test --filter "Category=Integration"
///
/// COMMON PATTERNS
/// ===============
///
/// Pattern 1: Verify Signal Received
/// ----------------------------------
///     ReactiveStreamObserver.expectSignals([ Accounts_Updated ])
///     let! (ok, _) = actions.createBrokerAccount("Test")
///     let! received = ReactiveStreamObserver.waitForAllSignalsAsync(timeout)
///     Assert.That(received, Is.True, "Signal should be received")
///
/// Pattern 2: Verify Collection Updated
/// ------------------------------------
///     let (success, count, _) = ReactiveTestVerifications.verifyAccounts(1)
///     Assert.That(success, Is.True, count)
///
/// Pattern 3: Run All Verifications
/// --------------------------------
///     let verifications = ReactiveTestVerifications.verifyFullDatabaseState()
///     for (success, message) in verifications do
///         Assert.That(success, Is.True, message)
///         printfn "✅ %s" message
///
/// Pattern 4: Print Phase Headers
/// -------------------------------
///     ReactiveTestSetup.printPhaseHeader 1 "Database Initialization"
///     let! result = actions.initDatabase()
///     ReactiveTestSetup.printPhaseHeader 2 "Account Creation"
///     // ...
///
/// TROUBLESHOOTING
/// ===============
///
/// Q: My test times out waiting for signals
/// A: Check that your action actually triggers the expected signal
///    - Verify the operation modifies Collections correctly
///    - Check ReactiveStreamObserver is started (inherited in base class)
///    - Ensure you're expecting the right signals
///
/// Q: Test passes locally but fails in CI
/// A: This shouldn't happen with signal-based tests!
///    - But if it does, check InMemory database initialization
///    - Ensure no platform-specific code is used
///    - Review signal timing in different CI environment
///
/// Q: How do I debug signal flow?
/// A: ReactiveStreamObserver logs signal reception:
///    - Watch output for "[ReactiveStreamObserver] Signal received"
///    - Check expected vs received signals in test output
///
/// PERFORMANCE
/// ===========
///
/// Signal-based tests are FAST:
/// - Overview test: ~180ms (includes all signals + verifications)
/// - No artificial waits
/// - Scales linearly with operation count, not with arbitrary sleeps
///
/// KEY RULES
/// =========
///
/// ✅ DO:
///    - Inherit from ReactiveTestFixtureBase
///    - Use signal-based waiting
///    - Follow Setup/Expect/Execute/Wait/Verify pattern
///    - Add methods to ReactiveTestActions for new operations
///    - Test in headless mode (no platform deps)
///    - Run tests in CI
///
/// ❌ DON'T:
///    - Use Thread.Sleep() or arbitrary delays
///    - Access UI components directly
///    - Rely on timing/ordering
///    - Create multiple test contexts in one test
///    - Mix async/sync without proper handling
///
/// RUNNING TESTS
/// =============
///
/// Run all integration tests:
///     dotnet test src/Tests/Core.Tests/Core.Tests.fsproj --filter "Category=Integration"
///
/// Run specific test:
///     dotnet test src/Tests/Core.Tests/Core.Tests.fsproj --filter "Name~ReactiveOverviewTests"
///
/// Run with verbose output:
///     dotnet test src/Tests/Core.Tests/Core.Tests.fsproj -v normal
///
/// REFERENCES
/// ==========
///
/// - Core.Platform.MauiTester: Reference implementation in C#
/// - Issue #386: Infrastructure foundation (ReactiveTestSetup, etc.)
/// - Issue #388: BrokerAccount test example
/// - This file: Implementation guide
///
/// SUPPORT
/// =======
///
/// For questions on this pattern:
/// 1. Check README.md in this directory
/// 2. Review ReactiveOverviewTests.fs for example
/// 3. Check issue #386 for infrastructure details
/// 4. Review Core.Platform.MauiTester for MAUI test patterns
///
/// When adding new tests, follow the pattern above and you should be good!
/// The base class and utilities handle all the plumbing.
///
/// Happy testing! 🚀
/// </summary>
module ReactiveIntegrationTestingPattern
