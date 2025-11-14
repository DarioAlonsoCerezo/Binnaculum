namespace Binnaculum.Core.Import

open System
open System.Threading
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Logging
open ImportDomainTypes

/// <summary>
/// Broker-agnostic persistence module for import movements.
/// Handles database persistence of validated movements for all brokers.
/// </summary>
module MovementPersistence =
    
    /// <summary>
    /// Persist a validated batch of movements to the database.
    /// This is the main entry point for broker-agnostic persistence.
    /// </summary>
    /// <param name="batch">Validated batch of movements to persist</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Persistence result with counts and any errors</returns>
    let internal persistMovementBatch
        (batch: ImportMovementBatch)
        (cancellationToken: CancellationToken)
        : Threading.Tasks.Task<MovementPersistenceResult> =
        
        task {
            CoreLogger.logInfo "MovementPersistence" $"Starting persistence of {batch.Movements.Length} movements"
            
            // First validate the batch
            let validationResult = MovementValidator.validateBatch batch
            
            let mutable brokerMovementsCreated = 0
            let mutable optionTradesCreated = 0
            let mutable stockTradesCreated = 0
            let mutable dividendsCreated = 0
            let mutable dividendTaxesCreated = 0
            let mutable persistenceErrors = []
            
            // Persist valid movements
            for movement in validationResult.Valid do
                cancellationToken.ThrowIfCancellationRequested()
                
                try
                    match movement with
                    | StockTradeMovement trade ->
                        do! TradeExtensions.Do.save trade |> Async.AwaitTask
                        stockTradesCreated <- stockTradesCreated + 1
                    
                    | OptionTradeMovement option ->
                        do! OptionTradeExtensions.Do.save option |> Async.AwaitTask
                        optionTradesCreated <- optionTradesCreated + 1
                    
                    | DividendMovement dividend ->
                        do! DividendExtensions.Do.save dividend |> Async.AwaitTask
                        dividendsCreated <- dividendsCreated + 1
                    
                    | DividendTaxMovement tax ->
                        do! DividendTaxExtensions.Do.save tax |> Async.AwaitTask
                        dividendTaxesCreated <- dividendTaxesCreated + 1
                    
                    | BrokerMovement bm ->
                        do! BrokerMovementExtensions.Do.save bm |> Async.AwaitTask
                        brokerMovementsCreated <- brokerMovementsCreated + 1
                        
                with ex ->
                    let errorMsg = $"Failed to persist movement: {ex.Message}"
                    CoreLogger.logError "MovementPersistence" errorMsg
                    persistenceErrors <- errorMsg :: persistenceErrors
            
            // Collect all errors (validation + persistence)
            let validationErrors = 
                validationResult.Invalid 
                |> List.map (fun (_, msg) -> $"Validation error: {msg}")
            
            let allErrors = validationErrors @ persistenceErrors
            
            CoreLogger.logInfo 
                "MovementPersistence" 
                $"Persistence complete: {stockTradesCreated} stocks, {optionTradesCreated} options, {dividendsCreated} dividends, {dividendTaxesCreated} dividend taxes, {brokerMovementsCreated} broker movements"
            
            if not (List.isEmpty allErrors) then
                CoreLogger.logWarning "MovementPersistence" $"Encountered {allErrors.Length} errors during persistence"
            
            return {
                BrokerMovementsCreated = brokerMovementsCreated
                OptionTradesCreated = optionTradesCreated
                StockTradesCreated = stockTradesCreated
                DividendsCreated = dividendsCreated
                DividendTaxesCreated = dividendTaxesCreated
                ValidationErrorsCount = validationResult.Invalid.Length
                PersistenceErrorsCount = persistenceErrors.Length
                Errors = allErrors
                ImportMetadata = batch.Metadata
            }
        }
    
    /// <summary>
    /// Convert PersistenceInput to ImportMovementBatch for backward compatibility.
    /// Allows existing code using PersistenceInput to work with new architecture.
    /// </summary>
    let internal convertPersistenceInputToBatch 
        (input: PersistenceInput) 
        (brokerAccountId: int)
        (sourceBroker: SupportedBroker)
        : ImportMovementBatch =
        
        let mutable movements = []
        let mutable affectedTickerIds = Set.empty
        let mutable movementDates = []
        
        // Convert broker movements
        for bm in input.BrokerMovements do
            movements <- BrokerMovement(bm) :: movements
            movementDates <- bm.TimeStamp.Value :: movementDates
            if bm.TickerId.IsSome then
                affectedTickerIds <- affectedTickerIds.Add(bm.TickerId.Value)
        
        // Convert option trades
        for option in input.OptionTrades do
            movements <- OptionTradeMovement(option) :: movements
            movementDates <- option.TimeStamp.Value :: movementDates
            affectedTickerIds <- affectedTickerIds.Add(option.TickerId)
        
        // Convert stock trades
        for trade in input.StockTrades do
            movements <- StockTradeMovement(trade) :: movements
            movementDates <- trade.TimeStamp.Value :: movementDates
            affectedTickerIds <- affectedTickerIds.Add(trade.TickerId)
        
        // Convert dividends
        for dividend in input.Dividends do
            movements <- DividendMovement(dividend) :: movements
            movementDates <- dividend.TimeStamp.Value :: movementDates
            affectedTickerIds <- affectedTickerIds.Add(dividend.TickerId)
        
        // Convert dividend taxes
        for tax in input.DividendTaxes do
            movements <- DividendTaxMovement(tax) :: movements
            movementDates <- tax.TimeStamp.Value :: movementDates
            affectedTickerIds <- affectedTickerIds.Add(tax.TickerId)
        
        // Get ticker symbols for metadata (we need to query database)
        // For now, we'll create empty set and let the caller populate it
        let affectedTickerSymbols = Set.empty
        
        let metadata = {
            OldestMovementDate = 
                if List.isEmpty movementDates then None
                else Some (movementDates |> List.min)
            AffectedBrokerAccountIds = Set.singleton brokerAccountId
            AffectedTickerSymbols = affectedTickerSymbols
            TotalMovementsImported = movements.Length
        }
        
        {
            Movements = List.rev movements
            BrokerAccountId = brokerAccountId
            SourceBroker = sourceBroker
            ImportDate = DateTime.UtcNow
            Metadata = metadata
        }

