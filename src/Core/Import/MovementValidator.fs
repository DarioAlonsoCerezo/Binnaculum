namespace Binnaculum.Core.Import

open Binnaculum.Core.Database.DatabaseModel
open ImportDomainTypes

/// <summary>
/// Validation module for import movements before database persistence.
/// Validates business rules and data integrity for all movement types.
/// </summary>
module MovementValidator =
    
    /// <summary>
    /// Validate stock trade business rules.
    /// </summary>
    let private validateStockTrade (trade: Trade) : Result<unit, string> =
        // Check quantity is positive
        if trade.Quantity <= 0m then
            Error $"Stock trade quantity must be positive, got {trade.Quantity}"
        // Check price is non-negative
        elif trade.Price.Value < 0m then
            Error $"Stock trade price cannot be negative, got {trade.Price.Value}"
        // Check ticker ID is valid
        elif trade.TickerId <= 0 then
            Error $"Stock trade must have valid ticker ID, got {trade.TickerId}"
        // Check broker account ID is valid
        elif trade.BrokerAccountId <= 0 then
            Error $"Stock trade must have valid broker account ID, got {trade.BrokerAccountId}"
        // Check currency ID is valid
        elif trade.CurrencyId <= 0 then
            Error $"Stock trade must have valid currency ID, got {trade.CurrencyId}"
        else
            Ok ()
    
    /// <summary>
    /// Validate option trade business rules.
    /// </summary>
    let private validateOptionTrade (option: OptionTrade) : Result<unit, string> =
        // Check strike is non-negative
        if option.Strike.Value < 0m then
            Error $"Option strike cannot be negative, got {option.Strike.Value}"
        // Check multiplier is positive
        elif option.Multiplier <= 0m then
            Error $"Option multiplier must be positive, got {option.Multiplier}"
        // Check ticker ID is valid
        elif option.TickerId <= 0 then
            Error $"Option trade must have valid ticker ID, got {option.TickerId}"
        // Check broker account ID is valid
        elif option.BrokerAccountId <= 0 then
            Error $"Option trade must have valid broker account ID, got {option.BrokerAccountId}"
        // Check currency ID is valid
        elif option.CurrencyId <= 0 then
            Error $"Option trade must have valid currency ID, got {option.CurrencyId}"
        // Check expiration date is after timestamp
        elif option.ExpirationDate.Value < option.TimeStamp.Value then
            Error $"Option expiration date must be after trade date"
        else
            Ok ()
    
    /// <summary>
    /// Validate dividend business rules.
    /// </summary>
    let private validateDividend (dividend: Dividend) : Result<unit, string> =
        // Check amount is positive
        if dividend.DividendAmount.Value <= 0m then
            Error $"Dividend amount must be positive, got {dividend.DividendAmount.Value}"
        // Check ticker ID is valid
        elif dividend.TickerId <= 0 then
            Error $"Dividend must have valid ticker ID, got {dividend.TickerId}"
        // Check broker account ID is valid
        elif dividend.BrokerAccountId <= 0 then
            Error $"Dividend must have valid broker account ID, got {dividend.BrokerAccountId}"
        // Check currency ID is valid
        elif dividend.CurrencyId <= 0 then
            Error $"Dividend must have valid currency ID, got {dividend.CurrencyId}"
        else
            Ok ()
    
    /// <summary>
    /// Validate dividend tax business rules.
    /// </summary>
    let private validateDividendTax (tax: DividendTax) : Result<unit, string> =
        // Check amount is positive
        if tax.DividendTaxAmount.Value <= 0m then
            Error $"Dividend tax amount must be positive, got {tax.DividendTaxAmount.Value}"
        // Check ticker ID is valid
        elif tax.TickerId <= 0 then
            Error $"Dividend tax must have valid ticker ID, got {tax.TickerId}"
        // Check broker account ID is valid
        elif tax.BrokerAccountId <= 0 then
            Error $"Dividend tax must have valid broker account ID, got {tax.BrokerAccountId}"
        // Check currency ID is valid
        elif tax.CurrencyId <= 0 then
            Error $"Dividend tax must have valid currency ID, got {tax.CurrencyId}"
        else
            Ok ()
    
    /// <summary>
    /// Validate broker movement business rules.
    /// </summary>
    let private validateBrokerMovement (bm: BrokerMovement) : Result<unit, string> =
        // Check broker account ID is valid
        if bm.BrokerAccountId <= 0 then
            Error $"Broker movement must have valid broker account ID, got {bm.BrokerAccountId}"
        // Check currency ID is valid
        elif bm.CurrencyId <= 0 then
            Error $"Broker movement must have valid currency ID, got {bm.CurrencyId}"
        // Check amount is non-negative
        elif bm.Amount.Value < 0m then
            Error $"Broker movement amount cannot be negative, got {bm.Amount.Value}"
        // If FromCurrencyId is specified, AmountChanged should also be specified
        elif bm.FromCurrencyId.IsSome && bm.AmountChanged.IsNone then
            Error "Broker movement with FromCurrencyId must have AmountChanged specified"
        else
            Ok ()
    
    /// <summary>
    /// Validate a single movement against business rules.
    /// </summary>
    let private validateMovement (movement: ImportMovement) : Result<unit, string> =
        match movement with
        | StockTradeMovement trade ->
            validateStockTrade trade
        
        | OptionTradeMovement option ->
            validateOptionTrade option
        
        | DividendMovement dividend ->
            validateDividend dividend
        
        | DividendTaxMovement tax ->
            validateDividendTax tax
        
        | BrokerMovement bm ->
            validateBrokerMovement bm
    
    /// <summary>
    /// Validate entire batch of movements.
    /// Returns separate lists of valid and invalid movements.
    /// </summary>
    /// <param name="batch">Batch of movements to validate</param>
    /// <returns>Validation result with valid and invalid movements</returns>
    let internal validateBatch (batch: ImportMovementBatch) : MovementValidationResult =
        
        let mutable valid = []
        let mutable invalid = []
        
        for movement in batch.Movements do
            match validateMovement movement with
            | Ok _ -> 
                valid <- movement :: valid
            | Error msg -> 
                invalid <- (movement, msg) :: invalid
        
        {
            Valid = List.rev valid
            Invalid = List.rev invalid
        }




