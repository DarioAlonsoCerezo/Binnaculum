using Binnaculum.Core.UI;
using Core.Platform.MauiTester.Models;
using System.Diagnostics;
using System.Linq;
using Microsoft.FSharp.Core;
using static Binnaculum.Core.Models;

namespace Core.Platform.MauiTester.Services
{
    /// <summary>
    /// Centralized verification utilities for Core Platform tests
    /// Groups related verification logic and makes it reusable across test scenarios
    /// </summary>
    public static class TestVerifications
    {
        #region Database and Data State Verifications

        /// <summary>
        /// Verify that the database has been properly initialized
        /// </summary>
        public static (bool success, string details, string error) VerifyDatabaseInitialized()
        {
            return Overview.Data.Value.IsDatabaseInitialized
                ? (true, "Database initialized: True", "")
                : (false, "", "Database should be initialized but state shows false");
        }

        /// <summary>
        /// Verify that data has been loaded from the database
        /// </summary>
        public static (bool success, string details, string error) VerifyDataLoaded()
        {
            return Overview.Data.Value.TransactionsLoaded
                ? (true, "Data loaded: True", "")
                : (false, "", "Data should be loaded but state shows false");
        }

        #endregion

        #region Collection Verifications

        /// <summary>
        /// Verify that the currencies collection has been populated
        /// </summary>
        public static (bool success, string details, string error) VerifyCurrenciesCollection()
        {
            var currencyCount = Collections.Currencies.Items.Count;
            return currencyCount > 0
                ? (true, $"Currencies: {currencyCount}", "")
                : (false, "", "Currencies collection should not be empty after LoadData");
        }

        /// <summary>
        /// Verify that USD currency exists in the collection
        /// </summary>
        public static (bool success, string details, string error) VerifyUsdCurrency()
        {
            return Collections.Currencies.Items.Any(c => c.Code == "USD")
                ? (true, "USD Found: True", "")
                : (false, "", "Should contain USD currency");
        }

        /// <summary>
        /// Verify that the brokers collection has the expected minimum count
        /// </summary>
        public static (bool success, string details, string error) VerifyBrokersCollection()
        {
            var brokerCount = Collections.Brokers.Items.Count;
            return brokerCount >= 2
                ? (true, $"Brokers: {brokerCount}", "")
                : (false, "", $"Expected at least 2 brokers but found {brokerCount}");
        }

        /// <summary>
        /// Verify that Interactive Brokers (IBKR) exists in the brokers collection
        /// </summary>
        public static (bool success, string details, string error) VerifyIbkrBroker()
        {
            return Collections.Brokers.Items.Any(b => b.Name == "Interactive Brokers")
                ? (true, "IBKR Found: True", "")
                : (false, "", "Should contain IBKR broker (Interactive Brokers)");
        }

        /// <summary>
        /// Verify that Tastytrade broker exists in the collection
        /// </summary>
        public static (bool success, string details, string error) VerifyTastytradeBroker()
        {
            return Collections.Brokers.Items.Any(b => b.Name == "Tastytrade")
                ? (true, "Tastytrade Found: True", "")
                : (false, "", "Should contain Tastytrade broker (Tastytrade)");
        }

        /// <summary>
        /// Verify that SPY ticker exists in the tickers collection
        /// </summary>
        public static (bool success, string details, string error) VerifySpyTicker()
        {
            return Collections.Tickers.Items.Any(t => t.Symbol == "SPY")
                ? (true, "SPY Ticker Found: True", "")
                : (false, "", "Should contain SPY ticker");
        }

        /// <summary>
        /// Verify that the snapshots collection has exactly one Empty snapshot initially
        /// </summary>
        public static (bool success, string details, string error) VerifySnapshotsCollection()
        {
            var snapshotCount = Collections.Snapshots.Items.Count;
            var emptySnapshotCount = Collections.Snapshots.Items.Count(s => s.Type == Binnaculum.Core.Models.OverviewSnapshotType.Empty);

            return (snapshotCount == 1 && emptySnapshotCount == 1)
                ? (true, "Single Empty Snapshot Found: True", "")
                : (false, "", $"Expected exactly 1 Empty snapshot but found {snapshotCount} total snapshots ({emptySnapshotCount} Empty)");
        }

