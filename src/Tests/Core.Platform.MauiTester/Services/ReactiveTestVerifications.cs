using Binnaculum.Core.UI;
using System.Diagnostics;
using System.Linq;
using Microsoft.FSharp.Core;
using System.Reactive.Linq;
using DynamicData;
using System.Collections.Generic;

namespace Core.Platform.MauiTester.Services
{
    /// <summary>
    /// Reactive test verification utilities that validate stream emissions and reactive behavior
    /// Complements TestVerifications with reactive-specific validations
    /// </summary>
    public static class ReactiveTestVerifications
    {
        /// <summary>
        /// Stores stream observations for comparison and validation
        /// </summary>
        private static readonly Dictionary<string, List<object>> StreamObservations = new();

        /// <summary>
        /// Subscription disposables for cleanup
        /// </summary>
        private static readonly List<IDisposable> ActiveSubscriptions = new();

        /// <summary>
        /// Start observing reactive streams before test execution
        /// </summary>
        public static void StartObserving()
        {
            ClearObservations();

            // Observe Overview.Data stream
            var overviewSubscription = Overview.Data
                .Select(data => new { Type = "Overview.Data", Data = data, Timestamp = DateTime.Now })
                .Subscribe(emission => RecordEmission("Overview.Data", emission));
            ActiveSubscriptions.Add(overviewSubscription);

            // Observe Collections.Currencies stream
            var currenciesSubscription = Collections.Currencies.Connect()
                .Select(changes => new { Type = "Currencies", ChangeCount = changes.Count, Timestamp = DateTime.Now })
                .Subscribe(emission => RecordEmission("Currencies", emission));
            ActiveSubscriptions.Add(currenciesSubscription);

            // Observe Collections.Brokers stream
            var brokersSubscription = Collections.Brokers.Connect()
                .Select(changes => new { Type = "Brokers", ChangeCount = changes.Count, Timestamp = DateTime.Now })
                .Subscribe(emission => RecordEmission("Brokers", emission));
            ActiveSubscriptions.Add(brokersSubscription);

            // Observe Collections.Tickers stream
            var tickersSubscription = Collections.Tickers.Connect()
                .Select(changes => new { Type = "Tickers", ChangeCount = changes.Count, Timestamp = DateTime.Now })
                .Subscribe(emission => RecordEmission("Tickers", emission));
            ActiveSubscriptions.Add(tickersSubscription);

            // Observe Collections.Snapshots stream
            var snapshotsSubscription = Collections.Snapshots.Connect()
                .Select(changes => new { Type = "Snapshots", ChangeCount = changes.Count, Timestamp = DateTime.Now })
                .Subscribe(emission => RecordEmission("Snapshots", emission));
            ActiveSubscriptions.Add(snapshotsSubscription);

            // Observe Collections.Accounts stream (for BrokerAccount tests)
            var accountsSubscription = Collections.Accounts.Connect()
                .Select(changes => new { Type = "Accounts", ChangeCount = changes.Count, Timestamp = DateTime.Now })
                .Subscribe(emission => RecordEmission("Accounts", emission));
            ActiveSubscriptions.Add(accountsSubscription);

            // Observe Collections.Movements stream (for BrokerAccount + Deposit tests)
            var movementsSubscription = Collections.Movements.Connect()
                .Select(changes => new { Type = "Movements", ChangeCount = changes.Count, Timestamp = DateTime.Now })
                .Subscribe(emission => RecordEmission("Movements", emission));
            ActiveSubscriptions.Add(movementsSubscription);
        }

        /// <summary>
        /// Stop observing and cleanup subscriptions
        /// </summary>
        public static void StopObserving()
        {
            foreach (var subscription in ActiveSubscriptions)
            {
                subscription?.Dispose();
            }
            ActiveSubscriptions.Clear();
        }

        /// <summary>
        /// Record stream emission for later verification
        /// </summary>
        private static void RecordEmission(string streamName, object emission)
        {
            if (!StreamObservations.ContainsKey(streamName))
            {
                StreamObservations[streamName] = new List<object>();
            }
            StreamObservations[streamName].Add(emission);
        }

        /// <summary>
        /// Clear all stream observations
        /// </summary>
        private static void ClearObservations()
        {
            StreamObservations.Clear();
        }

