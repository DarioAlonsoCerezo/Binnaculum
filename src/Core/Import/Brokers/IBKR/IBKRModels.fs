namespace Binnaculum.Core.Import

open System

/// <summary>
/// IBKR-specific data models and types for parsing Interactive Brokers CSV statements
/// Handles various statement sections while maintaining privacy compliance
/// </summary>
module IBKRModels =
    
    /// <summary>
    /// IBKR statement sections that we parse (excluding sensitive personal data)
    /// </summary>
    type IBKRSection = 
        | Trades
        | DepositsWithdrawals
        | OpenPositions
        | FinancialInstruments
        | CashReport
        | ExchangeRates
        | ForexBalances
        | CollateralBorrowing
        | SkippedSection of string
    
    /// <summary>
    /// Types of cash flows identified in IBKR statements
    /// </summary>
    type IBKRCashFlowType =
        | Deposit
        | Withdrawal
        | Commission
        | TradeSettlement
        | FXTranslationGain
        | FXTranslationLoss
        | InterestPayment
        | Fee
    
    /// <summary>
    /// IBKR trade record from Trades section
    /// </summary>
    type IBKRTrade = {
        AssetCategory: string
        Currency: string
        Symbol: string
        DateTime: DateTime
        Quantity: decimal
        TradePrice: decimal option
        ClosePrice: decimal option
        Proceeds: decimal
        CommissionFee: decimal
        Basis: decimal option
        RealizedPnL: decimal option
        RealizedPnLPercent: decimal option
        MTMPnL: decimal option
        Code: string option
    }
    
    /// <summary>
    /// IBKR forex trade record (GBP.USD, EUR.USD, etc.)
    /// </summary>
    type IBKRForexTrade = {
        CurrencyPair: string
        BaseCurrency: string
        QuoteCurrency: string
        DateTime: DateTime
        Quantity: decimal
        TradePrice: decimal
        Proceeds: decimal
        CommissionFee: decimal
        Code: string option
    }
    
    /// <summary>
    /// IBKR cash movement from Deposits & Withdrawals section
    /// </summary>
    type IBKRCashMovement = {
        Currency: string
        SettleDate: DateTime
        Description: string
        Amount: decimal
        MovementType: IBKRCashFlowType
    }
    
    /// <summary>
    /// IBKR cash flow from Cash Report section
    /// </summary>
    type IBKRCashFlow = {
        FlowType: IBKRCashFlowType
        Currency: string
        Amount: decimal
        AmountBase: decimal
        Description: string
    }
    
    /// <summary>
    /// IBKR open position record
    /// </summary>
    type IBKROpenPosition = {
        AssetCategory: string
        Currency: string
        Symbol: string
        Quantity: decimal
        Multiplier: decimal
        CostBasisPrice: decimal
        CostBasisMoney: decimal
        ClosePrice: decimal
        Value: decimal
        UnrealizedPnL: decimal
        UnrealizedPnLPercent: decimal
    }
    
    /// <summary>
    /// IBKR financial instrument metadata
    /// </summary>
    type IBKRInstrument = {
        AssetCategory: string
        Symbol: string
        Description: string
        ConId: string option
        SecurityId: string option
        ListingExchange: string option
        Multiplier: decimal option
        InstrumentType: string option
        Code: string option
    }
    
    /// <summary>
    /// IBKR base currency exchange rate
    /// </summary>
    type IBKRExchangeRate = {
        Currency: string
        Rate: decimal
    }
    
    /// <summary>
    /// IBKR forex balance position
    /// </summary>
    type IBKRForexBalance = {
        Currency: string
        Quantity: decimal
        CostBasisPrice: decimal option
        CostBasisMoney: decimal option
        ClosePrice: decimal option
        Value: decimal option
        UnrealizedPnL: decimal option
        UnrealizedPnLPercent: decimal option
    }
    
    /// <summary>
    /// Complete parsed IBKR statement data
    /// </summary>
    type IBKRStatementData = {
        StatementDate: DateTime option
        BrokerName: string option
        Trades: IBKRTrade list
        ForexTrades: IBKRForexTrade list
        CashMovements: IBKRCashMovement list
        CashFlows: IBKRCashFlow list
        OpenPositions: IBKROpenPosition list
        Instruments: IBKRInstrument list
        ExchangeRates: IBKRExchangeRate list
        ForexBalances: IBKRForexBalance list
    }
    
    /// <summary>
    /// IBKR CSV parsing result with error handling
    /// </summary>
    type IBKRParseResult = {
        Success: bool
        Data: IBKRStatementData option
        Errors: string list
        Warnings: string list
        SkippedSections: string list
    }
    
    /// <summary>
    /// Create empty IBKR statement data
    /// </summary>
    let createEmptyStatementData () = {
        StatementDate = None
        BrokerName = None
        Trades = []
        ForexTrades = []
        CashMovements = []
        CashFlows = []
        OpenPositions = []
        Instruments = []
        ExchangeRates = []
        ForexBalances = []
    }
    
    /// <summary>
    /// Create successful parse result
    /// </summary>
    let createSuccessResult (data: IBKRStatementData) = {
        Success = true
        Data = Some data
        Errors = []
        Warnings = []
        SkippedSections = []
    }
    
    /// <summary>
    /// Create failed parse result
    /// </summary>
    let createFailureResult (errors: string list) = {
        Success = false
        Data = None
        Errors = errors
        Warnings = []
        SkippedSections = []
    }