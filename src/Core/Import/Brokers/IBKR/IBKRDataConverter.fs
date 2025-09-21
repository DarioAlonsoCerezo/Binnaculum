namespace Binnaculum.Core.Import

open System
open Binnaculum.Core
open IBKRModels

/// <summary>
/// Converts parsed IBKR data models into Binnaculum database models
/// Handles section-based conversion for deposits, forex trades, and stock trades
/// </summary>
module IBKRDataConverter =
    
    /// <summary>
    /// Convert IBKR cash movement to BrokerMovement model
    /// </summary>
    let convertCashMovementToBrokerMovement (movement: IBKRCashMovement) (brokerAccount: Binnaculum.Core.Models.BrokerAccount) (currency: Binnaculum.Core.Models.Currency) : Binnaculum.Core.Models.BrokerMovement =
        {
            Id = 0 // Will be set by database
            TimeStamp = movement.SettleDate
            Amount = movement.Amount
            Currency = currency
            BrokerAccount = brokerAccount
            Commissions = 0m
            Fees = 0m
            MovementType = 
                match movement.MovementType with
                | IBKRModels.Deposit -> Binnaculum.Core.Models.BrokerMovementType.Deposit
                | IBKRModels.Withdrawal -> Binnaculum.Core.Models.BrokerMovementType.Withdrawal
                | IBKRModels.Fee -> Binnaculum.Core.Models.BrokerMovementType.Fee
                | IBKRModels.Commission -> Binnaculum.Core.Models.BrokerMovementType.Fee
                | IBKRModels.InterestPayment -> Binnaculum.Core.Models.BrokerMovementType.InterestsGained
                | _ -> Binnaculum.Core.Models.BrokerMovementType.Conversion
            Notes = Some movement.Description
            FromCurrency = None
            AmountChanged = None
            Ticker = None
            Quantity = None
        }
    
    /// <summary>
    /// Convert IBKR forex trade to BrokerMovement model for currency conversion
    /// </summary>
    let convertForexTradeToBrokerMovement (forexTrade: IBKRForexTrade) (brokerAccount: Binnaculum.Core.Models.BrokerAccount) (fromCurrency: Binnaculum.Core.Models.Currency) (toCurrency: Binnaculum.Core.Models.Currency) : Binnaculum.Core.Models.BrokerMovement =
        {
            Id = 0 // Will be set by database
            TimeStamp = forexTrade.DateTime
            Amount = forexTrade.Proceeds // Amount received in quote currency
            Currency = toCurrency // Target currency (e.g., USD in GBP.USD)
            BrokerAccount = brokerAccount
            Commissions = Math.Abs(forexTrade.CommissionFee)
            Fees = 0m
            MovementType = Binnaculum.Core.Models.BrokerMovementType.Conversion
            Notes = Some $"Forex conversion: {forexTrade.CurrencyPair}"
            FromCurrency = Some fromCurrency // Source currency (e.g., GBP in GBP.USD)
            AmountChanged = Some (Math.Abs(forexTrade.Quantity)) // Amount sold from base currency
            Ticker = None
            Quantity = None
        }
    
    /// <summary>
    /// Convert IBKR stock trade to Trade model
    /// </summary>
    let convertStockTradeToTrade (stockTrade: IBKRTrade) (brokerAccount: Binnaculum.Core.Models.BrokerAccount) (currency: Binnaculum.Core.Models.Currency) (ticker: Binnaculum.Core.Models.Ticker) : Binnaculum.Core.Models.Trade =
        let isLongTrade = stockTrade.Quantity > 0m
        let tradeType = if isLongTrade then Binnaculum.Core.Models.TradeType.Long else Binnaculum.Core.Models.TradeType.Short
        let tradeCode = if isLongTrade then Binnaculum.Core.Models.TradeCode.BuyToOpen else Binnaculum.Core.Models.TradeCode.SellToOpen
        
        {
            Id = 0 // Will be set by database
            TimeStamp = stockTrade.DateTime
            TotalInvestedAmount = Math.Abs(stockTrade.Proceeds)
            Ticker = ticker
            BrokerAccount = brokerAccount
            Currency = currency
            Quantity = Math.Abs(stockTrade.Quantity)
            Price = stockTrade.TradePrice |> Option.defaultValue 0m
            Commissions = Math.Abs(stockTrade.CommissionFee)
            Fees = 0m
            TradeCode = tradeCode
            TradeType = tradeType
            Leveraged = 1m // IBKR doesn't specify leverage in basic trades
            Notes = stockTrade.Code
        }
    
    /// <summary>
    /// Determine if an IBKR trade is a stock trade (not forex)
    /// </summary>
    let isStockTrade (trade: IBKRTrade) : bool =
        trade.AssetCategory = "Stocks" || trade.AssetCategory = "STK"
    
    /// <summary>
    /// Extract currency code from IBKR forex pair (e.g., "GBP.USD" -> ("GBP", "USD"))
    /// </summary>
    let parseForexCurrencyPair (currencyPair: string) : string * string =
        let parts = currencyPair.Split('.')
        if parts.Length = 2 then 
            parts.[0].Trim(), parts.[1].Trim()
        else 
            failwith $"Invalid forex currency pair format: {currencyPair}"
    
    /// <summary>
    /// Convert IBKR statement data to database models
    /// Returns tuple of (BrokerMovements, Trades) that can be saved to database
    /// </summary>
    let convertStatementToModels (statement: IBKRStatementData) (brokerAccount: Binnaculum.Core.Models.BrokerAccount) 
                                (getCurrency: string -> Binnaculum.Core.Models.Currency option) 
                                (getTicker: string -> Binnaculum.Core.Models.Ticker option) : (Binnaculum.Core.Models.BrokerMovement list) * (Binnaculum.Core.Models.Trade list) =
        
        let brokerMovements = ResizeArray<Binnaculum.Core.Models.BrokerMovement>()
        let trades = ResizeArray<Binnaculum.Core.Models.Trade>()
        
        // Convert cash movements (deposits/withdrawals)
        for cashMovement in statement.CashMovements do
            match getCurrency cashMovement.Currency with
            | Some currency ->
                let movement = convertCashMovementToBrokerMovement cashMovement brokerAccount currency
                brokerMovements.Add(movement)
            | None ->
                // Skip if currency not found - could log warning
                ()
        
        // Convert forex trades to currency conversion movements
        for forexTrade in statement.ForexTrades do
            try
                let baseCurrency, quoteCurrency = parseForexCurrencyPair forexTrade.CurrencyPair
                match getCurrency baseCurrency, getCurrency quoteCurrency with
                | Some fromCurr, Some toCurr ->
                    let movement = convertForexTradeToBrokerMovement forexTrade brokerAccount fromCurr toCurr
                    brokerMovements.Add(movement)
                | _ ->
                    // Skip if currencies not found
                    ()
            with
            | _ ->
                // Skip invalid forex pairs
                ()
        
        // Convert stock trades
        for stockTrade in statement.Trades do
            if isStockTrade stockTrade then
                match getCurrency stockTrade.Currency, getTicker stockTrade.Symbol with
                | Some currency, Some ticker ->
                    let trade = convertStockTradeToTrade stockTrade brokerAccount currency ticker
                    trades.Add(trade)
                | _ ->
                    // Skip if currency or ticker not found
                    ()
        
        (brokerMovements |> List.ofSeq, trades |> List.ofSeq)