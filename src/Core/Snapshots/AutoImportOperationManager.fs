namespace Binnaculum.Core.Storage

open System
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Database
open Binnaculum.Core.Logging
open Binnaculum.Core.Snapshots.CapitalDeployedCalculator

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
            TradesForDate: Binnaculum.Core.Database.DatabaseModel.Trade list
            DividendForDate: Binnaculum.Core.Database.DatabaseModel.Dividend list
            DividendTaxForDate: Binnaculum.Core.Database.DatabaseModel.DividendTax list
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
    /// Calculate CapitalDeployed from option trades on this date.
    /// Uses shared CapitalDeployedCalculator for consistency with ticker snapshots.
    /// Only opening trades deploy capital; closing trades return 0.
    /// </summary>
    let calculateCapitalDeployedFromTrades
        (optionTrades: Binnaculum.Core.Database.DatabaseModel.OptionTrade list)
        : decimal =
        calculateTotalOptionCapitalDeployed optionTrades

    let calculateFees (context: OperationContext) =
        [ context.TradesForDate |> List.map (fun t -> t.Fees.Value)
          context.OptionTradesForDate |> List.map (fun ot -> ot.Fees.Value) ]
        |> List.concat
        |> List.sum

    let calculateCommissions (context: OperationContext) =
        [ context.TradesForDate |> List.map (fun t -> t.Commissions.Value)
          context.OptionTradesForDate |> List.map (fun ot -> ot.Commissions.Value) ]
        |> List.concat
        |> List.sum

    /// <summary>
    /// Create a new AutoImportOperation from a snapshot when trades open.
    /// </summary>
    let createOperation (context: OperationContext) : DatabaseModel.AutoImportOperation =
        let snapshot = context.CurrentSnapshot

        let fees = calculateFees context
        let commissions = calculateCommissions context

        // Calculate initial capital deployed from actual option trades
        let capitalDeployed = calculateCapitalDeployedFromTrades context.OptionTradesForDate

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
        (context: OperationContext)
        : DatabaseModel.AutoImportOperation =

        let todayFees = calculateFees context
        let fees = operation.Fees.Value + todayFees
        let todayCommissions = calculateCommissions context
        let commissions = operation.Commissions.Value + todayCommissions

        // Calculate realized delta for today
        let realizedDelta = snapshot.Realized.Value - operation.Realized.Value

        // Calculate capital deployed today from actual option trades
        let capitalDeployedToday = calculateCapitalDeployedFromTrades optionTradesForDate

        // Cumulative capital = previous capital + today's capital
        let cumulativeCapital = operation.CapitalDeployed.Value + capitalDeployedToday

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
                            context

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
                        updateOperation
                            op
                            context.CurrentSnapshot
                            true
                            context.MovementDate
                            context.OptionTradesForDate
                            context

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
