module internal BrokerMovementExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open Binnaculum.Core.Database.TypeParser
open DataReaderExtensions
open CommandExtensions
open OptionExtensions
open Binnaculum.Core.SQL
open Binnaculum.Core.Patterns
open Binnaculum.Core.Logging

[<Extension>]
type Do() =

    [<Extension>]
    static member fill(brokerMovement: BrokerMovement, command: SqliteCommand) =
        command.fillEntityAuditable<BrokerMovement> (
            [ (SQLParameterName.TimeStamp, brokerMovement.TimeStamp.ToString())
              (SQLParameterName.Amount, brokerMovement.Amount.Value)
              (SQLParameterName.CurrencyId, brokerMovement.CurrencyId)
              (SQLParameterName.BrokerAccountId, brokerMovement.BrokerAccountId)
              (SQLParameterName.Commissions, brokerMovement.Commissions.Value)
              (SQLParameterName.Fees, brokerMovement.Fees.Value)
              (SQLParameterName.MovementType, fromMovementTypeToDatabase brokerMovement.MovementType)
              (SQLParameterName.Notes, brokerMovement.Notes.ToDbValue())
              (SQLParameterName.FromCurrencyId, brokerMovement.FromCurrencyId.ToDbValue())
              (SQLParameterName.AmountChanged,
               (brokerMovement.AmountChanged |> Option.map (fun m -> m.Value)).ToDbValue())
              (SQLParameterName.TickerId, brokerMovement.TickerId.ToDbValue())
              (SQLParameterName.Quantity, brokerMovement.Quantity.ToDbValue()) ],
            brokerMovement
        )

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        let movement =
            { Id = reader.getInt32 FieldName.Id
              TimeStamp = reader.getDateTimePattern FieldName.TimeStamp
              Amount = reader.getMoney FieldName.Amount
              CurrencyId = reader.getInt32 FieldName.CurrencyId
              BrokerAccountId = reader.getInt32 FieldName.BrokerAccountId
              Commissions = reader.getMoney FieldName.Commissions
              Fees = reader.getMoney FieldName.Fees
              MovementType = reader.getString FieldName.MovementType |> fromDataseToMovementType
              Notes = reader.getStringOrNone FieldName.Notes
              FromCurrencyId = reader.getIntOrNone FieldName.FromCurrencyId
              AmountChanged = reader.getMoneyOrNone FieldName.AmountChanged
              TickerId = reader.getIntOrNone FieldName.TickerId
              Quantity = reader.getDecimalOrNone FieldName.Quantity
              Audit = reader.getAudit () }

        // CoreLogger.logDebug
        //     "BrokerMovementExtensions"
        //     $"Read movement - ID: {movement.Id}, Type: {movement.MovementType}, Amount: {movement.Amount.Value}, BrokerAccountId: {movement.BrokerAccountId}"

        movement

    [<Extension>]
    static member save(brokerMovement: BrokerMovement) =
        // CoreLogger.logDebug
        //     "BrokerMovementExtensions"
        //     $"Starting save for movement - Amount: {brokerMovement.Amount.Value}, Type: {brokerMovement.MovementType}"

        let result = Database.Do.saveEntity brokerMovement (fun t c -> t.fill c)
        // CoreLogger.logDebug "BrokerMovementExtensions" "Save operation initiated for movement"
        result

    [<Extension>]
    static member delete(brokerMovement: BrokerMovement) = Database.Do.deleteEntity brokerMovement

    static member getAll() =
        Database.Do.getAllEntities Do.read BrokerMovementQuery.getAll

    static member getById(id: int) =
        Database.Do.getById Do.read id BrokerMovementQuery.getById

    static member getByBrokerAccountIdUntilDate(brokerAccountId: int, endDate: DateTimePattern) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- BrokerMovementQuery.getByBrokerAccountIdAndDateRange

            command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId)
            |> ignore

            command.Parameters.AddWithValue(SQLParameterName.DateEnd, endDate.ToString())
            |> ignore

            let! movements = Database.Do.readAll<BrokerMovement> (command, Do.read)
            return movements
        }

    static member getByBrokerAccountIdFromDate(brokerAccountId: int, startDate: DateTimePattern) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- BrokerMovementQuery.getByBrokerAccountIdFromDate

            command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId)
            |> ignore

            command.Parameters.AddWithValue(SQLParameterName.TimeStamp, startDate.ToString())
            |> ignore

            let! movements = Database.Do.readAll<BrokerMovement> (command, Do.read)
            return movements
        }

    static member getByBrokerAccountIdForDate(brokerAccountId: int, targetDate: DateTimePattern) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- BrokerMovementQuery.getByBrokerAccountIdForDate

            command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId)
            |> ignore

            command.Parameters.AddWithValue(SQLParameterName.TimeStamp, targetDate.ToString())
            |> ignore

            let! movements = Database.Do.readAll<BrokerMovement> (command, Do.read)
            return movements
        }

    /// <summary>
    /// Load movements with pagination support.
    /// Returns movements ordered by TimeStamp DESC (most recent first).
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID to filter by</param>
    /// <param name="pageNumber">Zero-based page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>List of broker movements for the specified page</returns>
    static member loadMovementsPaged(brokerAccountId: int, pageNumber: int, pageSize: int) =
        task {
            let offset = pageNumber * pageSize
            let! command = Database.Do.createCommand ()
            command.CommandText <- BrokerMovementQuery.getByBrokerAccountIdPaged

            command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId)
            |> ignore

            command.Parameters.AddWithValue("@PageSize", pageSize) |> ignore
            command.Parameters.AddWithValue("@Offset", offset) |> ignore

            let! movements = Database.Do.readAll<BrokerMovement> (command, Do.read)
            return movements
        }

    /// <summary>
    /// Get total count of movements for pagination UI.
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID to filter by</param>
    /// <returns>Total number of movements</returns>
    static member getMovementCount(brokerAccountId: int) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- BrokerMovementQuery.getCountByBrokerAccountId

            command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId)
            |> ignore

            let! result = command.ExecuteScalarAsync()
            return System.Convert.ToInt32(result)
        }

    /// <summary>
    /// Load movements within a date range (for calendar views).
    /// Returns movements ordered by TimeStamp DESC.
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID to filter by</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <returns>List of broker movements within the date range</returns>
    static member loadMovementsInDateRange(brokerAccountId: int, startDate: DateTimePattern, endDate: DateTimePattern) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- BrokerMovementQuery.getByBrokerAccountIdInDateRange

            command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId)
            |> ignore

            command.Parameters.AddWithValue(SQLParameterName.StartDate, startDate.ToString())
            |> ignore

            command.Parameters.AddWithValue(SQLParameterName.EndDate, endDate.ToString())
            |> ignore

            let! movements = Database.Do.readAll<BrokerMovement> (command, Do.read)
            return movements
        }

