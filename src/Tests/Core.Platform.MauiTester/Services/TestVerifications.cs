using Binnaculum.Core.UI;

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
            return brokerCount >= 3
                ? (true, $"Brokers: {brokerCount}", "")
                : (false, "", $"Expected at least 3 brokers but found {brokerCount}");
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
        /// Verify that Sigma Trade broker exists in the collection
        /// </summary>
        public static (bool success, string details, string error) VerifySigmaTradeBroker()
        {
            return Collections.Brokers.Items.Any(b => b.Name == "Sigma Trade")
                ? (true, "SigmaTrade Found: True", "")
                : (false, "", "Should contain SigmaTrade broker (Sigma Trade)");
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