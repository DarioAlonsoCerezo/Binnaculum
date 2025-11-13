module internal TradeExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open Binnaculum.Core.Database.TypeParser
open DataReaderExtensions
open CommandExtensions
open Binnaculum.Core.SQL
open OptionExtensions
open Binnaculum.Core.Patterns

[<Extension>]
type Do() =

    [<Extension>]
    static member fill(trade: Trade, command: SqliteCommand) =
        command.fillEntityAuditable<Trade> (
            [ (SQLParameterName.TimeStamp, trade.TimeStamp.ToString())
              (SQLParameterName.TickerId, trade.TickerId)
              (SQLParameterName.BrokerAccountId, trade.BrokerAccountId)
              (SQLParameterName.CurrencyId, trade.CurrencyId)
              (SQLParameterName.Quantity, trade.Quantity)
              (SQLParameterName.Price, trade.Price.Value)
              (SQLParameterName.Commissions, trade.Commissions.Value)
              (SQLParameterName.Fees, trade.Fees.Value)
              (SQLParameterName.TradeCode, fromTradeCodeToDatabase trade.TradeCode)
              (SQLParameterName.TradeType, fromTradeTypeToDatabase trade.TradeType)
              (SQLParameterName.Leveraged, trade.Leveraged)
              (SQLParameterName.Notes, trade.Notes.ToDbValue()) ],
            trade
        )

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        { Id = reader.getInt32 FieldName.Id
          TimeStamp = reader.getDateTimePattern FieldName.TimeStamp
          TickerId = reader.getInt32 FieldName.TickerId
          BrokerAccountId = reader.getInt32 FieldName.BrokerAccountId
          CurrencyId = reader.getInt32 FieldName.CurrencyId
          Quantity = reader.getDecimal FieldName.Quantity
          Price = reader.getMoney FieldName.Price
          Commissions = reader.getMoney FieldName.Commissions
          Fees = reader.getMoney FieldName.Fees
          TradeCode =
            reader.GetString(reader.GetOrdinal(FieldName.TradeCode))
            |> fromDatabaseToTradeCode
          TradeType =
            reader.GetString(reader.GetOrdinal(FieldName.TradeType))
            |> fromDatabaseToTradeType
          Leveraged = reader.getDecimal FieldName.Leveraged
          Notes = reader.getStringOrNone FieldName.Notes
          Audit = reader.getAudit () }

    [<Extension>]
    static member save(trade: Trade) =
        Database.Do.saveEntity trade (fun t c -> t.fill c)

    [<Extension>]
    static member delete(trade: Trade) = Database.Do.deleteEntity trade

    static member getAll() =
        Database.Do.getAllEntities Do.read TradesQuery.getAll

    static member getById(id: int) =
        Database.Do.getById Do.read id TradesQuery.getById

    static member getBetweenDates(startDate: string, endDate: string) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- TradesQuery.getBetweenDates
            command.Parameters.AddWithValue(SQLParameterName.StartDate, startDate) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.EndDate, endDate) |> ignore
            let! trades = Database.Do.readAll<Trade> (command, Do.read)
            return trades
        }

    static member getByTickerCurrencyAndDateRange
        (tickerId: int, currencyId: int, fromDate: string option, toDate: string)
        =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- TradesQuery.getByTickerCurrencyAndDateRange
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore

            command.Parameters.AddWithValue(SQLParameterName.CurrencyId, currencyId)
            |> ignore

            command.Parameters.AddWithValue(SQLParameterName.StartDate, fromDate |> Option.defaultValue "1900-01-01")
            |> ignore

            command.Parameters.AddWithValue(SQLParameterName.EndDate, toDate) |> ignore
            let! trades = Database.Do.readAll<Trade> (command, Do.read)
            return trades
        }

    static member getFilteredTrades(tickerId: int, currencyId: int, startDate: string, endDate: string) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- TradesQuery.getFilteredTrades
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore

            command.Parameters.AddWithValue(SQLParameterName.CurrencyId, currencyId)
            |> ignore

            command.Parameters.AddWithValue(SQLParameterName.StartDate, startDate) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.EndDate, endDate) |> ignore
            let! trades = Database.Do.readAll<Trade> (command, Do.read)
            return trades
        }

    static member getCurrenciesByTickerAndDate(tickerId: int, date: string) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- TradesQuery.getCurrenciesByTickerAndDate
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.Date, date) |> ignore
            let! reader = command.ExecuteReaderAsync()
            let mutable currencies = []

            while reader.Read() do
                let currencyId = reader.GetInt32(0)
                currencies <- currencyId :: currencies

            reader.Close()
            return currencies |> List.rev
        }

    static member getDistinctCurrenciesByTickerAndDate(tickerId: int, date: string) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- TradesQuery.getDistinctCurrenciesByTickerAndDate
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.Date, date) |> ignore
            let! reader = command.ExecuteReaderAsync()
            let mutable currencies = []

            while reader.Read() do
                let currencyId = reader.GetInt32(0)
                currencies <- currencyId :: currencies

            reader.Close()
            return currencies |> List.distinct |> List.rev
        }

    static member getByBrokerAccountAndCurrency
        (brokerAccountId: int, currencyId: int, ?startDate: string, ?endDate: string)
        =
        task {
            let! command = Database.Do.createCommand ()

            match startDate, endDate with
            | Some s, Some e ->
                command.CommandText <- TradesQuery.getByBrokerAccountAndCurrencyWithDates

                command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId)
                |> ignore

                command.Parameters.AddWithValue(SQLParameterName.CurrencyId, currencyId)
                |> ignore

                command.Parameters.AddWithValue(SQLParameterName.StartDate, s) |> ignore
                command.Parameters.AddWithValue(SQLParameterName.EndDate, e) |> ignore
            | _ ->
                command.CommandText <- TradesQuery.getByBrokerAccountAndCurrency

                command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId)
                |> ignore

                command.Parameters.AddWithValue(SQLParameterName.CurrencyId, currencyId)
                |> ignore

            let! trades = Database.Do.readAll<Trade> (command, Do.read)
            return trades
        }

    static member getByBrokerAccountIdFromDate(brokerAccountId: int, startDate: DateTimePattern) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- TradesQuery.getByBrokerAccountIdFromDate

            command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId)
            |> ignore

            command.Parameters.AddWithValue(SQLParameterName.TimeStamp, startDate.ToString())
            |> ignore

            let! trades = Database.Do.readAll<Trade> (command, Do.read)
            return trades
        }

    static member getByBrokerAccountIdForDate(brokerAccountId: int, targetDate: DateTimePattern) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- TradesQuery.getByBrokerAccountIdForDate

            command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId)
            |> ignore

            command.Parameters.AddWithValue(SQLParameterName.TimeStamp, targetDate.ToString())
            |> ignore

            let! trades = Database.Do.readAll<Trade> (command, Do.read)
            return trades
        }

    static member getByTickerIdFromDate(tickerId: int, startDate: DateTimePattern) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- TradesQuery.getByTickerIdFromDate
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore

            command.Parameters.AddWithValue(SQLParameterName.TimeStamp, startDate.ToString())
            |> ignore

            let! trades = Database.Do.readAll<Trade> (command, Do.read)
            return trades
        }

    static member getEarliestForTicker(tickerId: int) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- TradesQuery.getEarliestForTicker
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore
            let! trade = Database.Do.read<Trade> (command, Do.read)
            return trade
        }

    /// <summary>
    /// Load trades with pagination support for a specific broker account.
    /// Returns trades ordered by TimeStamp DESC (most recent first).
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID to filter by</param>
    /// <param name="pageNumber">Zero-based page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>List of trades for the specified page</returns>
    static member loadTradesPaged(brokerAccountId: int, pageNumber: int, pageSize: int) =
        task {
            let offset = pageNumber * pageSize
            let! command = Database.Do.createCommand ()
            command.CommandText <- TradesQuery.getByBrokerAccountIdPaged
            command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId) |> ignore
            command.Parameters.AddWithValue("@PageSize", pageSize) |> ignore
            command.Parameters.AddWithValue("@Offset", offset) |> ignore
            let! trades = Database.Do.readAll<Trade> (command, Do.read)
            return trades
        }

