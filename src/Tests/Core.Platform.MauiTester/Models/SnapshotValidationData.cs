using System;

namespace Core.Platform.MauiTester.Models
{
    /// <summary>
    /// Encapsulates all expected values for snapshot validation.
    /// Simplifies the validation method signatures and reduces parameter bloat.
    /// </summary>
    public class SnapshotValidationData
    {
        /// <summary>
        /// The expected date of the snapshot (as DateTime for comparison with snapshot.MainCurrency.Date)
        /// </summary>
        public DateTime ExpectedDate { get; set; }

        /// <summary>
        /// The currency code (e.g., "USD")
        /// </summary>
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Total number of shares held in the position
        /// </summary>
        public decimal TotalShares { get; set; }

        /// <summary>
        /// Weight/percentage of total portfolio
        /// </summary>
        public decimal Weight { get; set; }

        /// <summary>
        /// Cost basis of the position
        /// </summary>
        public decimal CostBasis { get; set; }

        /// <summary>
        /// Real/actual cost
        /// </summary>
        public decimal RealCost { get; set; }

        /// <summary>
        /// Dividends received
        /// </summary>
        public decimal Dividends { get; set; }

        /// <summary>
        /// Options premium/value
        /// </summary>
        public decimal Options { get; set; }

        /// <summary>
        /// Total income (dividends + options)
        /// </summary>
        public decimal TotalIncomes { get; set; }

        /// <summary>
        /// Unrealized gains/losses
        /// </summary>
        public decimal Unrealized { get; set; }

        /// <summary>
        /// Realized gains/losses
        /// </summary>
        public decimal Realized { get; set; }

        /// <summary>
        /// Performance percentage
        /// </summary>
        public decimal Performance { get; set; }

        /// <summary>
        /// Latest price of the asset
        /// </summary>
        public decimal LatestPrice { get; set; }

        /// <summary>
        /// Whether there are open trades
        /// </summary>
        public bool OpenTrades { get; set; }

        /// <summary>
        /// Context/description for validation (e.g., "TSLL Oldest Snapshot")
        /// </summary>
        public string ValidationContext { get; set; } = "Snapshot Validation";

        /// <summary>
        /// Description of the snapshot (e.g., "Oldest snapshot", "After expiration")
        /// </summary>
        public string Description { get; set; } = "";
    }
}