        #endregion

        #region BrokerAccount Test Verifications

        /// <summary>
        /// Find and store the Tastytrade broker ID for use in subsequent steps
        /// </summary>
        public static (bool success, string details, string error, int brokerIdFound) FindTastytradeBroker()
        {
            var tastytradeBroker = Collections.Brokers.Items.FirstOrDefault(b => b.Name == "Tastytrade");
            if (tastytradeBroker != null)
            {
                return (true, $"Tastytrade Broker Found: ID = {tastytradeBroker.Id}", "", tastytradeBroker.Id);
            }
            return (false, "", "Tastytrade broker not found in Collections.Brokers.Items", 0);
        }

        /// <summary>
        /// Find and store the USD currency ID for use in subsequent steps
        /// </summary>
        public static (bool success, string details, string error, int currencyIdFound) FindUsdCurrency()
        {
            var usdCurrency = Collections.Currencies.Items.FirstOrDefault(c => c.Code == "USD");
            if (usdCurrency != null)
            {
                return (true, $"USD Currency Found: ID = {usdCurrency.Id}", "", usdCurrency.Id);
            }
            return (false, "", "USD currency not found in Collections.Currencies.Items", 0);
        }

        /// <summary>
        /// Find a created BrokerAccount by the Tastytrade broker ID
        /// </summary>
        public static (bool success, string details, string error, int accountIdFound) FindCreatedBrokerAccount(int tastytradeId)
        {
            var brokerAccount = Collections.Accounts.Items
                .Where(a => a.Type == Binnaculum.Core.Models.AccountType.BrokerAccount)
                .FirstOrDefault(a => a.Broker != null && a.Broker.Value.Broker.Id == tastytradeId);

            if (brokerAccount?.Broker != null)
            {
                return (true, $"BrokerAccount Found: ID = {brokerAccount.Broker.Value.Id}", "", brokerAccount.Broker.Value.Id);
            }
            return (false, "", "Created BrokerAccount not found in Collections.Accounts.Items", 0);
        }

        /// <summary>
        /// Verify that exactly one snapshot exists in the collection
        /// </summary>
        public static (bool success, string details, string error) VerifySingleSnapshotExists()
        {
            var snapshotCount = Collections.Snapshots.Items.Count;
            return snapshotCount == 1
                ? (true, $"Single Snapshot Found: Count = {snapshotCount}", "")
                : (false, "", $"Expected exactly 1 snapshot but found {snapshotCount}");
        }

        /// <summary>
        /// Verify that the snapshot is of BrokerAccount type
        /// </summary>
        public static (bool success, string details, string error) VerifySnapshotIsBrokerAccountType()
        {
            if (Collections.Snapshots.Items.Count == 0)
                return (false, "", "No snapshots found to verify type");

            var snapshot = Collections.Snapshots.Items.First();
            var isBrokerAccount = snapshot.Type == Binnaculum.Core.Models.OverviewSnapshotType.BrokerAccount;
            return isBrokerAccount
                ? (true, "Snapshot Type: BrokerAccount", "")
                : (false, "", $"Expected BrokerAccount snapshot type but found {snapshot.Type}");
        }

        #endregion

        #region Financial Data Verifications

