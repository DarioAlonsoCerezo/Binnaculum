namespace Binnaculum.Core.Storage

open Binnaculum.Core.Patterns
open Binnaculum.Core.Logging
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Microsoft.Maui.Storage
open Binnaculum.Core.Keys
open System

/// <summary>
/// Utility functions for snapshot managers.
/// </summary>
module internal SnapshotManagerUtils =
    /// Helper function to get the date part only from a DateTimePattern, set to end of day (23:59:59)
    let getDateOnly (dateTime: DateTimePattern) =
        let date = dateTime.Value.Date.AddDays(1).AddTicks(-1)
        DateTimePattern.FromDateTime(date)

    /// Helper function to get the date part only from a DateTimePattern, set to start of day (00:00:01)
    /// This is used when retrieving movements FROM a specific date to ensure we capture all movements
    /// throughout the entire day, starting from the very beginning of that day.
    let getDateOnlyStartOfDay (dateTime: DateTimePattern) =
        let date = dateTime.Value.Date.AddSeconds(1.0) // 00:00:01
        DateTimePattern.FromDateTime(date)

    let getDateOnlyFromDateTime (dateTime: DateTime) =
        let pattern = DateTimePattern.FromDateTime(dateTime)
        getDateOnly pattern

    /// Helper function to get start of day from a DateTime, set to 00:00:01
    /// This is used when retrieving movements FROM a specific date to ensure we capture all movements
    /// throughout the entire day, starting from the very beginning of that day.
    let getDateOnlyStartOfDayFromDateTime (dateTime: DateTime) =
        let pattern = DateTimePattern.FromDateTime(dateTime)
        getDateOnlyStartOfDay pattern

    /// Normalizes a DateTimePattern to start of day for consistent snapshot date comparison
    /// This ensures all movement dates are treated as date-only for proper snapshot processing
    let normalizeToStartOfDay (dateTime: DateTimePattern) =
        let date = dateTime.Value.Date.AddSeconds(1.0) // 00:00:01 for start of day
        DateTimePattern.FromDateTime(date)

    /// Creates a base snapshot with the given date
    let createBaseSnapshot (date: DateTimePattern) : BaseSnapshot =
        try
            CoreLogger.logDebug
                "SnapshotManagerUtils"
                $"createBaseSnapshot - Step 1: Creating base snapshot for date {date}"

            let normalizedDate = getDateOnly date

            CoreLogger.logDebug
                "SnapshotManagerUtils"
                $"createBaseSnapshot - Step 2: Normalized date = {normalizedDate}"

            let auditEntity = AuditableEntity.FromDateTime(DateTime.UtcNow)

            CoreLogger.logDebug "SnapshotManagerUtils" $"createBaseSnapshot - Step 3: Created audit entity"

            let baseSnapshot =
                { Id = 0
                  Date = normalizedDate
                  Audit = auditEntity }

            CoreLogger.logDebug
                "SnapshotManagerUtils"
                $"createBaseSnapshot - Step 4: Base snapshot created successfully with ID = {baseSnapshot.Id}"

            baseSnapshot
        with ex ->
            CoreLogger.logDebugf "SnapshotManagerUtils" "createBaseSnapshot - EXCEPTION: %A" ex.Message

            CoreLogger.logDebug "SnapshotManagerUtils" $"createBaseSnapshot - STACK TRACE: {ex.StackTrace}"

            let innerMsg =
                if ex.InnerException <> null then
                    ex.InnerException.Message
                else
                    "None"

            CoreLogger.logDebug "SnapshotManagerUtils" $"createBaseSnapshot - INNER EXCEPTION: {innerMsg}"

            raise ex

    let getDefaultCurrency () =
        task {
            try
                CoreLogger.logDebug "SnapshotManagerUtils" "getDefaultCurrency - Step 1: Getting preference currency..."

                let preferenceCurrency = Preferences.Get(CurrencyKey, DefaultCurrency)

                CoreLogger.logDebug
                    "SnapshotManagerUtils"
                    $"getDefaultCurrency - Step 2: Preference currency = {preferenceCurrency}"

                CoreLogger.logDebug
                    "SnapshotManagerUtils"
                    "getDefaultCurrency - Step 3: Calling CurrencyExtensions.Do.getByCode..."

                let! defaultCurrency = CurrencyExtensions.Do.getByCode (preferenceCurrency)

                CoreLogger.logDebug
                    "SnapshotManagerUtils"
                    "getDefaultCurrency - Step 4: CurrencyExtensions.Do.getByCode completed"

                match defaultCurrency with
                | Some currency ->
                    CoreLogger.logDebug
                        "SnapshotManagerUtils"
                        $"getDefaultCurrency - Success: Found currency ID = {currency.Id}"

                    return currency.Id
                | None ->
                    CoreLogger.logDebug
                        "SnapshotManagerUtils"
                        $"getDefaultCurrency - Error: Currency {preferenceCurrency} not found"

                    failwithf "Default currency %s not found and no fallback currency available" preferenceCurrency
                    return -1 // This line will never be reached but satisfies the compiler
            with ex ->
                CoreLogger.logDebug "SnapshotManagerUtils" $"getDefaultCurrency - EXCEPTION: {ex.Message}"

                CoreLogger.logDebug "SnapshotManagerUtils" $"getDefaultCurrency - STACK TRACE: {ex.StackTrace}"

                let innerMsg =
                    if ex.InnerException <> null then
                        ex.InnerException.Message
                    else
                        "None"

                CoreLogger.logDebug "SnapshotManagerUtils" $"getDefaultCurrency - INNER EXCEPTION: {innerMsg}"

                raise ex
                return -1 // This line will never be reached but satisfies the compiler
        }

