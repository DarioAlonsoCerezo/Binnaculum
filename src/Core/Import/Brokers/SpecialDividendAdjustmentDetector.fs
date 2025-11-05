namespace Binnaculum.Core.Import

open System
open Binnaculum.Core.Logging
open TastytradeModels

/// <summary>
/// Detection and pattern recognition for special dividend-related strike price adjustments.
/// Identifies paired "Receive Deliver / Special Dividend" transactions that represent
/// strike price adjustments due to dividend payments.
/// </summary>
module SpecialDividendAdjustmentDetector =

    /// <summary>
    /// Represents a detected adjustment pair before application to option trades
    /// </summary>
    type DetectedAdjustment =
        {
            /// Original strike price (from the closing transaction)
            OriginalStrike: decimal
            /// New strike price (from the opening transaction)
            NewStrike: decimal
            /// Delta between strikes (typically -0.30)
            StrikeDelta: decimal
            /// Dividend amount that triggered the adjustment
            DividendAmount: decimal
            /// When the adjustment occurred
            AdjustmentTimestamp: DateTime
            /// Underlying ticker symbol
            TickerSymbol: string
            /// Option expiration date
            ExpirationDate: DateTime
            /// CALL or PUT
            OptionType: string
            /// IDs of paired transactions
            TransactionIds: int list
            /// Closing transaction (being removed)
            ClosingTransaction: TastytradeTransaction
            /// Opening transaction (being created)
            OpeningTransaction: TastytradeTransaction
        }

    /// <summary>
    /// Check if a transaction is a special dividend adjustment transaction
    /// </summary>
    let private isSpecialDividendTransaction (transaction: TastytradeTransaction) =
        match transaction.TransactionType with
        | ReceiveDeliver(subType) when subType = "Special Dividend" -> true
        | _ -> false

    /// <summary>
    /// Group transactions by (timestamp, ticker) for efficient pairing
    /// </summary>
    let private groupByTimeAndTicker (transactions: TastytradeTransaction list) =
        transactions
        |> List.filter isSpecialDividendTransaction
        |> List.groupBy (fun t ->
            // Use 1-second tolerance for timestamp grouping
            let baseTime = t.Date
            let groupKey = baseTime.ToString("yyyy-MM-dd HH:mm:ss")
            (groupKey, t.RootSymbol |> Option.defaultValue "UNKNOWN"))
        |> Map.ofList

    /// <summary>
    /// Validate that two transactions form a valid adjustment pair
    /// </summary>
    let private validateAdjustmentPair (closing: TastytradeTransaction) (opening: TastytradeTransaction) : bool =
        // Both must be special dividend transactions
        if not (isSpecialDividendTransaction closing && isSpecialDividendTransaction opening) then
            false
        else
            try
                // Same underlying ticker
                let sameTicker =
                    ((closing.RootSymbol |> Option.defaultValue "") = (opening.RootSymbol |> Option.defaultValue ""))

                // Same expiration date
                let sameExpiration =
                    match closing.ExpirationDate, opening.ExpirationDate with
                    | Some closeExp, Some openExp -> closeExp = openExp
                    | _ -> false

                // Same option type (both CALL or both PUT)
                let sameOptionType =
                    ((closing.CallOrPut |> Option.defaultValue "") = (opening.CallOrPut |> Option.defaultValue ""))

                // Within 2-second tolerance
                let withinTimeframe = Math.Abs((opening.Date - closing.Date).TotalSeconds) <= 2.0

                // Opposite actions based on CSV Action column values
                // The Action column contains: BUY_TO_OPEN, SELL_TO_CLOSE, etc.
                // For special dividends, we expect paired inverse actions like:
                // BUY_TO_OPEN with premium -X matched against SELL_TO_CLOSE with premium +X
                let oppositeActions =
                    // Check that premiums have opposite signs (one positive, one negative)
                    (closing.Value < 0m && opening.Value > 0m)
                    || (closing.Value > 0m && opening.Value < 0m)

                // Different strikes
                let differentStrikes =
                    match closing.StrikePrice, opening.StrikePrice with
                    | Some closeStrike, Some openStrike -> closeStrike <> openStrike
                    | _ -> false

                // Net premium balances to zero (within $0.01 tolerance)
                let netPremiumBalanced = Math.Abs((closing.Value + opening.Value)) <= 0.01m

                // Quantity matches
                let quantityMatches = closing.Quantity = opening.Quantity

                sameTicker
                && sameExpiration
                && sameOptionType
                && withinTimeframe
                && oppositeActions
                && differentStrikes
                && netPremiumBalanced
                && quantityMatches
            with ex ->
                CoreLogger.logDebugf "SpecialDividendAdjustmentDetector" "Pair validation failed: %s" ex.Message

                false

    /// <summary>
    /// Extract adjustment data from a validated pair of transactions
    /// </summary>
    let private extractAdjustmentData
        (closing: TastytradeTransaction)
        (opening: TastytradeTransaction)
        : DetectedAdjustment option =
        try
            // For special dividend adjustments:
            // Get the two strikes from the pair and determine original vs new
            // Original strike = higher value (before adjustment)
            // New strike = lower value (after adjustment due to dividend)
            // This handles all cases regardless of transaction order
            let strike1 = closing.StrikePrice |> Option.defaultValue 0m
            let strike2 = opening.StrikePrice |> Option.defaultValue 0m

            let originalStrike = max strike1 strike2
            let newStrike = min strike1 strike2
            let strikeDelta = newStrike - originalStrike
            let dividendAmount = Math.Abs(closing.Value) // Use absolute value of either transaction

            let adjustment =
                { OriginalStrike = originalStrike
                  NewStrike = newStrike
                  StrikeDelta = strikeDelta
                  DividendAmount = dividendAmount
                  AdjustmentTimestamp = closing.Date // Use the earlier transaction
                  TickerSymbol = closing.RootSymbol |> Option.defaultValue "UNKNOWN"
                  ExpirationDate = closing.ExpirationDate |> Option.defaultValue DateTime.MinValue
                  OptionType = closing.CallOrPut |> Option.defaultValue "CALL"
                  TransactionIds = []
                  ClosingTransaction = closing
                  OpeningTransaction = opening }

            Some adjustment
        with ex ->
            CoreLogger.logErrorf "SpecialDividendAdjustmentDetector" "Failed to extract adjustment data: %s" ex.Message

            None

    /// <summary>
    /// Find all valid adjustment pairs in a transaction group
    /// </summary>
    let private findValidPairs
        (transactions: TastytradeTransaction list)
        : (TastytradeTransaction * TastytradeTransaction) list =
        let mutable pairs = []
        let mutable used = Set.empty

        // For special dividend transactions, we need to identify closing and opening pairs
        // These come as ReceiveDeliver("Special Dividend") transactions
        // We identify them by the premium value sign and transaction description
        // Closing transactions have negative premiums (cost), opening have positive (credit)
        let specialDividendTransactions =
            transactions |> List.filter isSpecialDividendTransaction

        // Separate into "closing" (negative premium) and "opening" (positive premium) transactions
        let closingLikeTransactions =
            specialDividendTransactions |> List.filter (fun t -> t.Value < 0m)

        let openingLikeTransactions =
            specialDividendTransactions |> List.filter (fun t -> t.Value > 0m)

        CoreLogger.logDebugf
            "SpecialDividendAdjustmentDetector"
            "findValidPairs: Closing-like transactions=%d, Opening-like transactions=%d"
            closingLikeTransactions.Length
            openingLikeTransactions.Length

        for closing in closingLikeTransactions do
            if not (Set.contains closing.LineNumber used) then
                for opening in openingLikeTransactions do
                    if
                        not (Set.contains opening.LineNumber used)
                        && validateAdjustmentPair closing opening
                    then
                        CoreLogger.logDebugf
                            "SpecialDividendAdjustmentDetector"
                            "Found valid pair: Line %d (Closing: %.2f) matched with Line %d (Opening: %.2f)"
                            closing.LineNumber
                            closing.Value
                            opening.LineNumber
                            opening.Value

                        pairs <- (closing, opening) :: pairs
                        used <- Set.add closing.LineNumber used
                        used <- Set.add opening.LineNumber used
        // Only match each closing transaction once

        pairs

    /// <summary>
    /// Detect all strike price adjustments from a list of transactions.
    /// Returns list of detected adjustment pairs with metadata.
    /// </summary>
    let detectAdjustments (transactions: TastytradeTransaction list) : DetectedAdjustment list =
        try
            // Filter to only special dividend transactions
            let specialDividendTxns = transactions |> List.filter isSpecialDividendTransaction

            if List.isEmpty specialDividendTxns then
                // CoreLogger.logDebugf "SpecialDividendAdjustmentDetector" "No special dividend transactions found"
                []
            else
                // CoreLogger.logInfof
                //     "SpecialDividendAdjustmentDetector"
                //     "Detected %d special dividend transactions for adjustment detection"
                //     specialDividendTxns.Length

                let grouped = groupByTimeAndTicker specialDividendTxns

                let allAdjustments =
                    grouped |> Map.values |> Seq.map findValidPairs |> Seq.concat |> Seq.toList

                CoreLogger.logInfof
                    "SpecialDividendAdjustmentDetector"
                    "Found %d valid adjustment pairs"
                    allAdjustments.Length

                // Extract adjustment data from pairs
                let adjustments =
                    allAdjustments
                    |> List.choose (fun (closing, opening) -> extractAdjustmentData closing opening)

                // Log each detected adjustment
                adjustments
                |> List.iter (fun adj ->
                    CoreLogger.logDebugf
                        "SpecialDividendAdjustmentDetector"
                        "Adjustment detected: %s %s exp=%O original=%.2f new=%.2f delta=%.2f dividend=%.2f"
                        adj.TickerSymbol
                        adj.OptionType
                        adj.ExpirationDate
                        adj.OriginalStrike
                        adj.NewStrike
                        adj.StrikeDelta
                        adj.DividendAmount)

                adjustments
        with ex ->
            CoreLogger.logErrorf "SpecialDividendAdjustmentDetector" "Detection failed: %s" ex.Message
            []

    /// <summary>
    /// Format adjustment information for display in Notes field
    /// </summary>
    let formatAdjustmentNote (originalStrike: decimal) (newStrike: decimal) (dividendAmount: decimal) : string =
        let delta = newStrike - originalStrike

        sprintf
            "Strike adjusted from %.2f to %.2f due to special dividend (Δ %.2f, impact: $%.2f)"
            originalStrike
            newStrike
            delta
            dividendAmount