        /// <summary>
        /// Verify snapshot financial data for a single deposit movement
        /// </summary>
        public static (bool success, string details, string error) VerifySnapshotFinancialData(decimal expectedDeposited = 1200.0m, int expectedMovementCount = 1)
        {
            if (Collections.Snapshots.Items.Count == 0)
                return (false, "", "No snapshots found to verify financial data");

            var snapshot = Collections.Snapshots.Items.First();
            if (snapshot.BrokerAccount == null)
                return (false, "", "Snapshot does not contain BrokerAccount data");

            var brokerAccountSnapshot = snapshot.BrokerAccount.Value;
            var financial = brokerAccountSnapshot.Financial;

            // Verify the key financial data from the deposit
            if (financial.Deposited != expectedDeposited)
                return (false, "", $"Expected Deposited = {expectedDeposited} but found {financial.Deposited}");

            if (financial.MovementCounter != expectedMovementCount)
                return (false, "", $"Expected MovementCounter = {expectedMovementCount} but found {financial.MovementCounter}");

            if (financial.Currency.Code != "USD")
                return (false, "", $"Expected Currency = USD but found {financial.Currency.Code}");

            return (true, $"Financial Data: Deposited={financial.Deposited}, MovementCounter={financial.MovementCounter}, Currency=USD", "");
        }

        /// <summary>
        /// Verify snapshot financial data for multiple movements (deposits and withdrawals)
        /// </summary>
        public static (bool success, string details, string error) VerifyMultipleMovementsFinancialData()
        {
            if (Collections.Snapshots.Items.Count == 0)
                return (false, "", "No snapshots found to verify financial data");

            var snapshot = Collections.Snapshots.Items.First();
            if (snapshot.BrokerAccount == null)
                return (false, "", "Snapshot does not contain BrokerAccount data");

            var brokerAccountSnapshot = snapshot.BrokerAccount.Value;
            var financial = brokerAccountSnapshot.Financial;

            // Verify the key financial data from multiple movements
            if (financial.MovementCounter != 4)
                return (false, "", $"Expected MovementCounter = 4 but found {financial.MovementCounter}");

            // Calculate expected values based on test requirements
            var actualDeposited = financial.Deposited;
            var actualWithdrawn = financial.Withdrawn;
            var netAmount = actualDeposited - actualWithdrawn;

            var expectedTotalDeposited = 1800.0m; // 1200 + 600
            var expectedTotalWithdrawn = 600.0m;  // 300 + 300
            var expectedNetDeposited = 1200.0m;   // 1800 - 600

            if (actualDeposited != expectedTotalDeposited)
                return (false, "", $"Expected Total Deposited = {expectedTotalDeposited} but found {actualDeposited}");

            if (actualWithdrawn != expectedTotalWithdrawn)
                return (false, "", $"Expected Total Withdrawn = {expectedTotalWithdrawn} but found {actualWithdrawn}");

            if (netAmount != expectedNetDeposited)
                return (false, "", $"Expected Net Deposited = {expectedNetDeposited} but found {netAmount}");

            if (financial.Currency.Code != "USD")
                return (false, "", $"Expected Currency = USD but found {financial.Currency.Code}");

            return (true, $"Financial Data: TotalDeposited={actualDeposited}, TotalWithdrawn={actualWithdrawn}, NetDeposited={netAmount}, MovementCounter={financial.MovementCounter}, Currency=USD", "");
        }

