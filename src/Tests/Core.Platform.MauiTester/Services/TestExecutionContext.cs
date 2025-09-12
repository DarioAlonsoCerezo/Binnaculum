namespace Core.Platform.MauiTester.Services
{
    /// <summary>
    /// Encapsulates test execution state that gets passed between test steps
    /// This replaces the stateful fields in TestRunner, making test execution more explicit and testable
    /// </summary>
    public class TestExecutionContext
    {
        /// <summary>
        /// The Tastytrade broker ID found during test setup
        /// </summary>
        public int TastytradeId { get; set; } = 0;

        /// <summary>
        /// The broker account ID created during test execution
        /// </summary>
        public int BrokerAccountId { get; set; } = 0;

        /// <summary>
        /// The USD currency ID found during test setup
        /// </summary>
        public int UsdCurrencyId { get; set; } = 0;

        /// <summary>
        /// Reset all context values to their defaults
        /// </summary>
        public void Reset()
        {
            TastytradeId = 0;
            BrokerAccountId = 0;
            UsdCurrencyId = 0;
        }
    }
}