        /// <summary>
        /// Verify that Overview.Data stream emitted the expected sequence
        /// </summary>
        public static (bool success, string details, string error) VerifyOverviewDataStream()
        {
            if (!StreamObservations.ContainsKey("Overview.Data") || StreamObservations["Overview.Data"].Count < 2)
            {
                return (false, "", "Expected at least 2 Overview.Data emissions (IsDatabaseInitialized and TransactionsLoaded)");
            }

            var emissions = StreamObservations["Overview.Data"];
            var hasInitialized = emissions.Any(e => e.ToString()?.Contains("IsDatabaseInitialized") == true);
            var hasTransactionsLoaded = emissions.Any(e => e.ToString()?.Contains("TransactionsLoaded") == true);

            return (hasInitialized && hasTransactionsLoaded)
                ? (true, $"Overview.Data: {emissions.Count} emissions with database init and transactions loaded", "")
                : (false, "", $"Missing expected Overview.Data state changes. Init: {hasInitialized}, Loaded: {hasTransactionsLoaded}");
        }

        /// <summary>
        /// Verify that Currencies stream emitted changes during data loading
        /// </summary>
        public static (bool success, string details, string error) VerifyCurrenciesStream()
        {
            if (!StreamObservations.ContainsKey("Currencies") || StreamObservations["Currencies"].Count == 0)
            {
                return (false, "", "Expected Currencies stream emissions during data loading");
            }

            var emissions = StreamObservations["Currencies"];
            var totalChanges = emissions.Sum(e => GetChangeCount(e));

            return totalChanges > 0
                ? (true, $"Currencies Stream: {emissions.Count} emissions, {totalChanges} total changes", "")
                : (false, "", "Currencies stream emitted but no actual changes detected");
        }

        /// <summary>
        /// Verify that Brokers stream emitted changes during data loading
        /// </summary>
        public static (bool success, string details, string error) VerifyBrokersStream()
        {
            if (!StreamObservations.ContainsKey("Brokers") || StreamObservations["Brokers"].Count == 0)
            {
                return (false, "", "Expected Brokers stream emissions during data loading");
            }

            var emissions = StreamObservations["Brokers"];
            var totalChanges = emissions.Sum(e => GetChangeCount(e));

            return totalChanges > 0
                ? (true, $"Brokers Stream: {emissions.Count} emissions, {totalChanges} total changes", "")
                : (false, "", "Brokers stream emitted but no actual changes detected");
        }

        /// <summary>
        /// Verify that Tickers stream emitted changes during data loading
        /// </summary>
        public static (bool success, string details, string error) VerifyTickersStream()
        {
            if (!StreamObservations.ContainsKey("Tickers") || StreamObservations["Tickers"].Count == 0)
            {
                return (false, "", "Expected Tickers stream emissions during data loading");
            }

            var emissions = StreamObservations["Tickers"];
            var totalChanges = emissions.Sum(e => GetChangeCount(e));

            return totalChanges > 0
                ? (true, $"Tickers Stream: {emissions.Count} emissions, {totalChanges} total changes", "")
                : (false, "", "Tickers stream emitted but no actual changes detected");
        }

        /// <summary>
        /// Verify that Snapshots stream emitted changes during data loading
        /// </summary>
        public static (bool success, string details, string error) VerifySnapshotsStream()
        {
            if (!StreamObservations.ContainsKey("Snapshots") || StreamObservations["Snapshots"].Count == 0)
            {
                return (false, "", "Expected Snapshots stream emissions during data loading");
            }

            var emissions = StreamObservations["Snapshots"];
            var totalChanges = emissions.Sum(e => GetChangeCount(e));

            return totalChanges > 0
                ? (true, $"Snapshots Stream: {emissions.Count} emissions, {totalChanges} total changes", "")
                : (false, "", "Snapshots stream emitted but no actual changes detected");
        }

        /// <summary>
        /// Verify that Accounts stream emitted changes during broker account creation
        /// </summary>
        public static (bool success, string details, string error) VerifyAccountsStream()
        {
            if (!StreamObservations.ContainsKey("Accounts") || StreamObservations["Accounts"].Count == 0)
            {
                return (false, "", "Expected Accounts stream emissions during broker account creation");
            }

            var emissions = StreamObservations["Accounts"];
            var totalChanges = emissions.Sum(e => GetChangeCount(e));

            return totalChanges > 0
                ? (true, $"Accounts Stream: {emissions.Count} emissions, {totalChanges} total changes", "")
                : (false, "", "Accounts stream emitted but no actual changes detected");
        }

        /// <summary>
        /// Verify that specific broker account was created and emitted in Accounts stream
        /// </summary>
        public static (bool success, string details, string error) VerifyBrokerAccountCreation()
        {
            // First verify we have account stream emissions
            var accountsResult = VerifyAccountsStream();
            if (!accountsResult.success)
            {
                return accountsResult;
            }

            // Check that we actually have a broker account in the Collections.Accounts
            var brokerAccounts = Collections.Accounts.Items.Where(a => a.Broker != null).ToList();

            return brokerAccounts.Count > 0
                ? (true, $"BrokerAccount Created: {brokerAccounts.Count} broker accounts found", "")
                : (false, "", "Expected at least one broker account to be created and present in Collections.Accounts");
        }

