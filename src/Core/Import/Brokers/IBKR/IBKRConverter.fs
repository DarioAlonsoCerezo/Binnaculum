namespace Binnaculum.Core.Import

open System
open System.Threading
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Logging
open IBKRModels

/// <summary>
/// Converts IBKR-specific transaction models to broker-agnostic domain models
/// Enables IBKR persistence through the broker-agnostic persistence layer
/// </summary>
module internal IBKRConverter =

    /// <summary>
    /// Get or create currency ID for the specified currency code
    /// </summary>
    let private getCurrencyId (currencyCode: string) =
        task {
            let! currencyOption = CurrencyExtensions.Do.getByCode (currencyCode) |> Async.AwaitTask

            match currencyOption with
            | Some currency -> return currency.Id
            | None ->
                // Create currency if it doesn't exist
                let newCurrency =
                    { Id = 0
                      Name = $"{currencyCode} Currency"
                      Code = currencyCode
                      Symbol = "$" // Default symbol, could be enhanced with proper lookup
                    }

                do! CurrencyExtensions.Do.save (newCurrency) |> Async.AwaitTask
                let! createdCurrency = CurrencyExtensions.Do.getByCode (currencyCode) |> Async.AwaitTask

                match createdCurrency with
                | Some c -> return c.Id
                | None -> return failwith $"Failed to create currency: {currencyCode}"
        }

    /// <summary>
    /// Get or create ticker ID for a given symbol
    /// </summary>
    let private getOrCreateTickerId (symbol: string) =
        task {
            let! tickers = TickerExtensions.Do.getAll () |> Async.AwaitTask
            let existingTicker = tickers |> List.tryFind (fun t -> t.Symbol = symbol)

            match existingTicker with
            | Some ticker -> return ticker.Id
            | None ->
                // Create new ticker
                let newTicker =
                    { Id = 0
                      Symbol = symbol
                      Image = None
                      Name = Some symbol
                      Audit = AuditableEntity.FromDateTime(DateTime.UtcNow) }

                do! TickerExtensions.Do.save (newTicker) |> Async.AwaitTask
                let! allTickers = TickerExtensions.Do.getAll () |> Async.AwaitTask
                let createdTicker = allTickers |> List.find (fun t -> t.Symbol = symbol)

                return createdTicker.Id
        }

    /// <summary>
    /// Convert IBKR cash movement to BrokerMovement domain object
    /// </summary>
    let private createBrokerMovementFromCashMovement
        (cashMovement: IBKRCashMovement)
        (brokerAccountId: int)
        (currencyId: int)
        : BrokerMovement =
        let movementType =
            match cashMovement.MovementType with
            | IBKRCashFlowType.Deposit -> BrokerMovementType.Deposit
            | IBKRCashFlowType.Withdrawal -> BrokerMovementType.Withdrawal
            | IBKRCashFlowType.Fee -> BrokerMovementType.Fee
            | IBKRCashFlowType.Commission -> BrokerMovementType.Fee
            | IBKRCashFlowType.InterestPayment -> BrokerMovementType.InterestsGained
            | IBKRCashFlowType.FXTranslationGain
            | IBKRCashFlowType.FXTranslationLoss -> BrokerMovementType.Conversion
            | IBKRCashFlowType.TradeSettlement -> BrokerMovementType.Deposit // Map to deposit for now

        let timeStamp = DateTimePattern.FromDateTime(cashMovement.SettleDate)
        let amount = Money.FromAmount(Math.Abs(cashMovement.Amount))
        let audit = AuditableEntity.FromDateTime(DateTime.UtcNow)

        { Id = 0
          TimeStamp = timeStamp
          Amount = amount
          CurrencyId = currencyId
          BrokerAccountId = brokerAccountId
          Commissions = Money.FromAmount(0m)
          Fees = Money.FromAmount(0m)
          MovementType = movementType
          Notes = Some cashMovement.Description
          FromCurrencyId = None
          AmountChanged = None
          TickerId = None
          Quantity = None
          Audit = audit }

    /// <summary>
    /// Convert IBKR forex trade to BrokerMovement domain object (currency conversion)
    /// </summary>
    let private createBrokerMovementFromForexTrade
        (forexTrade: IBKRForexTrade)
        (brokerAccountId: int)
        (baseCurrencyId: int)
        (quoteCurrencyId: int)
        : BrokerMovement =
        let timeStamp = DateTimePattern.FromDateTime(forexTrade.DateTime)
        let amount = Money.FromAmount(Math.Abs(forexTrade.Quantity))
        let commissions = Money.FromAmount(Math.Abs(forexTrade.CommissionFee))
        let audit = AuditableEntity.FromDateTime(DateTime.UtcNow)

        { Id = 0
          TimeStamp = timeStamp
          Amount = amount
          CurrencyId = baseCurrencyId
          BrokerAccountId = brokerAccountId
          Commissions = commissions
          Fees = Money.FromAmount(0m)
          MovementType = BrokerMovementType.Conversion
          Notes = Some $"Forex: {forexTrade.CurrencyPair}"
          FromCurrencyId = Some quoteCurrencyId
          AmountChanged = Some(Money.FromAmount(Math.Abs(forexTrade.Proceeds)))
          TickerId = None
          Quantity = None
          Audit = audit }

    /// <summary>
    /// Convert IBKR stock trade to Trade domain object
    /// </summary>
    let private createTradeFromIBKRTrade
        (ibkrTrade: IBKRTrade)
        (brokerAccountId: int)
        (currencyId: int)
        (tickerId: int)
        : Trade option =
        // Only handle stock trades
        if ibkrTrade.AssetCategory <> "Stocks" && ibkrTrade.AssetCategory <> "STK" then
            None
        else
            // Determine trade code and type based on quantity sign
            let (tradeCode, tradeType) =
                if ibkrTrade.Quantity > 0m then
                    (TradeCode.BuyToOpen, TradeType.Long)
                else
                    (TradeCode.SellToClose, TradeType.Short)

            let price =
                match ibkrTrade.TradePrice with
                | Some p -> p
                | None -> 
                    // Calculate from proceeds if trade price not available
                    if ibkrTrade.Quantity <> 0m then
                        Math.Abs(ibkrTrade.Proceeds / ibkrTrade.Quantity)
                    else
                        0m

            Some
                { Id = 0
                  TimeStamp = DateTimePattern.FromDateTime(ibkrTrade.DateTime)
                  TickerId = tickerId
                  BrokerAccountId = brokerAccountId
                  CurrencyId = currencyId
                  Quantity = Math.Abs(ibkrTrade.Quantity)
                  Price = Money.FromAmount(Math.Abs(price))
                  Commissions = Money.FromAmount(Math.Abs(ibkrTrade.CommissionFee))
                  Fees = Money.FromAmount(0m)
                  TradeCode = tradeCode
                  TradeType = tradeType
                  Leveraged = 1.0m
                  Notes = ibkrTrade.Code
                  Audit = AuditableEntity.FromDateTime(DateTime.UtcNow) }

    /// <summary>
    /// Convert IBKR statement data to domain models
    /// Main entry point for IBKR-to-domain conversion
    /// Supports session tracking for resumable imports (integrates with PR #420)
    /// </summary>
    let convertToDomainModels
        (statementData: IBKRStatementData)
        (brokerAccountId: int)
        (sessionId: int option)
        (cancellationToken: CancellationToken)
        : Threading.Tasks.Task<ImportDomainTypes.PersistenceInput> =
        task {
            let mutable brokerMovements = []
            let mutable stockTrades = []

            // Process cash movements
            for cashMovement in statementData.CashMovements do
                cancellationToken.ThrowIfCancellationRequested()

                try
                    let! currencyId = getCurrencyId cashMovement.Currency
                    let movement =
                        createBrokerMovementFromCashMovement cashMovement brokerAccountId currencyId

                    brokerMovements <- movement :: brokerMovements
                with ex ->
                    CoreLogger.logWarningf
                        "IBKRConverter"
                        "Error converting cash movement: %s"
                        ex.Message

            // Process forex trades as currency conversions
            for forexTrade in statementData.ForexTrades do
                cancellationToken.ThrowIfCancellationRequested()

                try
                    let! baseCurrencyId = getCurrencyId forexTrade.BaseCurrency
                    let! quoteCurrencyId = getCurrencyId forexTrade.QuoteCurrency
                    let movement =
                        createBrokerMovementFromForexTrade
                            forexTrade
                            brokerAccountId
                            baseCurrencyId
                            quoteCurrencyId

                    brokerMovements <- movement :: brokerMovements
                with ex ->
                    CoreLogger.logWarningf
                        "IBKRConverter"
                        "Error converting forex trade: %s"
                        ex.Message

            // Process stock trades
            for ibkrTrade in statementData.Trades do
                cancellationToken.ThrowIfCancellationRequested()

                try
                    // Only process stock trades
                    if ibkrTrade.AssetCategory = "Stocks" || ibkrTrade.AssetCategory = "STK" then
                        let! currencyId = getCurrencyId ibkrTrade.Currency
                        let! tickerId = getOrCreateTickerId ibkrTrade.Symbol

                        match createTradeFromIBKRTrade ibkrTrade brokerAccountId currencyId tickerId with
                        | Some trade -> stockTrades <- trade :: stockTrades
                        | None -> ()
                with ex ->
                    CoreLogger.logWarningf
                        "IBKRConverter"
                        "Error converting IBKR trade for symbol %s: %s"
                        ibkrTrade.Symbol
                        ex.Message

            return
                { ImportDomainTypes.PersistenceInput.BrokerMovements = brokerMovements
                  OptionTrades = [] // IBKR options not yet supported
                  StockTrades = stockTrades
                  Dividends = [] // IBKR dividends not yet supported
                  DividendTaxes = [] // IBKR dividend taxes not yet supported
                  SessionId = sessionId }
        }
