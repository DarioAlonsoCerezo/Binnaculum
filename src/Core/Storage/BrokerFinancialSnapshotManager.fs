namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open SnapshotManagerUtils
open BrokerFinancialSnapshotExtensions

module internal BrokerFinancialSnapshotManager =
    
    let private defaultFinancialSnapshot
        (snapshotDate: DateTimePattern)
        (brokerId: int)
        (brokerAccountId: int)
        (brokerSnapshotId: int)
        (brokerAccountSnapshotId: int)
        =
        task {
            let! currencyId = getDefaultCurrency()
            let snapshot = {
                Base = createBaseSnapshot snapshotDate
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
            do! snapshot.save()
        }

    let brokerAccountCascadeUpdate
        (currentBrokerAccountSnapshot: BrokerAccountSnapshot)
        (snapshotsToUpdate: BrokerAccountSnapshot list)
        =
        task {
            //TODO: Use the movement data for cascade calculations
            return()
        }

    /// <summary>
    /// Handles changes to existing broker accounts, updating snapshots using a previous date.
    /// This function is used for one-day updates where the previous snapshot is used to calculate changes.
    /// </summary>
    let brokerAccountOneDayWithPrevious
        (brokerAccountSnapshot: BrokerAccountSnapshot)
        (previousSnapshot: BrokerAccountSnapshot)
        =
        task {
            //TODO
            return()
        }

    /// <summary>
    /// Handles changes to existing broker accounts, updating snapshots as necessary.
    /// This function performs a one-day update using the provided movement data.
    /// </summary>
    let brokerAccountOneDayUpdate
        (brokerAccountSnapshot: BrokerAccountSnapshot)
        (movementData: BrokerAccountMovementData)
        =
        task {
            //TODO: Implement one-day update logic using movementData
            // Use movementData.BrokerMovements, movementData.Trades, etc.
            // to calculate snapshot changes without additional database calls
            return()
        }

    /// <summary>
    /// Sets up the initial financial snapshot for a specific broker.
    /// This is used when a new broker is created or when initializing snapshots for existing brokers.
    /// </summary>
    let setupInitialFinancialSnapshotForBroker
        (snapshotDate: DateTimePattern)
        (brokerId: int)
        (brokerSnapshotId: int)
        =
        task {
            do! defaultFinancialSnapshot 
                    snapshotDate
                    brokerId
                    0 // BrokerAccountId set to 0 for broker-level snapshots
                    brokerSnapshotId
                    0 // BrokerAccountSnapshotId set to 0 for broker-level snapshots
        }

    /// <summary>
    /// Sets up the initial financial snapshot for a specific broker account.
    /// This is used when a new broker account is created or when initializing snapshots for existing accounts.
    /// 
    /// IMPORTANT: Currently creates only ONE financial snapshot using the default currency.
    /// TODO: Enhance to support multi-currency by creating one financial snapshot per currency.
    /// This will be essential when processing BrokerAccountMovementData with multiple currencies.
    /// </summary>
    let setupInitialFinancialSnapshotForBrokerAccount
        (brokerAccountSnapshot: BrokerAccountSnapshot)
        =
        task {
            // TODO: Multi-currency enhancement needed
            // When BrokerAccountMovementData contains movements in multiple currencies,
            // we should create one BrokerFinancialSnapshot per currency.
            // Current implementation creates only the default currency snapshot.
            do! defaultFinancialSnapshot 
                    brokerAccountSnapshot.Base.Date
                    0 // BrokerId set to 0 for account-level snapshots
                    brokerAccountSnapshot.BrokerAccountId
                    0 // BrokerSnapshotId set to 0 for account-level snapshots
                    brokerAccountSnapshot.Base.Id // Use the snapshot's own ID as BrokerAccountSnapshotId
        }