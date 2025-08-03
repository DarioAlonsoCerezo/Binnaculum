namespace Binnaculum.Core.Storage

open System
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open BrokerAccountSnapshotExtensions
open BrokerFinancialSnapshotExtensions
open Binnaculum.Core.Storage.SnapshotManagerUtils

/// <summary>
/// Handles creation, updating, and recalculation of BrokerAccountSnapshots.
/// Enhanced with multi-currency support for per-currency detail rows.
/// 
/// This module exposes only two public entry points:
/// - handleBrokerAccountChange: For handling changes to existing broker accounts
/// - handleNewBrokerAccount: For initializing snapshots for new broker accounts
/// 
/// All other functionality is internal to maintain proper encapsulation and prevent misuse.
/// </summary>
module internal BrokerAccountSnapshotManager =
    /// <summary>
    /// Calculates BrokerAccountSnapshot for a specific broker account on a specific date
    /// by aggregating all movements up to and including that date.
    /// This is an internal calculation method used by public entry points.
    /// </summary>
    let private calculateBrokerAccountSnapshot (brokerAccountId: int, date: DateTimePattern) =
        task {
            let snapshotDate = getDateOnly date
            // Aggregate all movements up to and including the date
            let! movements = BrokerMovementExtensions.Do.getByBrokerAccountIdAndDateRange(brokerAccountId, snapshotDate)
            let netCash = 
                movements
                |> List.sumBy (fun m -> 
                    match m.MovementType with
                    | BrokerMovementType.Deposit -> m.Amount.Value
                    | BrokerMovementType.Withdrawal -> -m.Amount.Value
                    | BrokerMovementType.Fee -> -m.Amount.Value
                    | BrokerMovementType.InterestsGained -> m.Amount.Value
                    | BrokerMovementType.InterestsPaid -> -m.Amount.Value
                    | _ -> 0m)
                |> Money.FromAmount
            return {
                Base = SnapshotManagerUtils.createBaseSnapshot snapshotDate
                BrokerAccountId = brokerAccountId
                PortfolioValue = netCash
            }
        }

    /// <summary>
    /// Creates or updates a BrokerAccountSnapshot for the given broker account and date.
    /// This is an internal update method used by public entry points.
    /// </summary>
    let private updateBrokerAccountSnapshot (brokerAccountId: int, date: DateTimePattern) =
        task {
            let snapshotDate = getDateOnly date
            let! existingSnapshot = BrokerAccountSnapshotExtensions.Do.getByBrokerAccountIdAndDate(brokerAccountId, snapshotDate)
            let! newSnapshot = calculateBrokerAccountSnapshot(brokerAccountId, snapshotDate)
            match existingSnapshot with
            | Some existing ->
                let updatedSnapshot = { newSnapshot with Base = { newSnapshot.Base with Id = existing.Base.Id } }
                do! updatedSnapshot.save()
            | None ->
                do! newSnapshot.save()
            // After saving, update the corresponding broker snapshot
            let! brokerAccountOpt = BrokerAccountExtensions.Do.getById(brokerAccountId)
            match brokerAccountOpt with
            | Some brokerAccount ->
                do! Binnaculum.Core.Storage.BrokerSnapshotManager.updateBrokerSnapshot(brokerAccount.BrokerId, snapshotDate)
            | None ->
                ()
        }

    // =================================================================================
    // Multi-Currency BrokerAccount Snapshot Functions
    // =================================================================================

    /// <summary>
    /// Gets all relevant currencies used by movements, trades, dividends, dividend-taxes 
    /// and option-trades for a specific broker account up to a given date.
    /// Returns default currency (USD, ID=1) if no currencies are found.
    /// Currently supports BrokerMovements only, with placeholders for other data sources.
    /// </summary>
    /// <param name="accountId">The broker account ID to analyze</param>
    /// <param name="date">The cutoff date for analysis (inclusive)</param>
    /// <returns>List of unique currency IDs used by the account</returns>
    let private getRelevantCurrencies (accountId: int, date: DateTimePattern) =
        task {
            // Get all movements, trades, dividends etc. and extract their currencies
            let! movements = BrokerMovementExtensions.Do.getByBrokerAccountIdAndDateRange(accountId, date)
            let movementCurrencies = movements |> List.map (fun m -> m.CurrencyId) |> Set.ofList
            
            // For now, we'll start with movement currencies and return default if empty
            // TODO: Add trades, dividends, dividend taxes, and option trades when needed
            let allCurrencies = Set.toList movementCurrencies
            
            let finalCurrencies = 
                if List.isEmpty allCurrencies then [1] // Default to USD currency ID 1
                else allCurrencies
                
            return finalCurrencies
        }

    /// <summary>
    /// Calculates BrokerFinancialSnapshot for a specific broker account, currency and date
    /// by aggregating all movements, trades, dividends, taxes and option-trades in that currency.
    /// Currently processes BrokerMovements only, with placeholders for comprehensive data sources.
    /// </summary>
    /// <param name="accountId">The broker account ID</param>
    /// <param name="currencyId">The currency ID to filter by</param>
    /// <param name="date">The snapshot date</param>
    /// <returns>A populated BrokerFinancialSnapshot for the specified currency</returns>
    let private calculateBrokerFinancialSnapshot (accountId: int, currencyId: int, date: DateTimePattern) =
        task {
            let snapshotDate = getDateOnly date
            
            // Get movements for this account and filter by currency
            let! allMovements = BrokerMovementExtensions.Do.getByBrokerAccountIdAndDateRange(accountId, date)
            let movements = allMovements |> List.filter (fun m -> m.CurrencyId = currencyId)
            
            // TODO: Add queries for trades, dividends, dividend taxes, and option trades
            // For now, we'll use movements only for the basic implementation
            
            // Calculate cash flows with explicit type annotations
            let deposited = 
                movements
                |> List.filter (fun (m: BrokerMovement) -> m.MovementType = BrokerMovementType.Deposit)
                |> List.sumBy (fun (m: BrokerMovement) -> m.Amount.Value)
                |> Money.FromAmount
                
            let withdrawn = 
                movements
                |> List.filter (fun (m: BrokerMovement) -> m.MovementType = BrokerMovementType.Withdrawal)
                |> List.sumBy (fun (m: BrokerMovement) -> m.Amount.Value)
                |> Money.FromAmount
                
            let fees = 
                movements
                |> List.filter (fun (m: BrokerMovement) -> m.MovementType = BrokerMovementType.Fee)
                |> List.sumBy (fun (m: BrokerMovement) -> m.Amount.Value)
                |> Money.FromAmount
                
            let otherIncome = 
                movements
                |> List.filter (fun (m: BrokerMovement) -> m.MovementType = BrokerMovementType.InterestsGained)
                |> List.sumBy (fun (m: BrokerMovement) -> m.Amount.Value)
                |> Money.FromAmount
            
            // Initialize other values for basic implementation
            let invested = Money.FromAmount(0m)
            let commissions = Money.FromAmount(0m)
            let tradesFees = Money.FromAmount(0m)
            let dividendsReceived = Money.FromAmount(0m)
            let dividendTaxesPaid = Money.FromAmount(0m)
            let optionsIncome = Money.FromAmount(0m)
            let openTrades = false
            
            // Calculate portfolio value (simplified - net cash for now)
            let netCash = deposited.Value - withdrawn.Value + otherIncome.Value - fees.Value - dividendTaxesPaid.Value + dividendsReceived.Value + optionsIncome.Value
            
            // Calculate realized gains (simplified)
            let realizedGains = Money.FromAmount(0m) // TODO: Implement proper calculation
            let realizedPercentage = 0m
            
            // Calculate unrealized gains (simplified)
            let unrealizedGains = Money.FromAmount(0m) // TODO: Implement proper calculation
            let unrealizedGainsPercentage = 0m
            
            // Movement counter
            let movementCounter = movements.Length
            
            return {
                Base = SnapshotManagerUtils.createBaseSnapshot snapshotDate
                BrokerId = -1 // Not for specific broker
                BrokerAccountId = accountId
                CurrencyId = currencyId
                MovementCounter = movementCounter
                BrokerSnapshotId = -1 // Will be set when needed
                BrokerAccountSnapshotId = -1 // Will be set when needed
                RealizedGains = realizedGains
                RealizedPercentage = realizedPercentage
                UnrealizedGains = unrealizedGains
                UnrealizedGainsPercentage = unrealizedGainsPercentage
                Invested = invested
                Commissions = Money.FromAmount(commissions.Value + tradesFees.Value)
                Fees = fees
                Deposited = deposited
                Withdrawn = withdrawn
                DividendsReceived = dividendsReceived
                OptionsIncome = optionsIncome
                OtherIncome = otherIncome
                OpenTrades = openTrades
            }
        }

    /// <summary>
    /// Creates or updates a BrokerFinancialSnapshot for the given broker account, currency and date.
    /// Preserves Base.Id on updates to maintain referential integrity.
    /// </summary>
    /// <param name="accountId">The broker account ID</param>
    /// <param name="currencyId">The currency ID</param>
    /// <param name="date">The snapshot date</param>
    /// <returns>Task that completes when the snapshot is saved</returns>
    let private updateBrokerFinancialSnapshot (accountId: int, currencyId: int, date: DateTimePattern) =
        task {
            // Check if financial snapshot already exists for this account, currency and date
            let! existingSnapshots = BrokerFinancialSnapshotExtensions.Do.getByBrokerAccountId(accountId)
            let existingSnapshot = 
                existingSnapshots 
                |> List.tryFind (fun s -> s.CurrencyId = currencyId && s.Base.Date.Value.Date = date.Value.Date)
            
            let! newSnapshot = calculateBrokerFinancialSnapshot(accountId, currencyId, date)
            
            match existingSnapshot with
            | Some existing ->
                // Preserve the existing ID
                let updatedSnapshot = { newSnapshot with Base = { newSnapshot.Base with Id = existing.Base.Id } }
                do! updatedSnapshot.save()
            | None ->
                do! newSnapshot.save()
        }

    /// <summary>
    /// Extended version of updateBrokerAccountSnapshot that handles per-currency detail rows.
    /// 1. Ensures summary snapshot exists
    /// 2. Gets all relevant currencies for the account and date
    /// 3. Updates BrokerFinancialSnapshot for each currency
    /// This is an internal extended update method used by public entry points.
    /// </summary>
    /// <param name="accountId">The broker account ID</param>
    /// <param name="date">The snapshot date</param>
    /// <returns>Task that completes when all snapshots are updated</returns>
    let private updateBrokerAccountSnapshotExtended (accountId: int, date: DateTimePattern) =
        task {
            let snapshotDate = getDateOnly date
            
            // 1. Ensure the main BrokerAccountSnapshot exists
            do! updateBrokerAccountSnapshot(accountId, date)
            
            // 2. Get all relevant currencies for this account and date
            let! currencies = getRelevantCurrencies(accountId, date)
            
            // 3. Update BrokerFinancialSnapshot for each currency
            for currencyId in currencies do
                do! updateBrokerFinancialSnapshot(accountId, currencyId, date)
        }

    /// <summary>
    /// Handles cascade updates for retroactive changes that affect future snapshots.
    /// 1. Runs one-day update for the specified date
    /// 2. Loads all future snapshots and re-applies per-currency updates in chronological order
    /// This ensures data consistency when historical changes are made.
    /// </summary>
    /// <param name="accountId">The broker account ID</param>
    /// <param name="date">The date of the retroactive change</param>
    /// <returns>Task that completes when all affected snapshots are updated</returns>
    let private updateBrokerAccountSnapshotWithCascade (accountId: int, date: DateTimePattern) =
        task {
            let startDate = getDateOnly date
            
            // 1. Run one-day update for the specified date
            do! updateBrokerAccountSnapshotExtended(accountId, date)
            
            // 2. Get all future snapshots that need to be recalculated
            let nextDay = DateTimePattern.FromDateTime(startDate.Value.Date.AddDays(1.0))
            let! futureSnapshots = BrokerAccountSnapshotExtensions.Do.getByDateRange(
                accountId, 
                nextDay, 
                DateTimePattern.FromDateTime(DateTime.MaxValue))
            
            // 3. Re-apply per-currency updates for each future snapshot date
            for snapshot in futureSnapshots do
                do! updateBrokerAccountSnapshotExtended(accountId, snapshot.Base.Date)
        }

    /// <summary>
    /// Handles snapshot updates when a new BrokerAccount is created
    /// Creates snapshots for the current day
    /// </summary>
    let handleNewBrokerAccount (brokerAccount: BrokerAccount) =
        task {
            let today = DateTimePattern.FromDateTime(DateTime.Today)
            // 1. Create default BrokerAccountSnapshot
            let defaultAccountSnapshot = 
                {
                    Base = createBaseSnapshot today
                    BrokerAccountId = brokerAccount.Id
                    PortfolioValue = Money.FromAmount(0m)
                }
            // 2. Save the default BrokerAccountSnapshot to the database
            do! defaultAccountSnapshot.save()
            // 3. Recover the saved BrokerAccountSnapshot to obtain its ID
            let! maybeSavedSnapshot = BrokerAccountSnapshotExtensions.Do.getByBrokerAccountIdAndDate(brokerAccount.Id, today)
            let savedSnapshot =
                match maybeSavedSnapshot with
                | Some s -> s
                | None -> failwithf "BrokerAccountSnapshot not found for account %i on %O" brokerAccount.Id today
            // 4. Get default BrokerFinancialSnapshot
            let! financialSnapshot = 
                BrokerFinancialSnapshotManager.getInitialFinancialSnapshot 
                    today 
                    0 
                    brokerAccount.Id 
                    0 
                    savedSnapshot.Base.Id
            // 5. Save the default BrokerFinancialSnapshot
            do! financialSnapshot.save()
        }

    /// <summary>
    /// Public API for handling broker account changes with multi-currency support.
    /// Automatically determines whether to use one-day or cascade update based on the date:
    /// - If date = today: runs one-day update (updateBrokerAccountSnapshotExtended)
    /// - Else: runs cascade update for retroactive changes (updateBrokerAccountSnapshotWithCascade)
    /// 
    /// This is the recommended entry point for triggering snapshot updates after account changes.
    /// </summary>
    /// <param name="accountId">The broker account ID that changed</param>
    /// <param name="date">The date of the change</param>
    /// <returns>Task that completes when the appropriate update strategy finishes</returns>
    let handleBrokerAccountChange (accountId: int, date: DateTimePattern) =
        task {
            // Convert the incoming pattern to a pure DateTime value
            let snapshotDate = getDateOnly date
            
            // Compute a DateTimePattern representing the day after the change
            let nextDayPattern =
                DateTimePattern.FromDateTime(snapshotDate.Value.Date.AddDays(1.0))
            
            // Load any snapshots after the change date to determine update strategy
            let! subsequentSnapshots =
                BrokerAccountSnapshotExtensions.Do.getByDateRange(
                    accountId,
                    nextDayPattern,
                    DateTimePattern.FromDateTime(DateTime.MaxValue))
            
            if List.isEmpty subsequentSnapshots then
                // No future snapshots: perform single-date update
                do! updateBrokerAccountSnapshotExtended(accountId, date)
            else
                // Future snapshots exist: perform cascade update
                do! updateBrokerAccountSnapshotWithCascade(accountId, date)
        }