        /// <summary>
        /// Verify snapshot financial data for options trading with realized and unrealized performance
        /// Uses accurate expected values based on analysis of TastytradeOptionsTest.csv
        /// </summary>
        public static (bool success, string details, string error) VerifyOptionsFinancialData()
        {
            if (Collections.Snapshots.Items.Count == 0)
                return (false, "", "No snapshots found to verify options financial data");

            // Log all snapshots for debugging
            Debug.WriteLine($"🔍 VERIFY: Found {Collections.Snapshots.Items.Count} snapshots:");
            for (int i = 0; i < Collections.Snapshots.Items.Count; i++)
            {
                var s = Collections.Snapshots.Items[i];
                Debug.WriteLine($"  [{i}] Snapshot Type: {s.Type}, BrokerAccount: {(s.BrokerAccount != null ? "YES" : "NO")}");
                if (s.BrokerAccount != null)
                {
                    var brokerAcct = s.BrokerAccount.Value;
                    var f = brokerAcct.Financial;
                    Debug.WriteLine($"      Date: {brokerAcct.Date}, Financial: Deposited={f.Deposited:F2}, Realized={f.RealizedGains:F2}, Unrealized={f.UnrealizedGains:F2}, Movements={f.MovementCounter}");
                }
            }

            // Get the LATEST snapshot with BrokerAccount data (by date)
            var snapshot = Collections.Snapshots.Items
                .Where(s => s.BrokerAccount != null)
                .OrderByDescending(s => s.BrokerAccount.Value.Date)
                .FirstOrDefault();
            if (snapshot == null)
                return (false, "", "No snapshots with BrokerAccount data found");

            Debug.WriteLine($"📊 VERIFY: Using LATEST BrokerAccount snapshot from Date: {snapshot.BrokerAccount.Value.Date}");

            var brokerAccountSnapshot = snapshot.BrokerAccount.Value;
            var financial = brokerAccountSnapshot.Financial;

            Debug.WriteLine($"💰 VERIFY: Latest Financial Data - Deposited={financial.Deposited:F2}, Realized={financial.RealizedGains:F2}, Unrealized={financial.UnrealizedGains:F2}, Movements={financial.MovementCounter}");

            // Expected values based on corrected analysis - NO automatic expiration
            // All values should come from explicit transactions only
            const decimal expectedDeposited = 878.79m;           // Total deposits: 844.56 + 24.23 + 10.00
            const decimal expectedRealizedGains = 23.65m;        // Only explicitly closed positions (SOFI + spreads)
            const decimal expectedUnrealizedGains = 14.86m;      // Only open SOFI 240510 position
            const decimal tolerance = 0.50m;                     // Tighter tolerance for final values

            Debug.WriteLine($"🎯 VERIFY: Expected values - Deposited={expectedDeposited:F2}, Realized={expectedRealizedGains:F2}, Unrealized={expectedUnrealizedGains:F2}, Movements=16");

            // Cash flow validation
            var depositedMatch = Math.Abs(financial.Deposited - expectedDeposited) <= tolerance;
            Debug.WriteLine($"💵 VERIFY: Deposited check - Expected={expectedDeposited:F2}, Actual={financial.Deposited:F2}, Diff={Math.Abs(financial.Deposited - expectedDeposited):F2}, Match={depositedMatch}");
            if (!depositedMatch)
                return (false, "", $"Expected Deposited ≈ {expectedDeposited} but found {financial.Deposited}");

            // Realized performance validation (completed strategies)
            var realizedMatch = Math.Abs(financial.RealizedGains - expectedRealizedGains) <= tolerance;
            Debug.WriteLine($"📈 VERIFY: Realized gains check - Expected={expectedRealizedGains:F2}, Actual={financial.RealizedGains:F2}, Diff={Math.Abs(financial.RealizedGains - expectedRealizedGains):F2}, Match={realizedMatch}");
            if (!realizedMatch)
                return (false, "", $"Expected Realized gains ≈ {expectedRealizedGains} but found {financial.RealizedGains}");

            // Unrealized performance validation (open positions)
            var unrealizedMatch = Math.Abs(financial.UnrealizedGains - expectedUnrealizedGains) <= tolerance;
            Debug.WriteLine($"📊 VERIFY: Unrealized gains check - Expected={expectedUnrealizedGains:F2}, Actual={financial.UnrealizedGains:F2}, Diff={Math.Abs(financial.UnrealizedGains - expectedUnrealizedGains):F2}, Match={unrealizedMatch}");
            if (!unrealizedMatch)
                return (false, "", $"Expected Unrealized gains ≈ {expectedUnrealizedGains} but found {financial.UnrealizedGains}");

            // Movement count validation (12 option trades + 3 deposits + 1 adjustment = 16)
            const int expectedMovements = 16;
            var movementMatch = financial.MovementCounter == expectedMovements;
            Debug.WriteLine($"🔢 VERIFY: Movement counter check - Expected={expectedMovements}, Actual={financial.MovementCounter}, Match={movementMatch}");
            if (!movementMatch)
                return (false, "", $"Expected MovementCounter = {expectedMovements} but found {financial.MovementCounter}");

            // Currency validation
            var currencyMatch = financial.Currency.Code == "USD";
            Debug.WriteLine($"💴 VERIFY: Currency check - Expected=USD, Actual={financial.Currency.Code}, Match={currencyMatch}");
            if (!currencyMatch)
                return (false, "", $"Expected Currency = USD but found {financial.Currency.Code}");

            // Calculate performance percentages
            var realizedPercentage = financial.Deposited > 0 ? (financial.RealizedGains / financial.Deposited) * 100 : 0;
            var unrealizedPercentage = financial.Deposited > 0 ? (financial.UnrealizedGains / financial.Deposited) * 100 : 0;
            var totalPerformance = realizedPercentage + unrealizedPercentage;

            Debug.WriteLine($"✅ VERIFY: All checks passed! Final result:");
            Debug.WriteLine($"   Deposited=${financial.Deposited:F2}, Realized=${financial.RealizedGains:F2} ({realizedPercentage:F2}%), Unrealized=${financial.UnrealizedGains:F2} ({unrealizedPercentage:F2}%), Total={totalPerformance:F2}%");

            return (true,
                $"Options Financial Data: " +
                $"Deposited=${financial.Deposited:F2}, " +
                $"Realized=${financial.RealizedGains:F2} ({realizedPercentage:F2}%), " +
                $"Unrealized=${financial.UnrealizedGains:F2} ({unrealizedPercentage:F2}%), " +
                $"Total={totalPerformance:F2}%, " +
                $"Movements={financial.MovementCounter}, " +
                $"Currency={financial.Currency.Code}", "");
        }