        /// <summary>
        /// Verify that broker account creation triggered snapshot generation
        /// </summary>
        public static (bool success, string details, string error) VerifyBrokerAccountSnapshots()
        {
            var snapshotsResult = VerifySnapshotsStream();
            if (!snapshotsResult.success)
            {
                return (false, "", "Expected snapshot generation after broker account creation");
            }

            // Check for broker-related snapshots in the collection
            var brokerSnapshots = Collections.Snapshots.Items.Where(s =>
                s.Type.ToString().Contains("Broker") || s.Type.ToString().Contains("Account")).ToList();

            return brokerSnapshots.Count > 0
                ? (true, $"BrokerAccount Snapshots: {brokerSnapshots.Count} broker-related snapshots generated", "")
                : (false, "", "Expected broker-related snapshots to be generated after account creation");
        }

        /// <summary>
        /// Verify that Movements stream emitted changes during deposit transaction
        /// </summary>
        public static (bool success, string details, string error) VerifyMovementsStream()
        {
            if (!StreamObservations.ContainsKey("Movements") || StreamObservations["Movements"].Count == 0)
            {
                return (false, "", "Expected Movements stream emissions during deposit transaction");
            }

            var emissions = StreamObservations["Movements"];
            var totalChanges = emissions.Sum(e => GetChangeCount(e));

            return totalChanges > 0
                ? (true, $"Movements Stream: {emissions.Count} emissions, {totalChanges} total changes", "")
                : (false, "", "Movements stream emitted but no actual changes detected");
        }

        /// <summary>
        /// Verify that broker account with deposit was created and includes movement transactions
        /// </summary>
        public static (bool success, string details, string error) VerifyBrokerAccountWithDeposit()
        {
            // First verify broker account creation
            var accountResult = VerifyBrokerAccountCreation();
            if (!accountResult.success)
            {
                return accountResult;
            }

            // Then verify movement stream for deposit
            var movementsResult = VerifyMovementsStream();
            if (!movementsResult.success)
            {
                return (false, "", "Expected deposit movements to be created and emitted");
            }

            // Check that we actually have deposit movements in the Collections.Movements
            var depositMovements = Collections.Movements.Items.Where(m =>
                Microsoft.FSharp.Core.OptionModule.IsSome(m.BrokerMovement) && (
                    m.BrokerMovement.Value.MovementType.ToString().Contains("Deposit") ||
                    m.BrokerMovement.Value.Amount > 0)).ToList();

            return depositMovements.Count > 0
                ? (true, $"BrokerAccount + Deposit: {depositMovements.Count} deposit movements found", "")
                : (false, "", "Expected at least one deposit movement to be created");
        }

        /// <summary>
        /// Verify that deposit transaction triggered appropriate snapshot updates
        /// </summary>
        public static (bool success, string details, string error) VerifyDepositSnapshots()
        {
            var snapshotsResult = VerifySnapshotsStream();
            if (!snapshotsResult.success)
            {
                return (false, "", "Expected snapshot updates after deposit transaction");
            }

            // Check for account-related snapshots with positive deposit amounts
            var depositSnapshots = Collections.Snapshots.Items.Where(s =>
                (s.Type.ToString().Contains("BrokerAccount") &&
                 Microsoft.FSharp.Core.OptionModule.IsSome(s.BrokerAccount) &&
                 s.BrokerAccount.Value.Financial.Deposited > 0) ||
                (s.Type.ToString().Contains("BankAccount") &&
                 Microsoft.FSharp.Core.OptionModule.IsSome(s.BankAccount))).ToList(); return depositSnapshots.Count > 0
                ? (true, $"Deposit Snapshots: {depositSnapshots.Count} account snapshots with positive deposits", "")
                : (false, "", "Expected account snapshots with positive deposited amounts after deposit");
        }

