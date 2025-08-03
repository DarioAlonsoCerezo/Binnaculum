namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database
open Binnaculum.Core.Patterns
open Microsoft.Maui.Storage
open Binnaculum.Core.Keys
open System

module internal BrokerFinancialSnapshotManager =
    
    let private defaultFinancialSnapshot
        (snapshotDate: DateTimePattern)
        (brokerId: int)
        (brokerAccountId: int)
        (brokerSnapshotId: int)
        (brokerAccountSnapshotId: int)
        =
        task {
            let preferenceCurrency = Preferences.Get(CurrencyKey, DefaultCurrency)
            let! currencyOpt = CurrencyExtensions.Do.getByCode(preferenceCurrency)
            let currencyId =
                match currencyOpt with
                | Some currency -> currency.Id
                | None -> failwithf "Currency %s not found" preferenceCurrency
            return {
                Base = SnapshotManagerUtils.createBaseSnapshot snapshotDate
                BrokerId = brokerId
                BrokerAccountId = brokerAccountId
                CurrencyId = currencyId
                MovementCounter = 0
                BrokerSnapshotId = brokerSnapshotId
                BrokerAccountSnapshotId = brokerAccountSnapshotId
                RealizedGains = Money.FromAmount 0m
                RealizedPercentage = 0m
                UnrealizedGains = Money.FromAmount 0m
                UnrealizedGainsPercentage = 0m
                Invested = Money.FromAmount 0m
                Commissions = Money.FromAmount 0m
                Fees = Money.FromAmount 0m
                Deposited = Money.FromAmount 0m
                Withdrawn = Money.FromAmount 0m
                DividendsReceived = Money.FromAmount 0m
                OptionsIncome = Money.FromAmount 0m
                OtherIncome = Money.FromAmount 0m
                OpenTrades = false
            }
        }

    /// <summary>
    /// Calculates BrokerFinancialSnapshots for a broker (not a specific account) for a given date and currency list.
    /// This function should:
    /// 1. For each currency in currencyList, aggregate all relevant financial data (movements, trades, dividends, fees, etc.) for the broker up to and including snapshotDate.
    /// 2. Calculate all financial fields (deposited, withdrawn, fees, commissions, invested, realized/unrealized gains, etc.) for each currency.
    /// 3. Link the snapshot to the correct BrokerSnapshotId.
    /// 4. Ensure the CurrencyId is valid and matches the currency being processed.
    /// 5. Return a list of BrokerFinancialSnapshot records, one for each currency in currencyList, fully populated with calculated values.
    ///
    /// Note: The current implementation is a placeholder and only returns a default snapshot for the first currency.
    ///       The full implementation should loop through currencyList and perform the calculations described above.
    /// </summary>
    let private calculateBrokerFinancials 
        (snapshotDate: DateTimePattern)
        (brokerId: int)
        (brokerSnapshotId: int)
        (currencyList: int list)
        =
        async {
            let! snapshot = defaultFinancialSnapshot snapshotDate brokerId 0 brokerSnapshotId 0 |> Async.AwaitTask
            return [snapshot]
        }

    /// <summary>
    /// Calculates a BrokerFinancialSnapshot for a specific broker account and currency.
    /// This method aggregates all relevant financial data (movements, trades, dividends, fees, etc.) 
    /// for the broker account and currency between the previous snapshot date and snapshotDate.
    /// </summary>
    let private calculateBrokerAccountFinancialForCurrency
        (snapshotDate: DateTimePattern)
        (brokerId: int)
        (brokerAccountId: int)
        (brokerAccountSnapshotId: int)
        (currencyId: int)
        = async {
            // Find the previous snapshot for this broker account and currency
            let! previousSnapshots = BrokerFinancialSnapshotExtensions.Do.getByBrokerAccountId(brokerAccountId) |> Async.AwaitTask
            let previousSnapshot = 
                previousSnapshots 
                |> List.filter (fun (s: BrokerFinancialSnapshot) -> s.CurrencyId = currencyId)
                |> List.sortByDescending (fun (s: BrokerFinancialSnapshot) -> s.Base.Date.Value)
                |> List.tryHead

            let previousDate = 
                match previousSnapshot with
                | Some (ps: BrokerFinancialSnapshot) -> ps.Base.Date
                | None -> DateTimePattern.FromDateTime(DateTime(1900, 1, 1))

            // Get all broker movements for this account and currency in the date range
            let! allMovements = BrokerMovementExtensions.Do.getByBrokerAccountIdAndDateRange(brokerAccountId, snapshotDate) |> Async.AwaitTask
            let filteredMovements = 
                allMovements 
                |> List.filter (fun (m: BrokerMovement) -> m.CurrencyId = currencyId && m.TimeStamp.Value > previousDate.Value)

            // Get all trades for this account and currency in the date range
            let! allTrades = TradeExtensions.Do.getBetweenDates(previousDate.ToString(), snapshotDate.ToString()) |> Async.AwaitTask
            let filteredTrades = 
                allTrades 
                |> List.filter (fun (t: Trade) -> t.BrokerAccountId = brokerAccountId && t.CurrencyId = currencyId)

            // Get all dividends for this account and currency in the date range
            let! allDividends = DividendExtensions.Do.getBetweenDates(previousDate.ToString(), snapshotDate.ToString()) |> Async.AwaitTask
            let filteredDividends = 
                allDividends 
                |> List.filter (fun (d: Dividend) -> d.BrokerAccountId = brokerAccountId && d.CurrencyId = currencyId)

            // Calculate aggregated values
            let deposited = 
                filteredMovements 
                |> List.filter (fun (m: BrokerMovement) -> m.MovementType = BrokerMovementType.Deposit)
                |> List.sumBy (fun (m: BrokerMovement) -> m.Amount.Value)

            let withdrawn = 
                filteredMovements 
                |> List.filter (fun (m: BrokerMovement) -> m.MovementType = BrokerMovementType.Withdrawal)
                |> List.sumBy (fun (m: BrokerMovement) -> m.Amount.Value)

            let commissions = 
                (filteredMovements |> List.sumBy (fun (m: BrokerMovement) -> m.Commissions.Value)) +
                (filteredTrades |> List.sumBy (fun (t: Trade) -> t.Commissions.Value))

            let fees = 
                (filteredMovements |> List.sumBy (fun (m: BrokerMovement) -> m.Fees.Value)) +
                (filteredTrades |> List.sumBy (fun (t: Trade) -> t.Fees.Value)) +
                (filteredMovements |> List.filter (fun (m: BrokerMovement) -> m.MovementType = BrokerMovementType.Fee) |> List.sumBy (fun (m: BrokerMovement) -> m.Amount.Value))

            let dividendsReceived = 
                filteredDividends |> List.sumBy (fun (d: Dividend) -> d.DividendAmount.Value)

            let invested = 
                filteredTrades 
                |> List.filter (fun (t: Trade) -> t.TradeCode = TradeCode.BuyToOpen || t.TradeCode = TradeCode.BuyToClose)
                |> List.sumBy (fun (t: Trade) -> t.Quantity * t.Price.Value)

            // Get base values from previous snapshot or default to zero
            let baseDeposited = previousSnapshot |> Option.map (fun (ps: BrokerFinancialSnapshot) -> ps.Deposited.Value) |> Option.defaultValue 0m
            let baseWithdrawn = previousSnapshot |> Option.map (fun (ps: BrokerFinancialSnapshot) -> ps.Withdrawn.Value) |> Option.defaultValue 0m
            let baseCommissions = previousSnapshot |> Option.map (fun (ps: BrokerFinancialSnapshot) -> ps.Commissions.Value) |> Option.defaultValue 0m
            let baseFees = previousSnapshot |> Option.map (fun (ps: BrokerFinancialSnapshot) -> ps.Fees.Value) |> Option.defaultValue 0m
            let baseDividends = previousSnapshot |> Option.map (fun (ps: BrokerFinancialSnapshot) -> ps.DividendsReceived.Value) |> Option.defaultValue 0m
            let baseInvested = previousSnapshot |> Option.map (fun (ps: BrokerFinancialSnapshot) -> ps.Invested.Value) |> Option.defaultValue 0m
            let baseRealizedGains = previousSnapshot |> Option.map (fun (ps: BrokerFinancialSnapshot) -> ps.RealizedGains.Value) |> Option.defaultValue 0m
            let baseMovementCounter = previousSnapshot |> Option.map (fun (ps: BrokerFinancialSnapshot) -> ps.MovementCounter) |> Option.defaultValue 0

            let openTrades = 
                filteredTrades 
                |> List.exists (fun (t: Trade) -> t.TradeCode = TradeCode.BuyToOpen || t.TradeCode = TradeCode.SellToOpen)

            return {
                Base = SnapshotManagerUtils.createBaseSnapshot snapshotDate
                BrokerId = brokerId
                BrokerAccountId = brokerAccountId
                CurrencyId = currencyId
                MovementCounter = baseMovementCounter + filteredMovements.Length + filteredTrades.Length + filteredDividends.Length
                BrokerSnapshotId = 0
                BrokerAccountSnapshotId = brokerAccountSnapshotId
                RealizedGains = Money.FromAmount (baseRealizedGains)  // TODO: Calculate realized gains from closed positions
                RealizedPercentage = 0m  // TODO: Calculate percentage
                UnrealizedGains = Money.FromAmount 0m  // TODO: Calculate unrealized gains from open positions
                UnrealizedGainsPercentage = 0m  // TODO: Calculate percentage
                Invested = Money.FromAmount (baseInvested + invested)
                Commissions = Money.FromAmount (baseCommissions + commissions)
                Fees = Money.FromAmount (baseFees + fees)
                Deposited = Money.FromAmount (baseDeposited + deposited)
                Withdrawn = Money.FromAmount (baseWithdrawn + withdrawn)
                DividendsReceived = Money.FromAmount (baseDividends + dividendsReceived)
                OptionsIncome = Money.FromAmount 0m  // TODO: Calculate from option trades
                OtherIncome = Money.FromAmount 0m  // TODO: Calculate from other income movements
                OpenTrades = openTrades
            }
        }

    /// <summary>
    /// Calculates BrokerFinancialSnapshots for a broker account for a given date and currency list.
    /// This function should:
    /// 1. For each currency in currencyList, aggregate all relevant financial data (movements, trades, dividends, fees, etc.) for the broker account up to and including snapshotDate.
    /// 2. Calculate all financial fields (deposited, withdrawn, fees, commissions, invested, realized/unrealized gains, etc.) for each currency.
    /// 3. Link the snapshot to the correct BrokerAccountSnapshotId and BrokerSnapshotId.
    /// 4. Ensure the CurrencyId is valid and matches the currency being processed.
    /// 5. Return a list of BrokerFinancialSnapshot records, one for each currency in currencyList, fully populated with calculated values.
    ///
    /// Note: The current implementation is a placeholder and only returns a default snapshot for the first currency.
    ///       The full implementation should loop through currencyList and perform the calculations described above.
    /// </summary>
    let private calculateBrokerAccountFinancials 
        (snapshotDate: DateTimePattern)
        (brokerId: int)
        (brokerAccountId: int)
        (brokerAccountSnapshotId: int)
        (currencyList: int list)
        =
        async {
            let! snapshots =
                currencyList
                |> List.map (fun currencyId ->
                    calculateBrokerAccountFinancialForCurrency snapshotDate brokerId brokerAccountId brokerAccountSnapshotId currencyId
                )
                |> Async.Parallel
            return snapshots |> Array.toList
        }

    /// <summary>
    /// Simplified: Calculates BrokerFinancialSnapshots for a broker account or broker and a snapshot date, based on previous snapshots only.
    /// </summary>
    let calculateFinancialSnapshots 
        (snapshotDate: DateTimePattern)
        (brokerId: int)
        (brokerAccountId: int)
        (brokerSnapshotId: int)
        (brokerAccountSnapshotId: int)
        (currencyList: int list)
        =
        if brokerId > 0 then
            if brokerSnapshotId <= 0 then
                failwithf "brokerSnapshotId must be > 0 when brokerId > 0"
            else
                calculateBrokerFinancials snapshotDate brokerId brokerSnapshotId currencyList
        elif brokerAccountId > 0 then
            if brokerAccountSnapshotId <= 0 then
                failwithf "brokerAccountSnapshotId must be > 0 when brokerAccountId > 0"
            else
                calculateBrokerAccountFinancials snapshotDate brokerId brokerAccountId brokerAccountSnapshotId currencyList
        else
            failwithf "Either brokerId or brokerAccountId must be > 0"

    let getInitialFinancialSnapshot
        (snapshotDate: DateTimePattern)
        (brokerId: int)
        (brokerAccountId: int)
        (brokerSnapshotId: int)
        (brokerAccountSnapshotId: int)
        =
        task {
            return! defaultFinancialSnapshot snapshotDate brokerId brokerAccountId brokerSnapshotId brokerAccountSnapshotId
        }