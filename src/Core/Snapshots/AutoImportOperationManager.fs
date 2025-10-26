namespace Binnaculum.Core.Storage

open System
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Database
open Binnaculum.Core.Logging

/// <summary>
/// Manages AutoImportOperation lifecycle during ticker snapshot batch processing.
/// Detects OpenTrades flag transitions and creates/updates operations accordingly.
/// </summary>
module internal AutoImportOperationManager =

    /// <summary>
    /// Context for operation management during snapshot processing.
    /// </summary>
    type OperationContext =
        {
            BrokerAccountId: int
            TickerId: int
            CurrencyId: int
            PreviousSnapshot: TickerCurrencySnapshot option
            CurrentSnapshot: TickerCurrencySnapshot
            MovementDate: DateTimePattern
            /// Option trades for this ticker/currency on this date (for capital calculation)
            OptionTradesForDate: Binnaculum.Core.Database.DatabaseModel.OptionTrade list
        }

    /// <summary>
    /// Result of operation processing.
    /// </summary>
    type OperationResult =
        { Operation: DatabaseModel.AutoImportOperation option
          WasCreated: bool
          WasUpdated: bool
          WasClosed: bool }

    /// <summary>
    /// Detect if we need to create, update, or close an operation based on OpenTrades flag transition.
    /// </summary>
    let detectOperationAction (context: OperationContext) : string =
        match context.PreviousSnapshot with
        | None when context.CurrentSnapshot.OpenTrades -> "CREATE" // First snapshot with open trades
        | Some prev when not prev.OpenTrades && context.CurrentSnapshot.OpenTrades -> "CREATE" // Transition: closed → open
        | Some prev when prev.OpenTrades && not context.CurrentSnapshot.OpenTrades -> "CLOSE" // Transition: open → closed
        | Some prev when prev.OpenTrades && context.CurrentSnapshot.OpenTrades -> "UPDATE" // Still open
        | _ -> "NONE" // No action needed

    /// <summary>
    /// Calculate capital deployed for a single option trade.
    /// Rules:
    /// - BuyToOpen (Call/Put): Premium + Commissions + Fees
    /// - SellToOpen Call: $0 (assume covered)
    /// - SellToOpen Put: (Strike × Multiplier) - Premium + Commissions + Fees
    /// - Close trades: $0 (don't deploy new capital)
    /// </summary>
    let calculateTradeCapitalDeployed (trade: Binnaculum.Core.Database.DatabaseModel.OptionTrade) : decimal =
        match trade.Code with
        | Binnaculum.Core.Database.DatabaseModel.OptionCode.BuyToOpen ->
            // Debit trade - deploy premium paid
            abs (trade.Premium: Money).Value
            + (trade.Commissions: Money).Value
            + (trade.Fees: Money).Value
        | Binnaculum.Core.Database.DatabaseModel.OptionCode.SellToOpen ->
            match trade.OptionType with
            | Binnaculum.Core.Database.DatabaseModel.OptionType.Call ->
                // Assume covered call - no capital deployed
                0m
            | Binnaculum.Core.Database.DatabaseModel.OptionType.Put ->
                // Cash-secured put - deploy strike obligation minus premium received
                let strikeObligation = (trade.Strike: Money).Value * trade.Multiplier
                let premiumReceived = abs (trade.Premium: Money).Value

                strikeObligation - premiumReceived
                + (trade.Commissions: Money).Value
                + (trade.Fees: Money).Value
        | _ ->
            // Close trades don't deploy new capital
            0m

    /// <summary>
    /// Calculate CapitalDeployed from option trades on this date.
    /// This should be called when creating or updating an operation.
    /// </summary>
    let calculateCapitalDeployedFromTrades
        (optionTrades: Binnaculum.Core.Database.DatabaseModel.OptionTrade list)
        : decimal =
        optionTrades |> List.sumBy calculateTradeCapitalDeployed

    /// <summary>
    /// Create a new AutoImportOperation from a snapshot when trades open.
    /// </summary>
    let createOperation (context: OperationContext) : DatabaseModel.AutoImportOperation =
        let snapshot = context.CurrentSnapshot

        // Calculate initial capital deployed from actual option trades
        let capitalDeployed = calculateCapitalDeployedFromTrades context.OptionTradesForDate

        // Calculate Invested from stock positions: CostBasis (already the total)
        // For options-only operations (TotalShares = 0), Invested = 0
        let invested = snapshot.CostBasis.Value

        { Id = 0
          BrokerAccountId = context.BrokerAccountId
          TickerId = context.TickerId
          CurrencyId = context.CurrencyId
          IsOpen = true
          Realized = snapshot.Realized
          RealizedToday = snapshot.Realized // Initial creation - full amount is "today"
          Commissions = snapshot.Commissions
          Fees = snapshot.Fees
          Premium = snapshot.Options
          Dividends = snapshot.Dividends
          DividendTaxes = snapshot.DividendTaxes
          CapitalDeployed = Money.FromAmount(capitalDeployed)
          CapitalDeployedToday = Money.FromAmount(capitalDeployed) // Initial = full amount
          Performance = 0m // No performance until closed
          Invested = Money.FromAmount(invested)
          Audit =
            { CreatedAt = Some context.MovementDate // Use movement date as OpenDate
              UpdatedAt = None } }

    /// <summary>
    /// Update an existing operation with current snapshot metrics.
    /// </summary>
    let updateOperation
        (operation: DatabaseModel.AutoImportOperation)
        (snapshot: TickerCurrencySnapshot)
        (isClosing: bool)
        (movementDate: DateTimePattern)
        (optionTradesForDate: Binnaculum.Core.Database.DatabaseModel.OptionTrade list)
        : DatabaseModel.AutoImportOperation =

        // Calculate realized delta for today
        let realizedDelta = snapshot.Realized.Value - operation.Realized.Value

        // Calculate capital deployed today from actual option trades
        let capitalDeployedToday = calculateCapitalDeployedFromTrades optionTradesForDate

        // Cumulative capital = previous capital + today's capital
        let cumulativeCapital = operation.CapitalDeployed.Value + capitalDeployedToday

        // Calculate Invested from stock positions: CostBasis (already the total)
        // When closing (TotalShares = 0), CostBasis should automatically become 0
        let invested = snapshot.CostBasis.Value

        // Calculate performance if closing or if we have capital deployed
        let performance =
            if cumulativeCapital <> 0m then
                (snapshot.Realized.Value / cumulativeCapital) * 100m
            else
                0m

        { operation with
            IsOpen = not isClosing
            Realized = snapshot.Realized
            RealizedToday = Money.FromAmount(realizedDelta) // Delta calculation
            Commissions = snapshot.Commissions
            Fees = snapshot.Fees
            Premium = snapshot.Options
            Dividends = snapshot.Dividends
            DividendTaxes = snapshot.DividendTaxes
            CapitalDeployed = Money.FromAmount(cumulativeCapital) // CUMULATIVE
            CapitalDeployedToday = Money.FromAmount(capitalDeployedToday) // DELTA
            Performance = performance
            Invested = Money.FromAmount(invested)
            Audit =
                if isClosing then
                    // Set UpdatedAt to movement date when closing
                    { operation.Audit with
                        UpdatedAt = Some movementDate }
                else
                    operation.Audit }

    /// <summary>
    /// Process operation lifecycle for a snapshot being calculated.
    /// This should be called BEFORE saving the snapshot to ensure operation data is available.
    /// </summary>
    let processOperation (context: OperationContext) : Async<OperationResult> =
        async {
            let action = detectOperationAction context

            match action with
            | "CREATE" ->
                // Create new operation
                let operation = createOperation context
                do! AutoImportOperationExtensions.Do.save (operation) |> Async.AwaitTask

                // Retrieve the saved operation to get the assigned ID
                let! savedOperation =
                    AutoImportOperationExtensions.Do.getOpenOperation (context.TickerId, context.BrokerAccountId)
                    |> Async.AwaitTask

                match savedOperation with
                | Some op ->
                    // CoreLogger.logDebugf
                    //     "AutoImportOperationManager"
                    //     "Created operation ID=%d for Ticker=%d on %s (CapitalDeployed=$%.2f)"
                    //     op.Id
                    //     context.TickerId
                    //     (context.MovementDate.ToString())
                    //     (op.CapitalDeployed: Money).Value

                    return
                        { Operation = Some op
                          WasCreated = true
                          WasUpdated = false
                          WasClosed = false }
                | None ->
                    // CoreLogger.logError "AutoImportOperationManager" "Failed to retrieve newly created operation"

                    return
                        { Operation = None
                          WasCreated = false
                          WasUpdated = false
                          WasClosed = false }

            | "UPDATE" ->
                // Update existing open operation
                let! existingOp =
                    AutoImportOperationExtensions.Do.getOpenOperation (context.TickerId, context.BrokerAccountId)
                    |> Async.AwaitTask

                match existingOp with
                | Some(op: DatabaseModel.AutoImportOperation) ->
                    let updatedOp =
                        updateOperation
                            op
                            context.CurrentSnapshot
                            false
                            context.MovementDate
                            context.OptionTradesForDate

                    do! AutoImportOperationExtensions.Do.save (updatedOp) |> Async.AwaitTask

                    // Retrieve updated operation
                    let! savedOp = AutoImportOperationExtensions.Do.getById (op.Id) |> Async.AwaitTask

                    match savedOp with
                    | Some op ->
                        // CoreLogger.logDebugf
                        //     "AutoImportOperationManager"
                        //     "Updated operation ID=%d for Ticker=%d on %s (Realized=$%.2f, Performance=%.2f%%)"
                        //     op.Id
                        //     context.TickerId
                        //     (context.MovementDate.ToString())
                        //     (op.Realized: Money).Value
                        //     (op.Performance: decimal)

                        return
                            { Operation = Some op
                              WasCreated = false
                              WasUpdated = true
                              WasClosed = false }
                    | None ->
                        // CoreLogger.logError "AutoImportOperationManager" "Failed to retrieve updated operation"

                        return
                            { Operation = None
                              WasCreated = false
                              WasUpdated = false
                              WasClosed = false }
                | None ->
                    // CoreLogger.logWarningf
                    //     "AutoImportOperationManager"
                    //     "Expected to find open operation for Ticker=%d but none found"
                    //     context.TickerId

                    return
                        { Operation = None
                          WasCreated = false
                          WasUpdated = false
                          WasClosed = false }

            | "CLOSE" ->
                // Close existing operation
                let! existingOp =
                    AutoImportOperationExtensions.Do.getOpenOperation (context.TickerId, context.BrokerAccountId)
                    |> Async.AwaitTask

                match existingOp with
                | Some(op: DatabaseModel.AutoImportOperation) ->
                    let closedOp =
                        updateOperation op context.CurrentSnapshot true context.MovementDate context.OptionTradesForDate

                    // Log before save
                    // CoreLogger.logDebugf
                    //     "AutoImportOperationManager"
                    //     "BEFORE SAVE - CloseDate: UpdatedAt=%s, IsOpen=%b"
                    //     (match closedOp.Audit.UpdatedAt with
                    //      | Some dt -> dt.ToString()
                    //      | None -> "None")
                    //     closedOp.IsOpen

                    do! AutoImportOperationExtensions.Do.save (closedOp) |> Async.AwaitTask

                    // Retrieve closed operation
                    let! savedOp = AutoImportOperationExtensions.Do.getById (op.Id) |> Async.AwaitTask

                    match savedOp with
                    | Some op ->
                        // Log after retrieval
                        // CoreLogger.logDebugf
                        //     "AutoImportOperationManager"
                        //     "AFTER RETRIEVAL - CloseDate: UpdatedAt=%s, IsOpen=%b"
                        //     (match op.Audit.UpdatedAt with
                        //      | Some dt -> dt.ToString()
                        //      | None -> "None")
                        //     op.IsOpen

                        // CoreLogger.logInfof
                        //     "AutoImportOperationManager"
                        //     "Closed operation ID=%d for Ticker=%d on %s (Realized=$%.2f, Performance=%.2f%%)"
                        //     op.Id
                        //     context.TickerId
                        //     (context.MovementDate.ToString())
                        //     (op.Realized: Money).Value
                        //     (op.Performance: decimal)

                        return
                            { Operation = Some op
                              WasCreated = false
                              WasUpdated = true
                              WasClosed = true }
                    | None ->
                        // CoreLogger.logError "AutoImportOperationManager" "Failed to retrieve closed operation"

                        return
                            { Operation = None
                              WasCreated = false
                              WasUpdated = false
                              WasClosed = false }
                | None ->
                    // CoreLogger.logWarningf
                    //     "AutoImportOperationManager"
                    //     "Expected to find open operation to close for Ticker=%d but none found"
                    //     context.TickerId

                    return
                        { Operation = None
                          WasCreated = false
                          WasUpdated = false
                          WasClosed = false }

            | _ ->
                // No action needed
                return
                    { Operation = None
                      WasCreated = false
                      WasUpdated = false
                      WasClosed = false }
        }