        #endregion

        #region Ticker Snapshot Verifications

        /// <summary>
        /// Validates a ticker snapshot against expected values using a strongly-typed data model.
        /// This is the preferred method for new tests - provides type safety, intellisense, and cleaner code.
        /// Validates all 14 financial fields with tolerance-based decimal comparison.
        /// </summary>
        /// <param name="snapshot">The actual TickerSnapshot from the core model (strongly typed)</param>
        /// <param name="expectedData">The expected values encapsulated in SnapshotValidationData model</param>
        /// <returns>Tuple of (success, details list) matching the standard verification format</returns>
        public static (bool success, List<string> details) ValidateTickerSnapshot(
            TickerSnapshot snapshot,
            SnapshotValidationData expectedData)
        {
            if (snapshot == null)
                return (false, new List<string> { "❌ Snapshot is null" });

            // Delegate to the detailed validation method with mapped expected values
            return ValidateTickerSnapshot(
                snapshot,
                expectedData.ExpectedDate,
                expectedData.Currency,
                expectedData.TotalShares,
                expectedData.Weight,
                expectedData.CostBasis,
                expectedData.RealCost,
                expectedData.Dividends,
                expectedData.Options,
                expectedData.TotalIncomes,
                expectedData.Unrealized,
                expectedData.Realized,
                expectedData.Performance,
                expectedData.LatestPrice,
                expectedData.OpenTrades,
                expectedData.ValidationContext);
        }

