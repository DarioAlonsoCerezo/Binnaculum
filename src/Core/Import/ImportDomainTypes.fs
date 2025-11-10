namespace Binnaculum.Core.Import

open System
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

    /// <summary>
    /// Unified movement representation for import processing.
    /// Standardizes all broker data into a common format before persistence.
    /// </summary>
    type internal ImportMovement =
        /// Stock trade (buy/sell equity)
        | StockTradeMovement of Trade
        
        /// Option trade (buy/sell options contract)
        | OptionTradeMovement of OptionTrade
        
        /// Dividend received from ticker
        | DividendMovement of Dividend
        
        /// Dividend tax withheld
        | DividendTaxMovement of DividendTax
        
        /// Broker account movement (deposits, withdrawals, fees, ACAT transfers, etc.)
        /// Note: Named BrokerMovement to match DatabaseModel.BrokerMovement
        /// Handles both cash movements and securities transfers
        | BrokerMovement of BrokerMovement

    /// <summary>
    /// Batch of movements ready for persistence.
    /// Groups movements with metadata for efficient processing.
    /// </summary>
    type internal ImportMovementBatch = {
        /// List of standardized movements to persist
        Movements: ImportMovement list
        
        /// Broker account ID these movements belong to
        BrokerAccountId: int
        
        /// Source broker (for broker-specific logic like strike adjustments)
        SourceBroker: SupportedBroker
        
        /// When this batch was created
        ImportDate: DateTime
        
        /// Metadata collected during conversion (for snapshot updates)
        Metadata: ImportMetadata
    }

    /// <summary>
    /// Result of validating a batch of movements.
    /// Separates valid movements from invalid ones with error messages.
    /// </summary>
    type internal MovementValidationResult = {
        /// Movements that passed validation
        Valid: ImportMovement list
        
        /// Movements that failed validation with error messages
        Invalid: (ImportMovement * string) list
    }

    /// <summary>
    /// Result of persisting movements to database.
    /// Provides detailed feedback about what was created and any errors.
    /// </summary>
    type internal MovementPersistenceResult = {
        /// Number of broker movements created
        BrokerMovementsCreated: int
        
        /// Number of option trades created
        OptionTradesCreated: int
        
        /// Number of stock trades created
        StockTradesCreated: int
        
        /// Number of dividends created
        DividendsCreated: int
        
        /// Number of dividend taxes created
        DividendTaxesCreated: int
        
        /// Number of validation errors encountered
        ValidationErrorsCount: int
        
        /// Number of persistence errors encountered
        PersistenceErrorsCount: int
        
        /// All error messages collected during processing
        Errors: string list
        
        /// Metadata for snapshot updates
        ImportMetadata: ImportMetadata
    }
