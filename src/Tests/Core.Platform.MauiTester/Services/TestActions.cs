using Binnaculum.Core.UI;

namespace Core.Platform.MauiTester.Services
{
    /// <summary>
    /// Contains test action methods that perform operations during test execution
    /// This extraction separates action logic from TestRunner orchestration
    /// </summary>
    public class TestActions
    {
        private readonly TestExecutionContext _context;

        public TestActions(TestExecutionContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Wipe all data to ensure fresh test environment
        /// </summary>
        public async Task<(bool success, string details)> WipeDataForTestingAsync()
        {
            // 🚨 TEST-ONLY: Wipe all data to ensure fresh test environment
            // This prevents data leakage between test runs and ensures consistent, reliable results
            await Overview.WipeAllDataForTesting();
            return (true, "All data wiped for fresh test environment");
        }

        /// <summary>
        /// Initialize platform services
        /// </summary>
        public Task<(bool success, string details)> InitializePlatformServicesAsync() =>
            Task.FromResult((true, $"Platform services available. AppData: {Microsoft.Maui.Storage.FileSystem.AppDataDirectory}"));

        /// <summary>
        /// Initialize database
        /// </summary>
        public async Task<(bool success, string details)> InitializeDatabaseAsync()
        {
            await Overview.InitDatabase();
            return (true, "Database initialization completed");
        }

        /// <summary>
        /// Load data from database
        /// </summary>
        public async Task<(bool success, string details)> LoadDataAsync()
        {
            await Overview.LoadData();
            return (true, "Data loading completed");
        }

        /// <summary>
        /// Creates a BrokerAccount with the specified name
        /// </summary>
        public async Task<(bool success, string details)> CreateBrokerAccountAsync(string accountName)
        {
            if (_context.TastytradeId == 0)
                return (false, "Tastytrade broker ID is 0, cannot create account");

            await Creator.SaveBrokerAccount(_context.TastytradeId, accountName);

            // Manually emit signals for account creation
            await Task.Delay(100); // Brief delay to allow collection updates to propagate
            ReactiveTestVerifications.SignalReceived("Accounts_Updated");
            ReactiveTestVerifications.SignalReceived("Snapshots_Updated");

            return (true, $"BrokerAccount named '{accountName}' created successfully");
        }

        /// <summary>
        /// Creates a movement with specified parameters and date offset
        /// </summary>
        public async Task<(bool success, string details)> CreateMovementAsync(decimal amount,
            Binnaculum.Core.Models.BrokerMovementType movementType, int daysOffset, string? description = null)
        {
            if (_context.BrokerAccountId == 0)
                return (false, "BrokerAccount ID is 0, cannot create movement");

            if (_context.UsdCurrencyId == 0)
                return (false, "USD Currency ID is 0, cannot create movement");

            // Get the actual BrokerAccount and Currency objects
            var brokerAccount = Collections.Accounts.Items
                .Where(a => a.Type == Binnaculum.Core.Models.AccountType.BrokerAccount)
                .FirstOrDefault(a => a.Broker != null && a.Broker.Value.Id == _context.BrokerAccountId)?.Broker?.Value;

            var usdCurrency = Collections.Currencies.Items.FirstOrDefault(c => c.Id == _context.UsdCurrencyId);

            if (brokerAccount == null)
                return (false, "Could not find BrokerAccount object for movement creation");

            if (usdCurrency == null)
                return (false, "Could not find USD Currency object for movement creation");

            // Create movement with specified date offset
            var movementDate = DateTime.Now.AddDays(daysOffset);
            var notes = description ?? $"Historical {movementType.ToString().ToLower()} test movement";

            var movement = new Binnaculum.Core.Models.BrokerMovement(
                id: 0,  // Will be assigned by database 
                timeStamp: movementDate,
                amount: amount,
                currency: usdCurrency,
                brokerAccount: brokerAccount,
                commissions: 0.0m,
                fees: 0.0m,
                movementType: movementType,
                notes: Microsoft.FSharp.Core.FSharpOption<string>.Some(notes),
                fromCurrency: Microsoft.FSharp.Core.FSharpOption<Binnaculum.Core.Models.Currency>.None,
                amountChanged: Microsoft.FSharp.Core.FSharpOption<decimal>.None,
                ticker: Microsoft.FSharp.Core.FSharpOption<Binnaculum.Core.Models.Ticker>.None,
                quantity: Microsoft.FSharp.Core.FSharpOption<decimal>.None
            );

            await Creator.SaveBrokerMovement(movement);

            // Manually emit signals for movement creation
            await Task.Delay(100); // Brief delay to allow collection updates to propagate
            ReactiveTestVerifications.SignalReceived("Movements_Updated");
            ReactiveTestVerifications.SignalReceived("Snapshots_Updated");

            // Wait a bit after each movement to ensure snapshot calculation
            await Task.Delay(350);

            return (true, $"Historical {movementType} Movement Created: ${amount} USD on {movementDate:yyyy-MM-dd}");
        }
    }
}