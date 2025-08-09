namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns

module internal BrokerFinancialValidator =

    /// <summary>
    /// Validates the input parameters for broker account snapshot operations.
    /// Throws detailed exceptions if any validation fails, does nothing if all validations pass.
    /// </summary>
    /// <param name="brokerAccountSnapshot">The broker account snapshot to validate</param>
    /// <param name="movementData">The movement data to validate</param>
    let internal validateSnapshotAndMovementData
        (brokerAccountSnapshot: BrokerAccountSnapshot)
        (movementData: BrokerAccountMovementData)
        =
        // Validate brokerAccountSnapshot parameters
        if brokerAccountSnapshot.Base.Id <= 0 then
            failwithf "Invalid broker account snapshot ID: %d. ID must be greater than 0." brokerAccountSnapshot.Base.Id
        
        if brokerAccountSnapshot.BrokerAccountId <= 0 then
            failwithf "Invalid broker account ID: %d. Broker account ID must be greater than 0." brokerAccountSnapshot.BrokerAccountId
        
        // Validate movementData parameters
        if movementData.BrokerAccountId <= 0 then
            failwithf "Invalid movement data broker account ID: %d. Must be greater than 0." movementData.BrokerAccountId
        
        if movementData.BrokerAccountId <> brokerAccountSnapshot.BrokerAccountId then
            failwithf "Movement data broker account ID (%d) does not match snapshot broker account ID (%d)." 
                movementData.BrokerAccountId brokerAccountSnapshot.BrokerAccountId
        
        if movementData.FromDate.Value > brokerAccountSnapshot.Base.Date.Value then
            failwithf "Movement data FromDate (%A) cannot be later than snapshot date (%A)." 
                movementData.FromDate.Value brokerAccountSnapshot.Base.Date.Value

    /// <summary>
    /// Validates consistency between previous and existing broker financial snapshots.
    /// Checks currency, broker account, date, and chronological order for correctness.
    /// Throws detailed exceptions if any validation fails.
    /// </summary>
    /// <param name="currencyId">The expected currency ID</param>
    /// <param name="brokerAccountId">The expected broker account ID</param>
    /// <param name="targetDate">The target date for the snapshot</param>
    /// <param name="previousSnapshot">The previous broker financial snapshot</param>
    /// <param name="existingSnapshot">The existing broker financial snapshot</param>
    let internal validateFinancialSnapshotsConsistency
        (currencyId: int)
        (brokerAccountId: int)
        (targetDate: DateTimePattern)
        (previousSnapshot: BrokerFinancialSnapshot)
        (existingSnapshot: BrokerFinancialSnapshot)
        =
            // =================================================================
            // VALIDATION AND CONSISTENCY CHECKS
            // =================================================================
            
            // Validate currency consistency across all snapshots
            if previousSnapshot.CurrencyId <> currencyId then
                failwithf "Previous snapshot currency (%d) does not match current currency (%d)" 
                    previousSnapshot.CurrencyId currencyId
            
            if existingSnapshot.CurrencyId <> currencyId then
                failwithf "Existing snapshot currency (%d) does not match current currency (%d)" 
                    existingSnapshot.CurrencyId currencyId
            
            // Validate broker account consistency
            if existingSnapshot.BrokerAccountId <> brokerAccountId then
                failwithf "Existing snapshot broker account (%d) does not match expected account (%d)"
                    existingSnapshot.BrokerAccountId brokerAccountId
            
            // Validate date consistency
            if existingSnapshot.Base.Date <> targetDate then
                failwithf "Existing snapshot date (%A) does not match target date (%A)"
                    existingSnapshot.Base.Date.Value targetDate.Value
            
            // Validate chronological order of snapshots
            if previousSnapshot.Base.Date.Value >= targetDate.Value then
                failwithf "Previous snapshot date (%A) must be before target date (%A)"
                    previousSnapshot.Base.Date.Value targetDate.Value

    /// <summary>
    /// Validates that the currency of the previous broker financial snapshot matches the expected currency.
    /// Throws a detailed exception if the currencies do not match.
    /// </summary>
    /// <param name="previousSnapshot">The previous broker financial snapshot</param>
    /// <param name="currencyId">The expected currency ID</param>
    let internal validatePreviousSnapshotCurrencyConsistency 
        (previousSnapshot: BrokerFinancialSnapshot)
        (currencyId: int)
        =
        if previousSnapshot.CurrencyId <> currencyId then
                failwithf "Previous snapshot currency (%d) does not match current currency (%d)" 
                    previousSnapshot.CurrencyId currencyId

    /// <summary>
    /// Validates that the existing broker financial snapshot matches the expected currency, broker account, and target date.
    /// Throws detailed exceptions if any validation fails.
    /// </summary>
    /// <param name="existingSnapshot">The existing broker financial snapshot</param>
    /// <param name="currencyId">The expected currency ID</param>
    /// <param name="brokerAccountId">The expected broker account ID</param>
    /// <param name="targetDate">The target date for the snapshot</param>
    let internal validateExistingSnapshotConsistency
        (existingSnapshot: BrokerFinancialSnapshot)
        (currencyId: int)
        (brokerAccountId: int)
        (targetDate: DateTimePattern)
        =
            // Validate currency consistency
            if existingSnapshot.CurrencyId <> currencyId then
                failwithf "Existing snapshot currency (%d) does not match current currency (%d)" 
                    existingSnapshot.CurrencyId currencyId
            
            // Validate broker account consistency
            if existingSnapshot.BrokerAccountId <> brokerAccountId then
                failwithf "Existing snapshot broker account (%d) does not match expected account (%d)"
                    existingSnapshot.BrokerAccountId brokerAccountId
            
            // Validate date consistency
            if existingSnapshot.Base.Date <> targetDate then
                failwithf "Existing snapshot date (%A) does not match target date (%A)"
                    existingSnapshot.Base.Date.Value targetDate.Value