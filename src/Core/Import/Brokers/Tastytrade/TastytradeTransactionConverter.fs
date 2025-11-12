namespace Binnaculum.Core.Import

open TastytradeModels

/// <summary>
/// Converts parsed TastytradeTransaction records into appropriate counts by type
/// Handles Money Movements → BrokerMovement, Trades → OptionTrade/StockTrade
/// </summary>
module TastytradeTransactionConverter =

    /// <summary>
    /// Conversion result for a single transaction
    /// </summary>
    type ConversionResult =
        { BrokerMovementsCount: int
          OptionTradesCount: int
          StockTradesCount: int
          Errors: string list }

    /// <summary>
    /// Conversion statistics for the entire file
    /// </summary>
    type ConversionStats =
        { TotalTransactions: int
          BrokerMovementsCreated: int
          OptionTradesCreated: int
          StockTradesCreated: int
          ErrorsCount: int
          Errors: string list }

    /// <summary>
    /// Convert a single TastytradeTransaction to appropriate counts by type
    /// </summary>
    let convertTransaction (transaction: TastytradeTransaction) : ConversionResult =
        let mutable brokerMovementsCount = 0
        let mutable optionTradesCount = 0
        let mutable stockTradesCount = 0
        let mutable errors = []

        try
            match transaction.TransactionType with
            | MoneyMovement(_) ->
                // Money Movement types should create BrokerMovement records
                brokerMovementsCount <- 1
            | Trade(_, _) when transaction.InstrumentType = Some "Equity Option" ->
                // Equity option trades should create OptionTrade records
                optionTradesCount <- 1
            | Trade(_, _) when transaction.InstrumentType = Some "Future Option" ->
                // Future option trades should also create OptionTrade records
                optionTradesCount <- 1
            | Trade(_, _) when transaction.InstrumentType = Some "Equity" ->
                // Stock trades should create Trade records
                stockTradesCount <- 1
            | ReceiveDeliver(_) ->
                // ReceiveDeliver transactions (Expiration, Special Dividend, etc.)
                // are informational and don't create tradeable records - skip them
                ()
            | _ ->
                // Unknown transaction type
                errors <-
                    $"Unsupported transaction type: {transaction.TransactionType} for line {transaction.LineNumber}"
                    :: errors
        with ex ->
            errors <-
                $"Error converting transaction on line {transaction.LineNumber}: {ex.Message}"
                :: errors

        { BrokerMovementsCount = brokerMovementsCount
          OptionTradesCount = optionTradesCount
          StockTradesCount = stockTradesCount
          Errors = errors }

    /// <summary>
    /// Convert a list of TastytradeTransactions to counts by type with date sorting
    /// </summary>
    let convertTransactions (transactions: TastytradeTransaction list) : ConversionStats =
        // Sort transactions by date in ascending order (chronological)
        let sortedTransactions = transactions |> List.sortBy (fun t -> t.Date)

        let mutable totalBrokerMovements = 0
        let mutable totalOptionTrades = 0
        let mutable totalStockTrades = 0
        let mutable allErrors = []

        // Convert each transaction
        for transaction in sortedTransactions do
            let result = convertTransaction transaction
            totalBrokerMovements <- totalBrokerMovements + result.BrokerMovementsCount
            totalOptionTrades <- totalOptionTrades + result.OptionTradesCount
            totalStockTrades <- totalStockTrades + result.StockTradesCount
            allErrors <- List.append allErrors result.Errors

        { TotalTransactions = sortedTransactions.Length
          BrokerMovementsCreated = totalBrokerMovements
          OptionTradesCreated = totalOptionTrades
          StockTradesCreated = totalStockTrades
          ErrorsCount = allErrors.Length
          Errors = allErrors }