/// <summary>
/// Financial calculation extension methods for BrokerMovement collections.
/// These methods provide reusable calculation logic for financial snapshot processing.
/// </summary>
[<Extension>]
type FinancialCalculations() =

    /// <summary>
    /// Calculates the total deposited amount from broker movements.
    /// Includes: Deposits, ACAT Money Transfers Received, Currency Conversions (destination amount).
    /// </summary>
    /// <param name="movements">List of broker movements to analyze</param>
    /// <returns>Total deposited amount as Money</returns>
    [<Extension>]
    static member calculateTotalDeposited(movements: BrokerMovement list) =
        // CoreLogger.logDebug
        //     "FinancialCalculations"
        //     $"calculateTotalDeposited - Processing {movements.Length} total movements"

        // Log each movement before filtering
        // movements
        // |> List.iter (fun movement ->
        //     CoreLogger.logDebug
        //         "FinancialCalculations"
        //         $"Movement ID {movement.Id}, Type: {movement.MovementType}, Amount: {movement.Amount.Value}")

        let depositMovements =
            movements
            |> List.filter (fun movement ->
                match movement.MovementType with
                | BrokerMovementType.Deposit -> true
                | BrokerMovementType.ACATMoneyTransferReceived -> true
                | BrokerMovementType.Conversion -> true // Conversion adds money to target currency
                | _ -> false)

        // CoreLogger.logDebugf "FinancialCalculations" "Found %A deposit movements" depositMovements.Length

        let totalAmount =
            depositMovements |> List.sumBy (fun movement -> movement.Amount.Value)

        // CoreLogger.logDebugf "FinancialCalculations" "Total deposited amount calculated: %A" totalAmount
        Money.FromAmount totalAmount

    /// <summary>
    /// Calculates the total withdrawn amount from broker movements.
    /// Includes: Withdrawals, ACAT Money Transfers Sent.
    /// Note: Currency conversions are handled separately as they affect two currencies.
    /// </summary>
    /// <param name="movements">List of broker movements to analyze</param>
    /// <returns>Total withdrawn amount as Money</returns>
    [<Extension>]
    static member calculateTotalWithdrawn(movements: BrokerMovement list) =
        movements
        |> List.filter (fun movement ->
            match movement.MovementType with
            | BrokerMovementType.Withdrawal -> true
            | BrokerMovementType.ACATMoneyTransferSent -> true
            | _ -> false)
        |> List.sumBy (fun movement -> movement.Amount.Value)
        |> Money.FromAmount

    /// <summary>
    /// Calculates the total fees paid from broker movements.
    /// Includes: Fee movements plus fees from other movement types (commissions, transaction fees).
    /// </summary>
    /// <param name="movements">List of broker movements to analyze</param>
    /// <returns>Total fees amount as Money</returns>
    [<Extension>]
    static member calculateTotalFees(movements: BrokerMovement list) =
        let directFees =
            movements
            |> List.filter (fun movement -> movement.MovementType = BrokerMovementType.Fee)
            |> List.sumBy (fun movement -> movement.Amount.Value)

        let transactionFees = movements |> List.sumBy (fun movement -> movement.Fees.Value)

        Money.FromAmount(directFees + transactionFees)

    /// <summary>
    /// Calculates the total commissions paid from broker movements.
    /// Sums all commission amounts from all movement types.
    /// </summary>
    /// <param name="movements">List of broker movements to analyze</param>
    /// <returns>Total commissions amount as Money</returns>
    [<Extension>]
    static member calculateTotalCommissions(movements: BrokerMovement list) =
        movements
        |> List.sumBy (fun movement -> movement.Commissions.Value)
        |> Money.FromAmount

    /// <summary>
    /// Calculates the total other income from broker movements.
    /// Includes: Interest Gained, Lending Income.
    /// </summary>
    /// <param name="movements">List of broker movements to analyze</param>
    /// <returns>Total other income amount as Money</returns>
    [<Extension>]
    static member calculateTotalOtherIncome(movements: BrokerMovement list) =
        movements
        |> List.filter (fun movement ->
            match movement.MovementType with
            | BrokerMovementType.InterestsGained -> true
            | BrokerMovementType.Lending -> true
            | _ -> false)
        |> List.sumBy (fun movement -> movement.Amount.Value)
        |> Money.FromAmount

    /// <summary>
    /// Calculates the total interest paid from broker movements.
    /// This represents interest charged by the broker (negative cash flow).
    /// </summary>
    /// <param name="movements">List of broker movements to analyze</param>
    /// <returns>Total interest paid amount as Money</returns>
    [<Extension>]
    static member calculateTotalInterestPaid(movements: BrokerMovement list) =
        movements
        |> List.filter (fun movement -> movement.MovementType = BrokerMovementType.InterestsPaid)
        |> List.sumBy (fun movement -> movement.Amount.Value)
        |> Money.FromAmount

    /// <summary>
    /// Calculates the net cash impact from currency conversion movements for a specific currency.
    /// For the destination currency: positive amount (money gained).
    /// For the source currency: negative amount (money lost).
    /// </summary>
    /// <param name="movements">List of broker movements to analyze</param>
    /// <param name="targetCurrencyId">The currency ID to calculate conversion impact for</param>
    /// <returns>Net conversion impact as Money (can be positive or negative)</returns>
    [<Extension>]
    static member calculateConversionImpact(movements: BrokerMovement list, targetCurrencyId: int) =
        let conversionMovements =
            movements
            |> List.filter (fun movement -> movement.MovementType = BrokerMovementType.Conversion)

        let positiveImpact =
            conversionMovements
            |> List.filter (fun movement -> movement.CurrencyId = targetCurrencyId)
            |> List.sumBy (fun movement -> movement.Amount.Value)

        let negativeImpact =
            conversionMovements
            |> List.filter (fun movement ->
                movement.FromCurrencyId.IsSome
                && movement.FromCurrencyId.Value = targetCurrencyId)
            |> List.sumBy (fun movement -> movement.AmountChanged.Value.Value)

        Money.FromAmount(positiveImpact - negativeImpact)

    /// <summary>
    /// Counts the total number of movement transactions.
    /// This can be used for MovementCounter calculations in financial snapshots.
    /// </summary>
    /// <param name="movements">List of broker movements to count</param>
    /// <returns>Total number of movements as integer</returns>
    [<Extension>]
    static member calculateMovementCount(movements: BrokerMovement list) = movements.Length

    /// <summary>
    /// Filters broker movements by specific movement types.
    /// Useful for focused calculations or reporting.
    /// </summary>
    /// <param name="movements">List of broker movements to filter</param>
    /// <param name="movementTypes">List of movement types to include</param>
    /// <returns>Filtered list of broker movements</returns>
    [<Extension>]
    static member filterByMovementTypes(movements: BrokerMovement list, movementTypes: BrokerMovementType list) =
        movements
        |> List.filter (fun movement -> movementTypes |> List.contains movement.MovementType)

    /// <summary>
    /// Gets all unique currency IDs involved in the broker movements.
    /// Includes both primary currencies and conversion source currencies.
    /// </summary>
    /// <param name="movements">List of broker movements to analyze</param>
    /// <returns>Set of unique currency IDs</returns>
    [<Extension>]
    static member getUniqueCurrencyIds(movements: BrokerMovement list) =
        let primaryCurrencies =
            movements |> List.map (fun movement -> movement.CurrencyId) |> Set.ofList

        let conversionSourceCurrencies =
            movements |> List.choose (fun movement -> movement.FromCurrencyId) |> Set.ofList

        Set.union primaryCurrencies conversionSourceCurrencies

    /// <summary>
    /// Filters broker movements by currency ID.
    /// Includes movements where the target currency is either the primary currency
    /// or the source currency in conversions.
    /// </summary>
    /// <param name="movements">List of broker movements to filter</param>
    /// <param name="currencyId">The currency ID to filter by</param>
    /// <returns>Filtered list of broker movements affecting the specified currency</returns>
    [<Extension>]
    static member filterByCurrency(movements: BrokerMovement list, currencyId: int) =
        movements
        |> List.filter (fun movement ->
            movement.CurrencyId = currencyId
            || (movement.FromCurrencyId.IsSome && movement.FromCurrencyId.Value = currencyId))

    /// <summary>
    /// Calculates a comprehensive financial summary for broker movements.
    /// Returns a record with all major financial metrics calculated.
    /// </summary>
    /// <param name="movements">List of broker movements to analyze</param>
    /// <param name="currencyId">Optional currency ID to filter calculations by</param>
    /// <returns>Financial summary record with calculated totals</returns>
    [<Extension>]
    static member calculateFinancialSummary(movements: BrokerMovement list, ?currencyId: int) =
        // CoreLogger.logDebug
        //     "FinancialCalculations"
        //     $"calculateFinancialSummary - Processing {movements.Length} movements, CurrencyFilter: {currencyId}"

        let relevantMovements =
            match currencyId with
            | Some id ->
                let filtered = movements.filterByCurrency (id)

                // CoreLogger.logDebug "FinancialCalculations" $"Filtered to {filtered.Length} movements for currency {id}"

                filtered
            | None -> movements

        let summary =
            {| TotalDeposited = relevantMovements.calculateTotalDeposited ()
               TotalWithdrawn = relevantMovements.calculateTotalWithdrawn ()
               TotalFees = relevantMovements.calculateTotalFees ()
               TotalCommissions = relevantMovements.calculateTotalCommissions ()
               TotalOtherIncome = relevantMovements.calculateTotalOtherIncome ()
               TotalInterestPaid = relevantMovements.calculateTotalInterestPaid ()
               ConversionImpact =
                match currencyId with
                | Some id -> relevantMovements.calculateConversionImpact (id)
                | None -> Money.FromAmount 0m
               MovementCount = relevantMovements.calculateMovementCount ()
               UniqueCurrencies = relevantMovements.getUniqueCurrencyIds () |}

        // CoreLogger.logDebug
        //     "FinancialCalculations"
        //     $"Financial summary calculated - TotalDeposited: {summary.TotalDeposited.Value}, MovementCount: {summary.MovementCount}"

        summary
