namespace Binnaculum.Core.Storage

open System
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Database
open BankAccountBalanceExtensions
open BrokerMovementExtensions
open BankAccountSnapshotExtensions
open BankSnapshotExtensions
open BrokerAccountSnapshotExtensions
open BrokerSnapshotExtensions
open TickerSnapshotExtensions
open BankAccountExtensions
open BrokerAccountExtensions

/// <summary>
/// This module handles the creation, updating, and recalculation of daily snapshots
/// for all relevant entities when movements are added, updated, or deleted.
/// It ensures data consistency by recalculating affected snapshots when retroactive changes occur.
/// </summary>
module internal SnapshotManager =

    /// Helper function to get the date part only from a DateTimePattern
    let private getDateOnly (dateTime: DateTimePattern) =
        let date = dateTime.Value.Date
        DateTimePattern.FromDateTime(date)
    
    /// Creates a base snapshot with the given date
    let private createBaseSnapshot (date: DateTimePattern) =
        {
            Id = 0
            Date = getDateOnly date
            Audit = AuditableEntity.Default
        }

    /// <summary>
    /// Calculates BankAccountSnapshot for a specific bank account on a specific date
    /// by aggregating all movements up to and including that date
    /// </summary>
    let private calculateBankAccountSnapshot (bankAccountId: int, date: DateTimePattern, currencyId: int) = 
        task {
            let snapshotDate = getDateOnly date
            
            // Get all movements and filter for this bank account up to the snapshot date
            let! allMovements = BankAccountBalanceExtensions.Do.getAll()
            
            let movementsUpToDate = 
                allMovements
                |> List.filter (fun m -> m.BankAccountId = bankAccountId && m.TimeStamp.Value.Date <= snapshotDate.Value.Date)
            
            // Calculate aggregated values
            let totalBalance = 
                movementsUpToDate
                |> List.sumBy (fun m -> 
                    match m.MovementType with
                    | BankAccountMovementType.Balance -> m.Amount.Value
                    | BankAccountMovementType.Interest -> m.Amount.Value
                    | BankAccountMovementType.Fee -> -m.Amount.Value)
                |> Money.FromAmount
                
            let interestEarned =
                movementsUpToDate
                |> List.filter (fun m -> m.MovementType = BankAccountMovementType.Interest)
                |> List.sumBy (fun m -> m.Amount.Value)
                |> Money.FromAmount
                
            let feesPaid =
                movementsUpToDate
                |> List.filter (fun m -> m.MovementType = BankAccountMovementType.Fee)
                |> List.sumBy (fun m -> m.Amount.Value)
                |> Money.FromAmount
            
            return {
                Base = createBaseSnapshot snapshotDate
                BankAccountId = bankAccountId
                Balance = totalBalance
                CurrencyId = currencyId
                InterestEarned = interestEarned
                FeesPaid = feesPaid
            }
        }

    /// <summary>
    /// Calculates BankSnapshot for a specific bank on a specific date
    /// by aggregating all BankAccountSnapshots for that bank on that date
    /// </summary>
    let private calculateBankSnapshot (bankId: int, date: DateTimePattern) =
        task {
            let snapshotDate = getDateOnly date
            
            // Get all bank accounts and filter by bank ID
            let! allBankAccounts = BankAccountExtensions.Do.getAll()
            let bankAccounts = allBankAccounts |> List.filter (fun acc -> acc.BankId = bankId)
            
            // Calculate or get snapshots for each bank account on this date
            let! accountSnapshots = 
                bankAccounts
                |> List.map (fun account -> 
                    task {
                        let! existing = BankAccountSnapshotExtensions.Do.getByBankAccountIdAndDate(account.Id, snapshotDate)
                        match existing with
                        | Some snapshot -> return snapshot
                        | None -> 
                            // Calculate new snapshot
                            return! calculateBankAccountSnapshot(account.Id, snapshotDate, account.CurrencyId)
                    })
                |> System.Threading.Tasks.Task.WhenAll
                
            let snapshots = accountSnapshots |> Array.toList
            
            // Aggregate values from all account snapshots
            let totalBalance = 
                snapshots
                |> List.sumBy (fun s -> s.Balance.Value)
                |> Money.FromAmount
                
            let totalInterestEarned =
                snapshots
                |> List.sumBy (fun s -> s.InterestEarned.Value)
                |> Money.FromAmount
                
            let totalFeesPaid =
                snapshots
                |> List.sumBy (fun s -> s.FeesPaid.Value)
                |> Money.FromAmount
                
            let accountCount = snapshots.Length
            
            return {
                Base = createBaseSnapshot snapshotDate
                BankId = bankId
                TotalBalance = totalBalance
                InterestEarned = totalInterestEarned
                FeesPaid = totalFeesPaid
                AccountCount = accountCount
            }
        }

    /// <summary>
    /// Calculates BrokerAccountSnapshot for a specific broker account on a specific date
    /// This is a simplified implementation - in a real system, this would calculate portfolio value
    /// from all holdings, cash positions, and market prices
    /// </summary>
    let private calculateBrokerAccountSnapshot (brokerAccountId: int, date: DateTimePattern) =
        task {
            let snapshotDate = getDateOnly date
            
            // Get all broker movements and filter for this account up to the snapshot date
            let! allMovements = BrokerMovementExtensions.Do.getAll()
            
            let movementsUpToDate = 
                allMovements
                |> List.filter (fun m -> m.BrokerAccountId = brokerAccountId && m.TimeStamp.Value.Date <= snapshotDate.Value.Date)
            
            // Calculate net cash from movements (simplified calculation)
            let netCash = 
                movementsUpToDate
                |> List.sumBy (fun m -> 
                    match m.MovementType with
                    | BrokerMovementType.Deposit -> m.Amount.Value
                    | BrokerMovementType.Withdrawal -> -m.Amount.Value
                    | BrokerMovementType.Fee -> -m.Amount.Value
                    | BrokerMovementType.InterestsGained -> m.Amount.Value
                    | BrokerMovementType.InterestsPaid -> -m.Amount.Value
                    | _ -> 0m)
                |> Money.FromAmount
            
            // TODO: In a complete implementation, this would also include:
            // - Current market value of all stock positions
            // - Options positions
            // - Other securities
            // For now, we'll use cash position as portfolio value
            
            return {
                Base = createBaseSnapshot snapshotDate
                BrokerAccountId = brokerAccountId
                PortfolioValue = netCash
            }
        }

    /// <summary>
    /// Calculates BrokerSnapshot for a specific broker on a specific date
    /// by aggregating all BrokerAccountSnapshots for that broker on that date
    /// </summary>
    let private calculateBrokerSnapshot (brokerId: int, date: DateTimePattern) =
        task {
            let snapshotDate = getDateOnly date
            
            // Get all broker accounts and filter by broker ID
            let! allBrokerAccounts = BrokerAccountExtensions.Do.getAll()
            let brokerAccounts = allBrokerAccounts |> List.filter (fun acc -> acc.BrokerId = brokerId)
            
            // Calculate or get snapshots for each broker account on this date
            let! accountSnapshots = 
                brokerAccounts
                |> List.map (fun account -> 
                    task {
                        let! existing = BrokerAccountSnapshotExtensions.Do.getByBrokerAccountIdAndDate(account.Id, snapshotDate)
                        match existing with
                        | Some snapshot -> return snapshot
                        | None -> 
                            // Calculate new snapshot
                            return! calculateBrokerAccountSnapshot(account.Id, snapshotDate)
                    })
                |> System.Threading.Tasks.Task.WhenAll
                
            let snapshots = accountSnapshots |> Array.toList
            
            // Aggregate values from all account snapshots
            let totalPortfolioValue = 
                snapshots
                |> List.sumBy (fun s -> s.PortfolioValue.Value)
                |> Money.FromAmount
                
            let accountCount = snapshots.Length
            
            return {
                Base = createBaseSnapshot snapshotDate
                BrokerId = brokerId
                PortfoliosValue = totalPortfolioValue
                AccountCount = accountCount
            }
        }

    /// <summary>
    /// Creates or updates a BankAccountSnapshot for the given bank account and date
    /// </summary>
    let updateBankAccountSnapshot (bankAccountId: int, date: DateTimePattern, currencyId: int) =
        task {
            let snapshotDate = getDateOnly date
            
            // Check if snapshot already exists
            let! existingSnapshot = BankAccountSnapshotExtensions.Do.getByBankAccountIdAndDate(bankAccountId, snapshotDate)
            
            // Calculate new snapshot values
            let! newSnapshot = calculateBankAccountSnapshot(bankAccountId, snapshotDate, currencyId)
            
            match existingSnapshot with
            | Some existing ->
                // Update existing snapshot
                let updatedSnapshot = { newSnapshot with Base = { newSnapshot.Base with Id = existing.Base.Id } }
                do! updatedSnapshot.save()
            | None ->
                // Create new snapshot
                do! newSnapshot.save()
        }

    /// <summary>
    /// Creates or updates a BankSnapshot for the given bank and date
    /// </summary>
    let updateBankSnapshot (bankId: int, date: DateTimePattern) =
        task {
            let snapshotDate = getDateOnly date
            
            // Check if snapshot already exists
            let! existingSnapshot = BankSnapshotExtensions.Do.getByBankIdAndDate(bankId, snapshotDate)
            
            // Calculate new snapshot values
            let! newSnapshot = calculateBankSnapshot(bankId, snapshotDate)
            
            match existingSnapshot with
            | Some existing ->
                // Update existing snapshot
                let updatedSnapshot = { newSnapshot with Base = { newSnapshot.Base with Id = existing.Base.Id } }
                do! updatedSnapshot.save()
            | None ->
                // Create new snapshot
                do! newSnapshot.save()
        }

    /// <summary>
    /// Creates or updates a BrokerAccountSnapshot for the given broker account and date
    /// </summary>
    let updateBrokerAccountSnapshot (brokerAccountId: int, date: DateTimePattern) =
        task {
            let snapshotDate = getDateOnly date
            
            // Check if snapshot already exists
            let! existingSnapshot = BrokerAccountSnapshotExtensions.Do.getByBrokerAccountIdAndDate(brokerAccountId, snapshotDate)
            
            // Calculate new snapshot values
            let! newSnapshot = calculateBrokerAccountSnapshot(brokerAccountId, snapshotDate)
            
            match existingSnapshot with
            | Some existing ->
                // Update existing snapshot
                let updatedSnapshot = { newSnapshot with Base = { newSnapshot.Base with Id = existing.Base.Id } }
                do! updatedSnapshot.save()
            | None ->
                // Create new snapshot
                do! newSnapshot.save()
        }

    /// <summary>
    /// Creates or updates a BrokerSnapshot for the given broker and date
    /// </summary>
    let updateBrokerSnapshot (brokerId: int, date: DateTimePattern) =
        task {
            let snapshotDate = getDateOnly date
            
            // Check if snapshot already exists
            let! existingSnapshot = BrokerSnapshotExtensions.Do.getByBrokerIdAndDate(brokerId, snapshotDate)
            
            // Calculate new snapshot values
            let! newSnapshot = calculateBrokerSnapshot(brokerId, snapshotDate)
            
            match existingSnapshot with
            | Some existing ->
                // Update existing snapshot
                let updatedSnapshot = { newSnapshot with Base = { newSnapshot.Base with Id = existing.Base.Id } }
                do! updatedSnapshot.save()
            | None ->
                // Create new snapshot
                do! newSnapshot.save()
        }

    /// <summary>
    /// Handles snapshot updates when a BankAccountMovement is saved
    /// Updates both the account snapshot and the parent bank snapshot
    /// </summary>
    let handleBankMovementSnapshot (movement: BankAccountMovement) =
        task {
            // Get the bank account to find the bank ID
            let! bankAccount = BankAccountExtensions.Do.getById movement.BankAccountId
            
            match bankAccount with
            | Some account ->
                // Update account snapshot
                do! updateBankAccountSnapshot(movement.BankAccountId, movement.TimeStamp, movement.CurrencyId)
                
                // Update parent bank snapshot
                do! updateBankSnapshot(account.BankId, movement.TimeStamp)
            | None ->
                // Log error or handle missing bank account
                ()
        }

    /// <summary>
    /// Handles snapshot updates when a BrokerMovement is saved
    /// Updates both the account snapshot and the parent broker snapshot
    /// </summary>
    let handleBrokerMovementSnapshot (movement: BrokerMovement) =
        task {
            // Get the broker account to find the broker ID
            let! brokerAccount = BrokerAccountExtensions.Do.getById movement.BrokerAccountId
            
            match brokerAccount with
            | Some account ->
                // Update account snapshot
                do! updateBrokerAccountSnapshot(movement.BrokerAccountId, movement.TimeStamp)
                
                // Update parent broker snapshot
                do! updateBrokerSnapshot(account.BrokerId, movement.TimeStamp)
            | None ->
                // Log error or handle missing broker account
                ()
        }

    /// <summary>
    /// Recalculates all snapshots from a given date forward for a specific bank account
    /// This is used when a retroactive movement affects existing snapshots
    /// </summary>
    let recalculateBankAccountSnapshotsFromDate (bankAccountId: int, fromDate: DateTimePattern, currencyId: int) =
        task {
            let startDate = getDateOnly fromDate
            
            // Get all snapshots for this account from the start date forward
            let! futureSnapshots = BankAccountSnapshotExtensions.Do.getByDateRange(bankAccountId, startDate, DateTimePattern.FromDateTime(DateTime.MaxValue))
            
            // Recalculate each snapshot
            for snapshot in futureSnapshots do
                do! updateBankAccountSnapshot(bankAccountId, snapshot.Base.Date, currencyId)
        }

    /// <summary>
    /// Recalculates all snapshots from a given date forward for a specific broker account
    /// This is used when a retroactive movement affects existing snapshots
    /// </summary>
    let recalculateBrokerAccountSnapshotsFromDate (brokerAccountId: int, fromDate: DateTimePattern) =
        task {
            let startDate = getDateOnly fromDate
            
            // Get all snapshots for this account from the start date forward
            let! futureSnapshots = BrokerAccountSnapshotExtensions.Do.getByDateRange(brokerAccountId, startDate, DateTimePattern.FromDateTime(DateTime.MaxValue))
            
            // Recalculate each snapshot
            for snapshot in futureSnapshots do
                do! updateBrokerAccountSnapshot(brokerAccountId, snapshot.Base.Date)
        }