        /// <summary>
        /// Compare reactive BrokerAccount + Deposit test results with traditional test results
        /// </summary>
        public static (bool success, string details, string error) CompareWithTraditionalBrokerAccountDepositTest()
        {
            // Run BrokerAccount + Deposit specific traditional verifications for comparison
            var traditionalResults = new[]
            {
                TestVerifications.VerifyDatabaseInitialized(),
                TestVerifications.VerifyDataLoaded(),
                // Note: We may need to add specific BrokerAccount + Deposit verifications to TestVerifications
            };

            var passedTraditional = traditionalResults.Count(r => r.success);
            var totalTraditional = traditionalResults.Length;

            var reactiveResults = new[]
            {
                VerifyAccountsStream(),
                VerifyBrokerAccountCreation(),
                VerifyMovementsStream(),
                VerifyBrokerAccountWithDeposit(),
                VerifyDepositSnapshots()
            };

            var passedReactive = reactiveResults.Count(r => r.success);
            var totalReactive = reactiveResults.Length;

            var allTraditionalPassed = passedTraditional == totalTraditional;
            var allReactivePassed = passedReactive == totalReactive;

            var details = $"Traditional: {passedTraditional}/{totalTraditional}, Reactive: {passedReactive}/{totalReactive}";

            if (allTraditionalPassed && allReactivePassed)
            {
                return (true, $"Both approaches successful. {details}", "");
            }
            else
            {
                var error = "";
                if (!allTraditionalPassed) error += "Traditional test failed. ";
                if (!allReactivePassed) error += "Reactive streams failed. ";
                return (false, details, error.Trim());
            }
        }

        /// <summary>
        /// Compare reactive BrokerAccount test results with traditional test results
        /// </summary>
        public static (bool success, string details, string error) CompareWithTraditionalBrokerAccountTest()
        {
            // Run BrokerAccount-specific traditional verifications for comparison
            var traditionalResults = new[]
            {
                TestVerifications.VerifyDatabaseInitialized(),
                TestVerifications.VerifyDataLoaded(),
                // Note: We need to check if there are BrokerAccount-specific verifications in TestVerifications
                // For now, we'll use the generic ones and add specific ones as needed
            };

            var passedTraditional = traditionalResults.Count(r => r.success);
            var totalTraditional = traditionalResults.Length;

            var reactiveResults = new[]
            {
                VerifyAccountsStream(),
                VerifyBrokerAccountCreation(),
                VerifyBrokerAccountSnapshots()
            };

            var passedReactive = reactiveResults.Count(r => r.success);
            var totalReactive = reactiveResults.Length;

            var allTraditionalPassed = passedTraditional == totalTraditional;
            var allReactivePassed = passedReactive == totalReactive;

            var details = $"Traditional: {passedTraditional}/{totalTraditional}, Reactive: {passedReactive}/{totalReactive}";

            if (allTraditionalPassed && allReactivePassed)
            {
                return (true, $"Both approaches successful. {details}", "");
            }
            else
            {
                var error = "";
                if (!allTraditionalPassed) error += "Traditional test failed. ";
                if (!allReactivePassed) error += "Reactive streams failed. ";
                return (false, details, error.Trim());
            }
        }

        /// <summary>
        /// Compare reactive test results with traditional test results
        /// </summary>
        public static (bool success, string details, string error) CompareWithTraditionalTest()
        {
            // Run the same verifications as the traditional test to compare results
            var traditionalResults = new[]
            {
                TestVerifications.VerifyDatabaseInitialized(),
                TestVerifications.VerifyDataLoaded(),
                TestVerifications.VerifyCurrenciesCollection(),
                TestVerifications.VerifyUsdCurrency(),
                TestVerifications.VerifyBrokersCollection(),
                TestVerifications.VerifyIbkrBroker(),
                TestVerifications.VerifyTastytradeBroker(),
                TestVerifications.VerifySpyTicker(),
                TestVerifications.VerifySnapshotsCollection()
            };

            var passedTraditional = traditionalResults.Count(r => r.success);
            var totalTraditional = traditionalResults.Length;

            var reactiveStreamsPassed = new[]
            {
                VerifyOverviewDataStream(),
                VerifyCurrenciesStream(),
                VerifyBrokersStream(),
                VerifyTickersStream(),
                VerifySnapshotsStream()
            }.Count(r => r.success);

            var allTraditionalPassed = passedTraditional == totalTraditional;
            var allReactivePassed = reactiveStreamsPassed == 5;

            var details = $"Traditional: {passedTraditional}/{totalTraditional}, Reactive: {reactiveStreamsPassed}/5";

            if (allTraditionalPassed && allReactivePassed)
            {
                return (true, $"Both approaches successful. {details}", "");
            }
            else
            {
                var error = "";
                if (!allTraditionalPassed) error += "Traditional test failed. ";
                if (!allReactivePassed) error += "Reactive streams failed. ";
                return (false, details, error.Trim());
            }
        }

        /// <summary>
        /// Helper method to extract change count from emission objects
        /// </summary>
        private static int GetChangeCount(object emission)
        {
            // Use reflection to get ChangeCount property
            var type = emission.GetType();
            var changeCountProperty = type.GetProperty("ChangeCount");
            var value = changeCountProperty?.GetValue(emission);
            return value is int intValue ? intValue : 0;
        }
    }
}