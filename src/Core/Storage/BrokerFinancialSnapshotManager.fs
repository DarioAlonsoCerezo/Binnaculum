namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open Microsoft.Maui.Storage
open Binnaculum.Core.Keys

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
            return [
                defaultFinancialSnapshot snapshotDate brokerId 0 brokerSnapshotId 0
            ]
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
            return [
                defaultFinancialSnapshot snapshotDate brokerId brokerAccountId 0 brokerAccountSnapshotId
            ]
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