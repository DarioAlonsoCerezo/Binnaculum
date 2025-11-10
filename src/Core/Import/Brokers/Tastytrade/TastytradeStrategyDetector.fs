namespace Binnaculum.Core.Import

open TastytradeModels

/// <summary>
/// Multi-leg strategy detection for complex option transactions grouped by Order #
/// Identifies spreads, straddles, and other multi-leg strategies
/// </summary>
module TastytradeStrategyDetector =

    /// <summary>
    /// Group transactions by Order # to identify multi-leg strategies
    /// </summary>
    /// <param name="transactions">List of all transactions</param>
    /// <returns>List of strategy groups</returns>
    let groupTransactionsByOrder (transactions: TastytradeTransaction list) : TastytradeStrategy list =
        transactions
        |> List.filter (fun t -> t.OrderNumber.IsSome)
        |> List.groupBy (fun t -> t.OrderNumber.Value)
        |> List.map (fun (orderNumber, transactionList) ->
            {
                OrderNumber = orderNumber
                Transactions = transactionList |> List.sortBy (fun t -> t.Date)
                StrategyType = None // Will be determined by detectStrategyType
            })

    /// <summary>
    /// Detect the type of strategy based on the transactions in a group
    /// </summary>
    /// <param name="transactions">Transactions in the same order</param>
    /// <returns>Detected strategy type</returns>
    let private detectStrategyType (transactions: TastytradeTransaction list) : StrategyType =
        let optionTransactions = 
            transactions 
            |> List.filter (fun t -> TransactionTypeDetection.isOptionTransaction t.InstrumentType)

        let equityTransactions = 
            transactions
            |> List.filter (fun t -> TransactionTypeDetection.isEquityTrade t.InstrumentType)

        match optionTransactions.Length, equityTransactions.Length with
        | 1, 0 -> SingleLeg
        | 2, 0 -> 
            // Analyze the two option transactions
            let calls = optionTransactions |> List.filter (fun t -> t.CallOrPut = Some "CALL")
            let puts = optionTransactions |> List.filter (fun t -> t.CallOrPut = Some "PUT")
            let strikes = optionTransactions |> List.map (fun t -> t.StrikePrice) |> List.choose id |> List.distinct
            let expirations = optionTransactions |> List.map (fun t -> t.ExpirationDate) |> List.choose id |> List.distinct

            match calls.Length, puts.Length with
            | 1, 1 when strikes.Length = 1 -> Straddle // Same strike, different option types
            | 1, 1 when strikes.Length = 2 -> Strangle // Different strikes, different option types
            | 2, 0 | 0, 2 -> 
                // Both same type (calls or puts)
                if strikes.Length = 2 && expirations.Length = 1 then VerticalSpread
                elif strikes.Length = 1 && expirations.Length = 2 then CalendarSpread
                else Unknown
            | _ -> Unknown
        | 4, 0 ->
            // Could be Iron Condor (2 calls + 2 puts with 4 different strikes)
            let calls = optionTransactions |> List.filter (fun t -> t.CallOrPut = Some "CALL")
            let puts = optionTransactions |> List.filter (fun t -> t.CallOrPut = Some "PUT")
            let strikes = optionTransactions |> List.map (fun t -> t.StrikePrice) |> List.choose id |> List.distinct

            if calls.Length = 2 && puts.Length = 2 && strikes.Length = 4 then
                IronCondor
            else
                Unknown
        | _ -> Unknown

    /// <summary>
    /// Analyze and classify all strategy groups
    /// </summary>
    /// <param name="strategies">Strategy groups to classify</param>
    /// <returns>Strategies with detected types</returns>
    let classifyStrategies (strategies: TastytradeStrategy list) : TastytradeStrategy list =
        strategies
        |> List.map (fun strategy ->
            let strategyType = detectStrategyType strategy.Transactions
            { strategy with StrategyType = Some strategyType })

    /// <summary>
    /// Complete strategy detection workflow
    /// </summary>
    /// <param name="transactions">All transactions to analyze</param>
    /// <returns>Detected and classified strategies</returns>
    let detectStrategies (transactions: TastytradeTransaction list) : TastytradeStrategy list =
        transactions
        |> groupTransactionsByOrder
        |> classifyStrategies

    /// <summary>
    /// Get transactions that are not part of any multi-leg strategy
    /// </summary>
    /// <param name="allTransactions">All transactions</param>
    /// <param name="strategies">Detected strategies</param>
    /// <returns>Individual transactions not part of strategies</returns>
    let getIndividualTransactions (allTransactions: TastytradeTransaction list) (strategies: TastytradeStrategy list) : TastytradeTransaction list =
        let strategyOrderNumbers = 
            strategies 
            |> List.map (fun s -> s.OrderNumber)
            |> Set.ofList

        allTransactions
        |> List.filter (fun t -> 
            match t.OrderNumber with
            | Some orderNum -> not (Set.contains orderNum strategyOrderNumbers)
            | None -> true)

    /// <summary>
    /// Validate that a strategy group has consistent characteristics
    /// </summary>
    /// <param name="strategy">Strategy to validate</param>
    /// <returns>List of validation warnings</returns>
    let validateStrategy (strategy: TastytradeStrategy) : string list =
        let warnings = []
        
        let underlyingSymbols = 
            strategy.Transactions 
            |> List.map (fun t -> t.UnderlyingSymbol)
            |> List.choose id
            |> List.distinct

        let currencies = 
            strategy.Transactions
            |> List.map (fun t -> t.Currency)
            |> List.distinct

        let warnings = 
            if underlyingSymbols.Length > 1 then
                let symbolsStr = String.concat ", " underlyingSymbols
                $"Strategy {strategy.OrderNumber} has multiple underlying symbols: {symbolsStr}" :: warnings
            else warnings

        let warnings = 
            if currencies.Length > 1 then
                let currenciesStr = String.concat ", " currencies
                $"Strategy {strategy.OrderNumber} has multiple currencies: {currenciesStr}" :: warnings
            else warnings

        warnings

    /// <summary>
    /// Get summary statistics for detected strategies
    /// </summary>
    /// <param name="strategies">List of strategies</param>
    /// <returns>Summary information</returns>
    let getStrategySummary (strategies: TastytradeStrategy list) : Map<StrategyType, int> =
        strategies
        |> List.choose (fun s -> s.StrategyType)
        |> List.groupBy id
        |> List.map (fun (strategyType, strategies) -> strategyType, strategies.Length)
        |> Map.ofList

    /// <summary>
    /// Find strategies that involve a specific ticker
    /// </summary>
    /// <param name="ticker">Ticker symbol to search for</param>
    /// <param name="strategies">List of strategies to search</param>
    /// <returns>Strategies involving the ticker</returns>
    let findStrategiesByTicker (ticker: string) (strategies: TastytradeStrategy list) : TastytradeStrategy list =
        strategies
        |> List.filter (fun strategy ->
            strategy.Transactions
            |> List.exists (fun t ->
                match t.UnderlyingSymbol with
                | Some symbol -> symbol.Equals(ticker, System.StringComparison.OrdinalIgnoreCase)
                | None -> 
                    match t.RootSymbol with
                    | Some symbol -> symbol.Equals(ticker, System.StringComparison.OrdinalIgnoreCase)
                    | None -> false))

    /// <summary>
    /// Get all unique tickers involved in strategies
    /// </summary>
    /// <param name="strategies">List of strategies</param>
    /// <returns>Set of unique ticker symbols</returns>
    let getStrategyTickers (strategies: TastytradeStrategy list) : Set<string> =
        strategies
        |> List.collect (fun strategy ->
            strategy.Transactions
            |> List.choose (fun t -> 
                t.UnderlyingSymbol 
                |> Option.orElse t.RootSymbol))
        |> Set.ofList