namespace Binnaculum.Core.Import

open System
open TastytradeModels
open Binnaculum.Core
open Binnaculum.Core.Database.DatabaseExtensions

/// <summary>
/// Converter for transforming Tastytrade transactions into Binnaculum database models
/// Handles options, equities, money movements, and ACAT transfers
/// </summary>
module TastytradeDataConverter =

    /// <summary>
    /// Convert Tastytrade option transactions to OptionTrade models
    /// </summary>
    /// <param name="transactions">Filtered option transactions</param>
    /// <param name="brokerAccountId">Target broker account ID</param>
    /// <returns>List of OptionTrade models</returns>
    let convertEquityOptionTrades (transactions: TastytradeTransaction list) (brokerAccountId: int) : Models.OptionTrade list =
        transactions
        |> List.filter (fun t -> TransactionTypeDetection.isOptionTransaction t.InstrumentType)
        |> List.map (fun transaction ->
            // Extract ticker from symbol or root symbol
            let tickerSymbol = 
                match transaction.RootSymbol, transaction.UnderlyingSymbol with
                | Some root, _ -> root
                | None, Some underlying -> underlying
                | None, None -> failwith $"No ticker information found in transaction: {transaction.Symbol}"

            // Get or create ticker
            let ticker = TickerExtensions.Do.getOrCreateBySymbol tickerSymbol |> Async.RunSynchronously

            // Get broker account
            let brokerAccount = BrokerAccountExtensions.Do.getById brokerAccountId |> Async.RunSynchronously
            let brokerAccountModel = brokerAccount.brokerAccountToModel()

            // Get currency (default to USD for Tastytrade)
            let currency = CurrencyExtensions.Do.getByCode transaction.Currency |> Async.RunSynchronously
            let currencyModel = currency.currencyToModel()

            // Map option type
            let optionType = 
                match transaction.CallOrPut with
                | Some "CALL" -> Models.OptionType.Call
                | Some "PUT" -> Models.OptionType.Put
                | _ -> failwith $"Invalid or missing option type: {transaction.CallOrPut}"

            // Map option code from transaction type
            let optionCode = 
                match transaction.TransactionType with
                | Trade(BuyToOpen, _) -> Models.OptionCode.BuyToOpen
                | Trade(SellToOpen, _) -> Models.OptionCode.SellToOpen
                | Trade(BuyToClose, _) -> Models.OptionCode.BuyToClose
                | Trade(SellToClose, _) -> Models.OptionCode.SellToClose
                | _ -> failwith $"Invalid option transaction type: {transaction.TransactionType}"

            // Calculate premium and net premium
            let premium = abs transaction.Value
            let netPremium = transaction.Value - transaction.Commissions - transaction.Fees

            // Get expiration date
            let expirationDate = 
                match transaction.ExpirationDate with
                | Some date -> date
                | None -> failwith $"Missing expiration date for option transaction: {transaction.Description}"

            // Get strike price
            let strike = 
                match transaction.StrikePrice with
                | Some price -> price
                | None -> failwith $"Missing strike price for option transaction: {transaction.Description}"

            // Get multiplier (default 100 for equity options)
            let multiplier = 
                match transaction.Multiplier with
                | Some mult -> mult
                | None -> 100m

            {
                Id = 0 // Will be set by database
                TimeStamp = transaction.Date
                ExpirationDate = expirationDate
                Premium = premium
                NetPremium = netPremium
                Ticker = ticker
                BrokerAccount = brokerAccountModel
                Currency = currencyModel
                OptionType = optionType
                Code = optionCode
                Strike = strike
                Commissions = abs transaction.Commissions
                Fees = abs transaction.Fees
                IsOpen = true // Will be updated by trade matching logic
                ClosedWith = 0 // Will be set by trade matching
                Multiplier = multiplier
                Quantity = int (abs transaction.Quantity)
                Notes = Some transaction.Description
                FeesPerOperation = false
            })

    /// <summary>
    /// Convert Tastytrade equity transactions to Trade models
    /// </summary>
    /// <param name="transactions">Filtered equity transactions</param>
    /// <param name="brokerAccountId">Target broker account ID</param>
    /// <returns>List of Trade models</returns>
    let convertEquityTrades (transactions: TastytradeTransaction list) (brokerAccountId: int) : Models.Trade list =
        transactions
        |> List.filter (fun t -> TransactionTypeDetection.isEquityTrade t.InstrumentType)
        |> List.map (fun transaction ->
            // Extract ticker symbol
            let tickerSymbol = 
                match transaction.Symbol with
                | Some symbol -> symbol
                | None -> failwith $"No ticker symbol found in equity transaction: {transaction.Description}"

            // Get or create ticker
            let ticker = TickerExtensions.Do.getOrCreateBySymbol tickerSymbol |> Async.RunSynchronously

            // Get broker account
            let brokerAccount = BrokerAccountExtensions.Do.getById brokerAccountId |> Async.RunSynchronously
            let brokerAccountModel = brokerAccount.brokerAccountToModel()

            // Get currency
            let currency = CurrencyExtensions.Do.getByCode transaction.Currency |> Async.RunSynchronously
            let currencyModel = currency.currencyToModel()

            // Determine trade code and type
            let tradeCode, tradeType = 
                match transaction.TransactionType with
                | Trade(BuyToOpen, _) -> Models.TradeCode.BuyToOpen, Models.TradeType.Long
                | Trade(SellToOpen, _) -> Models.TradeCode.SellToOpen, Models.TradeType.Short
                | Trade(BuyToClose, _) -> Models.TradeCode.BuyToClose, Models.TradeType.Long
                | Trade(SellToClose, _) -> Models.TradeCode.SellToClose, Models.TradeType.Short
                | _ -> failwith $"Invalid equity transaction type: {transaction.TransactionType}"

            // Calculate price and total
            let price = 
                match transaction.AveragePrice with
                | Some avgPrice -> abs avgPrice
                | None -> 
                    if transaction.Quantity <> 0m then abs (transaction.Value / transaction.Quantity)
                    else failwith $"Cannot determine price for equity transaction: {transaction.Description}"

            let quantity = abs transaction.Quantity
            let totalInvestedAmount = abs transaction.Value

            {
                Id = 0 // Will be set by database
                TimeStamp = transaction.Date
                TotalInvestedAmount = totalInvestedAmount
                Ticker = ticker
                BrokerAccount = brokerAccountModel
                Currency = currencyModel
                Quantity = quantity
                Price = price
                Commissions = abs transaction.Commissions
                Fees = abs transaction.Fees
                TradeCode = tradeCode
                TradeType = tradeType
                Leveraged = 1m
                Notes = Some transaction.Description
            })

    /// <summary>
    /// Convert Tastytrade money movements to BrokerMovement models
    /// </summary>
    /// <param name="transactions">Filtered money movement transactions</param>
    /// <param name="brokerAccountId">Target broker account ID</param>
    /// <returns>List of BrokerMovement models</returns>
    let convertMoneyMovements (transactions: TastytradeTransaction list) (brokerAccountId: int) : Models.BrokerMovement list =
        transactions
        |> List.filter (fun t -> 
            match t.TransactionType with
            | MoneyMovement(_) -> true
            | _ -> false)
        |> List.map (fun transaction ->
            // Get broker account
            let brokerAccount = BrokerAccountExtensions.Do.getById brokerAccountId |> Async.RunSynchronously
            let brokerAccountModel = brokerAccount.brokerAccountToModel()

            // Get currency
            let currency = CurrencyExtensions.Do.getByCode transaction.Currency |> Async.RunSynchronously
            let currencyModel = currency.currencyToModel()

            // Map movement type
            let movementType = 
                match transaction.TransactionType with
                | MoneyMovement(Deposit) -> Models.BrokerMovementType.Deposit
                | MoneyMovement(Withdrawal) -> Models.BrokerMovementType.Withdrawal
                | MoneyMovement(BalanceAdjustment) -> Models.BrokerMovementType.Fee
                | MoneyMovement(CreditInterest) -> Models.BrokerMovementType.InterestsGained
                | MoneyMovement(Transfer) -> 
                    // Determine direction based on amount sign
                    if transaction.Value >= 0m then Models.BrokerMovementType.Deposit
                    else Models.BrokerMovementType.Withdrawal
                | _ -> failwith $"Not a money movement transaction: {transaction.TransactionType}"

            {
                Id = 0 // Will be set by database
                TimeStamp = transaction.Date
                Amount = transaction.Value
                Currency = currencyModel
                BrokerAccount = brokerAccountModel
                Commissions = abs transaction.Commissions
                Fees = abs transaction.Fees
                MovementType = movementType
                Notes = Some transaction.Description
                FromCurrency = None
                AmountChanged = None
                Ticker = None
                Quantity = None
            })

    /// <summary>
    /// Convert Tastytrade ACAT transfers to BrokerMovement models
    /// </summary>
    /// <param name="transactions">Filtered ACAT transactions</param>
    /// <param name="brokerAccountId">Target broker account ID</param>
    /// <returns>List of BrokerMovement models</returns>
    let convertACATTransfers (transactions: TastytradeTransaction list) (brokerAccountId: int) : Models.BrokerMovement list =
        transactions
        |> List.filter (fun t -> 
            match t.TransactionType with
            | ReceiveDeliver("ACAT") -> true
            | _ -> false)
        |> List.map (fun transaction ->
            // Get broker account
            let brokerAccount = BrokerAccountExtensions.Do.getById brokerAccountId |> Async.RunSynchronously
            let brokerAccountModel = brokerAccount.brokerAccountToModel()

            // Get currency
            let currency = CurrencyExtensions.Do.getByCode transaction.Currency |> Async.RunSynchronously
            let currencyModel = currency.currencyToModel()

            // Handle both cash and securities transfers
            let ticker, quantity = 
                match transaction.Symbol with
                | Some symbol when not (String.IsNullOrWhiteSpace(symbol)) ->
                    let ticker = TickerExtensions.Do.getOrCreateBySymbol symbol |> Async.RunSynchronously
                    Some ticker, Some (abs transaction.Quantity)
                | _ -> None, None

            {
                Id = 0 // Will be set by database
                TimeStamp = transaction.Date
                Amount = transaction.Value
                Currency = currencyModel
                BrokerAccount = brokerAccountModel
                Commissions = 0m // ACAT transfers typically have no commissions
                Fees = 0m
                MovementType = 
                    if ticker.IsSome then Models.BrokerMovementType.ACATSecuritiesTransferReceived
                    else Models.BrokerMovementType.ACATMoneyTransferReceived
                Notes = Some transaction.Description
                FromCurrency = None
                AmountChanged = None
                Ticker = ticker
                Quantity = quantity
            })

    /// <summary>
    /// Convert all Tastytrade transactions to appropriate database models
    /// </summary>
    /// <param name="parsingResult">Parsed transaction data</param>
    /// <param name="brokerAccountId">Target broker account ID</param>
    /// <returns>Tuple of all converted models</returns>
    let convertAllTransactions (parsingResult: TastytradeParsingResult) (brokerAccountId: int) : 
        (Models.OptionTrade list * Models.Trade list * Models.BrokerMovement list) =
        
        let optionTrades = convertEquityOptionTrades parsingResult.Transactions brokerAccountId
        let equityTrades = convertEquityTrades parsingResult.Transactions brokerAccountId
        let moneyMovements = convertMoneyMovements parsingResult.Transactions brokerAccountId
        let acatTransfers = convertACATTransfers parsingResult.Transactions brokerAccountId
        
        let allBrokerMovements = moneyMovements @ acatTransfers
        
        (optionTrades, equityTrades, allBrokerMovements)

    /// <summary>
    /// Get summary of converted data
    /// </summary>
    /// <param name="optionTrades">Converted option trades</param>
    /// <param name="equityTrades">Converted equity trades</param>
    /// <param name="brokerMovements">Converted broker movements</param>
    /// <returns>ImportedDataSummary</returns>
    let getConversionSummary (optionTrades: Models.OptionTrade list) (equityTrades: Models.Trade list) 
                            (brokerMovements: Models.BrokerMovement list) : ImportedDataSummary =
        let newTickers = 
            let optionTickers = optionTrades |> List.map (fun t -> t.Ticker.Symbol) |> Set.ofList
            let equityTickers = equityTrades |> List.map (fun t -> t.Ticker.Symbol) |> Set.ofList
            let movementTickers = 
                brokerMovements 
                |> List.choose (fun m -> m.Ticker |> Option.map (fun t -> t.Symbol))
                |> Set.ofList
            
            Set.unionMany [optionTickers; equityTickers; movementTickers] |> Set.count

        {
            Trades = equityTrades.Length
            BrokerMovements = brokerMovements.Length
            Dividends = 0 // Tastytrade transactions don't include dividend data
            OptionTrades = optionTrades.Length
            NewTickers = newTickers
        }