/// <summary>
/// Represents calculated financial metrics from movement data that can be combined with previous snapshots.
/// This record encapsulates all the financial calculations needed to create or update a financial snapshot.
/// </summary>
type internal CalculatedFinancialMetrics =
    {
      // Primary financial flows
      Deposited: Money
      Withdrawn: Money
      Invested: Money
      RealizedGains: Money

      // Income sources
      DividendsReceived: Money
      OptionsIncome: Money
      OtherIncome: Money

      // Costs
      Commissions: Money
      Fees: Money

      // Position tracking
      CurrentPositions: Map<int, decimal>
      CostBasisInfo: Map<int, decimal>
      HasOpenPositions: bool
      OptionUnrealizedGains: Money // NEW: Unrealized gains from open option positions

      // Activity counters
      MovementCounter: int }

/// <summary>
/// Comprehensive data structure containing all movement types for a broker account from a specific date onwards.
/// This type is designed to be returned from movement retrieval methods and used throughout snapshot calculations.
/// Enhanced with multi-currency support for per-currency financial calculations.
/// </summary>
type internal BrokerAccountMovementData =
    {
        /// The start date used for movement retrieval (inclusive)
        FromDate: DateTimePattern

        /// Broker account ID for which movements were retrieved
        BrokerAccountId: int

        /// All broker-level movements (deposits, withdrawals, fees, interest, ACAT transfers, etc.)
        BrokerMovements: BrokerMovement list

        /// All stock and ETF trades (buy/sell orders with full transaction details)
        Trades: Trade list

        /// All dividend payments received (cash dividends from owned securities)
        Dividends: Dividend list

        /// All dividend taxes withheld (especially foreign dividend withholdings)
        DividendTaxes: DividendTax list

        /// All options trading activity (purchases, sales, exercises, expirations)
        OptionTrades: OptionTrade list

        /// Total count of all movements for quick reference
        TotalMovementCount: int

        /// Set of unique dates that have movements (normalized to start-of-day for proper snapshot comparison)
        UniqueDates: Set<DateTimePattern>

        /// Set of unique currencies that have movements (critical for multi-currency processing)
        UniqueCurrencies: Set<int>

        /// Indicates whether any movements were found from the specified date
        HasMovements: bool

        /// Date range covered by the movements (from earliest to latest movement date)
        DateRange: DateTimePattern * DateTimePattern

        /// Movements grouped by currency ID for per-currency financial calculations
        MovementsByCurrency: Map<int, CurrencyMovementData>
    }