/// <summary>
/// Financial calculation extension methods for Trade collections.
/// These methods provide reusable calculation logic for investment tracking and financial snapshot processing.
/// </summary>
[<Extension>]
type TradeCalculations() =

    /// <summary>
    /// Calculates the total investment amount from trades.
    /// This includes the cost basis of all buy trades (BuyToOpen, BuyToClose) plus associated costs.
    /// Formula: (Price × Quantity + Commissions + Fees) for each buy trade
    /// </summary>
    /// <param name="trades">List of trades to analyze</param>
    /// <returns>Total invested amount as Money</returns>
    [<Extension>]
    static member calculateTotalInvested(trades: Trade list) =
        trades
        |> List.filter (fun trade -> trade.TradeCode = TradeCode.BuyToOpen || trade.TradeCode = TradeCode.BuyToClose)
        |> List.sumBy (fun trade -> trade.Price.Value * trade.Quantity + trade.Commissions.Value + trade.Fees.Value)
        |> Money.FromAmount

    /// <summary>
    /// Calculates the total commission costs from all trades.
    /// Sums commission amounts from all trade types.
    /// </summary>
    /// <param name="trades">List of trades to analyze</param>
    /// <returns>Total commissions as Money</returns>
    [<Extension>]
    static member calculateTotalCommissions(trades: Trade list) =
        trades |> List.sumBy (fun trade -> trade.Commissions.Value) |> Money.FromAmount

    /// <summary>
    /// Calculates the total fee costs from all trades.
    /// Sums fee amounts from all trade types.
    /// </summary>
    /// <param name="trades">List of trades to analyze</param>
    /// <returns>Total fees as Money</returns>
    [<Extension>]
    static member calculateTotalFees(trades: Trade list) =
        trades |> List.sumBy (fun trade -> trade.Fees.Value) |> Money.FromAmount

    /// <summary>
    /// Calculates realized gains from closed positions using FIFO (First In, First Out) method.
    /// This method matches sell trades with corresponding buy trades to determine profit/loss.
    /// Realized gains = (Sell Price - Buy Price) × Quantity - (Total Commissions + Fees)
    /// </summary>
    /// <param name="trades">List of trades to analyze (should be sorted by timestamp)</param>
    /// <returns>Total realized gains as Money (can be negative for losses)</returns>
    [<Extension>]
    static member calculateRealizedGains(trades: Trade list) =
        // Group trades by ticker for position tracking
        let tradesByTicker =
            trades
            |> List.sortBy (fun trade -> trade.TimeStamp.Value)
            |> List.groupBy (fun trade -> trade.TickerId)

        let mutable totalRealizedGains = 0m

        // Process each ticker separately for FIFO calculation
        for (tickerId, tickerTrades) in tradesByTicker do
            let mutable buyQueue = [] // Queue of buy positions (FIFO)
            let mutable realizedGains = 0m

            for trade in tickerTrades do
                match trade.TradeCode with
                | TradeCode.BuyToOpen
                | TradeCode.BuyToClose ->
                    // Add to buy queue for future matching
                    let buyPosition =
                        {| Quantity = trade.Quantity
                           Price = trade.Price.Value
                           Commissions = trade.Commissions.Value
                           Fees = trade.Fees.Value |}

                    buyQueue <- buyQueue @ [ buyPosition ]

                | TradeCode.SellToOpen
                | TradeCode.SellToClose ->
                    // Match against buy positions using FIFO
                    let mutable remainingToSell = trade.Quantity
                    let mutable updatedBuyQueue = buyQueue

                    while remainingToSell > 0m && not updatedBuyQueue.IsEmpty do
                        let oldestBuy = updatedBuyQueue.Head
                        let quantityToMatch = min remainingToSell oldestBuy.Quantity

                        // Calculate realized gain for this matched portion
                        let sellValue = trade.Price.Value * quantityToMatch
                        let buyValue = oldestBuy.Price * quantityToMatch

                        let proportionalCosts =
                            (oldestBuy.Commissions
                             + oldestBuy.Fees
                             + trade.Commissions.Value
                             + trade.Fees.Value)
                            * (quantityToMatch / trade.Quantity)

                        realizedGains <- realizedGains + (sellValue - buyValue - proportionalCosts)
                        remainingToSell <- remainingToSell - quantityToMatch

                        // Update or remove buy position from queue
                        if oldestBuy.Quantity <= quantityToMatch then
                            updatedBuyQueue <- updatedBuyQueue.Tail
                        else
                            let reducedPosition =
                                {| oldestBuy with
                                    Quantity = oldestBuy.Quantity - quantityToMatch |}

                            updatedBuyQueue <- reducedPosition :: updatedBuyQueue.Tail

                    buyQueue <- updatedBuyQueue

            totalRealizedGains <- totalRealizedGains + realizedGains

        Money.FromAmount totalRealizedGains

    /// <summary>
    /// Determines if there are any open positions based on trade history.
    /// Calculates net position for each ticker and returns true if any positions remain open.
    /// </summary>
    /// <param name="trades">List of trades to analyze</param>
    /// <returns>True if open positions exist, false otherwise</returns>
    [<Extension>]
    static member hasOpenPositions(trades: Trade list) =
        let positionsByTicker =
            trades
            |> List.groupBy (fun trade -> trade.TickerId)
            |> List.map (fun (tickerId, tickerTrades) ->
                let netPosition =
                    tickerTrades
                    |> List.sumBy (fun trade ->
                        match trade.TradeCode with
                        | TradeCode.BuyToOpen -> trade.Quantity
                        | TradeCode.SellToClose -> -trade.Quantity
                        | TradeCode.SellToOpen -> -trade.Quantity // Short position
                        | TradeCode.BuyToClose -> trade.Quantity // Covering short
                    )

                (tickerId, netPosition))

        positionsByTicker |> List.exists (fun (_, netPosition) -> netPosition <> 0m)

    /// <summary>
    /// Calculates current position quantities by ticker.
    /// Returns a map of ticker ID to net position quantity.
    /// Positive values indicate long positions, negative values indicate short positions.
    /// </summary>
    /// <param name="trades">List of trades to analyze</param>
    /// <returns>Map of ticker ID to net position quantity</returns>
    [<Extension>]
    static member calculatePositions(trades: Trade list) =
        trades
        |> List.groupBy (fun trade -> trade.TickerId)
        |> List.map (fun (tickerId, tickerTrades) ->
            let netPosition =
                tickerTrades
                |> List.sumBy (fun trade ->
                    match trade.TradeCode with
                    | TradeCode.BuyToOpen -> trade.Quantity
                    | TradeCode.SellToClose -> -trade.Quantity
                    | TradeCode.SellToOpen -> -trade.Quantity // Short position
                    | TradeCode.BuyToClose -> trade.Quantity // Covering short
                )

            (tickerId, netPosition))
        |> List.filter (fun (_, position) -> position <> 0m) // Only return non-zero positions
        |> Map.ofList

    /// <summary>
    /// Calculates the average cost basis for current positions using FIFO method.
    /// Returns cost basis per share for each ticker with open positions.
    /// </summary>
    /// <param name="trades">List of trades to analyze (should be sorted by timestamp)</param>
    /// <returns>Map of ticker ID to average cost basis per share</returns>
    [<Extension>]
    static member calculateCostBasis(trades: Trade list) =
        let tradesByTicker =
            trades
            |> List.sortBy (fun trade -> trade.TimeStamp.Value)
            |> List.groupBy (fun trade -> trade.TickerId)

        [ for (tickerId, tickerTrades) in tradesByTicker do
              let mutable buyQueue = [] // Queue of buy positions with costs

              // Process trades to build final position queue
              for trade in tickerTrades do
                  match trade.TradeCode with
                  | TradeCode.BuyToOpen
                  | TradeCode.BuyToClose ->
                      let buyPosition =
                          {| Quantity = trade.Quantity
                             TotalCost = trade.Price.Value * trade.Quantity + trade.Commissions.Value + trade.Fees.Value |}

                      buyQueue <- buyQueue @ [ buyPosition ]

                  | TradeCode.SellToOpen
                  | TradeCode.SellToClose ->
                      // Remove sold quantities using FIFO
                      let mutable remainingToSell = trade.Quantity
                      let mutable updatedBuyQueue = buyQueue

                      while remainingToSell > 0m && not updatedBuyQueue.IsEmpty do
                          let oldestBuy = updatedBuyQueue.Head
                          let quantityToSell = min remainingToSell oldestBuy.Quantity

                          remainingToSell <- remainingToSell - quantityToSell

                          if oldestBuy.Quantity <= quantityToSell then
                              updatedBuyQueue <- updatedBuyQueue.Tail
                          else
                              let proportionalCost = oldestBuy.TotalCost * (quantityToSell / oldestBuy.Quantity)

                              let reducedPosition =
                                  {| Quantity = oldestBuy.Quantity - quantityToSell
                                     TotalCost = oldestBuy.TotalCost - proportionalCost |}

                              updatedBuyQueue <- reducedPosition :: updatedBuyQueue.Tail

                      buyQueue <- updatedBuyQueue

              // Calculate weighted average cost basis for remaining positions
              if not buyQueue.IsEmpty then
                  let totalQuantity = buyQueue |> List.sumBy (fun pos -> pos.Quantity)
                  let totalCost = buyQueue |> List.sumBy (fun pos -> pos.TotalCost)

                  if totalQuantity > 0m then
                      yield (tickerId, totalCost / totalQuantity) ]
        |> Map.ofList

    /// <summary>
    /// Counts the total number of trade transactions.
    /// This can be used for MovementCounter calculations in financial snapshots.
    /// </summary>
    /// <param name="trades">List of trades to count</param>
    /// <returns>Total number of trades as integer</returns>
    [<Extension>]
    static member calculateTradeCount(trades: Trade list) = trades.Length

    /// <summary>
    /// Filters trades by specific trade codes.
    /// Useful for focused calculations or reporting.
    /// </summary>
    /// <param name="trades">List of trades to filter</param>
    /// <param name="tradeCodes">List of trade codes to include</param>
    /// <returns>Filtered list of trades</returns>
    [<Extension>]
    static member filterByTradeCodes(trades: Trade list, tradeCodes: TradeCode list) =
        trades |> List.filter (fun trade -> tradeCodes |> List.contains trade.TradeCode)

    /// <summary>
    /// Filters trades by currency ID.
    /// </summary>
    /// <param name="trades">List of trades to filter</param>
    /// <param name="currencyId">The currency ID to filter by</param>
    /// <returns>Filtered list of trades for the specified currency</returns>
    [<Extension>]
    static member filterByCurrency(trades: Trade list, currencyId: int) =
        trades |> List.filter (fun trade -> trade.CurrencyId = currencyId)

    /// <summary>
    /// Gets all unique currency IDs involved in the trades.
    /// </summary>
    /// <param name="trades">List of trades to analyze</param>
    /// <returns>Set of unique currency IDs</returns>
    [<Extension>]
    static member getUniqueCurrencyIds(trades: Trade list) =
        trades |> List.map (fun trade -> trade.CurrencyId) |> Set.ofList

    /// <summary>
    /// Calculates a comprehensive trading summary for trades.
    /// Returns a record with all major trading metrics calculated.
    /// </summary>
    /// <param name="trades">List of trades to analyze</param>
    /// <param name="currencyId">Optional currency ID to filter calculations by</param>
    /// <returns>Trading summary record with calculated totals</returns>
    [<Extension>]
    static member calculateTradingSummary(trades: Trade list, ?currencyId: int) =
        let relevantTrades =
            match currencyId with
            | Some id -> trades.filterByCurrency (id)
            | None -> trades

        {| TotalInvested = relevantTrades.calculateTotalInvested ()
           TotalCommissions = relevantTrades.calculateTotalCommissions ()
           TotalFees = relevantTrades.calculateTotalFees ()
           RealizedGains = relevantTrades.calculateRealizedGains ()
           HasOpenPositions = relevantTrades.hasOpenPositions ()
           CurrentPositions = relevantTrades.calculatePositions ()
           CostBasis = relevantTrades.calculateCostBasis ()
           TradeCount = relevantTrades.calculateTradeCount ()
           UniqueCurrencies = relevantTrades.getUniqueCurrencyIds () |}