        /// <summary>
        /// Validates a specific ticker snapshot's financial data against expected values.
        /// Reusable method for all ticker snapshot validation scenarios across different test cases.
        /// Validates all 14 financial fields with tolerance-based decimal comparison.
        /// [LEGACY] Use the strongly-typed overload ValidateTickerSnapshot(TickerSnapshot, SnapshotValidationData) instead.
        /// </summary>
        public static (bool success, List<string> details) ValidateTickerSnapshot(
            dynamic snapshot,
            DateTime expectedDate,
            string expectedCurrency,
            decimal expectedTotalShares,
            decimal expectedWeight,
            decimal expectedCostBasis,
            decimal expectedRealCost,
            decimal expectedDividends,
            decimal expectedOptions,
            decimal expectedTotalIncomes,
            decimal expectedUnrealized,
            decimal expectedRealized,
            decimal expectedPerformance,
            decimal expectedLatestPrice,
            bool expectedOpenTrades,
            string validationContext = "")
        {
            var details = new List<string>();
            var tolerance = 0.01m; // 0.01 tolerance for decimal comparisons
            bool allValid = true;

            string contextLabel = string.IsNullOrEmpty(validationContext) ? "" : $" ({validationContext})";

            details.Add($"=== Ticker Snapshot Validation{contextLabel} ===");
            details.Add($"Date: {snapshot.MainCurrency.Date:yyyy-MM-dd} | Expected: {expectedDate:yyyy-MM-dd}");

            // Date validation
            var snapshotDate = new DateTime(snapshot.MainCurrency.Date.Year, snapshot.MainCurrency.Date.Month, snapshot.MainCurrency.Date.Day);
            if (snapshotDate.Date != expectedDate.Date)
            {
                details.Add($"❌ Date mismatch: {snapshot.MainCurrency.Date:yyyy-MM-dd} != {expectedDate:yyyy-MM-dd}");
                allValid = false;
            }
            else
            {
                details.Add($"✅ Date matches");
            }

            // Currency validation
            if (snapshot.MainCurrency.Currency.Code != expectedCurrency)
            {
                details.Add($"❌ Currency mismatch: {snapshot.MainCurrency.Currency.Code} != {expectedCurrency}");
                allValid = false;
            }
            else
            {
                details.Add($"✅ Currency: {snapshot.MainCurrency.Currency.Code}");
            }

            // TotalShares validation
            if (Math.Abs(snapshot.MainCurrency.TotalShares - expectedTotalShares) > tolerance)
            {
                details.Add($"❌ TotalShares: {snapshot.MainCurrency.TotalShares:F4} != {expectedTotalShares:F4}");
                allValid = false;
            }
            else
            {
                details.Add($"✅ TotalShares: {snapshot.MainCurrency.TotalShares:F4}");
            }

            // Weight validation
            if (Math.Abs(snapshot.MainCurrency.Weight - expectedWeight) > tolerance)
            {
                details.Add($"❌ Weight: {snapshot.MainCurrency.Weight:F2}% != {expectedWeight:F2}%");
                allValid = false;
            }
            else
            {
                details.Add($"✅ Weight: {snapshot.MainCurrency.Weight:F2}%");
            }

            // CostBasis validation
            if (Math.Abs(snapshot.MainCurrency.CostBasis - expectedCostBasis) > tolerance)
            {
                details.Add($"❌ CostBasis: ${snapshot.MainCurrency.CostBasis:F2} != ${expectedCostBasis:F2}");
                allValid = false;
            }
            else
            {
                details.Add($"✅ CostBasis: ${snapshot.MainCurrency.CostBasis:F2}");
            }

            // RealCost validation
            if (Math.Abs(snapshot.MainCurrency.RealCost - expectedRealCost) > tolerance)
            {
                details.Add($"❌ RealCost: ${snapshot.MainCurrency.RealCost:F2} != ${expectedRealCost:F2}");
                allValid = false;
            }
            else
            {
                details.Add($"✅ RealCost: ${snapshot.MainCurrency.RealCost:F2}");
            }

            // Dividends validation
            if (Math.Abs(snapshot.MainCurrency.Dividends - expectedDividends) > tolerance)
            {
                details.Add($"❌ Dividends: ${snapshot.MainCurrency.Dividends:F2} != ${expectedDividends:F2}");
                allValid = false;
            }
            else
            {
                details.Add($"✅ Dividends: ${snapshot.MainCurrency.Dividends:F2}");
            }

            // Options validation
            if (Math.Abs(snapshot.MainCurrency.Options - expectedOptions) > tolerance)
            {
                details.Add($"❌ Options: ${snapshot.MainCurrency.Options:F2} != ${expectedOptions:F2}");
                allValid = false;
            }
            else
            {
                details.Add($"✅ Options: ${snapshot.MainCurrency.Options:F2}");
            }

            // TotalIncomes validation
            if (Math.Abs(snapshot.MainCurrency.TotalIncomes - expectedTotalIncomes) > tolerance)
            {
                details.Add($"❌ TotalIncomes: ${snapshot.MainCurrency.TotalIncomes:F2} != ${expectedTotalIncomes:F2}");
                allValid = false;
            }
            else
            {
                details.Add($"✅ TotalIncomes: ${snapshot.MainCurrency.TotalIncomes:F2}");
            }

            // Unrealized validation
            if (Math.Abs(snapshot.MainCurrency.Unrealized - expectedUnrealized) > tolerance)
            {
                details.Add($"❌ Unrealized: ${snapshot.MainCurrency.Unrealized:F2} != ${expectedUnrealized:F2}");
                allValid = false;
            }
            else
            {
                details.Add($"✅ Unrealized: ${snapshot.MainCurrency.Unrealized:F2}");
            }

            // Realized validation
            if (Math.Abs(snapshot.MainCurrency.Realized - expectedRealized) > tolerance)
            {
                details.Add($"❌ Realized: ${snapshot.MainCurrency.Realized:F2} != ${expectedRealized:F2}");
                allValid = false;
            }
            else
            {
                details.Add($"✅ Realized: ${snapshot.MainCurrency.Realized:F2}");
            }

            // Performance validation
            if (Math.Abs(snapshot.MainCurrency.Performance - expectedPerformance) > tolerance)
            {
                details.Add($"❌ Performance: {snapshot.MainCurrency.Performance:F2}% != {expectedPerformance:F2}%");
                allValid = false;
            }
            else
            {
                details.Add($"✅ Performance: {snapshot.MainCurrency.Performance:F2}%");
            }

            // LatestPrice validation
            if (Math.Abs(snapshot.MainCurrency.LatestPrice - expectedLatestPrice) > tolerance)
            {
                details.Add($"❌ LatestPrice: ${snapshot.MainCurrency.LatestPrice:F2} != ${expectedLatestPrice:F2}");
                allValid = false;
            }
            else
            {
                details.Add($"✅ LatestPrice: ${snapshot.MainCurrency.LatestPrice:F2}");
            }

            // OpenTrades validation
            if (snapshot.MainCurrency.OpenTrades != expectedOpenTrades)
            {
                details.Add($"❌ OpenTrades: {snapshot.MainCurrency.OpenTrades} != {expectedOpenTrades}");
                allValid = false;
            }
            else
            {
                details.Add($"✅ OpenTrades: {snapshot.MainCurrency.OpenTrades}");
            }

            details.Add($"\nResult: {(allValid ? "✅ ALL FIELDS VALID" : "❌ VALIDATION FAILED")}");

            return (allValid, details);
        }

        #endregion

        #region Helper Methods for Common Verification Patterns

        /// <summary>
        /// Verify that a collection has a minimum count
        /// </summary>
        public static (bool success, string details, string error) VerifyCollectionMinimumCount<T>(
            IEnumerable<T> collection,
            int minimumCount,
            string collectionName)
        {
            var actualCount = collection.Count();
            return actualCount >= minimumCount
                ? (true, $"{collectionName}: {actualCount}", "")
                : (false, "", $"Expected at least {minimumCount} {collectionName} but found {actualCount}");
        }

        /// <summary>
        /// Verify that a collection contains an item matching a predicate
        /// </summary>
        public static (bool success, string details, string error) VerifyCollectionContains<T>(
            IEnumerable<T> collection,
            Func<T, bool> predicate,
            string itemDescription,
            string collectionName)
        {
            return collection.Any(predicate)
                ? (true, $"{itemDescription} Found: True", "")
                : (false, "", $"Should contain {itemDescription} in {collectionName}");
        }

        #endregion
    }
}