/// <summary>
/// Movement data for a specific currency - essential for accurate financial calculations
/// </summary>
and internal CurrencyMovementData =
    {
        /// Currency ID for this group
        CurrencyId: int

        /// Broker movements in this currency
        BrokerMovements: BrokerMovement list

        /// Trades in this currency
        Trades: Trade list

        /// Dividends in this currency
        Dividends: Dividend list

        /// Dividend taxes in this currency
        DividendTaxes: DividendTax list

        /// Option trades in this currency
        OptionTrades: OptionTrade list

        /// Total count for this currency
        TotalCount: int

        /// Unique dates with movements in this currency (normalized to start-of-day)
        UniqueDates: Set<DateTimePattern>
    }

/// <summary>
/// Helper functions for working with BrokerAccountMovementData
/// </summary>
module internal BrokerAccountMovementData =

    /// Creates CurrencyMovementData for a specific currency
    let private createCurrencyMovementData
        (currencyId: int)
        (brokerMovements: BrokerMovement list)
        (trades: Trade list)
        (dividends: Dividend list)
        (dividendTaxes: DividendTax list)
        (optionTrades: OptionTrade list)
        =

        // Extract and normalize dates to start of day for proper snapshot comparison
        let brokerMovementDates =
            brokerMovements
            |> List.map (fun m -> SnapshotManagerUtils.normalizeToStartOfDay m.TimeStamp)
            |> Set.ofList

        let tradeDates =
            trades
            |> List.map (fun t -> SnapshotManagerUtils.normalizeToStartOfDay t.TimeStamp)
            |> Set.ofList

        let dividendDates =
            dividends
            |> List.map (fun d -> SnapshotManagerUtils.normalizeToStartOfDay d.TimeStamp)
            |> Set.ofList

        let dividendTaxDates =
            dividendTaxes
            |> List.map (fun dt -> SnapshotManagerUtils.normalizeToStartOfDay dt.TimeStamp)
            |> Set.ofList

        let optionTradeDates =
            optionTrades
            |> List.map (fun ot -> SnapshotManagerUtils.normalizeToStartOfDay ot.TimeStamp)
            |> Set.ofList

        let allDatesForCurrency =
            Set.unionMany
                [ brokerMovementDates
                  tradeDates
                  dividendDates
                  dividendTaxDates
                  optionTradeDates ]

        let totalCountForCurrency =
            brokerMovements.Length
            + trades.Length
            + dividends.Length
            + dividendTaxes.Length
            + optionTrades.Length

        { CurrencyId = currencyId
          BrokerMovements = brokerMovements
          Trades = trades
          Dividends = dividends
          DividendTaxes = dividendTaxes
          OptionTrades = optionTrades
          TotalCount = totalCountForCurrency
          UniqueDates = allDatesForCurrency }

    /// Creates a BrokerAccountMovementData instance from individual movement collections
    let create
        (fromDate: DateTimePattern)
        (brokerAccountId: int)
        (brokerMovements: BrokerMovement list)
        (trades: Trade list)
        (dividends: Dividend list)
        (dividendTaxes: DividendTax list)
        (optionTrades: OptionTrade list)
        =

        // Extract all unique dates from all movement types and normalize to start of day
        // This ensures proper date-only comparison for snapshot management
        let brokerMovementDates =
            brokerMovements
            |> List.map (fun m -> SnapshotManagerUtils.normalizeToStartOfDay m.TimeStamp)
            |> Set.ofList

        let tradeDates =
            trades
            |> List.map (fun t -> SnapshotManagerUtils.normalizeToStartOfDay t.TimeStamp)
            |> Set.ofList

        let dividendDates =
            dividends
            |> List.map (fun d -> SnapshotManagerUtils.normalizeToStartOfDay d.TimeStamp)
            |> Set.ofList

        let dividendTaxDates =
            dividendTaxes
            |> List.map (fun dt -> SnapshotManagerUtils.normalizeToStartOfDay dt.TimeStamp)
            |> Set.ofList

        let optionTradeDates =
            optionTrades
            |> List.map (fun ot -> SnapshotManagerUtils.normalizeToStartOfDay ot.TimeStamp)
            |> Set.ofList

        let allDates =
            Set.unionMany
                [ brokerMovementDates
                  tradeDates
                  dividendDates
                  dividendTaxDates
                  optionTradeDates ]

        // Extract all unique currencies from all movement types
        let brokerMovementCurrencies =
            brokerMovements |> List.map (fun m -> m.CurrencyId) |> Set.ofList

        let tradeCurrencies = trades |> List.map (fun t -> t.CurrencyId) |> Set.ofList
        let dividendCurrencies = dividends |> List.map (fun d -> d.CurrencyId) |> Set.ofList

        let dividendTaxCurrencies =
            dividendTaxes |> List.map (fun dt -> dt.CurrencyId) |> Set.ofList

        let optionTradeCurrencies =
            optionTrades |> List.map (fun ot -> ot.CurrencyId) |> Set.ofList

        let allCurrencies =
            Set.unionMany
                [ brokerMovementCurrencies
                  tradeCurrencies
                  dividendCurrencies
                  dividendTaxCurrencies
                  optionTradeCurrencies ]

        // Group movements by currency
        do
            CoreLogger.logDebug
                "BrokerAccountMovementData"
                $"Creating movement data - Total BrokerMovements: {brokerMovements.Length}, Total Trades: {trades.Length}, Total OptionTrades: {optionTrades.Length}, UniqueCurrencies: {allCurrencies.Count}"

        let movementsByCurrency =
            allCurrencies
            |> Set.toList
            |> List.map (fun currencyId ->
                let brokerMovementsForCurrency =
                    brokerMovements |> List.filter (fun m -> m.CurrencyId = currencyId)

                let tradesForCurrency = trades |> List.filter (fun t -> t.CurrencyId = currencyId)

                let dividendsForCurrency =
                    dividends |> List.filter (fun d -> d.CurrencyId = currencyId)

                let dividendTaxesForCurrency =
                    dividendTaxes |> List.filter (fun dt -> dt.CurrencyId = currencyId)

                let optionTradesForCurrency =
                    optionTrades |> List.filter (fun ot -> ot.CurrencyId = currencyId)

                do
                    CoreLogger.logDebug
                        "BrokerAccountMovementData"
                        $"Currency {currencyId} - BrokerMovements: {brokerMovementsForCurrency.Length}, Trades: {tradesForCurrency.Length}, OptionTrades: {optionTradesForCurrency.Length}"

                if brokerMovementsForCurrency.Length > 0 then
                    brokerMovementsForCurrency
                    |> List.iter (fun m ->
                        CoreLogger.logDebug
                            "BrokerAccountMovementData"
                            $"Movement for currency {currencyId} - ID: {m.Id}, Type: {m.MovementType}, Amount: {m.Amount.Value}")

                if optionTradesForCurrency.Length > 0 then
                    optionTradesForCurrency
                    |> List.iter (fun ot ->
                        CoreLogger.logDebug
                            "BrokerAccountMovementData"
                            $"OptionTrade for currency {currencyId} - ID: {ot.Id}, Code: {ot.Code}, Strike: {ot.Strike.Value}, Expiration: {ot.ExpirationDate.Value}, NetPremium: {ot.NetPremium.Value}")

                let currencyData =
                    createCurrencyMovementData
                        currencyId
                        brokerMovementsForCurrency
                        tradesForCurrency
                        dividendsForCurrency
                        dividendTaxesForCurrency
                        optionTradesForCurrency

                (currencyId, currencyData))
            |> Map.ofList

        // Calculate total movement count
        let totalCount =
            brokerMovements.Length
            + trades.Length
            + dividends.Length
            + dividendTaxes.Length
            + optionTrades.Length

        // Determine date range (if any movements exist) - use normalized dates for consistency
        let dateRange =
            if allDates.IsEmpty then
                (fromDate, fromDate)
            else
                let minDate = allDates |> Set.minElement
                let maxDate = allDates |> Set.maxElement
                (minDate, maxDate)

        { FromDate = fromDate
          BrokerAccountId = brokerAccountId
          BrokerMovements = brokerMovements
          Trades = trades
          Dividends = dividends
          DividendTaxes = dividendTaxes
          OptionTrades = optionTrades
          TotalMovementCount = totalCount
          UniqueDates = allDates
          UniqueCurrencies = allCurrencies
          HasMovements = totalCount > 0
          DateRange = dateRange
          MovementsByCurrency = movementsByCurrency }

    /// Creates an empty BrokerAccountMovementData instance for a specific date and broker account
    let createEmpty (date: DateTimePattern) (brokerAccountId: int) =
        { FromDate = date
          BrokerAccountId = brokerAccountId
          BrokerMovements = []
          Trades = []
          Dividends = []
          DividendTaxes = []
          OptionTrades = []
          TotalMovementCount = 0
          UniqueDates = Set.empty
          UniqueCurrencies = Set.empty
          HasMovements = false
          DateRange = (date, date)
          MovementsByCurrency = Map.empty }

    /// Returns true if the movement data is empty (no movements found)
    let isEmpty (data: BrokerAccountMovementData) = not data.HasMovements

    /// Gets movements for a specific date
    let getMovementsForDate (date: DateTimePattern) (data: BrokerAccountMovementData) =
        let normalizedDate = SnapshotManagerUtils.normalizeToStartOfDay date

        let brokerMovementsForDate =
            data.BrokerMovements
            |> List.filter (fun m -> SnapshotManagerUtils.normalizeToStartOfDay m.TimeStamp = normalizedDate)

        let tradesForDate =
            data.Trades
            |> List.filter (fun t -> SnapshotManagerUtils.normalizeToStartOfDay t.TimeStamp = normalizedDate)

        let dividendsForDate =
            data.Dividends
            |> List.filter (fun d -> SnapshotManagerUtils.normalizeToStartOfDay d.TimeStamp = normalizedDate)

        let dividendTaxesForDate =
            data.DividendTaxes
            |> List.filter (fun dt -> SnapshotManagerUtils.normalizeToStartOfDay dt.TimeStamp = normalizedDate)

        let optionTradesUpToDate =
            data.OptionTrades
            |> List.filter (fun ot -> SnapshotManagerUtils.normalizeToStartOfDay ot.TimeStamp <= normalizedDate)

        create
            date
            data.BrokerAccountId
            brokerMovementsForDate
            tradesForDate
            dividendsForDate
            dividendTaxesForDate
            optionTradesUpToDate

    /// Gets movements for a specific currency
    let getMovementsForCurrency (currencyId: int) (data: BrokerAccountMovementData) =
        data.MovementsByCurrency.TryFind(currencyId)

    /// Gets movements for a specific currency and date combination
    let getMovementsForCurrencyAndDate (currencyId: int) (date: DateTimePattern) (data: BrokerAccountMovementData) =
        match data.MovementsByCurrency.TryFind(currencyId) with
        | Some currencyData ->
            let normalizedDate = SnapshotManagerUtils.normalizeToStartOfDay date

            let brokerMovementsForDate =
                currencyData.BrokerMovements
                |> List.filter (fun m -> SnapshotManagerUtils.normalizeToStartOfDay m.TimeStamp = normalizedDate)

            let tradesForDate =
                currencyData.Trades
                |> List.filter (fun t -> SnapshotManagerUtils.normalizeToStartOfDay t.TimeStamp = normalizedDate)

            let dividendsForDate =
                currencyData.Dividends
                |> List.filter (fun d -> SnapshotManagerUtils.normalizeToStartOfDay d.TimeStamp = normalizedDate)

            let dividendTaxesForDate =
                currencyData.DividendTaxes
                |> List.filter (fun dt -> SnapshotManagerUtils.normalizeToStartOfDay dt.TimeStamp = normalizedDate)

            let optionTradesUpToDate =
                currencyData.OptionTrades
                |> List.filter (fun ot -> SnapshotManagerUtils.normalizeToStartOfDay ot.TimeStamp <= normalizedDate)

            Some(
                createCurrencyMovementData
                    currencyId
                    brokerMovementsForDate
                    tradesForDate
                    dividendsForDate
                    dividendTaxesForDate
                    optionTradesUpToDate
            )
        | None -> None

    /// Groups all movements by date for efficient processing
    let groupByDate (data: BrokerAccountMovementData) =
        data.UniqueDates
        |> Set.toList
        |> List.map (fun date -> (date, getMovementsForDate date data))
        |> Map.ofList

    /// Groups all movements by currency for per-currency financial calculations
    let groupByCurrency (data: BrokerAccountMovementData) = data.MovementsByCurrency

    /// Gets all currencies with movements (essential for multi-currency processing)
    let getCurrencies (data: BrokerAccountMovementData) = data.UniqueCurrencies |> Set.toList

    /// Calculates total cash impact by currency (useful for financial snapshot calculations)
    let calculateCashImpactByCurrency (data: BrokerAccountMovementData) =
        data.MovementsByCurrency
        |> Map.map (fun currencyId currencyData ->
            // Example calculation - total cash impact for this currency
            let brokerMovementImpact =
                currencyData.BrokerMovements |> List.sumBy (fun m -> m.Amount.Value)

            let tradeImpact =
                currencyData.Trades
                |> List.sumBy (fun t -> -(t.Quantity * t.Price.Value + t.Commissions.Value + t.Fees.Value)) // Negative because spending cash

            let dividendImpact =
                currencyData.Dividends |> List.sumBy (fun d -> d.DividendAmount.Value)

            let dividendTaxImpact =
                currencyData.DividendTaxes |> List.sumBy (fun dt -> -dt.DividendTaxAmount.Value) // Negative for taxes

            let optionImpact =
                currencyData.OptionTrades |> List.sumBy (fun ot -> ot.NetPremium.Value)

            brokerMovementImpact
            + tradeImpact
            + dividendImpact
            + dividendTaxImpact
            + optionImpact)

    /// Gets movement count by currency (useful for validation and reporting)
    let getMovementCountByCurrency (data: BrokerAccountMovementData) =
        data.MovementsByCurrency
        |> Map.map (fun currencyId currencyData -> currencyData.TotalCount)

    /// <summary>
    /// Filters movements to only include those from a specific date onwards.
    /// This is critical for batch processing optimization - allows filtering pre-loaded data
    /// without additional database queries.
    /// </summary>
    /// <param name="fromDate">The date from which to include movements (inclusive)</param>
    /// <param name="data">The movement data to filter</param>
    /// <returns>New BrokerAccountMovementData containing only movements from the specified date onwards</returns>
    let filterFromDate (fromDate: DateTimePattern) (data: BrokerAccountMovementData) =
        let normalizedFromDate = SnapshotManagerUtils.normalizeToStartOfDay fromDate

        // Filter each movement type to only include items from the specified date onwards
        let filteredBrokerMovements =
            data.BrokerMovements
            |> List.filter (fun m -> SnapshotManagerUtils.normalizeToStartOfDay m.TimeStamp >= normalizedFromDate)

        let filteredTrades =
            data.Trades
            |> List.filter (fun t -> SnapshotManagerUtils.normalizeToStartOfDay t.TimeStamp >= normalizedFromDate)

        let filteredDividends =
            data.Dividends
            |> List.filter (fun d -> SnapshotManagerUtils.normalizeToStartOfDay d.TimeStamp >= normalizedFromDate)

        let filteredDividendTaxes =
            data.DividendTaxes
            |> List.filter (fun dt -> SnapshotManagerUtils.normalizeToStartOfDay dt.TimeStamp >= normalizedFromDate)

        let filteredOptionTrades =
            data.OptionTrades
            |> List.filter (fun ot -> SnapshotManagerUtils.normalizeToStartOfDay ot.TimeStamp >= normalizedFromDate)

        // Recreate the movement data with filtered collections
        create
            fromDate
            data.BrokerAccountId
            filteredBrokerMovements
            filteredTrades
            filteredDividends
            filteredDividendTaxes
            filteredOptionTrades

    /// Validates that all movements have valid currency IDs (data integrity check)
    let validateCurrencyIntegrity (data: BrokerAccountMovementData) =
        let allMovementsCurrencies =
            [ data.BrokerMovements |> List.map (fun m -> m.CurrencyId)
              data.Trades |> List.map (fun t -> t.CurrencyId)
              data.Dividends |> List.map (fun d -> d.CurrencyId)
              data.DividendTaxes |> List.map (fun dt -> dt.CurrencyId)
              data.OptionTrades |> List.map (fun ot -> ot.CurrencyId) ]
            |> List.concat
            |> Set.ofList

        // Verify that UniqueCurrencies matches the actual currencies in movements
        allMovementsCurrencies = data.UniqueCurrencies
