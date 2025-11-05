namespace Binnaculum.Core.Storage

open System
open System.Threading.Tasks
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Storage.SnapshotManagerUtils
open Binnaculum.Core.Logging
open Microsoft.Maui.Storage
open Binnaculum.Core.Providers
open Binnaculum.Core.Keys
open TickerSnapshotExtensions
open TickerCurrencySnapshotExtensions
open TradeExtensions

type private SnapshotCalculationData =
    { Trades: Trade list
      Dividends: Dividend list
      DividendTaxes: DividendTax list
      OptionTrades: OptionTrade list
      LatestPrice: decimal
      PreviousSnapshot: TickerCurrencySnapshot option }

type private SnapshotCalculationResult =
    { TotalShares: decimal
      CostBasis: decimal
      RealCost: decimal
      Dividends: decimal
      Options: decimal
      TotalIncomes: decimal
      Unrealized: decimal
      Realized: decimal
      Performance: decimal
      OpenTrades: bool }

module internal TickerSnapshotManager =

    /// Calculate snapshot values based on historical data and previous snapshot
    let private calculateSnapshotValues (data: SnapshotCalculationData) : SnapshotCalculationResult =
        let trades = data.Trades
        let dividends = data.Dividends
        let optionTrades = data.OptionTrades
        let latestPrice = data.LatestPrice
        let prevSnapshot = data.PreviousSnapshot

        // Calculate shares and cost basis
        let totalShares =
            let prevShares =
                prevSnapshot |> Option.map (fun s -> s.TotalShares) |> Option.defaultValue 0.0M

            prevShares + (trades |> List.sumBy (fun t -> t.Quantity))

        let tradeCostBasis = trades |> List.sumBy (fun t -> t.Price.Value * t.Quantity)

        let costBasis =
            let prevCost =
                prevSnapshot
                |> Option.map (fun s -> s.CostBasis.Value)
                |> Option.defaultValue 0.0M

            prevCost + tradeCostBasis

        // Calculate commissions and fees from trades
        let commissions = trades |> List.sumBy (fun t -> t.Commissions.Value)
        let fees = trades |> List.sumBy (fun t -> t.Fees.Value)

        // Calculate dividends (gross dividends minus taxes)
        let dividendAmount =
            let currentDividends = dividends |> List.sumBy (fun d -> d.DividendAmount.Value)

            let currentDividendTaxes =
                data.DividendTaxes |> List.sumBy (fun dt -> dt.DividendTaxAmount.Value)

            let netDividends = currentDividends - currentDividendTaxes

            let prevDividends =
                prevSnapshot
                |> Option.map (fun s -> s.Dividends.Value)
                |> Option.defaultValue 0.0M

            prevDividends + netDividends

        // Calculate options income
        let optionsIncome =
            let currentOptions = optionTrades |> List.sumBy (fun o -> o.NetPremium.Value)

            let prevOptions =
                prevSnapshot
                |> Option.map (fun s -> s.Options.Value)
                |> Option.defaultValue 0.0M

            prevOptions + currentOptions

        // Calculate total incomes
        let totalIncomes = dividendAmount + optionsIncome

        // Calculate real cost (cost basis + commissions + fees + dividend taxes)
        let dividendTaxAmount =
            data.DividendTaxes |> List.sumBy (fun dt -> dt.DividendTaxAmount.Value)

        let realCost = costBasis + commissions + fees + dividendTaxAmount

        // Calculate unrealized gains/losses
        let marketValue = latestPrice * totalShares
        let unrealized = marketValue - costBasis

        // Calculate realized gains (pass-through from previous snapshot)
        let realized =
            let prevRealized =
                prevSnapshot
                |> Option.map (fun s -> s.Realized.Value)
                |> Option.defaultValue 0.0m

            prevRealized

        // Calculate performance percentage
        let performance =
            if costBasis <> 0.0m then
                (unrealized / costBasis) * 100.0m
            else
                0.0m

        // Check for open trades - include both share positions and open option trades
        // Per business rules: OpenTrades = true if (TotalShares > 0) OR (Unrealized ≠ 0)
        // Unrealized gains/losses only exist if position is open at this snapshot date
        let hasOpenShares = totalShares <> 0.0m
        let hasOpenOptions = unrealized <> 0.0m // Any unrealized = position open at this date
        let openTrades = hasOpenShares || hasOpenOptions

        { TotalShares = totalShares
          CostBasis = costBasis
          RealCost = realCost
          Dividends = dividendAmount
          Options = optionsIncome
          TotalIncomes = totalIncomes
          Unrealized = unrealized
          Realized = realized
          Performance = performance
          OpenTrades = openTrades }

    /// Get all relevant currencies for a ticker based on trades, prices, dividends, and options using optimized SQL queries
    let private getRelevantCurrencies (tickerId: int) (date: DateTimePattern) =
        task {
            let dateStr = date.Value.ToString("yyyy-MM-dd")

            // Get currencies from trades for this ticker on this date using optimized query
            let! tradeCurrencies = TradeExtensions.Do.getCurrenciesByTickerAndDate (tickerId, dateStr)

            // Get currencies from prices for this ticker on this date using optimized query
            let! priceCurrencies = TickerPriceExtensions.Do.getCurrenciesByTickerAndDate (tickerId, dateStr)

            // Get currencies from dividends for this ticker on this date using optimized query
            let! dividendCurrencies = DividendExtensions.Do.getCurrenciesByTickerAndDate (tickerId, dateStr)

            // Get currencies from dividend taxes for this ticker on this date using optimized query
            let! dividendTaxCurrencies = DividendTaxExtensions.Do.getCurrenciesByTickerAndDate (tickerId, dateStr)

            // Get currencies from option trades for this ticker on this date using optimized query
            let! optionCurrencies = OptionTradeExtensions.Do.getCurrenciesByTickerAndDate (tickerId, dateStr)

            // Combine and deduplicate currencies from all sources
            let currencies =
                [ tradeCurrencies
                  priceCurrencies
                  dividendCurrencies
                  dividendTaxCurrencies
                  optionCurrencies ]
                |> List.concat
                |> List.distinct

            // If no currencies found, use default currency
            if currencies.IsEmpty then
                let! currency = getDefaultCurrency ()
                return [ currency ]
            else
                return currencies
        }

    /// Get calculation data for a specific ticker, currency and date range
    let private getCalculationData
        (tickerId: int)
        (currencyId: int)
        (fromDate: DateTimePattern option)
        (toDate: DateTimePattern)
        =
        task {
            // Get trades in date range for this ticker and currency using optimized query
            let fromDateStr = fromDate |> Option.map (fun d -> d.Value.ToString())
            let toDateStr = toDate.Value.ToString()

            let! relevantTrades =
                match fromDateStr with
                | Some startDate -> TradeExtensions.Do.getFilteredTrades (tickerId, currencyId, startDate, toDateStr)
                | None -> TradeExtensions.Do.getByTickerCurrencyAndDateRange (tickerId, currencyId, None, toDateStr)

            // Get dividends in date range using optimized query
            let! relevantDividends =
                match fromDateStr with
                | Some startDate ->
                    DividendExtensions.Do.getFilteredDividends (tickerId, currencyId, startDate, toDateStr)
                | None -> DividendExtensions.Do.getByTickerCurrencyAndDateRange (tickerId, currencyId, None, toDateStr)

            // Get dividend taxes in date range using optimized query
            let! relevantDividendTaxes =
                match fromDateStr with
                | Some startDate ->
                    DividendTaxExtensions.Do.getFilteredDividendTaxes (tickerId, currencyId, startDate, toDateStr)
                | None ->
                    DividendTaxExtensions.Do.getByTickerCurrencyAndDateRange (tickerId, currencyId, None, toDateStr)

            // Get option trades in date range using optimized query
            let! relevantOptionTrades =
                match fromDateStr with
                | Some startDate ->
                    OptionTradeExtensions.Do.getFilteredOptionTrades (tickerId, currencyId, startDate, toDateStr)
                | None ->
                    OptionTradeExtensions.Do.getByTickerCurrencyAndDateRange (tickerId, currencyId, None, toDateStr)

            // Get latest price
            let! latestPrice = TickerPriceExtensions.Do.getPriceByDateOrPrevious (tickerId, toDate.Value.ToString())

            return
                { Trades = relevantTrades
                  Dividends = relevantDividends
                  DividendTaxes = relevantDividendTaxes
                  OptionTrades = relevantOptionTrades
                  LatestPrice = latestPrice
                  PreviousSnapshot = None // Will be set separately
                }
        }

    /// Get or create a TickerSnapshot for the given date
    let private getOrCreateTickerSnapshot (tickerId: int) (date: DateTimePattern) =
        task {
            let! existingSnapshot = TickerSnapshotExtensions.Do.getByTickerIdAndDate (tickerId, date)

            match existingSnapshot with
            | Some snapshot -> return snapshot
            | None ->
                let snapshotDate = getDateOnly date

                let newSnapshot =
                    { Base = createBaseSnapshot snapshotDate
                      TickerId = tickerId }

                do! newSnapshot.save ()
                let! createdSnapshot = TickerSnapshotExtensions.Do.getByTickerIdAndDate (tickerId, date)

                match createdSnapshot with
                | Some snapshot -> return snapshot
                | None ->
                    failwithf
                        "Failed to create TickerSnapshot for ticker %d on date %s"
                        tickerId
                        (date.Value.ToString())

                    return
                        { Base = createBaseSnapshot snapshotDate
                          TickerId = tickerId } // Won't be reached but satisfies compiler
        }

    /// Update or create a TickerCurrencySnapshot
    let private updateTickerCurrencySnapshot
        (tickerId: int)
        (currencyId: int)
        (date: DateTimePattern)
        (tickerSnapshotId: int)
        =
        task {
            // Get previous snapshot to use as baseline
            let! prevTickerSnapshot = TickerSnapshotExtensions.Do.getLatestByTickerId (tickerId)

            let prevDateOpt =
                prevTickerSnapshot
                |> Option.bind (fun s ->
                    if s.Base.Date.Value.Date < date.Value.Date then
                        Some s.Base.Date
                    else
                        None)

            let! prevCurrencySnapshot =
                match prevDateOpt with
                | Some prevDate ->
                    task {
                        let! snapshots =
                            TickerCurrencySnapshotExtensions.Do.getAllByTickerIdAndDate (tickerId, prevDate)

                        return snapshots |> List.tryFind (fun s -> s.CurrencyId = currencyId)
                    }
                | None -> task { return None }

            // Get calculation data
            let! calculationData = getCalculationData tickerId currencyId prevDateOpt date

            let dataWithPrevious =
                { calculationData with
                    PreviousSnapshot = prevCurrencySnapshot }

            let calculatedValues = calculateSnapshotValues dataWithPrevious

            let snapshot =
                { Base = createBaseSnapshot date
                  TickerId = tickerId
                  CurrencyId = currencyId
                  TickerSnapshotId = tickerSnapshotId
                  TotalShares = calculatedValues.TotalShares
                  Weight = 0.0m // TODO: Calculate weight in portfolio context
                  CostBasis = Money.FromAmount(calculatedValues.CostBasis)
                  RealCost = Money.FromAmount(calculatedValues.RealCost)
                  Dividends = Money.FromAmount(calculatedValues.Dividends)
                  DividendTaxes = Money.FromAmount(0m) // TODO: Track dividend taxes
                  Options = Money.FromAmount(calculatedValues.Options)
                  TotalIncomes = Money.FromAmount(calculatedValues.TotalIncomes)
                  CapitalDeployed = Money.FromAmount(0m) // TODO: Calculate capital deployed
                  Realized = Money.FromAmount(calculatedValues.Realized)
                  Performance = calculatedValues.Performance
                  OpenTrades = calculatedValues.OpenTrades
                  Commissions = Money.FromAmount(0m)
                  Fees = Money.FromAmount(0m) }

            do! snapshot.save ()
        }

    let private createDefaultTickerCurrencySnapshot
        (date: DateTimePattern)
        (tickerId: int)
        (currencyId: int)
        (snapshotId: int)
        =
        task {
            let! priceByDate = TickerPriceExtensions.Do.getPriceByDateOrPrevious (tickerId, date.Value.ToString())

            let snapshotDate = getDateOnly date

            let snapshot =
                { Base = createBaseSnapshot snapshotDate
                  TickerId = tickerId
                  CurrencyId = currencyId
                  TickerSnapshotId = snapshotId
                  TotalShares = 0.0M
                  Weight = 0.0M
                  CostBasis = Money.FromAmount(0.0m)
                  RealCost = Money.FromAmount(0.0m)
                  Dividends = Money.FromAmount(0.0m)
                  DividendTaxes = Money.FromAmount(0.0m)
                  Options = Money.FromAmount(0.0m)
                  TotalIncomes = Money.FromAmount(0.0m)
                  CapitalDeployed = Money.FromAmount(0.0m)
                  Realized = Money.FromAmount(0.0m)
                  Performance = 0.0m
                  OpenTrades = false
                  Commissions = Money.FromAmount(0.0m)
                  Fees = Money.FromAmount(0.0m) }

            return snapshot
        }

    /// Create a new snapshot for a ticker and date
    let private createTickerSnapshot (tickerId: int) (date: DateTimePattern) =
        task {
            let dateStr = date.Value.ToString("yyyy-MM-dd")

            CoreLogger.logInfo
                "TickerSnapshotManager"
                $"[createTickerSnapshot] START - TickerId: {tickerId}, Date: {dateStr}"

            let snapshotDate = getDateOnly date

            let newSnapshot =
                { Base = createBaseSnapshot snapshotDate
                  TickerId = tickerId }

            CoreLogger.logInfo "TickerSnapshotManager" "[createTickerSnapshot] Saving TickerSnapshot to database..."
            do! newSnapshot.save ()
            CoreLogger.logInfo "TickerSnapshotManager" "[createTickerSnapshot] TickerSnapshot saved successfully"

            CoreLogger.logInfo
                "TickerSnapshotManager"
                "[createTickerSnapshot] Retrieving created snapshot from database..."

            let! createdSnapshot = TickerSnapshotExtensions.Do.getByTickerIdAndDate (tickerId, snapshotDate)

            match createdSnapshot with
            | None ->
                CoreLogger.logError
                    "TickerSnapshotManager"
                    "[createTickerSnapshot] FAILED - Could not retrieve created TickerSnapshot from database"

                failwith "Failed to create ticker snapshot"
            | Some snapshot ->
                CoreLogger.logInfo
                    "TickerSnapshotManager"
                    $"[createTickerSnapshot] TickerSnapshot retrieved. ID: {snapshot.Base.Id}"

                let preferenceCurrency = PreferencesProvider.getString CurrencyKey DefaultCurrency

                CoreLogger.logInfo
                    "TickerSnapshotManager"
                    $"[createTickerSnapshot] Getting currency: {preferenceCurrency}"

                let! currency = CurrencyExtensions.Do.getByCode (preferenceCurrency)

                match currency with
                | None ->
                    CoreLogger.logError
                        "TickerSnapshotManager"
                        $"[createTickerSnapshot] FAILED - Currency '{preferenceCurrency}' not found in database"

                    failwithf "Currency %s not found" preferenceCurrency
                | Some currency ->
                    CoreLogger.logInfo
                        "TickerSnapshotManager"
                        $"[createTickerSnapshot] Currency found. ID: {currency.Id}, Code: {currency.Code}"

                    CoreLogger.logInfo
                        "TickerSnapshotManager"
                        "[createTickerSnapshot] Creating default TickerCurrencySnapshot..."

                    let! currencySnapshot =
                        createDefaultTickerCurrencySnapshot date tickerId currency.Id snapshot.Base.Id

                    CoreLogger.logInfo
                        "TickerSnapshotManager"
                        "[createTickerSnapshot] Saving TickerCurrencySnapshot to database..."

                    do! currencySnapshot.save ()

                    CoreLogger.logInfo
                        "TickerSnapshotManager"
                        $"[createTickerSnapshot] SUCCESS - TickerCurrencySnapshot saved for TickerId: {tickerId}, CurrencyId: {currency.Id}"
        }

    /// Create or update a snapshot for a ticker and date with improved error handling that throws exceptions to the UI
    let private updateTickerSnapshot (tickerId: int) (date: DateTimePattern) =
        task {
            let dateStr = date.Value.ToString("yyyy-MM-dd")

            CoreLogger.logInfo
                "TickerSnapshotManager"
                $"[updateTickerSnapshot] START - TickerId: {tickerId}, Date: {dateStr}"

            // Step 1: Get or create the TickerSnapshot for the date
            CoreLogger.logInfo
                "TickerSnapshotManager"
                "[updateTickerSnapshot] Step 1: Getting or creating TickerSnapshot..."

            let! tickerSnapshot = getOrCreateTickerSnapshot tickerId date

            CoreLogger.logInfo
                "TickerSnapshotManager"
                $"[updateTickerSnapshot] TickerSnapshot obtained. ID: {tickerSnapshot.Base.Id}"

            // Step 2: Get all relevant currencies for this ticker and date
            CoreLogger.logInfo "TickerSnapshotManager" "[updateTickerSnapshot] Step 2: Getting relevant currencies..."
            let! currencies = getRelevantCurrencies tickerId date
            let currenciesStr = String.Join(", ", currencies)

            CoreLogger.logInfo
                "TickerSnapshotManager"
                $"[updateTickerSnapshot] Found {currencies.Length} currencies: [{currenciesStr}]"

            if currencies.IsEmpty then
                CoreLogger.logError
                    "TickerSnapshotManager"
                    $"[updateTickerSnapshot] FAILED - No currencies found for ticker {tickerId} on date {dateStr}"

                failwithf "No currencies found for ticker %d on date %s" tickerId (date.Value.ToString())

            // Step 3: Update/create TickerCurrencySnapshot for each currency
            CoreLogger.logInfo
                "TickerSnapshotManager"
                "[updateTickerSnapshot] Step 3: Creating/updating TickerCurrencySnapshots..."

            let updateTasks =
                currencies
                |> List.map (fun currencyId ->
                    task {
                        CoreLogger.logInfo
                            "TickerSnapshotManager"
                            $"[updateTickerSnapshot] Processing currency ID: {currencyId}"

                        do! updateTickerCurrencySnapshot tickerId currencyId date tickerSnapshot.Base.Id

                        CoreLogger.logInfo
                            "TickerSnapshotManager"
                            $"[updateTickerSnapshot] Currency ID {currencyId} processed successfully"
                    })

            let! _ = Task.WhenAll(updateTasks)

            CoreLogger.logInfo
                "TickerSnapshotManager"
                "[updateTickerSnapshot] SUCCESS - All TickerCurrencySnapshots created/updated"

            return ()
        }

    /// Update a ticker snapshot and cascade the changes to all subsequent dates
    let private updateTickerSnapshotWithCascade
        (tickerId: int)
        (subsequentSnapshots: TickerSnapshot list)
        (date: DateTimePattern)
        =
        task {
            // Step 1: Update the target date
            do! updateTickerSnapshot tickerId date

            // Step 2: Update each subsequent snapshot in chronological order
            // Process snapshots sequentially to maintain proper calculation dependencies
            for snapshot in subsequentSnapshots do
                do! updateTickerSnapshot tickerId snapshot.Base.Date

            return ()
        }

    /// <summary>
    /// Handles snapshot initialization when a new ticker is created in the system.
    /// This method creates the initial TickerSnapshot and TickerCurrencySnapshot for today's date
    /// using the default currency preference and zero values for all financial metrics.
    /// </summary>
    /// <param name="ticker">The newly created ticker entity that requires snapshot initialization</param>
    /// <returns>A task that represents the asynchronous snapshot creation operation</returns>
    /// <remarks>
    /// This method should be called immediately after a new ticker is successfully saved to the database.
    /// It ensures that:
    /// - A baseline TickerSnapshot is created for today's date
    /// - A corresponding TickerCurrencySnapshot is created with default currency
    /// - All financial metrics are initialized to zero (no trades, dividends, or positions yet)
    /// - The latest price is retrieved from the ticker price data if available
    ///
    /// Note: This is different from handleTickerChange, which handles updates to existing tickers.
    /// For new tickers without any trading history, this provides the foundation for future calculations.
    /// </remarks>
    let handleNewTicker (ticker: Ticker) =
        CoreLogger.logInfo
            "TickerSnapshotManager"
            $"[handleNewTicker] CALLED - Ticker: {ticker.Symbol} (ID: {ticker.Id})"

        let today = DateTimePattern.FromDateTime(DateTime.Today)
        let dateStr = today.Value.ToString("yyyy-MM-dd")
        CoreLogger.logInfo "TickerSnapshotManager" $"[handleNewTicker] Creating snapshot for date: {dateStr}"
        createTickerSnapshot ticker.Id today

    /// <summary>
    /// Handles ticker-related changes for a specific date, automatically cascading updates if future snapshots exist.
    /// This method intelligently determines whether to perform a simple update or a cascading update based on
    /// the presence of future snapshots. When future snapshots exist, all subsequent snapshots are recalculated
    /// to maintain data consistency across the timeline.
    /// </summary>
    /// <param name="tickerId">The unique identifier of the ticker that changed</param>
    /// <param name="date">The date when the ticker change occurred</param>
    /// <returns>A task that represents the asynchronous snapshot update operation</returns>
    /// <remarks>
    /// This is the primary entry point for handling ticker changes in the snapshot system.
    /// Use this method when:
    /// - A trade is added, modified, or deleted for a ticker
    /// - A dividend or dividend tax is recorded for a ticker
    /// - An option trade is executed for a ticker
    /// - A ticker price is updated
    ///
    /// The method ensures that all financial calculations remain accurate by updating the target date
    /// and cascading changes to all future snapshots when necessary.
    /// </remarks>
    let handleTickerChange (tickerId: int, date: DateTimePattern) =
        task {
            let dateStr = date.Value.ToString("yyyy-MM-dd")

            CoreLogger.logInfo
                "TickerSnapshotManager"
                $"[handleTickerChange] CALLED - TickerId: {tickerId}, Date: {dateStr}"

            // Check if there are any snapshots after this date
            let! subsequentSnapshots = TickerSnapshotExtensions.Do.getTickerSnapshotsAfterDate (tickerId, date)

            CoreLogger.logInfo
                "TickerSnapshotManager"
                $"[handleTickerChange] Found {subsequentSnapshots.Length} subsequent snapshots"

            if subsequentSnapshots.IsEmpty then
                // No future snapshots, just update this date
                CoreLogger.logInfo
                    "TickerSnapshotManager"
                    "[handleTickerChange] No future snapshots, updating single date"

                do! updateTickerSnapshot tickerId date
            else
                // Future snapshots exist, use cascade update
                CoreLogger.logInfo
                    "TickerSnapshotManager"
                    "[handleTickerChange] Future snapshots exist, using cascade update"

                do! updateTickerSnapshotWithCascade tickerId subsequentSnapshots date

            CoreLogger.logInfo "TickerSnapshotManager" $"[handleTickerChange] COMPLETED for TickerId: {tickerId}"
        }
