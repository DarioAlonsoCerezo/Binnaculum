namespace Binnaculum.Core.Storage

open Binnaculum.Core.Patterns
open Binnaculum.Core.Logging
open Binnaculum.Core.Database.SnapshotsModel
open BrokerFinancialSnapshotExtensions

module internal BrokerFinancialDefault =

    /// <summary>
    /// Creates a default financial snapshot with zero values for initial setup.
    /// This is used when creating initial snapshots for brokers and accounts with no activity.
    /// </summary>
    /// <param name="snapshotDate">The date for the snapshot</param>
    /// <param name="brokerId">The broker ID (0 for account-level snapshots)</param>
    /// <param name="brokerAccountId">The broker account ID (0 for broker-level snapshots)</param>
    /// <param name="brokerSnapshotId">The broker snapshot ID (0 for account-level snapshots)</param>
    /// <param name="brokerAccountSnapshotId">The broker account snapshot ID (0 for broker-level snapshots)</param>
    /// <returns>Task that completes when the default snapshot is saved</returns>
    let internal create
        (snapshotDate: DateTimePattern)
        (brokerId: int)
        (brokerAccountId: int)
        (brokerSnapshotId: int)
        (brokerAccountSnapshotId: int)
        =
        task {
            // CoreLogger.logDebugf "BrokerFinancialDefault" "Creating default snapshot for BrokerAccountId: %A, Date: %A" brokerAccountId snapshotDate
            // CoreLogger.logDebug "BrokerFinancialDefault" "Step 1: Getting default currency..."
            let! currencyId = SnapshotManagerUtils.getDefaultCurrency ()
            // CoreLogger.logDebugf "BrokerFinancialDefault" "Step 2: Currency ID obtained: %A" currencyId

            // CoreLogger.logDebug "BrokerFinancialDefault" "Step 3: Creating base snapshot..."
            let baseSnapshot = SnapshotManagerUtils.createBaseSnapshot snapshotDate
            // CoreLogger.logDebugf "BrokerFinancialDefault" "Step 4: Base snapshot created with ID: %A" baseSnapshot.Id

            // CoreLogger.logDebug "BrokerFinancialDefault" "Step 5: Creating snapshot object..."
            let snapshot =
                { Base = baseSnapshot
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
                  NetCashFlow = Money.FromAmount 0m
                  OpenTrades = false }
            // CoreLogger.logDebug "BrokerFinancialDefault" "Step 6: Snapshot object created successfully"
            // CoreLogger.logDebugf "BrokerFinancialDefault" "About to save default snapshot with CurrencyId: %A, Deposited: %A" currencyId snapshot.Deposited.Value

            // CoreLogger.logDebug "BrokerFinancialDefault" "Step 7: Calling snapshot.save()..."
            do! snapshot.save ()
        // CoreLogger.logDebug "BrokerFinancialDefault" "Step 8: snapshot.save() completed successfully"
        // CoreLogger.logDebug "BrokerFinancialDefault" "Default snapshot saved successfully"
        }
