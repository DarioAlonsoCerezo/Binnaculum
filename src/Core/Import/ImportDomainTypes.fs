namespace Binnaculum.Core.Import

open Binnaculum.Core.Database.DatabaseModel

/// <summary>
/// Domain types for broker-agnostic import persistence
/// Defines structures for converting broker-specific data to domain models
/// </summary>
module ImportDomainTypes =

    /// <summary>
    /// Broker-agnostic input for database persistence containing domain models
    /// Supports session tracking for resumable imports (integrated with PR #420)
    /// </summary>
    type internal PersistenceInput =
        {
            BrokerMovements: BrokerMovement list
            OptionTrades: OptionTrade list
            StockTrades: Trade list
            Dividends: Dividend list
            DividendTaxes: DividendTax list
            /// Optional session ID for tracking resumable imports
            SessionId: int option
        }
