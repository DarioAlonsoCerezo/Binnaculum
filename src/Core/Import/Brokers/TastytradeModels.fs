namespace Binnaculum.Core.Import

open System

/// <summary>
/// Tastytrade-specific data types for transaction parsing and processing
/// </summary>
module TastytradeModels =

    /// <summary>
    /// Main transaction types in Tastytrade CSV files
    /// </summary>
    type TastytradeTransactionType = 
        | Trade of TradeSubType * TradeAction
        | MoneyMovement of MoneyMovementSubType
        | ReceiveDeliver of string // ACAT, etc.
        
    /// <summary>
    /// Trade sub-types for equity and option transactions
    /// </summary>
    and TradeSubType = 
        | BuyToOpen 
        | SellToOpen 
        | BuyToClose 
        | SellToClose
        
    /// <summary>
    /// Trade action mapping from Tastytrade CSV Action field
    /// </summary>
    and TradeAction =
        | BUY_TO_OPEN
        | SELL_TO_OPEN  
        | BUY_TO_CLOSE
        | SELL_TO_CLOSE
        
    /// <summary>
    /// Money movement sub-types for cash transactions
    /// </summary>
    and MoneyMovementSubType = 
        | Deposit 
        | BalanceAdjustment 
        | CreditInterest 
        | Transfer
        | Withdrawal

    /// <summary>
    /// Parsed option symbol components from complex Tastytrade format
    /// Examples: PLTR  240531C00022000 -> ticker=PLTR, exp=5/31/24, strike=22, type=CALL
    /// </summary>
    type ParsedOptionSymbol = {
        Ticker: string
        ExpirationDate: DateTime
        Strike: decimal
        OptionType: string // "CALL" or "PUT"
    }

    /// <summary>
    /// Core transaction record parsed from Tastytrade CSV line
    /// Maps directly to CSV column structure with proper type conversion
    /// </summary>
    type TastytradeTransaction = {
        Date: DateTime
        TransactionType: TastytradeTransactionType
        Symbol: string option
        InstrumentType: string option // "Equity Option", "Equity"
        Description: string
        Value: decimal
        Quantity: decimal
        AveragePrice: decimal option
        Commissions: decimal
        Fees: decimal
        Multiplier: decimal option
        RootSymbol: string option
        UnderlyingSymbol: string option
        ExpirationDate: DateTime option
        StrikePrice: decimal option
        CallOrPut: string option
        OrderNumber: string option
        Currency: string
        // Raw CSV line for error reporting
        RawCsvLine: string
        LineNumber: int
    }

    /// <summary>
    /// Multi-leg strategy grouping by order number
    /// Used to identify spreads, straddles, and other complex strategies
    /// </summary>
    type TastytradeStrategy = {
        OrderNumber: string
        Transactions: TastytradeTransaction list
        StrategyType: StrategyType option
    }
        
    /// <summary>
    /// Detected strategy types for multi-leg options
    /// </summary>
    and StrategyType =
        | CalendarSpread
        | VerticalSpread  
        | IronCondor
        | Straddle
        | Strangle
        | SingleLeg
        | Unknown

    /// <summary>
    /// Error information for parsing issues
    /// </summary>
    type TastytradeParsingError = {
        LineNumber: int
        ErrorMessage: string
        RawCsvLine: string
        ErrorType: TastytradeErrorType
    }
        
    /// <summary>
    /// Classification of Tastytrade-specific parsing errors
    /// </summary>
    and TastytradeErrorType =
        | InvalidDateFormat
        | InvalidOptionSymbol
        | MissingRequiredField of string
        | InvalidTransactionType
        | InvalidNumericValue of string
        | UnsupportedInstrumentType of string

    /// <summary>
    /// Result of parsing a Tastytrade CSV file
    /// </summary>
    type TastytradeParsingResult = {
        Transactions: TastytradeTransaction list
        Strategies: TastytradeStrategy list
        Errors: TastytradeParsingError list
        ProcessedLines: int
        SkippedLines: int
    }

    /// Helper functions for transaction type detection
    module TransactionTypeDetection =
        
        /// <summary>
        /// Parse transaction type from CSV Type and Sub Type columns
        /// </summary>
        let parseTransactionType (typeCol: string) (subTypeCol: string) (actionCol: string) =
            match typeCol.Trim(), subTypeCol.Trim() with
            | "Trade", "Buy to Open" -> Trade(BuyToOpen, BUY_TO_OPEN)
            | "Trade", "Sell to Open" -> Trade(SellToOpen, SELL_TO_OPEN)
            | "Trade", "Buy to Close" -> Trade(BuyToClose, BUY_TO_CLOSE)
            | "Trade", "Sell to Close" -> Trade(SellToClose, SELL_TO_CLOSE)
            | "Money Movement", "Deposit" -> MoneyMovement(Deposit)
            | "Money Movement", "Balance Adjustment" -> MoneyMovement(BalanceAdjustment)
            | "Money Movement", "Credit Interest" -> MoneyMovement(CreditInterest)
            | "Money Movement", "Transfer" -> MoneyMovement(Transfer)
            | "Money Movement", "Withdrawal" -> MoneyMovement(Withdrawal)
            | "Receive Deliver", subType -> ReceiveDeliver(subType)
            | _ -> failwith $"Unsupported transaction type: {typeCol} / {subTypeCol}"

        /// <summary>
        /// Determine if transaction involves options based on instrument type
        /// </summary>
        let isOptionTransaction (instrumentType: string option) =
            match instrumentType with
            | Some "Equity Option" -> true
            | _ -> false

        /// <summary>
        /// Determine if transaction is an equity trade
        /// </summary>
        let isEquityTrade (instrumentType: string option) =
            match instrumentType with
            | Some "Equity" -> true
            | _ -> false