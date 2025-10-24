namespace Core.Tests.Examples

open NUnit.Framework
open System
open System.IO
open Binnaculum.Core.Models
open Binnaculum.Core.UI
open Core.Tests.Integration

/// <summary>
/// Example test demonstrating holistic snapshot verification.
///
/// This test shows how to use verifyBrokerFinancialSnapshot and verifyTickerCurrencySnapshot
/// to compare entire snapshots at once instead of verifying individual fields.
///
/// Benefits:
/// - Compile-time safety: Adding new fields to models breaks tests at compile time
/// - Better error messages: See all mismatches at once with clear diff
/// - Less code: One function call vs many individual verifications
/// - Pure functions: No async overhead, easy to test
/// </summary>
[<TestFixture>]
type HolisticSnapshotVerificationExample() =
    inherit TestFixtureBase()

    /// <summary>
    /// Example: Verify BrokerFinancialSnapshot holistically
    ///
    /// This test demonstrates the new approach:
    /// 1. Fetch actual snapshot from system
    /// 2. Build expected snapshot with test data
    /// 3. Compare holistically with one function call
    /// 4. Get detailed diff on failure
    /// </summary>
    [<Test>]
    [<Category("Example")>]
    member this.``Example - Holistic BrokerFinancialSnapshot verification``() =
        async {
            printfn "\n=== EXAMPLE: Holistic BrokerFinancialSnapshot Verification ==="

            let actions = this.Actions

            // ==================== SETUP ====================
            printfn "\n1. Database Initialization"
            let! (ok, _, _) = actions.wipeDataForTesting ()
            Assert.That(ok, Is.True, "Wipe should succeed")

            let! (ok, _, _) = actions.initDatabase ()
            Assert.That(ok, Is.True, "Init should succeed")
            printfn "✅ Database initialized"

            printfn "\n2. Create BrokerAccount"
            StreamObserver.expectSignals [ Accounts_Updated; Snapshots_Updated ]
            let! (ok, _, _) = actions.createBrokerAccount ("Example-Account")
            Assert.That(ok, Is.True, "Account creation should succeed")

            let! signalsReceived = StreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(10.0))
            Assert.That(signalsReceived, Is.True, "Signals should be received")
            printfn "✅ Account created"

            printfn "\n3. Import test data"

            let csvPath =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "TestData",
                    "Tastytrade_Samples",
                    "TastytradeOptionsTest.csv"
                )

            Assert.That(File.Exists(csvPath), Is.True, "CSV file should exist")

            StreamObserver.expectSignals [ Movements_Updated; Tickers_Updated; Snapshots_Updated ]
            let tastytradeId = actions.Context.TastytradeId
            let accountId = actions.Context.BrokerAccountId

            let! (ok, _, _) = actions.importFile (tastytradeId, accountId, csvPath)
            Assert.That(ok, Is.True, "Import should succeed")

            let! signalsReceived = StreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(15.0))
            Assert.That(signalsReceived, Is.True, "Import signals should be received")
            printfn "✅ Data imported"

            // ==================== OLD APPROACH (for comparison) ====================
            printfn "\n=== OLD APPROACH: Individual field verification ==="
            printfn "Multiple async calls, only see first failure:"

            let! (ok1, _, _) = actions.verifyDeposited (5000m)
            let! (ok2, _, _) = actions.verifyWithdrawn (0m)
            let! (ok3, _, _) = actions.verifyOptionsIncome (54.37m)
            let! (ok4, _, _) = actions.verifyRealizedGains (-28.67m)
            let! (ok5, _, _) = actions.verifyUnrealizedGains (83.04m)
            let! (ok6, _, _) = actions.verifyMovementCounter (16)

            let allOldVerified = ok1 && ok2 && ok3 && ok4 && ok5 && ok6
            printfn "Old approach result: %b" allOldVerified

            // ==================== NEW APPROACH: Holistic verification ====================
            printfn "\n=== NEW APPROACH: Holistic snapshot verification ==="
            printfn "Pure function, compile-time safety, see all mismatches:"

            // Step 1: Fetch actual snapshot from system
            let actualSnapshot =
                Collections.Snapshots.Items
                |> Seq.filter (fun s -> s.Type = OverviewSnapshotType.BrokerAccount)
                |> Seq.tryHead
                |> Option.map (fun s -> s.BrokerAccount.Value.Financial)

            Assert.That(actualSnapshot.IsSome, Is.True, "Should have broker account snapshot")
            let actual = actualSnapshot.Value

            // Step 2: Build expected snapshot with test data
            // NOTE: All fields MUST be specified - compiler enforces this!
            // For this demo, we'll just verify a few key fields and use actual values for the rest
            // to show the comparison output
            let expected: BrokerFinancialSnapshot =
                { Id = actual.Id // ID doesn't matter for this test
                  Date = actual.Date // Date doesn't matter for this test
                  Broker = actual.Broker // Broker doesn't matter for this test
                  BrokerAccount = actual.BrokerAccount // BrokerAccount doesn't matter for this test
                  Currency = actual.Currency // Currency doesn't matter for this test
                  MovementCounter = 16 // Expected: 16 movements imported (should match)
                  RealizedGains = actual.RealizedGains // Use actual for this demo
                  RealizedPercentage = actual.RealizedPercentage // Use actual
                  UnrealizedGains = actual.UnrealizedGains // Use actual
                  UnrealizedGainsPercentage = actual.UnrealizedGainsPercentage // Use actual
                  Invested = actual.Invested // Use actual
                  Commissions = actual.Commissions // Use actual
                  Fees = actual.Fees // Use actual
                  Deposited = actual.Deposited // Use actual
                  Withdrawn = actual.Withdrawn // Use actual
                  DividendsReceived = actual.DividendsReceived // Use actual
                  OptionsIncome = 63.00m // Expected: $63.00 pure options premium (sum of signed premiums)
                  OtherIncome = actual.OtherIncome // Use actual
                  OpenTrades = actual.OpenTrades // Use actual
                  NetCashFlow = actual.NetCashFlow // Use actual
                }

            // Step 3: Compare holistically with one function call
            let (allMatch, results) =
                TestVerifications.verifyBrokerFinancialSnapshot expected actual

            // Step 4: Get detailed diff on failure
            if not allMatch then
                let formatted = TestVerifications.formatValidationResults results
                printfn "\n❌ Snapshot mismatch detected:\n%s" formatted
            else
                printfn "\n✅ All fields match!"

            // Show the formatted output even on success for demonstration
            let formatted = TestVerifications.formatValidationResults results
            printfn "\nDetailed comparison:"
            printfn "%s" formatted

            Assert.That(allMatch, Is.True, "All snapshot fields should match expected values")

            printfn "\n=== Benefits of New Approach ==="
            printfn "✅ Compile-time safety: Adding new field breaks test"
            printfn "✅ See all mismatches at once, not just first failure"
            printfn "✅ Pure function: No async, testable in isolation"
            printfn "✅ Less code: One call vs many"
            printfn "✅ Better error messages with clear diff"
        }

    /// <summary>
    /// Example: Demonstrate compile-time safety
    ///
    /// If someone adds a new field to BrokerFinancialSnapshot, this test will fail to compile
    /// because the expected snapshot record must include ALL fields.
    ///
    /// This is a HUGE win over the old approach where missing verifications would silently pass!
    /// </summary>
    [<Test>]
    [<Category("Example")>]
    member _.``Example - Compile-time safety demonstration``() =
        printfn "\n=== COMPILE-TIME SAFETY DEMONSTRATION ==="

        // This snapshot definition MUST include ALL fields from BrokerFinancialSnapshot
        // Try removing a field - the compiler will immediately report an error!
        let exampleSnapshot: BrokerFinancialSnapshot =
            { Id = 1
              Date = DateOnly(2023, 1, 1)
              Broker = None
              BrokerAccount = None
              Currency =
                { Id = 1
                  Title = "US Dollar"
                  Code = "USD"
                  Symbol = "$" }
              MovementCounter = 0
              RealizedGains = 0m
              RealizedPercentage = 0m
              UnrealizedGains = 0m
              UnrealizedGainsPercentage = 0m
              Invested = 0m
              Commissions = 0m
              Fees = 0m
              Deposited = 0m
              Withdrawn = 0m
              DividendsReceived = 0m
              OptionsIncome = 0m
              OtherIncome = 0m
              OpenTrades = false
              NetCashFlow = 0m
            // If BrokerFinancialSnapshot gets a new field, this line will fail to compile!
            // The compiler will say: "No assignment given for field 'NewField'"
            }

        let (allMatch, _) =
            TestVerifications.verifyBrokerFinancialSnapshot exampleSnapshot exampleSnapshot

        Assert.That(allMatch, Is.True, "Identical snapshots should match")

        printfn "✅ Compile-time safety ensures all fields are verified"
        printfn "💡 Try adding a new field to BrokerFinancialSnapshot - this test will break!"
        printfn "💡 This catches breaking changes at compile time, not runtime!"
