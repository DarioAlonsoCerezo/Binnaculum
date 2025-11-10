namespace Binnaculum.Core.Import

open System
open Binnaculum.Core.Logging
open SpecialDividendAdjustmentDetector

/// <summary>
/// Validation rules for detected strike price adjustments.
/// Ensures detected adjustments are correct before being applied to option trades.
/// </summary>
module AdjustmentValidation =

    /// <summary>
    /// Result type for validation operations
    /// </summary>
    type ValidationResult =
        { IsValid: bool
          Errors: string list
          Warnings: string list }

    let inline private addError (result: ValidationResult) (error: string) : ValidationResult =
        { result with
            IsValid = false
            Errors = error :: result.Errors }

    let inline private addWarning (result: ValidationResult) (warning: string) : ValidationResult =
        { result with
            Warnings = warning :: result.Warnings }

    let private emptyResult =
        { IsValid = true
          Errors = []
          Warnings = [] }

    /// <summary>
    /// Rule 1: Validate that original strike is positive
    /// </summary>
    let private validateOriginalStrikePositive (adjustment: DetectedAdjustment) : ValidationResult =
        if adjustment.OriginalStrike > 0m then
            emptyResult
        else
            { emptyResult with
                IsValid = false
                Errors = [ $"Original strike must be positive, got {adjustment.OriginalStrike}" ] }

    /// <summary>
    /// Rule 2: Validate that new strike is positive
    /// </summary>
    let private validateNewStrikePositive (adjustment: DetectedAdjustment) : ValidationResult =
        if adjustment.NewStrike > 0m then
            emptyResult
        else
            { emptyResult with
                IsValid = false
                Errors = [ $"New strike must be positive, got {adjustment.NewStrike}" ] }

    /// <summary>
    /// Rule 3: Validate that dividend amount is non-negative
    /// </summary>
    let private validateDividendAmountNonNegative (adjustment: DetectedAdjustment) : ValidationResult =
        if adjustment.DividendAmount >= 0m then
            emptyResult
        else
            { emptyResult with
                IsValid = false
                Errors = [ $"Dividend amount must be non-negative, got {adjustment.DividendAmount}" ] }

    /// <summary>
    /// Rule 4: Validate strike delta calculation
    /// </summary>
    let private validateStrikeDeltaCalculation (adjustment: DetectedAdjustment) : ValidationResult =
        let calculatedDelta = adjustment.NewStrike - adjustment.OriginalStrike

        if Math.Abs(calculatedDelta - adjustment.StrikeDelta) < 0.001m then
            emptyResult
        else
            { emptyResult with
                IsValid = false
                Errors = [ $"Strike delta calculation error: expected {calculatedDelta}, got {adjustment.StrikeDelta}" ] }

    /// <summary>
    /// Rule 5: Warn if strike adjustment is unusually large
    /// </summary>
    let private warnIfLargeAdjustment (adjustment: DetectedAdjustment) : ValidationResult =
        // Flag adjustments larger than $1.00 per contract as a warning
        let adjustmentPercentage =
            if adjustment.OriginalStrike <> 0m then
                Math.Abs(adjustment.StrikeDelta) / adjustment.OriginalStrike * 100m
            else
                0m

        if adjustmentPercentage > 5m then
            let warningMsg =
                sprintf
                    "Unusually large strike adjustment: %.2f%% (delta: %.2f)"
                    adjustmentPercentage
                    adjustment.StrikeDelta

            { emptyResult with
                Warnings = [ warningMsg ] }
        else
            emptyResult

    /// <summary>
    /// Run all validation rules on an adjustment
    /// </summary>
    let validateAdjustment (adjustment: DetectedAdjustment) : ValidationResult =
        emptyResult
        |> (fun result ->
            let rule1 = validateOriginalStrikePositive adjustment

            if not rule1.IsValid then
                addError result rule1.Errors.Head
            else
                result)
        |> (fun result ->
            let rule2 = validateNewStrikePositive adjustment

            if not rule2.IsValid then
                addError result rule2.Errors.Head
            else
                result)
        |> (fun result ->
            let rule3 = validateDividendAmountNonNegative adjustment

            if not rule3.IsValid then
                addError result rule3.Errors.Head
            else
                result)
        |> (fun result ->
            let rule4 = validateStrikeDeltaCalculation adjustment

            if not rule4.IsValid then
                addError result rule4.Errors.Head
            else
                result)
        |> (fun result ->
            let rule5 = warnIfLargeAdjustment adjustment

            if not (List.isEmpty rule5.Warnings) then
                { result with
                    Warnings = result.Warnings @ rule5.Warnings }
            else
                result)

    /// <summary>
    /// Validate a list of adjustments and return valid ones, logging errors/warnings
    /// </summary>
    let validateAndFilterAdjustments (adjustments: DetectedAdjustment list) : DetectedAdjustment list =
        adjustments
        |> List.filter (fun adj ->
            let result = validateAdjustment adj

            if not result.IsValid then
                CoreLogger.logWarningf
                    "AdjustmentValidation"
                    "Adjustment validation failed for %s %s: %s"
                    adj.TickerSymbol
                    adj.OptionType
                    (String.concat "; " result.Errors)

                false
            else
                // Log any warnings
                if not (List.isEmpty result.Warnings) then
                    CoreLogger.logWarningf
                        "AdjustmentValidation"
                        "Adjustment validation warning for %s %s: %s"
                        adj.TickerSymbol
                        adj.OptionType
                        (String.concat "; " result.Warnings)

                true)
