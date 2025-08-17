namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open BrokerFinancialSnapshotExtensions

module internal BrokerFinancialReset = 

    /// <summary>
    /// Resets all financial fields of an existing snapshot to zero/default values.
    /// Used for SCENARIO H: No movements, no previous snapshot, has existing snapshot.
    /// </summary>
    let internal zeroOutFinancialSnapshot (existing: BrokerFinancialSnapshot) =
        task {
            let zeroedSnapshot = {
                existing with
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
                    MovementCounter = 0
            }
            do! zeroedSnapshot.save()
        }