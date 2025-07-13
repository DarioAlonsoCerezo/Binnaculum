namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Storage.SnapshotManagerUtils

module internal BrokerFinancialSnapshotManager =
    
    let private defaultFinancialSnapshot(snapshotDate: DateTimePattern) (brokerId: int) (brokerAccountId: int) =
        {
            Base = SnapshotManagerUtils.createBaseSnapshot snapshotDate
            BrokerId = brokerId
            BrokerAccountId = brokerAccountId
            CurrencyId = -1
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

    /// <summary>
    /// Simplified: Calculates BrokerFinancialSnapshots for a broker account or broker and a snapshot date, based on previous snapshots only.
    /// </summary>
    let private calculateFinancialSnapshots (snapshotDate: DateTimePattern) (brokerId: int) (brokerAccountId: int) =
        async {
            // Placeholder implementation
            return [
                defaultFinancialSnapshot snapshotDate brokerId brokerAccountId
            ]
        }

    /// <summary>
    /// Calculates a placeholder BrokerFinancialSnapshot for a specific broker account and date (single currency, placeholder currencyId = 1)
    /// </summary>
    let calculateForBrokerAccount (brokerAccountId: int) (date: DateTimePattern) =
        async {
            let snapshotDate = getDateOnly date
            let! snapshot = calculateFinancialSnapshots snapshotDate -1 brokerAccountId
            return snapshot
        }

    /// <summary>
    /// Calculates a placeholder BrokerFinancialSnapshot for a specific broker and date (single currency, placeholder currencyId = 1)
    /// </summary>
    let calculateForBroker (brokerId: int) (date: DateTimePattern) =
        async {
            let snapshotDate = getDateOnly date
            let! snapshot = calculateFinancialSnapshots snapshotDate brokerId -1
            return snapshot
        }

