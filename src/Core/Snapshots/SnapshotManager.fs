namespace Binnaculum.Core.Storage

open System
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Patterns

/// <summary>
/// This module orchestrates the creation, updating, and recalculation of daily snapshots
/// for all relevant entities by delegating to specialized managers.
/// </summary>
module internal SnapshotManager =

    let handleBankMovementSnapshot (movement: BankAccountMovement) =
        task {
            let! bankAccount = BankAccountExtensions.Do.getById movement.BankAccountId
            match bankAccount with
            | Some account ->
                do! BankAccountSnapshotManager.handleBankAccountChange(movement.BankAccountId, movement.TimeStamp)
                do! BankSnapshotManager.handleBankAccountChange(account.BankId, movement.TimeStamp)
            | None -> ()
        }

    let handleBrokerMovementSnapshot (movement: BrokerMovement) =
        task {
            let! brokerAccount = BrokerAccountExtensions.Do.getById movement.BrokerAccountId
            match brokerAccount with
            | Some account ->
                do! SnapshotProcessingCoordinator.handleBrokerAccountChange(movement.BrokerAccountId, movement.TimeStamp)
                do! BrokerSnapshotManager.handleBrokerMovementSnapshot(account.BrokerId, movement.TimeStamp)
            | None -> ()
        }

    let handleNewBankAccount (bankAccount: BankAccount) =
        task {
            do! BankAccountSnapshotManager.handleNewBankAccount(bankAccount)
            do! BankSnapshotManager.updateBankSnapshot(bankAccount.BankId, DateTimePattern.FromDateTime(DateTime.Today))
        }

    let handleNewBrokerAccount (brokerAccount: BrokerAccount) =
        task {
            do! BrokerAccountSnapshotManager.handleNewBrokerAccount(brokerAccount)
            do! BrokerSnapshotManager.updateBrokerSnapshot(brokerAccount.BrokerId, DateTimePattern.FromDateTime(DateTime.Today))
        }