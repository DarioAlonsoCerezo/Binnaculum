namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Storage.SnapshotManagerUtils
open BrokerFinancialSnapshotExtensions

module internal BrokerFinancialSnapshotManager =
    /// <summary>
    /// Simplified: Calculates BrokerFinancialSnapshots for a broker account or broker and a snapshot date, based on previous snapshots only.
    /// </summary>
    let private calculateFinancialSnapshots (snapshotDate: DateTimePattern) (brokerId: int) (brokerAccountId: int) =
        async {
            // Fetch latest BrokerFinancialSnapshot list before calculation
            let! previousSnapshots =
                if brokerId <> -1 then
                    Do.getLatestByBrokerIdGroupedByDate brokerId |> Async.AwaitTask
                elif brokerAccountId <> -1 then
                    Do.getLatestByBrokerAccountIdGroupedByDate brokerAccountId |> Async.AwaitTask
                else
                    async { return [] }
            // Placeholder implementation
            return {
                Base = SnapshotManagerUtils.createBaseSnapshot snapshotDate
                BrokerId = brokerId
                BrokerAccountId = brokerAccountId
                CurrencyId = currencyId
                MovementCounter = 0
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
    /// Calculates a placeholder BrokerFinancialSnapshot for a specific broker account and date (single currency, placeholder currencyId = 1)
    /// </summary>
    let calculateForBrokerAccount (brokerAccountId: int) (date: DateTimePattern) =
        async {
            let snapshotDate = getDateOnly date
            let! snapshot = calculateFinancialSnapshots snapshotDate -1 brokerAccountId
            return [snapshot]
        }

    /// <summary>
    /// Calculates a placeholder BrokerFinancialSnapshot for a specific broker and date (single currency, placeholder currencyId = 1)
    /// </summary>
    let calculateForBroker (brokerId: int) (date: DateTimePattern) =
        async {
            let snapshotDate = getDateOnly date
            let! snapshot = calculateFinancialSnapshots snapshotDate brokerId -1
            return [snapshot]
        }

